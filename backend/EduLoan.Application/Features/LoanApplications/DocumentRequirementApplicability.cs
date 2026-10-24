using EduLoan.Domain.Entities;

namespace EduLoan.Application.Features.LoanApplications;

public enum DocumentRequirementApplicabilityResult
{
    Applicable,
    NotApplicable,
    EmploymentTypeMissing
}

/// <summary>Deterministic applicability check shared by submission and approval gates.</summary>
public static class DocumentRequirementApplicability
{
    public static DocumentRequirementApplicabilityResult Evaluate(
        DocumentRequirement requirement, decimal requestedAmount, string? educationLevel, string? employeeEmploymentType)
    {
        if (!requirement.IsActive || !requirement.IsRequired) return DocumentRequirementApplicabilityResult.NotApplicable;
        if (requirement.MinimumLoanAmount.HasValue && requestedAmount < requirement.MinimumLoanAmount.Value) return DocumentRequirementApplicabilityResult.NotApplicable;
        if (requirement.MaximumLoanAmount.HasValue && requestedAmount > requirement.MaximumLoanAmount.Value) return DocumentRequirementApplicabilityResult.NotApplicable;
        if (!string.IsNullOrWhiteSpace(requirement.EducationLevel) &&
            (string.IsNullOrWhiteSpace(educationLevel) || !string.Equals(requirement.EducationLevel.Trim(), educationLevel.Trim(), StringComparison.OrdinalIgnoreCase)))
            return DocumentRequirementApplicabilityResult.NotApplicable;
        if (!string.IsNullOrWhiteSpace(requirement.EmploymentType))
        {
            if (string.IsNullOrWhiteSpace(employeeEmploymentType)) return DocumentRequirementApplicabilityResult.EmploymentTypeMissing;
            if (!string.Equals(requirement.EmploymentType.Trim(), employeeEmploymentType.Trim(), StringComparison.OrdinalIgnoreCase))
                return DocumentRequirementApplicabilityResult.NotApplicable;
        }
        return DocumentRequirementApplicabilityResult.Applicable;
    }
}
