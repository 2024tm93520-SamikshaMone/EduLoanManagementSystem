namespace EduLoan.Domain.Entities;

/// <summary>Simulation-only installment ledger entry; no payment processor or real funds are involved.</summary>
public sealed class SimulatedRepaymentInstallment
{
    public Guid Id { get; set; }
    public Guid ActivationRecordId { get; set; }
    public LoanActivationRecord? ActivationRecord { get; set; }
    public int InstallmentNumber { get; set; }
    public DateTime DueDateUtc { get; set; }
    public decimal ScheduledAmount { get; set; }
    public decimal SimulatedAmountPaid { get; set; }
    public DateTime? SimulatedPaidAtUtc { get; set; }
    public string Status { get; set; } = "Scheduled";
    public string? SimulationNote { get; set; }
    public Guid? RecordedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
