import type { ApiResponse } from "../types/auth";

const API_BASE_URL =
  (import.meta as ImportMeta & {
    env?: { VITE_API_BASE_URL?: string };
  }).env?.VITE_API_BASE_URL ??
  "https://localhost:7001/api/v1";

export interface AdminDashboardSummary {
  totalEmployees: number;
  pendingApplications: number;
  approvedApplications: number;
  pendingFinanceConfirmation: number;
}

export interface AdminRecentApplication {
  id: string;
  applicationNumber: string;
  employeeName: string;
  employeeCode: string;
  collegeName: string;
  courseName: string;
  requestedAmount: number;
  status: string;
  createdAt: string;
}

export interface AdminApplicationStatus {
  status: string;
  count: number;
  percentage: number;
}

export interface AdminDashboardData {
  summary: AdminDashboardSummary;
  recentApplications: AdminRecentApplication[];
  statusOverview: AdminApplicationStatus[];
}

export class AdminDashboardError extends Error {
  errors: string[];

  constructor(
    message: string,
    errors: string[] = []
  ) {
    super(message);
    this.errors = errors;
  }
}

function getAuthHeader(): Record<string, string> {
  const token = sessionStorage.getItem(
    "eduloan_access_token"
  );

  return token
    ? {
        Authorization: `Bearer ${token}`,
      }
    : {};
}

async function handle<T>(
  response: Response
): Promise<T> {
  const body: ApiResponse<T> =
    await response.json();

  if (
    !response.ok ||
    !body.success ||
    body.data === null
  ) {
    throw new AdminDashboardError(
      body.message ||
        "Unable to load dashboard.",
      body.errors
    );
  }

  return body.data;
}

export async function getAdminDashboard(): Promise<AdminDashboardData> {
  const res = await fetch(
    `${API_BASE_URL}/admin/dashboard`,
    {
      headers: getAuthHeader(),
    }
  );

  return handle<AdminDashboardData>(res);
}