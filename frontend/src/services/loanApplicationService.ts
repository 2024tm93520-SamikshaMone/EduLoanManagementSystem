import type { ApiResponse } from "../types/auth";
import type {
  ApplicationDocument,
  LoanApplicationDetail,
  LoanApplicationFormValues,
  LoanApplicationSummary,
  LookupItem,
} from "../types/loanApplication";

const API_BASE_URL =
  (import.meta as ImportMeta & { env?: { VITE_API_BASE_URL?: string } }).env?.VITE_API_BASE_URL ??
  "https://localhost:7001/api/v1";

export class LoanApplicationError extends Error {}

function getAuthHeader(): Record<string, string> {
  const token = sessionStorage.getItem("eduloan_access_token");
  return token ? { Authorization: `Bearer ${token}` } : {};
}

async function handle<T>(response: Response): Promise<T> {
  const body: ApiResponse<T> = await response.json();
  if (!response.ok || !body.success || body.data === null) {
    throw new LoanApplicationError(body.message || "Something went wrong. Please try again.");
  }
  return body.data;
}

export async function getColleges(): Promise<LookupItem[]> {
  const res = await fetch(`${API_BASE_URL}/lookups/colleges`, { headers: getAuthHeader() });
  return handle<LookupItem[]>(res);
}

export async function getCourses(): Promise<LookupItem[]> {
  const res = await fetch(`${API_BASE_URL}/lookups/courses`, { headers: getAuthHeader() });
  return handle<LookupItem[]>(res);
}

export async function getMyApplications(): Promise<LoanApplicationSummary[]> {
  const res = await fetch(`${API_BASE_URL}/loan-applications/mine`, { headers: getAuthHeader() });
  return handle<LoanApplicationSummary[]>(res);
}

export async function getApplicationById(id: string): Promise<LoanApplicationDetail> {
  const res = await fetch(`${API_BASE_URL}/loan-applications/${id}`, { headers: getAuthHeader() });
  return handle<LoanApplicationDetail>(res);
}

export async function createApplication(values: LoanApplicationFormValues): Promise<LoanApplicationDetail> {
  const res = await fetch(`${API_BASE_URL}/loan-applications`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...getAuthHeader() },
    body: JSON.stringify(values),
  });
  return handle<LoanApplicationDetail>(res);
}

export async function updateApplication(
  id: string,
  values: LoanApplicationFormValues
): Promise<LoanApplicationDetail> {
  const res = await fetch(`${API_BASE_URL}/loan-applications/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...getAuthHeader() },
    body: JSON.stringify(values),
  });
  return handle<LoanApplicationDetail>(res);
}

export async function submitApplication(id: string): Promise<LoanApplicationDetail> {
  const res = await fetch(`${API_BASE_URL}/loan-applications/${id}/submit`, {
    method: "POST",
    headers: getAuthHeader(),
  });
  return handle<LoanApplicationDetail>(res);
}

export async function cancelApplication(id: string): Promise<LoanApplicationDetail> {
  const res = await fetch(`${API_BASE_URL}/loan-applications/${id}/cancel`, {
    method: "POST",
    headers: getAuthHeader(),
  });
  return handle<LoanApplicationDetail>(res);
}

export async function addDocument(
  applicationId: string,
  documentType: string,
  file: File
): Promise<ApplicationDocument> {
  const formData = new FormData();
  formData.append("documentType", documentType);
  formData.append("file", file);

  // No Content-Type header here on purpose — the browser sets the correct
  // multipart/form-data boundary automatically; setting it manually breaks the upload.
  const res = await fetch(`${API_BASE_URL}/loan-applications/${applicationId}/documents`, {
    method: "POST",
    headers: getAuthHeader(),
    body: formData,
  });
  return handle<ApplicationDocument>(res);
}

export async function removeDocument(applicationId: string, documentId: string): Promise<void> {
  const res = await fetch(`${API_BASE_URL}/loan-applications/${applicationId}/documents/${documentId}`, {
    method: "DELETE",
    headers: getAuthHeader(),
  });
  await handle<object>(res);
}

export async function downloadDocument(
  applicationId: string,
  documentId: string,
  fileName: string
): Promise<void> {
  const res = await fetch(
    `${API_BASE_URL}/loan-applications/${applicationId}/documents/${documentId}/download`,
    { headers: getAuthHeader() }
  );
  if (!res.ok) {
    throw new LoanApplicationError("Could not download this document.");
  }
  const blob = await res.blob();
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(url);
}
