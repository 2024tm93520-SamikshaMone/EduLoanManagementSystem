import type {
  ApiResponse,
  LoginRequest,
  LoginResponse,
} from "../types/auth";

const API_BASE_URL =
  (import.meta as ImportMeta & {
    env?: { VITE_API_BASE_URL?: string };
  }).env?.VITE_API_BASE_URL ??
  "https://localhost:7001/api/v1";

export class AuthError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "AuthError";
  }
}

const ACCESS_TOKEN_KEY = "eduloan_access_token";
const REFRESH_TOKEN_KEY = "eduloan_refresh_token";
const USER_KEY = "eduloan_user";

// ===============================
// LOGIN
// ===============================

export async function login(
  payload: LoginRequest
): Promise<LoginResponse> {
  const response = await fetch(`${API_BASE_URL}/auth/login`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(payload),
  });

  const body: ApiResponse<LoginResponse> = await response.json();

  if (!response.ok || !body.success || !body.data) {
    throw new AuthError(body.message || "Invalid email or password.");
  }

  return body.data;
}

// ===============================
// SESSION MANAGEMENT
// ===============================

export function storeSession(result: LoginResponse): void {
  sessionStorage.setItem(ACCESS_TOKEN_KEY, result.accessToken);
  sessionStorage.setItem(REFRESH_TOKEN_KEY, result.refreshToken);
  sessionStorage.setItem(USER_KEY, JSON.stringify(result.user));
}

export function getStoredUser() {
  const raw = sessionStorage.getItem(USER_KEY);
  return raw ? JSON.parse(raw) : null;
}

export function getAccessToken(): string | null {
  return sessionStorage.getItem(ACCESS_TOKEN_KEY);
}

export function clearSession(): void {
  sessionStorage.removeItem(ACCESS_TOKEN_KEY);
  sessionStorage.removeItem(REFRESH_TOKEN_KEY);
  sessionStorage.removeItem(USER_KEY);
}

// ===============================
// PASSWORD TYPES
// ===============================

export type ChangePasswordPayload = {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
};

export type SendOtpPayload = {
  email: string;
};

export type VerifyOtpPayload = {
  email: string;
  otp: string;
};

export type ResetPasswordPayload = {
  email: string;
  newPassword: string;
  confirmPassword: string;
  resetToken: string;
};

// ===============================
// GENERIC PASSWORD REQUEST
// ===============================

async function passwordRequest(
  path: string,
  payload: unknown,
  authenticated = false
): Promise<string> {
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
  };

  if (authenticated) {
    const token = sessionStorage.getItem(ACCESS_TOKEN_KEY);

    if (token) {
      headers.Authorization = `Bearer ${token}`;
    }
  }

  const response = await fetch(`${API_BASE_URL}/auth/${path}`, {
    method: "POST",
    headers,
    body: JSON.stringify(payload),
  });

  const body: ApiResponse<unknown> = await response.json();

  if (!response.ok || !body.success) {
    throw new AuthError(body.message || "Request failed.");
  }

  return body.message || "Operation completed successfully.";
}

// ===============================
// CHANGE PASSWORD
// ===============================

export function changePassword(
  payload: ChangePasswordPayload
): Promise<string> {
  return passwordRequest("change-password", payload, true);
}

// ===============================
// FORGOT PASSWORD - SEND OTP
// ===============================

export async function sendForgotPasswordOtp(
  payload: SendOtpPayload
): Promise<string> {
  return passwordRequest("forgot-password/request-otp", payload);
}

// ===============================
// FORGOT PASSWORD - VERIFY OTP
// ===============================

export async function verifyForgotPasswordOtp(
  payload: VerifyOtpPayload
): Promise<string> {
  const headers = {
    "Content-Type": "application/json",
  };

  const response = await fetch(
    `${API_BASE_URL}/auth/forgot-password/verify-otp`,
    {
      method: "POST",
      headers,
      body: JSON.stringify(payload),
    }
  );

  const body: ApiResponse<{ resetToken: string }> =
    await response.json();

  if (!response.ok || !body.success || !body.data?.resetToken) {
    throw new AuthError(body.message || "Invalid or expired OTP.");
  }

  return body.data.resetToken;
}

// ===============================
// FORGOT PASSWORD - RESET
// ===============================

export async function resetForgottenPassword(
  payload: ResetPasswordPayload
): Promise<string> {
  return passwordRequest("forgot-password/reset", payload);
}