namespace EduLoan.Domain.Entities;

/// <summary>Demo-only record of a verified activation/disbursement event. It never transfers funds.</summary>
public sealed class LoanActivationRecord
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public LoanApplication? Application { get; set; }
    public Guid RecordedByUserId { get; set; }
    public DateTime ActivationDateUtc { get; set; }
    public string ActivationReference { get; set; } = string.Empty;
    public decimal PrincipalAmount { get; set; }
    public decimal AnnualInterestRatePercent { get; set; }
    public int TenureMonths { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsSimulation { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public List<SimulatedRepaymentInstallment> Installments { get; set; } = new();
}
