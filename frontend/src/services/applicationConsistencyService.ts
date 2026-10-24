export interface ApplicationConsistencyFlag {
  code: string;
  title: string;
  severity: "High" | "Medium" | "Low" | string;
  description: string;
}

export interface DuplicateApplicationReference {
  applicationId: string;
  applicationNumber: string;
  status: string;
}

export interface ApplicationConsistencySummary {
  applicationId: string;
  flags: ApplicationConsistencyFlag[];
  potentialDuplicates: DuplicateApplicationReference[];
  disclaimer: string;
}

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "https://localhost:7001/api/v1";

export async function getApplicationConsistencyChecks(applicationId: string): Promise<ApplicationConsistencySummary> {
  const token = sessionStorage.getItem("eduloan_access_token");
  const response = await fetch(`${API_BASE_URL}/admin/applications/${applicationId}/consistency-checks`, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });
  const body = await response.json();
  if (!response.ok || !body.success || !body.data) {
    throw new Error(body.message || "Could not load application consistency checks.");
  }
  return body.data as ApplicationConsistencySummary;
}

export type ConsistencyFlagStatus = "Open" | "UnderInvestigation" | "Resolved" | "Dismissed";
export interface PersistedConsistencyFlag {
  id: string;
  applicationId: string;
  applicationNumber: string;
  ruleCode: string;
  title: string;
  category: string;
  severity: string;
  description: string;
  detectedAtUtc: string;
  status: ConsistencyFlagStatus;
  reviewedByUserId?: string | null;
  reviewedAtUtc?: string | null;
  resolutionNotes?: string | null;
}

async function apiRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const token = sessionStorage.getItem("eduloan_access_token");
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers: { "Content-Type": "application/json", ...(token ? { Authorization: `Bearer ${token}` } : {}), ...(init?.headers ?? {}) },
  });
  const body = await response.json();
  if (!response.ok || !body.success) throw new Error(body.message || "Consistency flag request failed.");
  return body.data as T;
}

export interface PagedConsistencyFlags {
  items: PersistedConsistencyFlag[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export function getPersistedConsistencyFlags(
  applicationId: string,
  filters: { status?: ConsistencyFlagStatus | "All"; severity?: string | "All"; page?: number; pageSize?: number } = {},
): Promise<PagedConsistencyFlags> {
  const params = new URLSearchParams({ applicationId, page: String(filters.page ?? 1), pageSize: String(filters.pageSize ?? 10) });
  if (filters.status && filters.status !== "All") params.set("status", filters.status);
  if (filters.severity && filters.severity !== "All") params.set("severity", filters.severity);
  return apiRequest<PagedConsistencyFlags>(`/admin/consistency-flags?${params.toString()}`);
}

export function updateConsistencyFlagStatus(id: string, status: ConsistencyFlagStatus, notes?: string): Promise<unknown> {
  return apiRequest(`/admin/consistency-flags/${encodeURIComponent(id)}/status`, {
    method: "PATCH", body: JSON.stringify({ status, notes: notes ?? null }),
  });
}
