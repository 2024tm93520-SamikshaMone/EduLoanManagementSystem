using EduLoan.Application.Features.LoanApplications;
using EduLoan.Domain.Entities;
using Xunit;

namespace EduLoan.Application.Tests.Features.LoanApplications;

public sealed class DocumentRequirementApplicabilityTests
{
    private static DocumentRequirement Requirement(string? employmentType = null, string? educationLevel = null) => new()
    {
        DocumentType = "EmploymentProof",
        DisplayName = "Employment proof",
        IsActive = true,
        IsRequired = true,
        EmploymentType = employmentType,
        EducationLevel = educationLevel
    };

    [Fact]
    public void EmploymentSpecificRequirement_IsApplicable_WhenTypesMatchCaseInsensitively()
    {
        var result = DocumentRequirementApplicability.Evaluate(Requirement("Permanent"), 1000m, "Masters", " permanent ");
        Assert.Equal(DocumentRequirementApplicabilityResult.Applicable, result);
    }

    [Fact]
    public void EmploymentSpecificRequirement_IsNotApplicable_WhenTypesDiffer()
    {
        var result = DocumentRequirementApplicability.Evaluate(Requirement("Permanent"), 1000m, "Masters", "Contract");
        Assert.Equal(DocumentRequirementApplicabilityResult.NotApplicable, result);
    }

    [Fact]
    public void EmploymentSpecificRequirement_FailsClosed_WhenEmployeeTypeIsUnknown()
    {
        var result = DocumentRequirementApplicability.Evaluate(Requirement("Permanent"), 1000m, "Masters", null);
        Assert.Equal(DocumentRequirementApplicabilityResult.EmploymentTypeMissing, result);
    }

    [Fact]
    public void RequirementWithoutEmploymentCondition_DoesNotRequireEmployeeType()
    {
        var result = DocumentRequirementApplicability.Evaluate(Requirement(), 1000m, "Masters", null);
        Assert.Equal(DocumentRequirementApplicabilityResult.Applicable, result);
    }

    [Fact]
    public void NonApplicableEducationLevel_IsExcludedBeforeEmploymentTypeCheck()
    {
        var result = DocumentRequirementApplicability.Evaluate(Requirement("Permanent", "Doctorate"), 1000m, "Masters", null);
        Assert.Equal(DocumentRequirementApplicabilityResult.NotApplicable, result);
    }
}
