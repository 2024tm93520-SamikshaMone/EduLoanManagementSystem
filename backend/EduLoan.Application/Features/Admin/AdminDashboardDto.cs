namespace EduLoan.Application.Features.AdminDashboard;

public record AdminDashboardDto(
    AdminDashboardSummaryDto Summary,
    List<AdminRecentApplicationDto> RecentApplications,
    List<AdminApplicationStatusDto> StatusOverview
);

public record AdminDashboardSummaryDto(
    int TotalEmployees,
    int PendingApplications,
    int ApprovedApplications,
    int PendingFinanceConfirmation
);

public record AdminRecentApplicationDto(
    Guid Id,
    string ApplicationNumber,
    string EmployeeName,
    string EmployeeCode,
    string CollegeName,
    string CourseName,
    decimal RequestedAmount,
    string Status,
    DateTime CreatedAt
);

public record AdminApplicationStatusDto(
    string Status,
    int Count,
    int Percentage
);