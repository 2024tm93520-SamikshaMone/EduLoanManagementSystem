using EduLoan.Application.Common;
using EduLoan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using EduLoan.Application.Features.LoanApplications;
using EduLoan.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduLoan.API.Controllers;

[ApiController]
[Route("api/v1/loan-applications")]
[Authorize]
public class LoanApplicationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _fileStorage;
    private readonly AppDbContext _db;

    private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public LoanApplicationsController(IMediator mediator, ICurrentUserService currentUser, IFileStorageService fileStorage, AppDbContext db)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
        _db = db;
    }

    private Guid UserId => _currentUser.UserId!.Value; // [Authorize] guarantees this is set

    public record CreateApplicationRequest(
        int CollegeId, int CourseId, string Specialization, int CourseDurationMonths,
        decimal TotalEducationFees, decimal RequestedAmount, int RequestedTenureMonths, string EducationPurpose);

    public record UpdateApplicationRequest(
        int CollegeId, int CourseId, string Specialization, int CourseDurationMonths,
        decimal TotalEducationFees, decimal RequestedAmount, int RequestedTenureMonths, string EducationPurpose);

    [HttpGet("mine")]
    public async Task<ActionResult<ApiResponse<List<LoanApplicationSummaryDto>>>> GetMine(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMyApplicationsQuery(UserId), ct);
        return Ok(ApiResponse<List<LoanApplicationSummaryDto>>.SuccessResponse(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<LoanApplicationDetailDto>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetApplicationByIdQuery(id, UserId), ct);
        return Ok(ApiResponse<LoanApplicationDetailDto>.SuccessResponse(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<LoanApplicationDetailDto>>> Create(
        [FromBody] CreateApplicationRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateLoanApplicationCommand(
            UserId, request.CollegeId, request.CourseId, request.Specialization, request.CourseDurationMonths,
            request.TotalEducationFees, request.RequestedAmount, request.RequestedTenureMonths, request.EducationPurpose), ct);
        return Ok(ApiResponse<LoanApplicationDetailDto>.SuccessResponse(result, "Application draft created."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<LoanApplicationDetailDto>>> Update(
        Guid id, [FromBody] UpdateApplicationRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateLoanApplicationCommand(
            id, UserId, request.CollegeId, request.CourseId, request.Specialization, request.CourseDurationMonths,
            request.TotalEducationFees, request.RequestedAmount, request.RequestedTenureMonths, request.EducationPurpose), ct);
        return Ok(ApiResponse<LoanApplicationDetailDto>.SuccessResponse(result, "Application updated."));
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<ApiResponse<LoanApplicationDetailDto>>> Submit(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new SubmitLoanApplicationCommand(id, UserId), ct);
        return Ok(ApiResponse<LoanApplicationDetailDto>.SuccessResponse(result, "Application submitted."));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<LoanApplicationDetailDto>>> Cancel(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new CancelLoanApplicationCommand(id, UserId), ct);
        return Ok(ApiResponse<LoanApplicationDetailDto>.SuccessResponse(result, "Application rejected."));
    }

    [HttpPost("{id:guid}/documents")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    public async Task<ActionResult<ApiResponse<ApplicationDocumentDto>>> AddDocument(
        Guid id, [FromForm] string documentType, [FromForm] IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            throw new InvalidFileException("Please choose a file to upload.");

        if (file.Length > MaxFileSizeBytes)
            throw new InvalidFileException("File is too large. Maximum size is 5 MB.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            throw new InvalidFileException("Only PDF, JPG, and PNG files are allowed.");

        await using var stream = file.OpenReadStream();
        var storedFileName = await _fileStorage.SaveAsync(stream, file.FileName, ct);

        var result = await _mediator.Send(new AddDocumentCommand(
            id, UserId, documentType, file.FileName, storedFileName, file.ContentType, file.Length), ct);

        return Ok(ApiResponse<ApplicationDocumentDto>.SuccessResponse(result, "Document uploaded."));
    }

    [HttpPost("{id:guid}/documents/from-library/{libraryDocumentId:guid}")]
    public async Task<ActionResult<ApiResponse<ApplicationDocumentDto>>> AttachLibraryDocument(
        Guid id, Guid libraryDocumentId, CancellationToken ct)
    {
        var application = await _db.LoanApplications.FirstOrDefaultAsync(a => a.Id == id && a.EmployeeId == UserId, ct);
        if (application is null) return NotFound(ApiResponse<ApplicationDocumentDto>.FailureResponse("Application not found."));
        if (application.Status is not (EduLoan.Domain.Entities.ApplicationStatus.Draft or EduLoan.Domain.Entities.ApplicationStatus.InfoRequested))
            return Conflict(ApiResponse<ApplicationDocumentDto>.FailureResponse("Documents can only be attached while the application is a draft or awaiting information."));
        var libraryDoc = await _db.EmployeeDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == libraryDocumentId && d.EmployeeId == UserId, ct);
        if (libraryDoc is null) return NotFound(ApiResponse<ApplicationDocumentDto>.FailureResponse("Library document not found."));

        // Copy into application-owned storage so removing an application attachment never deletes the library original.
        string storedName;
        await using (var source = _fileStorage.OpenRead(libraryDoc.StoredFileName))
            storedName = await _fileStorage.SaveAsync(source, libraryDoc.FileName, ct);
        try
        {
            var result = await _mediator.Send(new AddDocumentCommand(id, UserId, libraryDoc.DocumentType, libraryDoc.FileName, storedName, libraryDoc.ContentType, libraryDoc.FileSizeBytes), ct);
            return Ok(ApiResponse<ApplicationDocumentDto>.SuccessResponse(result, "Library document attached to application."));
        }
        catch
        {
            await _fileStorage.DeleteAsync(storedName, ct);
            throw;
        }
    }

    [HttpGet("{id:guid}/documents/{documentId:guid}/download")]
    public async Task<IActionResult> DownloadDocument(Guid id, Guid documentId, CancellationToken ct)
    {
        var doc = await _mediator.Send(new GetDocumentForDownloadQuery(id, documentId, UserId), ct);
        var stream = _fileStorage.OpenRead(doc.StoredFileName);
        return File(stream, doc.ContentType, doc.OriginalFileName);
    }

    [HttpDelete("{id:guid}/documents/{documentId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveDocument(Guid id, Guid documentId, CancellationToken ct)
    {
        await _mediator.Send(new RemoveDocumentCommand(id, documentId, UserId), ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Document removed."));
    }
}
