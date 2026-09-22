export interface AdminReportSummary {
  totalApplications: number;
  approvedApplications: number;
  rejectedApplications: number;
  submittedApplications: number;
  totalEmployees: number;
  totalRequestedAmount: number;
}

export interface AdminReportStatus {
  status: string;
  count: number;
}

export interface AdminReportDepartment {
  departmentId: number;
  departmentName: string;
  applicationCount: number;
  requestedAmount: number;
}

export interface AdminReportCourse {
  courseId: number;
  courseName: string;
  applicationCount: number;
  requestedAmount: number;
}

export interface AdminReportCollege {
  collegeId: number;
  collegeName: string;
  applicationCount: number;
  requestedAmount: number;
}

export interface AdminReports {
  summary: AdminReportSummary;
  statusOverview: AdminReportStatus[];
  departmentOverview: AdminReportDepartment[];
  courseOverview: AdminReportCourse[];
  collegeOverview: AdminReportCollege[];
}

interface ApiResponse<T> {
  success: boolean;
  message?: string;
  data: T;
}

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ??
  "https://localhost:7001/api/v1";

export interface AdminReportFilters {
  year?: number;
  departmentId?: number;
  courseId?: number;
  collegeId?: number;
}

export async function getAdminReports(
  filters: AdminReportFilters = {}
): Promise<AdminReports> {
  const token = sessionStorage.getItem("eduloan_access_token");

  const params = new URLSearchParams();

  if (filters.year !== undefined) {
    params.append("year", filters.year.toString());
  }

  if (filters.departmentId !== undefined) {
    params.append("departmentId", filters.departmentId.toString());
  }

  if (filters.courseId !== undefined) {
    params.append("courseId", filters.courseId.toString());
  }

  if (filters.collegeId !== undefined) {
    params.append("collegeId", filters.collegeId.toString());
  }

  const queryString = params.toString();

  const response = await fetch(
    `${API_BASE_URL}/admin/reports${
      queryString ? `?${queryString}` : ""
    }`,
    {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${token}`,
      },
    }
  );

  if (!response.ok) {
    if (response.status === 401) {
      throw new Error("Unauthorized. Please login again.");
    }

    if (response.status === 403) {
      throw new Error("You do not have permission to view reports.");
    }

    throw new Error(
      `Failed to load reports. Status: ${response.status}`
    );
  }

  const result: ApiResponse<AdminReports> = await response.json();

  if (!result.success) {
    throw new Error(result.message || "Failed to load reports.");
  }

  return result.data;
}
