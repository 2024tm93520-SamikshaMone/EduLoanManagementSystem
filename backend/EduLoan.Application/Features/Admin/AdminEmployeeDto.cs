namespace EduLoan.Application.Features.AdminApplications;

public record AdminEmployeeDto(
    Guid Id,
    string EmployeeCode,
    string FullName,
    string Email,
    string? DepartmentName,
    string? Designation,
    DateOnly DateOfJoining,
    decimal MonthlySalary,
    string? PhoneNumber,
    bool IsActive
);