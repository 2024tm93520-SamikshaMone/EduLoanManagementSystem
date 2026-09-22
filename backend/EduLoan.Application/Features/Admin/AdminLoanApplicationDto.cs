namespace EduLoan.Application.Features.AdminApplications;

public record AdminLoanApplicationDto(
    Guid Id,
    string ApplicationNumber,
    string EmployeeCode,
    string EmployeeName,
    string DepartmentName,
    string CollegeName,
    string CourseName,
    decimal RequestedAmount,
    DateTime? SubmittedAt,
    string Status
);