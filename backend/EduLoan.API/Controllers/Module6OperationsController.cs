using System.Security.Claims;
using EduLoan.API.Services;
using EduLoan.Application.Common;
using EduLoan.Application.Interfaces;
using EduLoan.Domain.Entities;
using EduLoan.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduLoan.API.Controllers;

[ApiController]
[Route("api/v1/module6")]
[Authorize]
public sealed class Module6OperationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IFileStorageService _fileStorage;
    public Module6OperationsController(AppDbContext db, ICurrentUserService current, IFileStorageService fileStorage) { _db = db; _current = current; _fileStorage = fileStorage; }
    private Guid ActorId => _current.UserId ?? throw new UnauthorizedAccessException("Authenticated user id is missing.");

    public sealed record CreateRfiRequest(string Question, string? RequiredDocumentType, string Reason, DateTime? DueDateUtc, string? Instructions);
    public sealed record RespondRfiRequest(string ResponseText, Guid? DocumentId = null);
    public sealed record DocumentReviewRequest(string Status, string? Comments);
    public sealed record FinanceActionRequest(string? BankReferenceNumber, DateTime? BankSubmissionDateUtc, decimal? ProcessedAmount, string? ConfirmationReference, DateTime? ConfirmationDateUtc, string? Comments, string? FailureReason);
    public sealed record SaveDocumentRequirementRequest(int? Id, string DocumentType, string DisplayName, string Category, bool IsRequired, decimal? MinimumLoanAmount, decimal? MaximumLoanAmount, string? EmploymentType, string? EducationLevel, bool RequiresCollateral, bool IsActive, int DisplayOrder);

    [HttpGet("document-requirements")]
    public async Task<IActionResult> GetDocumentRequirements(CancellationToken ct)
    {
        var query = _db.DocumentRequirements.AsNoTracking();
        if (!User.IsInRole("Admin")) query = query.Where(x => x.IsActive);
        var rows = await query.OrderBy(x => x.DisplayOrder).ThenBy(x => x.DisplayName)
            .Select(x => new { x.Id, x.DocumentType, x.DisplayName, x.Category, x.IsRequired, x.MinimumLoanAmount, x.MaximumLoanAmount, x.EmploymentType, x.EducationLevel, x.RequiresCollateral, x.IsActive, x.DisplayOrder, x.UpdatedAtUtc })
            .ToListAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(rows));
    }

    [HttpPost("document-requirements")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SaveDocumentRequirement([FromBody] SaveDocumentRequirementRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.DocumentType) || string.IsNullOrWhiteSpace(request.DisplayName) || string.IsNullOrWhiteSpace(request.Category))
            return BadRequest(ApiResponse<object>.FailureResponse("Document type, display name and category are required."));
        if (request.MinimumLoanAmount < 0 || request.MaximumLoanAmount < 0 || (request.MinimumLoanAmount.HasValue && request.MaximumLoanAmount.HasValue && request.MinimumLoanAmount > request.MaximumLoanAmount))
            return BadRequest(ApiResponse<object>.FailureResponse("Loan amount range is invalid."));
        DocumentRequirement item;
        if (request.Id.HasValue)
        {
            var existing = await _db.DocumentRequirements.FirstOrDefaultAsync(x => x.Id == request.Id.Value, ct);
            if (existing is null) return NotFound(ApiResponse<object>.FailureResponse("Document requirement was not found."));
            item = existing;
        }
        else
        {
            item = new DocumentRequirement();
            _db.DocumentRequirements.Add(item);
        }
        item.DocumentType = request.DocumentType.Trim(); item.DisplayName = request.DisplayName.Trim(); item.Category = request.Category.Trim();
        item.IsRequired = request.IsRequired; item.MinimumLoanAmount = request.MinimumLoanAmount; item.MaximumLoanAmount = request.MaximumLoanAmount;
        item.EmploymentType = string.IsNullOrWhiteSpace(request.EmploymentType) ? null : request.EmploymentType.Trim();
        item.EducationLevel = string.IsNullOrWhiteSpace(request.EducationLevel) ? null : request.EducationLevel.Trim();
        item.RequiresCollateral = request.RequiresCollateral; item.IsActive = request.IsActive; item.DisplayOrder = request.DisplayOrder; item.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { item.Id }, "Document requirement saved."));
    }

    [HttpGet("applications/{applicationId:guid}/rfis")]
    public async Task<IActionResult> GetRfis(Guid applicationId, CancellationToken ct)
    {
        var app = await _db.LoanApplications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == applicationId, ct);
        if (app is null) return NotFound(ApiResponse<object>.FailureResponse("Application not found."));
        if (User.IsInRole("Employee") && app.EmployeeId != ActorId) return Forbid();
        var items = await _db.InformationRequests.AsNoTracking().Where(x => x.ApplicationId == applicationId)
            .Include(x => x.Responses).OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new { x.Id, x.ApplicationId, x.Question, x.RequiredDocumentType, x.Reason, x.DueDateUtc, x.Instructions, Status = x.Status.ToString(), x.RequestedByName, x.CreatedAtUtc, Responses = x.Responses.OrderBy(r => r.SubmittedAtUtc).Select(r => new { r.Id, r.RespondedByName, r.ResponseText, r.DocumentFileName, r.ApplicationDocumentId, r.SubmittedAtUtc }) }).ToListAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(items));
    }

    [HttpPost("applications/{applicationId:guid}/rfis")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateRfi(Guid applicationId, [FromBody] CreateRfiRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Question) || string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(ApiResponse<object>.FailureResponse("Question and reason are required."));
        var app = await _db.LoanApplications.FirstOrDefaultAsync(x => x.Id == applicationId, ct);
        if (app is null) return NotFound(ApiResponse<object>.FailureResponse("Application not found."));
        if (app.Status is not (ApplicationStatus.Submitted or ApplicationStatus.UnderReview or ApplicationStatus.ManualReview or ApplicationStatus.InfoRequested)) return Conflict(ApiResponse<object>.FailureResponse("Information can only be requested during application review."));
        var actor = await _db.Users.FirstAsync(x => x.Id == ActorId, ct);
        var rfi = new InformationRequest { Id = Guid.NewGuid(), ApplicationId = applicationId, RequestedByUserId = actor.Id, RequestedByName = actor.FullName, Question = request.Question.Trim(), RequiredDocumentType = string.IsNullOrWhiteSpace(request.RequiredDocumentType) ? null : request.RequiredDocumentType.Trim(), Reason = request.Reason.Trim(), DueDateUtc = request.DueDateUtc, Instructions = request.Instructions?.Trim(), Status = InformationRequestStatus.Pending };
        app.Status = ApplicationStatus.InfoRequested; app.UpdatedAt = DateTime.UtcNow;
        _db.InformationRequests.Add(rfi);
        _db.ApprovalWorkflowSteps.Add(new ApprovalWorkflowStep { Id = Guid.NewGuid(), ApplicationId = app.Id, ActorUserId = actor.Id, ActorName = actor.FullName, ActorRole = actor.Role == UserRole.HR ? UserRole.Admin.ToString() : actor.Role.ToString(), Action = WorkflowAction.RequestedInfo, Comments = $"{rfi.Question} Reason: {rfi.Reason}" });
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { rfi.Id, Status = rfi.Status.ToString() }, "Information request created."));
    }

    [HttpPost("rfis/{rfiId:guid}/responses")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> RespondToRfi(Guid rfiId, [FromBody] RespondRfiRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ResponseText)) return BadRequest(ApiResponse<object>.FailureResponse("A response is required."));
        var rfi = await _db.InformationRequests.Include(x => x.Application).FirstOrDefaultAsync(x => x.Id == rfiId, ct);
        if (rfi is null) return NotFound(ApiResponse<object>.FailureResponse("Information request not found."));
        if (rfi.Application!.EmployeeId != ActorId) return Forbid();
        if (rfi.Status != InformationRequestStatus.Pending) return Conflict(ApiResponse<object>.FailureResponse("This request is no longer pending."));
        var actor = await _db.Users.FirstAsync(x => x.Id == ActorId, ct);
        ApplicationDocument? linkedDocument = null;
        if (request.DocumentId.HasValue)
        {
            linkedDocument = await _db.ApplicationDocuments.FirstOrDefaultAsync(d => d.Id == request.DocumentId.Value && d.ApplicationId == rfi.ApplicationId, ct);
            if (linkedDocument is null) return BadRequest(ApiResponse<object>.FailureResponse("The selected document does not belong to this application."));
            if (linkedDocument.UploadedByUserId.HasValue && linkedDocument.UploadedByUserId != ActorId) return Forbid();
        }
        _db.InformationRequestResponses.Add(new InformationRequestResponse { Id = Guid.NewGuid(), InformationRequestId = rfi.Id, RespondedByUserId = actor.Id, RespondedByName = actor.FullName, ResponseText = request.ResponseText.Trim(), DocumentFileName = linkedDocument?.FileName, DocumentStoredFileName = linkedDocument?.StoredFileName, ApplicationDocumentId = linkedDocument?.Id });
        rfi.Status = InformationRequestStatus.Submitted;
        // Keep the application in Information Requested until every active request is resolved.
        rfi.Application.Status = ApplicationStatus.InfoRequested; rfi.Application.UpdatedAt = DateTime.UtcNow;
        _db.ApprovalWorkflowSteps.Add(new ApprovalWorkflowStep { Id = Guid.NewGuid(), ApplicationId = rfi.ApplicationId, ActorUserId = actor.Id, ActorName = actor.FullName, ActorRole = actor.Role == UserRole.HR ? UserRole.Admin.ToString() : actor.Role.ToString(), Action = WorkflowAction.Resubmitted, Comments = "Employee submitted an RFI response." });
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { rfi.Id, Status = rfi.Status.ToString() }, "Response submitted."));
    }

    [HttpPost("rfis/{rfiId:guid}/resolve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ResolveRfi(Guid rfiId, CancellationToken ct)
    {
        var rfi = await _db.InformationRequests.Include(x => x.Application).FirstOrDefaultAsync(x => x.Id == rfiId, ct);
        if (rfi is null) return NotFound(ApiResponse<object>.FailureResponse("Information request not found."));
        if (rfi.Status != InformationRequestStatus.Submitted) return Conflict(ApiResponse<object>.FailureResponse("Only submitted requests can be resolved."));
        rfi.Status = InformationRequestStatus.Resolved;
        await Audit(rfi.ApplicationId, WorkflowAction.StartedReview, $"Information request {rfi.Id} resolved by Admin.", ct);
        var otherOpenRequests = await _db.InformationRequests.AnyAsync(x => x.ApplicationId == rfi.ApplicationId && x.Id != rfi.Id && (x.Status == InformationRequestStatus.Pending || x.Status == InformationRequestStatus.Submitted), ct);
        if (!otherOpenRequests && rfi.Application is not null)
        {
            rfi.Application.Status = ApplicationStatus.UnderReview;
            rfi.Application.UpdatedAt = DateTime.UtcNow;
            await Audit(rfi.ApplicationId, WorkflowAction.StartedReview, "All information requests have been resolved; review resumed.", ct);
        }
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { rfi.Id, Status = rfi.Status.ToString() }, "Information request resolved."));
    }

    [HttpGet("applications/{applicationId:guid}/documents")]
    [Authorize(Roles = "Admin,Finance,Employee")]
    public async Task<IActionResult> GetApplicationDocuments(Guid applicationId, CancellationToken ct)
    {
        var application = await _db.LoanApplications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == applicationId, ct);
        if (application is null) return NotFound(ApiResponse<object>.FailureResponse("Application not found."));
        if (User.IsInRole("Employee") && application.EmployeeId != ActorId) return Forbid();
        var documents = await _db.ApplicationDocuments.AsNoTracking().Where(x => x.ApplicationId == applicationId)
            .OrderBy(x => x.UploadedAt).Select(x => new { x.Id, x.ApplicationId, x.DocumentType, x.FileName, x.ContentType, x.FileSizeBytes, x.UploadedAt, x.UploadedByUserId, x.IsRequired, Status = x.VerificationStatus.ToString(), x.VerificationComments, x.VerifiedAtUtc, x.VerifiedByUserId }).ToListAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(documents));
    }

    [HttpGet("documents/{documentId:guid}")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> GetDocument(Guid documentId, CancellationToken ct)
    {
        var d = await _db.ApplicationDocuments.AsNoTracking().Where(x => x.Id == documentId).Select(x => new { x.Id, x.ApplicationId, x.DocumentType, x.FileName, x.ContentType, x.FileSizeBytes, x.UploadedAt, UploadedByUserId = x.UploadedByUserId, Status = x.VerificationStatus.ToString(), x.VerificationComments, x.VerifiedAtUtc, x.VerifiedByUserId, x.IsRequired }).FirstOrDefaultAsync(ct);
        return d is null ? NotFound(ApiResponse<object>.FailureResponse("Document not found.")) : Ok(ApiResponse<object>.SuccessResponse(d));
    }

    [HttpGet("documents/{documentId:guid}/download")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> DownloadReviewedDocument(Guid documentId, CancellationToken ct)
    {
        var doc = await _db.ApplicationDocuments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == documentId, ct);
        if (doc is null) return NotFound(ApiResponse<object>.FailureResponse("Document not found."));
        var stream = _fileStorage.OpenRead(doc.StoredFileName);
        return File(stream, string.IsNullOrWhiteSpace(doc.ContentType) ? "application/octet-stream" : doc.ContentType, doc.FileName);
    }

    [HttpPost("documents/{documentId:guid}/review")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReviewDocument(Guid documentId, [FromBody] DocumentReviewRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<DocumentVerificationStatus>(request.Status, true, out var status) || status is not (DocumentVerificationStatus.Verified or DocumentVerificationStatus.Rejected or DocumentVerificationStatus.UnderReview)) return BadRequest(ApiResponse<object>.FailureResponse("Status must be Verified, Rejected or UnderReview."));
        if (status == DocumentVerificationStatus.Rejected && string.IsNullOrWhiteSpace(request.Comments)) return BadRequest(ApiResponse<object>.FailureResponse("A rejection reason is required."));
        var doc = await _db.ApplicationDocuments.Include(x => x.Application).FirstOrDefaultAsync(x => x.Id == documentId, ct);
        if (doc is null) return NotFound(ApiResponse<object>.FailureResponse("Document not found."));
        doc.VerificationStatus = status; doc.VerificationComments = request.Comments?.Trim(); doc.VerifiedByUserId = ActorId; doc.VerifiedAtUtc = DateTime.UtcNow;
        var actor = await _db.Users.FirstAsync(x => x.Id == ActorId, ct);
        _db.ApprovalWorkflowSteps.Add(new ApprovalWorkflowStep { Id = Guid.NewGuid(), ApplicationId = doc.ApplicationId, ActorUserId = actor.Id, ActorName = actor.FullName, ActorRole = actor.Role == UserRole.HR ? UserRole.Admin.ToString() : actor.Role.ToString(), Action = WorkflowAction.StartedReview, Comments = $"Document {doc.DocumentType} marked {status}. {request.Comments}" });
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { doc.Id, Status = doc.VerificationStatus.ToString(), doc.VerificationComments }, "Document review saved."));
    }

    [HttpGet("finance/applications")]
    [Authorize(Roles = "Finance,Admin")]
    public async Task<IActionResult> FinanceApplications(CancellationToken ct)
    {
        var rows = await _db.LoanApplications.AsNoTracking().Where(a => a.Status == ApplicationStatus.PendingFinance || a.Status == ApplicationStatus.Processed)
            .Include(a => a.Employee).Include(a => a.College).Include(a => a.Course)
            .Select(a => new { a.Id, a.ApplicationNumber, EmployeeName = a.Employee!.FullName, EmployeeCode = a.Employee.EmployeeCode, a.RequestedAmount, a.TotalEducationFees, a.Status, a.SubmittedAt, a.UpdatedAt, Finance = _db.FinanceHandoffs.Where(f => f.ApplicationId == a.Id).Select(f => new { f.Status, f.ReceivedAtUtc, f.ReceivedByUserId, f.BankReferenceNumber, f.BankSubmissionDateUtc, f.ConfirmationReference, f.ConfirmationDateUtc, f.ProcessedAmount, f.FinanceComments, f.FailureReason }).FirstOrDefault() }).OrderByDescending(a => a.UpdatedAt).ToListAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(rows));
    }

    [HttpPost("finance/applications/{applicationId:guid}/received")]
    [Authorize(Roles = "Finance,Admin")]
    public async Task<IActionResult> FinanceReceived(Guid applicationId, [FromBody] FinanceActionRequest request, CancellationToken ct)
    {
        var app = await _db.LoanApplications.FirstOrDefaultAsync(x => x.Id == applicationId, ct);
        if (app is null) return NotFound(ApiResponse<object>.FailureResponse("Application not found."));
        if (app.Status != ApplicationStatus.PendingFinance) return Conflict(ApiResponse<object>.FailureResponse("Only approved applications sent to Finance can be received."));
        var h = await _db.FinanceHandoffs.FirstOrDefaultAsync(x => x.ApplicationId == app.Id, ct);
        if (h is not null && h.ReceivedAtUtc is not null) return Conflict(ApiResponse<object>.FailureResponse("Finance has already received this application."));
        if (h is null) { h = new FinanceHandoff { Id = Guid.NewGuid(), ApplicationId = app.Id }; _db.FinanceHandoffs.Add(h); }
        h.ReceivedByUserId = ActorId; h.ReceivedAtUtc = DateTime.UtcNow; h.Status = "Pending Finance Review"; h.FinanceComments = request.Comments; h.UpdatedAtUtc = DateTime.UtcNow;
        await Audit(app.Id, WorkflowAction.FinanceReceived, "Finance marked application received.", ct); await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { h.Id, h.Status }, "Application received by Finance."));
    }

    [HttpPost("finance/applications/{applicationId:guid}/send-to-bank")]
    [Authorize(Roles = "Finance,Admin")]
    public async Task<IActionResult> SendToBank(Guid applicationId, [FromBody] FinanceActionRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.BankReferenceNumber) || request.BankSubmissionDateUtc is null) return BadRequest(ApiResponse<object>.FailureResponse("Bank reference and submission date are required."));
        var app = await _db.LoanApplications.FirstOrDefaultAsync(x => x.Id == applicationId, ct);
        if (app is null) return NotFound(ApiResponse<object>.FailureResponse("Application not found."));
        if (app.Status != ApplicationStatus.PendingFinance) return Conflict(ApiResponse<object>.FailureResponse("Only applications pending Finance can be sent to a bank."));
        var h = await _db.FinanceHandoffs.FirstOrDefaultAsync(x => x.ApplicationId == app.Id, ct);
        if (h is null || h.Status != "Pending Finance Review" || h.ReceivedAtUtc is null)
            return Conflict(ApiResponse<object>.FailureResponse("Mark the application received by Finance before sending it to the bank."));
        h.BankReferenceNumber = request.BankReferenceNumber.Trim(); h.BankSubmissionDateUtc = request.BankSubmissionDateUtc; h.Status = "Awaiting Bank Confirmation"; h.FinanceComments = request.Comments; h.UpdatedAtUtc = DateTime.UtcNow;
        await Audit(app.Id, WorkflowAction.SentToBank, "Finance sent application to bank. Reference: " + h.BankReferenceNumber, ct); await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { h.Id, h.Status }, "Bank submission recorded."));
    }

    [HttpPost("finance/applications/{applicationId:guid}/confirm")]
    [Authorize(Roles = "Finance,Admin")]
    public async Task<IActionResult> ConfirmBankProcessing(Guid applicationId, [FromBody] FinanceActionRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ConfirmationReference) || request.ConfirmationDateUtc is null || request.ProcessedAmount is null || request.ProcessedAmount <= 0) return BadRequest(ApiResponse<object>.FailureResponse("Confirmation reference, date and positive processed amount are required."));
        var app = await _db.LoanApplications.FirstOrDefaultAsync(x => x.Id == applicationId, ct);
        if (app is null) return NotFound(ApiResponse<object>.FailureResponse("Application not found."));
        var h = await _db.FinanceHandoffs.FirstOrDefaultAsync(x => x.ApplicationId == app.Id, ct);
        if (h is null || h.Status != "Awaiting Bank Confirmation") return Conflict(ApiResponse<object>.FailureResponse("Send the application to the bank before recording confirmation."));
        if (request.ProcessedAmount > app.RequestedAmount) return BadRequest(ApiResponse<object>.FailureResponse("Confirmed amount cannot exceed the requested amount."));
        h.ConfirmationReference = request.ConfirmationReference.Trim(); h.ConfirmationDateUtc = request.ConfirmationDateUtc; h.ProcessedAmount = request.ProcessedAmount; h.Status = "Processing Confirmed"; h.FinanceComments = request.Comments; h.UpdatedAtUtc = DateTime.UtcNow;
        app.Status = ApplicationStatus.Processed; app.UpdatedAt = DateTime.UtcNow;
        await Audit(app.Id, WorkflowAction.ConfirmedProcessed, "External bank processing confirmation recorded. Reference: " + h.ConfirmationReference, ct); await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { h.Id, h.Status, ApplicationStatus = app.Status.ToString() }, "Processing confirmation recorded. No funds were transferred by this system."));
    }

    [HttpPost("finance/applications/{applicationId:guid}/failed")]
    [Authorize(Roles = "Finance,Admin")]
    public async Task<IActionResult> MarkBankProcessingFailed(Guid applicationId, [FromBody] FinanceActionRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FailureReason)) return BadRequest(ApiResponse<object>.FailureResponse("Failure reason is required."));
        var app = await _db.LoanApplications.FirstOrDefaultAsync(x => x.Id == applicationId, ct);
        if (app is null) return NotFound(ApiResponse<object>.FailureResponse("Application not found."));
        var h = await _db.FinanceHandoffs.FirstOrDefaultAsync(x => x.ApplicationId == app.Id, ct);
        if (h is null || h.Status != "Awaiting Bank Confirmation") return Conflict(ApiResponse<object>.FailureResponse("Only bank-submitted applications can be marked failed."));
        h.Status = "Processing Failed"; h.FailureReason = request.FailureReason.Trim(); h.FinanceComments = request.Comments; h.UpdatedAtUtc = DateTime.UtcNow;
        await Audit(app.Id, WorkflowAction.ProcessingFailed, "External bank processing failed: " + h.FailureReason, ct); await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { h.Id, h.Status }, "Bank processing failure recorded."));
    }

    private async Task Audit(Guid applicationId, WorkflowAction action, string comments, CancellationToken ct)
    {
        var actor = await _db.Users.FirstAsync(x => x.Id == ActorId, ct);
        _db.ApprovalWorkflowSteps.Add(new ApprovalWorkflowStep { Id = Guid.NewGuid(), ApplicationId = applicationId, ActorUserId = actor.Id, ActorName = actor.FullName, ActorRole = actor.Role == UserRole.HR ? UserRole.Admin.ToString() : actor.Role.ToString(), Action = action, Comments = comments });
    }
}
