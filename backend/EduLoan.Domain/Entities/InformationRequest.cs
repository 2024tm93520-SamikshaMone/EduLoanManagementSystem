namespace EduLoan.Domain.Entities;

public enum InformationRequestStatus { Pending, Submitted, Resolved }

public class InformationRequest
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public LoanApplication? Application { get; set; }
    public Guid RequestedByUserId { get; set; }
    public string RequestedByName { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string? RequiredDocumentType { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime? DueDateUtc { get; set; }
    public string? Instructions { get; set; }
    public InformationRequestStatus Status { get; set; } = InformationRequestStatus.Pending;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public List<InformationRequestResponse> Responses { get; set; } = new();
}

public class InformationRequestResponse
{
    public Guid Id { get; set; }
    public Guid InformationRequestId { get; set; }
    public InformationRequest? InformationRequest { get; set; }
    public Guid RespondedByUserId { get; set; }
    public string RespondedByName { get; set; } = string.Empty;
    public string ResponseText { get; set; } = string.Empty;
    public string? DocumentFileName { get; set; }
    public string? DocumentStoredFileName { get; set; }
    public Guid? ApplicationDocumentId { get; set; }
    public ApplicationDocument? ApplicationDocument { get; set; }
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
}
