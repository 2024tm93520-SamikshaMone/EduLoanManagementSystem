import type { ApiResponse } from "../types/auth";
import type { EmployeeProfile } from "../types/employee";

const API_BASE_URL =
  (import.meta as ImportMeta & { env?: { VITE_API_BASE_URL?: string } }).env
    ?.VITE_API_BASE_URL ?? "https://localhost:7001/api/v1";

export class ProfileError extends Error {}

function getAuthHeader(): Record<string, string> {
  const token = sessionStorage.getItem("eduloan_access_token");
  return token ? { Authorization: `Bearer ${token}` } : {};
}

export async function getMyProfile(): Promise<EmployeeProfile> {
  const response = await fetch(`${API_BASE_URL}/employees/me`, {
    method: "GET",
    headers: { "Content-Type": "application/json", ...getAuthHeader() },
  });

  const body: ApiResponse<EmployeeProfile> = await response.json();

  if (!response.ok || !body.success || !body.data) {
    throw new ProfileError(body.message || "Could not load your profile.");
  }

  return body.data;
}

export async function updateMyProfile(phoneNumber: string): Promise<EmployeeProfile> {
  const response = await fetch(`${API_BASE_URL}/employees/me`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...getAuthHeader() },
    body: JSON.stringify({ phoneNumber }),
  });

  const body: ApiResponse<EmployeeProfile> = await response.json();

  if (!response.ok || !body.success || !body.data) {
    throw new ProfileError(body.message || "Could not update your profile.");
  }

  return body.data;
}
