const API_BASE_URL =
  (import.meta as ImportMeta & { env?: { VITE_API_BASE_URL?: string } }).env?.VITE_API_BASE_URL ??
  "https://localhost:7001/api/v1";

export interface EmployeeDocument {
  id: string; documentType: string; fileName: string; contentType: string; fileSizeBytes: number; uploadedAtUtc: string;
}
export interface EmployeeDocumentRequirement { id: number; documentType: string; displayName: string; category: string; isRequired: boolean; isActive: boolean; displayOrder: number; }
interface ApiResponse<T> { success: boolean; data: T | null; errors: string[]; message: string; }
function headers(): Record<string, string> { const token = sessionStorage.getItem("eduloan_access_token"); return token ? { Authorization: `Bearer ${token}` } : {}; }
async function read<T>(response: Response): Promise<T> {
  const body = await response.json() as ApiResponse<T>;
  if (!response.ok || !body.success || body.data === null) throw new Error(body.message || "Request failed.");
  return body.data;
}
export async function getEmployeeDocuments(): Promise<EmployeeDocument[]> { return read(await fetch(`${API_BASE_URL}/employee-documents`, { headers: headers() })); }
export async function getEmployeeDocumentRequirements(): Promise<EmployeeDocumentRequirement[]> {
  const rows = await read<EmployeeDocumentRequirement[]>(await fetch(`${API_BASE_URL}/module6/document-requirements`, { headers: headers() }));
  return rows.filter(x => x.isActive);
}
export async function uploadEmployeeDocument(documentType: string, file: File): Promise<EmployeeDocument> {
  const form = new FormData(); form.append("documentType", documentType); form.append("file", file);
  return read(await fetch(`${API_BASE_URL}/employee-documents`, { method: "POST", headers: headers(), body: form }));
}
export async function downloadEmployeeDocument(documentId: string, fileName: string): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/employee-documents/${documentId}/download`, { headers: headers() });
  if (!response.ok) throw new Error("Could not download document. Please check that the stored file exists.");
  const blob = await response.blob(); const url = URL.createObjectURL(blob); const link = document.createElement("a");
  link.href = url; link.download = fileName; document.body.appendChild(link); link.click(); link.remove(); URL.revokeObjectURL(url);
}
