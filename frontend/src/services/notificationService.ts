export interface AppNotification {
  id: string; title: string; message: string; category: string; link?: string | null;
  createdAtUtc: string; readAtUtc?: string | null; isRead: boolean;
}
export interface NotificationFeed { items: AppNotification[]; unreadCount: number; page?: number; pageSize?: number; totalCount?: number; totalPages?: number; }
interface ApiResponse<T> { success: boolean; message?: string; data: T; }
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "https://localhost:7001/api/v1";
function headers() { return { Authorization: `Bearer ${sessionStorage.getItem("eduloan_access_token") ?? ""}`, "Content-Type": "application/json" }; }
async function readResponse<T>(response: Response, fallback: string): Promise<T> {
  if (!response.ok) throw new Error(`${fallback} (${response.status}).`);
  const body: ApiResponse<T> = await response.json();
  if (!body.success) throw new Error(body.message || fallback);
  return body.data;
}
export async function getNotifications(unreadOnly = false, page = 1, pageSize = 50): Promise<NotificationFeed> {
  const response = await fetch(`${API_BASE_URL}/notifications?unreadOnly=${unreadOnly}&page=${page}&pageSize=${pageSize}`, { headers: headers() });
  return readResponse<NotificationFeed>(response, "Failed to load notifications");
}
export async function getUnreadNotificationCount(): Promise<number> {
  const response = await fetch(`${API_BASE_URL}/notifications/unread-count`, { headers: headers() });
  const data = await readResponse<{ unreadCount: number }>(response, "Failed to load unread notification count");
  return data.unreadCount;
}
export async function getNotification(id: string): Promise<AppNotification> {
  const response = await fetch(`${API_BASE_URL}/notifications/${encodeURIComponent(id)}`, { headers: headers() });
  return readResponse<AppNotification>(response, "Failed to load notification");
}
export async function markNotificationRead(id: string): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/notifications/${encodeURIComponent(id)}/read`, { method: "PATCH", headers: headers() });
  await readResponse<unknown>(response, "Unable to mark notification as read");
}
export async function markAllNotificationsRead(): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/notifications/read-all`, { method: "PATCH", headers: headers() });
  await readResponse<unknown>(response, "Unable to mark notifications as read");
}
