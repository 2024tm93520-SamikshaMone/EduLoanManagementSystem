using EduLoan.Application.Common;
using EduLoan.Application.Features.EligibilityRules;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduLoan.API.Controllers;

// Deliberately a separate controller from AdminController (rather than extending it)
// so the existing, working AdminController.cs file stays completely untouched.
[ApiController]
[Route("api/v1/admin/eligibility-rules")]
[Authorize(Roles = "Admin")]
public class AdminEligibilityRulesController : ControllerBase
{
    private readonly IMediator _mediator;
    public AdminEligibilityRulesController(IMediator mediator) => _mediator = mediator;

    public record UpsertEligibilityRuleRequest(
        string RuleName, string RuleCategory, string ConditionField, string Operator,
        decimal ConditionValue, string ErrorMessage, string Severity, bool IsActive = true);

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<EligibilityRuleDto>>>> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetEligibilityRulesQuery(), ct);
        return Ok(ApiResponse<List<EligibilityRuleDto>>.SuccessResponse(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<EligibilityRuleDto>>> Create(
        [FromBody] UpsertEligibilityRuleRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateEligibilityRuleCommand(
            request.RuleName, request.RuleCategory, request.ConditionField, request.Operator,
            request.ConditionValue, request.ErrorMessage, request.Severity), ct);
        return Ok(ApiResponse<EligibilityRuleDto>.SuccessResponse(result, "Rule created."));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<EligibilityRuleDto>>> Update(
        int id, [FromBody] UpsertEligibilityRuleRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateEligibilityRuleCommand(
            id, request.RuleName, request.RuleCategory, request.ConditionField, request.Operator,
            request.ConditionValue, request.ErrorMessage, request.Severity, request.IsActive), ct);
        return Ok(ApiResponse<EligibilityRuleDto>.SuccessResponse(result, "Rule updated."));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteEligibilityRuleCommand(id), ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Rule deleted."));
    }
}
