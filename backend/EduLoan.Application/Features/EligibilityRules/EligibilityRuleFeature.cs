using EduLoan.Application.Common;
using EduLoan.Application.Interfaces;
using EduLoan.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduLoan.Application.Features.EligibilityRules;

public record EligibilityRuleDto(
    int Id, string RuleName, string RuleCategory, string ConditionField,
    string Operator, decimal ConditionValue, string ErrorMessage, string Severity, bool IsActive
);

internal static class RuleFieldsAndOperators
{
    public static readonly string[] ValidOperators = { ">=", "<=", ">", "<", "==", "!=" };
}

// ---- Get all ----
public record GetEligibilityRulesQuery : IRequest<List<EligibilityRuleDto>>;

public class GetEligibilityRulesQueryHandler : IRequestHandler<GetEligibilityRulesQuery, List<EligibilityRuleDto>>
{
    private readonly IAppDbContext _db;
    public GetEligibilityRulesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<EligibilityRuleDto>> Handle(GetEligibilityRulesQuery request, CancellationToken ct)
        => (await _db.EligibilityRules.OrderBy(r => r.RuleCategory).ThenBy(r => r.RuleName).ToListAsync(ct))
            .Select(ToDto).ToList();

    internal static EligibilityRuleDto ToDto(EligibilityRule r) => new(
        r.Id, r.RuleName, r.RuleCategory, r.ConditionField, r.Operator, r.ConditionValue,
        r.ErrorMessage, r.Severity.ToString(), r.IsActive);
}

// ---- Create ----
public record CreateEligibilityRuleCommand(
    string RuleName, string RuleCategory, string ConditionField, string Operator,
    decimal ConditionValue, string ErrorMessage, string Severity
) : IRequest<EligibilityRuleDto>;

public class CreateEligibilityRuleCommandValidator : AbstractValidator<CreateEligibilityRuleCommand>
{
    public CreateEligibilityRuleCommandValidator()
    {
        RuleFor(x => x.RuleName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.RuleCategory).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ConditionField).NotEmpty().Must(f => RuleEvaluationContext.KnownFields.Contains(f))
            .WithMessage($"ConditionField must be one of: {string.Join(", ", RuleEvaluationContext.KnownFields)}.");
        RuleFor(x => x.Operator).NotEmpty().Must(o => RuleFieldsAndOperators.ValidOperators.Contains(o))
            .WithMessage($"Operator must be one of: {string.Join(", ", RuleFieldsAndOperators.ValidOperators)}.");
        RuleFor(x => x.ErrorMessage).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Severity).Must(s => s is "Blocking" or "Warning")
            .WithMessage("Severity must be 'Blocking' or 'Warning'.");
    }
}

public class CreateEligibilityRuleCommandHandler : IRequestHandler<CreateEligibilityRuleCommand, EligibilityRuleDto>
{
    private readonly IAppDbContext _db;
    public CreateEligibilityRuleCommandHandler(IAppDbContext db) => _db = db;

    public async Task<EligibilityRuleDto> Handle(CreateEligibilityRuleCommand request, CancellationToken ct)
    {
        var rule = new EligibilityRule
        {
            RuleName = request.RuleName,
            RuleCategory = request.RuleCategory,
            ConditionField = request.ConditionField,
            Operator = request.Operator,
            ConditionValue = request.ConditionValue,
            ErrorMessage = request.ErrorMessage,
            Severity = Enum.Parse<RuleSeverity>(request.Severity),
            IsActive = true,
        };
        _db.AddEligibilityRule(rule);
        await _db.SaveChangesAsync(ct);

        return GetEligibilityRulesQueryHandler.ToDto(rule);
    }
}

// ---- Update ----
public record UpdateEligibilityRuleCommand(
    int Id, string RuleName, string RuleCategory, string ConditionField, string Operator,
    decimal ConditionValue, string ErrorMessage, string Severity, bool IsActive
) : IRequest<EligibilityRuleDto>;

public class UpdateEligibilityRuleCommandValidator : AbstractValidator<UpdateEligibilityRuleCommand>
{
    public UpdateEligibilityRuleCommandValidator()
    {
        RuleFor(x => x.RuleName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.RuleCategory).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ConditionField).NotEmpty().Must(f => RuleEvaluationContext.KnownFields.Contains(f))
            .WithMessage($"ConditionField must be one of: {string.Join(", ", RuleEvaluationContext.KnownFields)}.");
        RuleFor(x => x.Operator).NotEmpty().Must(o => RuleFieldsAndOperators.ValidOperators.Contains(o))
            .WithMessage($"Operator must be one of: {string.Join(", ", RuleFieldsAndOperators.ValidOperators)}.");
        RuleFor(x => x.ErrorMessage).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Severity).Must(s => s is "Blocking" or "Warning")
            .WithMessage("Severity must be 'Blocking' or 'Warning'.");
    }
}

public class UpdateEligibilityRuleCommandHandler : IRequestHandler<UpdateEligibilityRuleCommand, EligibilityRuleDto>
{
    private readonly IAppDbContext _db;
    public UpdateEligibilityRuleCommandHandler(IAppDbContext db) => _db = db;

    public async Task<EligibilityRuleDto> Handle(UpdateEligibilityRuleCommand request, CancellationToken ct)
    {
        var rule = await _db.EligibilityRules.FirstOrDefaultAsync(r => r.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(EligibilityRule), request.Id);

        rule.RuleName = request.RuleName;
        rule.RuleCategory = request.RuleCategory;
        rule.ConditionField = request.ConditionField;
        rule.Operator = request.Operator;
        rule.ConditionValue = request.ConditionValue;
        rule.ErrorMessage = request.ErrorMessage;
        rule.Severity = Enum.Parse<RuleSeverity>(request.Severity);
        rule.IsActive = request.IsActive;
        await _db.SaveChangesAsync(ct);

        return GetEligibilityRulesQueryHandler.ToDto(rule);
    }
}

// ---- Delete ----
// Blocked if the rule has historical evaluation results, to preserve audit-trail integrity —
// deactivating (IsActive = false via Update) is the intended way to retire a rule instead.
public record DeleteEligibilityRuleCommand(int Id) : IRequest;

public class DeleteEligibilityRuleCommandHandler : IRequestHandler<DeleteEligibilityRuleCommand>
{
    private readonly IAppDbContext _db;
    public DeleteEligibilityRuleCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteEligibilityRuleCommand request, CancellationToken ct)
    {
        var rule = await _db.EligibilityRules.FirstOrDefaultAsync(r => r.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(EligibilityRule), request.Id);

        var hasHistory = await _db.RuleEvaluationResults.AnyAsync(r => r.RuleId == request.Id, ct);
        if (hasHistory)
            throw new InUseException("This rule has already been used to evaluate applications and cannot be deleted. Deactivate it instead.");

        _db.RemoveEligibilityRule(rule);
        await _db.SaveChangesAsync(ct);
    }
}
