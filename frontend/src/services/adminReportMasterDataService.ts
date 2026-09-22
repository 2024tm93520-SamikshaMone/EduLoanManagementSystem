const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ??
  "https://localhost:7001/api/v1";

/* =========================================================
   TYPES
   ========================================================= */

export interface ReportDepartment {
  id: number;
  name: string;
  isActive: boolean;
}

export interface ReportCourse {
  id: number;
  name: string;
  level: string;
  standardDurationMonths: number;
  isActive: boolean;
}

export interface ReportCollege {
  id: number;
  name: string;
  city: string;
  isActive: boolean;
}

interface ApiResponse<T> {
  success: boolean;
  message?: string;
  data: T;
}

/* =========================================================
   COMMON GET
   ========================================================= */

async function get<T>(endpoint: string): Promise<T> {
  const token = sessionStorage.getItem("eduloan_access_token");

  const response = await fetch(
    `${API_BASE_URL}${endpoint}`,
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
      throw new Error(
        "Unauthorized. Please login again."
      );
    }

    if (response.status === 403) {
      throw new Error(
        "You do not have permission to access master data."
      );
    }

    throw new Error(
      `Failed to load ${endpoint}. Status: ${response.status}`
    );
  }

  const result: ApiResponse<T> = await response.json();

  if (!result.success) {
    throw new Error(
      result.message ||
        `Failed to load ${endpoint}.`
    );
  }

  return result.data;
}

/* =========================================================
   DEPARTMENTS
   ========================================================= */

export async function getReportDepartments(): Promise<
  ReportDepartment[]
> {
  return get<ReportDepartment[]>(
    "/admin/departments"
  );
}

/* =========================================================
   COURSES
   ========================================================= */

export async function getReportCourses(): Promise<
  ReportCourse[]
> {
  return get<ReportCourse[]>(
    "/admin/courses"
  );
}

/* =========================================================
   COLLEGES
   ========================================================= */

export async function getReportColleges(): Promise<
  ReportCollege[]
> {
  return get<ReportCollege[]>(
    "/admin/colleges"
  );
}