using EduLoan.Application.Common;
using EduLoan.Application.Interfaces;
using EduLoan.Domain.Entities;
using EduLoan.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduLoan.API.Controllers;

[ApiController]
[Route("api/v1/employee-documents")]
[Authorize(Roles = "Employee")]
public sealed class EmployeeDocumentsController : ControllerBase
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = [".pdf", ".jpg", ".jpeg", ".png"];
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IFileStorageService _storage;

    public EmployeeDocumentsController(AppDbContext db, ICurrentUserService current, IFileStorageService storage)
    { _db = db; _current = current; _storage = storage; }

    private Guid EmployeeId => _current.UserId ?? throw new UnauthorizedAccessException("Authenticated user id is missing.");

    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var docs = await _db.EmployeeDocuments.AsNoTracking().Where(x => x.EmployeeId == EmployeeId)
            .OrderByDescending(x => x.UploadedAtUtc)
            .Select(x => new { x.Id, x.DocumentType, x.FileName, x.ContentType, x.FileSizeBytes, x.UploadedAtUtc })
            .ToListAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(docs));
    }

    [HttpPost]
    [RequestSizeLimit(MaxFileSizeBytes)]
    public async Task<IActionResult> Upload([FromForm] string documentType, [FromForm] IFormFile file, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(documentType) || documentType.Trim().Length > 100)
            return BadRequest(ApiResponse<object>.FailureResponse("Choose a valid document type (maximum 100 characters)."));
        if (file is null || file.Length == 0) return BadRequest(ApiResponse<object>.FailureResponse("Choose a file to upload."));
        if (file.Length > MaxFileSizeBytes) return BadRequest(ApiResponse<object>.FailureResponse("Maximum file size is 5 MB."));
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension)) return BadRequest(ApiResponse<object>.FailureResponse("Only PDF, JPG, JPEG, and PNG files are allowed."));

        string storedName;
        await using (var stream = file.OpenReadStream())
            storedName = await _storage.SaveAsync(stream, Path.GetFileName(file.FileName), ct);

        try
        {
            var doc = new EmployeeDocument
            {
                Id = Guid.NewGuid(), EmployeeId = EmployeeId, DocumentType = documentType.Trim(),
                FileName = Path.GetFileName(file.FileName), StoredFileName = storedName,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                FileSizeBytes = file.Length, UploadedAtUtc = DateTime.UtcNow
            };
            _db.EmployeeDocuments.Add(doc);
            await _db.SaveChangesAsync(ct);
            return Ok(ApiResponse<object>.SuccessResponse(new { doc.Id, doc.DocumentType, doc.FileName, doc.ContentType, doc.FileSizeBytes, doc.UploadedAtUtc }, "Document uploaded to My Documents."));
        }
        catch
        {
            await _storage.DeleteAsync(storedName, ct);
            throw;
        }
    }

    [HttpGet("{documentId:guid}/download")]
    public async Task<IActionResult> Download(Guid documentId, CancellationToken ct)
    {
        var doc = await _db.EmployeeDocuments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == documentId && x.EmployeeId == EmployeeId, ct);
        if (doc is null) return NotFound(ApiResponse<object>.FailureResponse("Document not found."));
        try { return File(_storage.OpenRead(doc.StoredFileName), doc.ContentType, doc.FileName); }
        catch (FileNotFoundException) { return NotFound(ApiResponse<object>.FailureResponse("The stored file is missing. Please upload this document again.")); }
    }
}
