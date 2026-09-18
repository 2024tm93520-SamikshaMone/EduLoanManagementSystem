import type { ApiResponse } from "../types/auth";
import type { MasterDataEntity } from "../types/masterData";

const API_BASE_URL =
  (import.meta as ImportMeta & { env?: { VITE_API_BASE_URL?: string } }).env?.VITE_API_BASE_URL ??
  "https://localhost:7001/api/v1";

export class MasterDataError extends Error {}

function getAuthHeader(): Record<string, string> {
  const token = sessionStorage.getItem("eduloan_access_token");
  return token ? { Authorization: `Bearer ${token}` } : {};
}

async function handle<T>(response: Response): Promise<T> {
  const body: ApiResponse<T> = await response.json();
  if (!response.ok || !body.success || body.data === null) {
    throw new MasterDataError(body.message || "Something went wrong. Please try again.");
  }
  return body.data;
}

export async function getMasterDataList<T>(entity: MasterDataEntity): Promise<T[]> {
  const response = await fetch(`${API_BASE_URL}/admin/${entity}`, {
    headers: { "Content-Type": "application/json", ...getAuthHeader() },
  });
  return handle<T[]>(response);
}

export async function createMasterDataItem<T>(entity: MasterDataEntity, payload: object): Promise<T> {
  const response = await fetch(`${API_BASE_URL}/admin/${entity}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...getAuthHeader() },
    body: JSON.stringify(payload),
  });
  return handle<T>(response);
}

export async function updateMasterDataItem<T>(entity: MasterDataEntity, id: number, payload: object): Promise<T> {
  const response = await fetch(`${API_BASE_URL}/admin/${entity}/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...getAuthHeader() },
    body: JSON.stringify(payload),
  });
  return handle<T>(response);
}

export async function deleteMasterDataItem(entity: MasterDataEntity, id: number): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/admin/${entity}/${id}`, {
    method: "DELETE",
    headers: { "Content-Type": "application/json", ...getAuthHeader() },
  });
  await handle<object>(response);
}
