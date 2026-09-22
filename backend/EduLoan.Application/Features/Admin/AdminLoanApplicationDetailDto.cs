namespace EduLoan.Application.Features.AdminApplications;

public record AdminLoanApplicationDetailDto(
    Guid Id,
    string ApplicationNumber,
    string Status,

    // Employee
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string Email,
    string DepartmentName,
    string Designation,
    DateOnly DateOfJoining,
    decimal MonthlySalary,
    string? PhoneNumber,

    // Education
    string CollegeName,
    string CollegeCity,
    string CourseName,
    string CourseLevel,
    string Specialization,
    int CourseDurationMonths,

    // Loan
    decimal TotalEducationFees,
    decimal RequestedAmount,
    int RequestedTenureMonths,
    string EducationPurpose,

    DateTime CreatedAt,
    DateTime? SubmittedAt
);