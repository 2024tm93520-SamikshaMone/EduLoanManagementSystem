namespace EduLoan.Domain.Entities;

public enum ConsistencyFlagStatus { Open, UnderInvestigation, Resolved, Dismissed }

/// <summary>Persisted deterministic review signal. It is not a lending decision.</summary>
public sealed class ConsistencyFlag
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public LoanApplication? Application { get; set; }
    public string RuleCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "Consistency";
    public string Severity { get; set; } = "Medium";
    public string Description { get; set; } = string.Empty;
    public DateTime DetectedAtUtc { get; set; } = DateTime.UtcNow;
    public ConsistencyFlagStatus Status { get; set; } = ConsistencyFlagStatus.Open;
    public Guid? ReviewedByUserId { get; set; }
    public User? ReviewedBy { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ResolutionNotes { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public ICollection<ConsistencyFlagHistory> History { get; set; } = new List<ConsistencyFlagHistory>();
}

public sealed class ConsistencyFlagHistory
{
    public Guid Id { get; set; }
    public Guid ConsistencyFlagId { get; set; }
    public ConsistencyFlag? ConsistencyFlag { get; set; }
    public Guid ActorUserId { get; set; }
    public User? Actor { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
