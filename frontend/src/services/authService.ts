import type { ApiResponse, LoginRequest, LoginResponse } from "../types/auth";

const API_BASE_URL =
  (import.meta as ImportMeta & { env?: { VITE_API_BASE_URL?: string } }).env?.VITE_API_BASE_URL ??
  "https://localhost:7001/api/v1";

export class AuthError extends Error {}

export async function login(payload: LoginRequest): Promise<LoginResponse> {
  const response = await fetch(`${API_BASE_URL}/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  const body: ApiResponse<LoginResponse> = await response.json();

  if (!response.ok || !body.success || !body.data) {
    throw new AuthError(body.message || "Invalid email or password.");
  }

  return body.data;
}

const ACCESS_TOKEN_KEY = "eduloan_access_token";
const REFRESH_TOKEN_KEY = "eduloan_refresh_token";
const USER_KEY = "eduloan_user";

export function storeSession(result: LoginResponse): void {
  sessionStorage.setItem(ACCESS_TOKEN_KEY, result.accessToken);
  sessionStorage.setItem(REFRESH_TOKEN_KEY, result.refreshToken);
  sessionStorage.setItem(USER_KEY, JSON.stringify(result.user));
}

export function getStoredUser() {
  const raw = sessionStorage.getItem(USER_KEY);
  return raw ? JSON.parse(raw) : null;
}

export function clearSession(): void {
  sessionStorage.removeItem(ACCESS_TOKEN_KEY);
  sessionStorage.removeItem(REFRESH_TOKEN_KEY);
  sessionStorage.removeItem(USER_KEY);
}
