export interface AppNotification {
  id: string; title: string; message: string; category: string; link?: string | null;
  createdAtUtc: string; readAtUtc?: string | null; isRead: boolean;
}
export interface NotificationFeed { items: AppNotification[]; unreadCount: number; }
interface ApiResponse<T> { success: boolean; message?: string; data: T; }
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "https://localhost:7001/api/v1";
function headers() { return { Authorization: `Bearer ${sessionStorage.getItem("eduloan_access_token") ?? ""}`, "Content-Type": "application/json" }; }
export async function getNotifications(unreadOnly = false): Promise<NotificationFeed> {
  const response = await fetch(`${API_BASE_URL}/notifications?unreadOnly=${unreadOnly}&take=50`, { headers: headers() });
  if (!response.ok) throw new Error(`Failed to load notifications (${response.status}).`);
  const body: ApiResponse<NotificationFeed> = await response.json();
  if (!body.success) throw new Error(body.message || "Failed to load notifications.");
  return body.data;
}
export async function markNotificationRead(id: string): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/notifications/${id}/read`, { method: "PATCH", headers: headers() });
  if (!response.ok) throw new Error(`Unable to mark notification as read (${response.status}).`);
}
export async function markAllNotificationsRead(): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/notifications/read-all`, { method: "PATCH", headers: headers() });
  if (!response.ok) throw new Error(`Unable to mark notifications as read (${response.status}).`);
}
