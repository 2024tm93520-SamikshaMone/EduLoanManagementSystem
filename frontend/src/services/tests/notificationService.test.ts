import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { getNotifications, getUnreadNotificationCount, markAllNotificationsRead, markNotificationRead } from "../notificationService";

describe("notificationService", () => {
  beforeEach(() => {
    sessionStorage.clear();
    sessionStorage.setItem("eduloan_access_token", "test-token");
    vi.stubGlobal("fetch", vi.fn());
  });
  afterEach(() => vi.unstubAllGlobals());

  it("requests the feed with auth and unreadOnly parameters", async () => {
    vi.mocked(fetch).mockResolvedValue({ ok: true, json: async () => ({ success: true, data: { items: [], unreadCount: 0 } }) } as Response);
    await expect(getNotifications(true)).resolves.toEqual({ items: [], unreadCount: 0 });
    expect(fetch).toHaveBeenCalledWith(expect.stringContaining("/notifications?unreadOnly=true&page=1&pageSize=50"), expect.objectContaining({ headers: expect.objectContaining({ Authorization: "Bearer test-token" }) }));
  });

  it("loads the server-authoritative unread count", async () => {
    vi.mocked(fetch).mockResolvedValue({ ok: true, json: async () => ({ success: true, data: { unreadCount: 4 } }) } as Response);
    await expect(getUnreadNotificationCount()).resolves.toBe(4);
    expect(fetch).toHaveBeenCalledWith(expect.stringContaining("/notifications/unread-count"), expect.any(Object));
  });

  it("surfaces HTTP failures while loading notifications", async () => {
    vi.mocked(fetch).mockResolvedValue({ ok: false, status: 503, json: async () => ({}) } as Response);
    await expect(getNotifications()).rejects.toThrow(/503/);
  });

  it("uses PATCH to mark one notification as read", async () => {
    vi.mocked(fetch).mockResolvedValue({ ok: true, json: async () => ({ success: true, data: {} }) } as Response);
    await markNotificationRead("notification-123");
    expect(fetch).toHaveBeenCalledWith(expect.stringContaining("/notifications/notification-123/read"), expect.objectContaining({ method: "PATCH" }));
  });

  it("uses PATCH to mark the complete feed as read", async () => {
    vi.mocked(fetch).mockResolvedValue({ ok: true, json: async () => ({ success: true, data: {} }) } as Response);
    await markAllNotificationsRead();
    expect(fetch).toHaveBeenCalledWith(expect.stringContaining("/notifications/read-all"), expect.objectContaining({ method: "PATCH" }));
  });
});
