namespace EduLoan.Application.Features.AdminReports;

public record AdminReportsDto(
    AdminReportSummaryDto Summary,
    List<AdminReportStatusDto> StatusOverview,
    List<AdminReportDepartmentDto> DepartmentOverview,
    List<AdminReportCourseDto> CourseOverview,
    List<AdminReportCollegeDto> CollegeOverview
);

public record AdminReportSummaryDto(
    int TotalApplications,
    int ApprovedApplications,
    int RejectedApplications,
    int SubmittedApplications,
    int TotalEmployees,
    decimal TotalRequestedAmount
);

public record AdminReportStatusDto(
    string Status,
    int Count
);

public record AdminReportDepartmentDto(
    int DepartmentId,
    string DepartmentName,
    int ApplicationCount,
    decimal RequestedAmount
);

public record AdminReportCourseDto(
    int CourseId,
    string CourseName,
    int ApplicationCount,
    decimal RequestedAmount
);

public record AdminReportCollegeDto(
    int CollegeId,
    string CollegeName,
    int ApplicationCount,
    decimal RequestedAmount
);