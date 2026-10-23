using EduLoan.Application.Common;
using EduLoan.Application.Features.EligibilityRules;
using EduLoan.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduLoan.API.Controllers;

/// <summary>Read-only employee-facing eligibility guidance. The same rule engine is used at submission.</summary>
[ApiController]
[Route("api/v1/eligibility")]
[Authorize(Roles = "Employee,Admin,Finance")]
public sealed class EligibilityPreviewController : ControllerBase
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IEligibilityRuleEngine _engine;

    public EligibilityPreviewController(IAppDbContext db, ICurrentUserService currentUser, IEligibilityRuleEngine engine)
    { _db = db; _currentUser = currentUser; _engine = engine; }

    public sealed record PreviewRequest(decimal RequestedAmount, int RequestedTenureMonths, decimal TotalEducationFees);
    public sealed record PreviewResponse(decimal MonthlySalary, decimal TenureMonths, decimal? MaximumEligibleAmount,
        bool HasBlockingFailures, IReadOnlyList<EligibilityRuleDto> Rules, IReadOnlyList<RuleEvaluationOutcome> Results);

    [HttpGet("rules")]
    public async Task<ActionResult<ApiResponse<List<EligibilityRuleDto>>>> GetRules(CancellationToken ct)
    {
        var rules = await _db.EligibilityRules.Where(r => r.IsActive).OrderBy(r => r.RuleCategory).ThenBy(r => r.RuleName).ToListAsync(ct);
        return Ok(ApiResponse<List<EligibilityRuleDto>>.SuccessResponse(rules.Select(r => new EligibilityRuleDto(
            r.Id, r.RuleName, r.RuleCategory, r.ConditionField, r.Operator, r.ConditionValue,
            r.ErrorMessage, r.Severity.ToString(), r.IsActive)).ToList()));
    }

    [HttpPost("preview")]
    public async Task<ActionResult<ApiResponse<PreviewResponse>>> Preview([FromBody] PreviewRequest request, CancellationToken ct)
    {
        if (request.RequestedAmount <= 0 || request.RequestedTenureMonths <= 0 || request.TotalEducationFees <= 0)
            return BadRequest(ApiResponse<PreviewResponse>.FailureResponse("Enter a positive loan amount, tenure, and education fee."));
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        var employee = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value, ct);
        if (employee is null) return NotFound(ApiResponse<PreviewResponse>.FailureResponse("Employee profile was not found."));
        var rules = await _db.EligibilityRules.Where(r => r.IsActive).OrderBy(r => r.RuleCategory).ThenBy(r => r.RuleName).ToListAsync(ct);
        var context = new RuleEvaluationContext { TenureMonths = employee.TenureMonths(), MonthlySalary = employee.MonthlySalary,
            RequestedAmount = request.RequestedAmount, RequestedTenureMonths = request.RequestedTenureMonths,
            TotalEducationFees = request.TotalEducationFees };
        var outcomes = _engine.Evaluate(context, rules);
        var amountCaps = rules.Where(r => r.ConditionField == "RequestedAmount" && r.Severity == EduLoan.Domain.Entities.RuleSeverity.Blocking && r.Operator is "<=" or "<").Select(r => r.ConditionValue).ToList();
        decimal? cap = amountCaps.Count == 0 ? null : amountCaps.Min();
        var result = new PreviewResponse(employee.MonthlySalary, context.TenureMonths, cap,
            outcomes.Any(o => !o.Passed && o.Severity == "Blocking"),
            rules.Select(r => new EligibilityRuleDto(r.Id, r.RuleName, r.RuleCategory, r.ConditionField, r.Operator,
                r.ConditionValue, r.ErrorMessage, r.Severity.ToString(), r.IsActive)).ToList(), outcomes);
        return Ok(ApiResponse<PreviewResponse>.SuccessResponse(result));
    }
}

internal static class EligibilityPreviewUserExtensions
{
    public static decimal TenureMonths(this EduLoan.Domain.Entities.User user)
    {
        var start = user.DateOfJoining.ToDateTime(TimeOnly.MinValue);
        var now = DateTime.UtcNow;
        return Math.Max(0, (now.Year - start.Year) * 12 + now.Month - start.Month - (now.Day < start.Day ? 1 : 0));
    }
}
