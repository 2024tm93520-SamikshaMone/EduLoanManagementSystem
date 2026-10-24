using EduLoan.API.Services;
using EduLoan.Application.Common;
using EduLoan.Application.Interfaces;
using EduLoan.Domain.Entities;
using EduLoan.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduLoan.API.Controllers;

/// <summary>Academic/demo-only repayment schedule and ledger. No money movement or payment gateway is integrated.</summary>
[ApiController]
[Route("api/v1/module11/simulation")]
[Authorize]
public sealed class Module11SimulationController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _current;
    public Module11SimulationController(AppDbContext db, ICurrentUserService current) { _db = db; _current = current; }
    private Guid ActorId => _current.UserId ?? throw new UnauthorizedAccessException("Authenticated user id is missing.");

    public sealed record RecordActivationRequest(DateTime ActivationDateUtc, string ActivationReference, decimal PrincipalAmount, decimal AnnualInterestRatePercent, string Notes);
    public sealed record RecordSimulatedPaymentRequest(decimal Amount, DateTime PaidAtUtc, string Note);

    [HttpGet("activations")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> GetActivations(CancellationToken ct)
    {
        var items = await _db.LoanActivationRecords.AsNoTracking().Include(x => x.Application).Include(x => x.Installments)
            .OrderByDescending(x => x.CreatedAtUtc).Select(x => new {
                x.Id, x.ApplicationId, x.Application!.ApplicationNumber, x.ActivationDateUtc, x.ActivationReference,
                x.PrincipalAmount, x.AnnualInterestRatePercent, x.TenureMonths, x.Notes, x.IsSimulation,
                Installments = x.Installments.OrderBy(i => i.InstallmentNumber).Select(i => new { i.Id, i.InstallmentNumber, i.DueDateUtc, i.ScheduledAmount, i.SimulatedAmountPaid, i.Status, i.SimulatedPaidAtUtc, i.SimulationNote })
            }).ToListAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(items));
    }

    [HttpPost("applications/{applicationId:guid}/activation")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> RecordActivation(Guid applicationId, [FromBody] RecordActivationRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ActivationReference) || string.IsNullOrWhiteSpace(request.Notes)) return BadRequest(ApiResponse<object>.FailureResponse("A simulation reference and notes are required."));
        if (request.ActivationDateUtc == default || request.ActivationDateUtc > DateTime.UtcNow.AddMinutes(5)) return BadRequest(ApiResponse<object>.FailureResponse("Activation date must be valid and cannot be in the future."));
        if (request.PrincipalAmount <= 0 || request.AnnualInterestRatePercent < 0 || request.AnnualInterestRatePercent > 100) return BadRequest(ApiResponse<object>.FailureResponse("Principal must be positive and the explicitly supplied annual interest rate must be between 0 and 100 percent."));
        var app = await _db.LoanApplications.FirstOrDefaultAsync(x => x.Id == applicationId, ct);
        if (app is null) return NotFound(ApiResponse<object>.FailureResponse("Application not found."));
        if (app.Status != ApplicationStatus.Processed) return Conflict(ApiResponse<object>.FailureResponse("A schedule can only be created after Finance has recorded external processing confirmation."));
        var finance = await _db.FinanceHandoffs.FirstOrDefaultAsync(x => x.ApplicationId == applicationId, ct);
        if (finance is null || finance.Status != "Processing Confirmed" || !finance.ProcessedAmount.HasValue) return Conflict(ApiResponse<object>.FailureResponse("A confirmed Finance processing record is required before recording the demo activation event."));
        if (request.PrincipalAmount > finance.ProcessedAmount.Value) return BadRequest(ApiResponse<object>.FailureResponse("Simulation principal cannot exceed the externally confirmed processed amount."));
        if (app.RequestedTenureMonths is < 1 or > 480) return Conflict(ApiResponse<object>.FailureResponse("The application does not have a valid approved repayment tenure."));
        if (await _db.LoanActivationRecords.AnyAsync(x => x.ApplicationId == applicationId, ct)) return Conflict(ApiResponse<object>.FailureResponse("A simulation activation record already exists for this application."));

        var activation = new LoanActivationRecord { Id = Guid.NewGuid(), ApplicationId = app.Id, RecordedByUserId = ActorId,
            ActivationDateUtc = DateTime.SpecifyKind(request.ActivationDateUtc, DateTimeKind.Utc), ActivationReference = request.ActivationReference.Trim(),
            PrincipalAmount = decimal.Round(request.PrincipalAmount, 2), AnnualInterestRatePercent = request.AnnualInterestRatePercent,
            TenureMonths = app.RequestedTenureMonths, Notes = request.Notes.Trim(), IsSimulation = true, CreatedAtUtc = DateTime.UtcNow };
        var monthlyRate = request.AnnualInterestRatePercent / 1200m;
        var pow = monthlyRate == 0 ? 1m : (decimal)Math.Pow((double)(1m + monthlyRate), activation.TenureMonths);
        var payment = monthlyRate == 0 ? activation.PrincipalAmount / activation.TenureMonths : activation.PrincipalAmount * monthlyRate * pow / (pow - 1m);
        var remaining = activation.PrincipalAmount;
        for (var n = 1; n <= activation.TenureMonths; n++)
        {
            var interest = decimal.Round(remaining * monthlyRate, 2);
            var principal = n == activation.TenureMonths ? remaining : decimal.Round(payment - interest, 2);
            var scheduled = n == activation.TenureMonths ? decimal.Round(remaining + interest, 2) : decimal.Round(interest + principal, 2);
            activation.Installments.Add(new SimulatedRepaymentInstallment { Id = Guid.NewGuid(), ActivationRecordId = activation.Id, InstallmentNumber = n,
                DueDateUtc = activation.ActivationDateUtc.AddMonths(n), ScheduledAmount = scheduled, SimulatedAmountPaid = 0, Status = "Scheduled" });
            remaining = Math.Max(0, remaining - principal);
        }
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        _db.LoanActivationRecords.Add(activation);
        _db.ApprovalWorkflowSteps.Add(new ApprovalWorkflowStep { Id = Guid.NewGuid(), ApplicationId = app.Id, ActorUserId = ActorId,
            ActorName = User.Identity?.Name ?? "Authenticated reviewer", ActorRole = User.IsInRole("Admin") ? "Admin" : "Finance",
            Action = WorkflowAction.ConfirmedProcessed, Comments = $"SIMULATION ONLY: activation event recorded; repayment schedule {activation.TenureMonths} installments generated. Reference: {activation.ActivationReference}", ActionedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { activation.Id, activation.ApplicationId, activation.IsSimulation, activation.TenureMonths, InstallmentCount = activation.Installments.Count }, "Simulation activation event recorded and schedule generated. No funds were transferred."));
    }

    [HttpPost("installments/{installmentId:guid}/payment-entry")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> RecordSimulatedPayment(Guid installmentId, [FromBody] RecordSimulatedPaymentRequest request, CancellationToken ct)
    {
        if (request.Amount <= 0 || string.IsNullOrWhiteSpace(request.Note)) return BadRequest(ApiResponse<object>.FailureResponse("A positive simulated amount and note are required."));
        if (request.PaidAtUtc == default || request.PaidAtUtc > DateTime.UtcNow.AddMinutes(5)) return BadRequest(ApiResponse<object>.FailureResponse("Simulated payment date is invalid."));
        var item = await _db.SimulatedRepaymentInstallments.Include(x => x.ActivationRecord).FirstOrDefaultAsync(x => x.Id == installmentId, ct);
        if (item is null) return NotFound(ApiResponse<object>.FailureResponse("Simulated installment not found."));
        if (item.ActivationRecord?.IsSimulation != true) return Conflict(ApiResponse<object>.FailureResponse("Only simulation installments can accept simulated entries."));
        if (item.Status == "Paid") return Conflict(ApiResponse<object>.FailureResponse("This installment is already fully paid in the simulation."));
        var outstanding = item.ScheduledAmount - item.SimulatedAmountPaid;
        if (request.Amount > outstanding) return BadRequest(ApiResponse<object>.FailureResponse("Simulated amount cannot exceed the installment's remaining simulated balance."));
        item.SimulatedAmountPaid = decimal.Round(item.SimulatedAmountPaid + request.Amount, 2);
        item.SimulatedPaidAtUtc = DateTime.SpecifyKind(request.PaidAtUtc, DateTimeKind.Utc);
        item.SimulationNote = request.Note.Trim(); item.RecordedByUserId = ActorId;
        item.Status = item.SimulatedAmountPaid >= item.ScheduledAmount ? "Paid" : "PartiallyPaid";
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Conflict(ApiResponse<object>.FailureResponse("This installment changed in another request. Refresh the schedule and retry.")); }
        return Ok(ApiResponse<object>.SuccessResponse(new { item.Id, item.Status, item.SimulatedAmountPaid, Outstanding = item.ScheduledAmount - item.SimulatedAmountPaid, SimulationOnly = true }, "Simulated payment entry recorded. No real payment was processed."));
    }

    [HttpGet("applications/{applicationId:guid}/schedule")]
    public async Task<IActionResult> GetSchedule(Guid applicationId, CancellationToken ct)
    {
        var app = await _db.LoanApplications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == applicationId, ct);
        if (app is null) return NotFound(ApiResponse<object>.FailureResponse("Application not found."));
        if (User.IsInRole("Employee") && app.EmployeeId != ActorId) return Forbid();
        if (!User.IsInRole("Employee") && !User.IsInRole("Admin") && !User.IsInRole("HR") && !User.IsInRole("Finance")) return Forbid();
        var activation = await _db.LoanActivationRecords.AsNoTracking().Include(x => x.Installments).FirstOrDefaultAsync(x => x.ApplicationId == applicationId, ct);
        if (activation is null) return NotFound(ApiResponse<object>.FailureResponse("No simulation activation or repayment schedule has been recorded."));
        return Ok(ApiResponse<object>.SuccessResponse(new { activation.Id, activation.ApplicationId, activation.ActivationDateUtc, activation.ActivationReference, activation.PrincipalAmount, activation.AnnualInterestRatePercent, activation.TenureMonths, activation.IsSimulation, Installments = activation.Installments.OrderBy(x => x.InstallmentNumber).Select(x => new { x.Id, x.InstallmentNumber, x.DueDateUtc, x.ScheduledAmount, x.SimulatedAmountPaid, x.Status, x.SimulatedPaidAtUtc, x.SimulationNote }) }));
    }
}
