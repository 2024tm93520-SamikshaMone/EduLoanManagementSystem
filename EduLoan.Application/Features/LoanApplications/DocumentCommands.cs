using EduLoan.Application.Common;
using EduLoan.Application.Interfaces;
using EduLoan.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduLoan.Application.Features.LoanApplications;

// The actual file bytes are saved to disk by the controller (via IFileStorageService)
// BEFORE this command runs, since MediatR commands shouldn't carry raw streams/IFormFile —
// that keeps the Application layer free of ASP.NET Core-specific types. This command just
// records the already-saved file's metadata against the application.
public record AddDocumentCommand(
    Guid ApplicationId, Guid RequestingUserId, string DocumentType,
    string OriginalFileName, string StoredFileName, string ContentType, long FileSizeBytes
) : IRequest<ApplicationDocumentDto>;

public class AddDocumentCommandValidator : AbstractValidator<AddDocumentCommand>
{
    public AddDocumentCommandValidator()
    {
        RuleFor(x => x.DocumentType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.OriginalFileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.StoredFileName).NotEmpty();
    }
}

public class AddDocumentCommandHandler : IRequestHandler<AddDocumentCommand, ApplicationDocumentDto>
{
    private readonly IAppDbContext _db;
    private readonly IFileStorageService _fileStorage;

    public AddDocumentCommandHandler(IAppDbContext db, IFileStorageService fileStorage)
    {
        _db = db;
        _fileStorage = fileStorage;
    }

    public async Task<ApplicationDocumentDto> Handle(AddDocumentCommand request, CancellationToken ct)
    {
        var application = await _db.LoanApplications.FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct);

        // If the DB-side checks fail, the file was already written to disk by the controller —
        // clean it up so we don't leak orphaned files for rejected uploads.
        async Task<Exception> Fail(Exception ex)
        {
            await _fileStorage.DeleteAsync(request.StoredFileName, ct);
            return ex;
        }

        if (application is null)
            throw await Fail(new NotFoundException(nameof(LoanApplication), request.ApplicationId));

        if (application.EmployeeId != request.RequestingUserId)
            throw await Fail(new ForbiddenException("You can only add documents to your own application."));

        if (application.Status != ApplicationStatus.Draft)
            throw await Fail(new InUseException("Documents can only be added while the application is a draft."));

        var document = new ApplicationDocument
        {
            Id = Guid.NewGuid(),
            ApplicationId = application.Id,
            DocumentType = request.DocumentType,
            FileName = request.OriginalFileName,
            StoredFileName = request.StoredFileName,
            ContentType = request.ContentType,
            FileSizeBytes = request.FileSizeBytes,
        };
        _db.AddApplicationDocument(document);
        await _db.SaveChangesAsync(ct);

        return new ApplicationDocumentDto(document.Id, document.DocumentType, document.FileName, document.FileSizeBytes, document.UploadedAt);
    }
}

public record RemoveDocumentCommand(Guid ApplicationId, Guid DocumentId, Guid RequestingUserId) : IRequest;

public class RemoveDocumentCommandHandler : IRequestHandler<RemoveDocumentCommand>
{
    private readonly IAppDbContext _db;
    private readonly IFileStorageService _fileStorage;

    public RemoveDocumentCommandHandler(IAppDbContext db, IFileStorageService fileStorage)
    {
        _db = db;
        _fileStorage = fileStorage;
    }

    public async Task Handle(RemoveDocumentCommand request, CancellationToken ct)
    {
        var application = await _db.LoanApplications
            .Include(a => a.Documents)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct)
            ?? throw new NotFoundException(nameof(LoanApplication), request.ApplicationId);

        if (application.EmployeeId != request.RequestingUserId)
            throw new ForbiddenException("You can only remove documents from your own application.");

        if (application.Status != ApplicationStatus.Draft)
            throw new InUseException("Documents can only be removed while the application is a draft.");

        var document = application.Documents.FirstOrDefault(d => d.Id == request.DocumentId)
            ?? throw new NotFoundException(nameof(ApplicationDocument), request.DocumentId);

        _db.RemoveApplicationDocument(document);
        await _db.SaveChangesAsync(ct);

        // Best-effort — the DB row is the source of truth for what's "attached" to the
        // application, so we don't roll back the DB change if disk cleanup has an issue.
        await _fileStorage.DeleteAsync(document.StoredFileName, ct);
    }
}

// ---- Download: returns enough info for the controller to stream the file back ----
public record GetDocumentForDownloadQuery(Guid ApplicationId, Guid DocumentId, Guid RequestingUserId)
    : IRequest<DocumentDownloadDto>;

public record DocumentDownloadDto(string StoredFileName, string OriginalFileName, string ContentType);

public class GetDocumentForDownloadQueryHandler : IRequestHandler<GetDocumentForDownloadQuery, DocumentDownloadDto>
{
    private readonly IAppDbContext _db;
    public GetDocumentForDownloadQueryHandler(IAppDbContext db) => _db = db;

    public async Task<DocumentDownloadDto> Handle(GetDocumentForDownloadQuery request, CancellationToken ct)
    {
        var application = await _db.LoanApplications
            .Include(a => a.Documents)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct)
            ?? throw new NotFoundException(nameof(LoanApplication), request.ApplicationId);

        if (application.EmployeeId != request.RequestingUserId)
            throw new ForbiddenException("You can only download documents from your own application.");

        var document = application.Documents.FirstOrDefault(d => d.Id == request.DocumentId)
            ?? throw new NotFoundException(nameof(ApplicationDocument), request.DocumentId);

        return new DocumentDownloadDto(document.StoredFileName, document.FileName, document.ContentType);
    }
}
