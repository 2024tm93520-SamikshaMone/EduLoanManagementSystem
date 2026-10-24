namespace EduLoan.Domain.Entities;

/// <summary>Append-only record of successful mutating API operations. Sensitive request bodies are never stored.</summary>
public sealed class AuditLog
{
    public Guid Id { get; set; }
    public Guid? ActorUserId { get; set; }
    public string ActorRole { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Outcome { get; set; } = "Success";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? CorrelationId { get; set; }
    public string? Metadata { get; set; }
}
