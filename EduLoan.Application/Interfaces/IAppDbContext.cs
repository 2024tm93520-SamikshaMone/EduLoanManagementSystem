using EduLoan.Domain.Entities;

namespace EduLoan.Application.Interfaces;

/// Deliberately narrow: exposes only what master-data CRUD handlers need
/// (read access to Users is for referential-integrity checks, e.g. "can't
/// delete a department employees are still assigned to").
public interface IAppDbContext
{
    IQueryable<Department> Departments { get; }
    IQueryable<College> Colleges { get; }
    IQueryable<Course> Courses { get; }
    IQueryable<User> Users { get; }
    IQueryable<LoanApplication> LoanApplications { get; }
    IQueryable<ApplicationDocument> ApplicationDocuments { get; }

    void AddDepartment(Department department);
    void AddCollege(College college);
    void AddCourse(Course course);
    void AddLoanApplication(LoanApplication application);
    void AddApplicationDocument(ApplicationDocument document);

    void RemoveDepartment(Department department);
    void RemoveCollege(College college);
    void RemoveCourse(Course course);
    void RemoveApplicationDocument(ApplicationDocument document);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
