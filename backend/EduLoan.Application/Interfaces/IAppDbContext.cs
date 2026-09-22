using EduLoan.Domain.Entities;

namespace EduLoan.Application.Interfaces;

public interface IAppDbContext
{
    IQueryable<Department> Departments { get; }
    IQueryable<College> Colleges { get; }
    IQueryable<Course> Courses { get; }
    IQueryable<User> Users { get; }
    IQueryable<LoanApplication> LoanApplications { get; }
    IQueryable<ApplicationDocument> ApplicationDocuments { get; }
    IQueryable<PasswordResetOtp> PasswordResetOtps { get; }
    IQueryable<EligibilityRule> EligibilityRules { get; }
    IQueryable<RuleEvaluationResult> RuleEvaluationResults { get; }

    void AddDepartment(Department department);
    void AddCollege(College college);
    void AddCourse(Course course);
    void AddLoanApplication(LoanApplication application);
    void AddApplicationDocument(ApplicationDocument document);
    void AddPasswordResetOtp(PasswordResetOtp otp);
    void AddEligibilityRule(EligibilityRule rule);
    void AddRuleEvaluationResults(IEnumerable<RuleEvaluationResult> results);

    void RemoveDepartment(Department department);
    void RemoveCollege(College college);
    void RemoveCourse(Course course);
    void RemoveApplicationDocument(ApplicationDocument document);
    void RemoveEligibilityRule(EligibilityRule rule);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
