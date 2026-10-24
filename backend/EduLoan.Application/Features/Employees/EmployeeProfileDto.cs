namespace EduLoan.Application.Features.Employees;

public record EmployeeProfileDto(
    Guid Id,
    string EmployeeCode,
    string FullName,
    string Email,
    string Role,
    string? DepartmentName,
    string? Designation,
    string? EmploymentType,
    DateOnly DateOfJoining,
    int TenureMonths,
    decimal MonthlySalary,
    string? PhoneNumber
);
