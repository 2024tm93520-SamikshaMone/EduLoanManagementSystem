import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes, useLocation } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import NotificationsPage from "../NotificationsPage";
import * as notificationService from "../../../services/notificationService";

vi.mock("../../../services/notificationService", () => ({
  getNotifications: vi.fn(),
  markNotificationRead: vi.fn(),
  markAllNotificationsRead: vi.fn(),
}));

const feed = {
  unreadCount: 1,
  items: [{
    id: "n-1", title: "Application submitted", message: "Your application was submitted.",
    category: "Application", link: "/employee/applications", createdAtUtc: "2026-10-01T10:00:00Z",
    readAtUtc: null, isRead: false,
  }],
};

function CurrentPath() {
  const location = useLocation();
  return <output aria-label="current-path">{location.pathname}</output>;
}

function renderPage() {
  return render(
    <MemoryRouter initialEntries={["/employee/notifications"]}>
      <Routes>
        <Route path="/employee/notifications" element={<NotificationsPage />} />
        <Route path="/employee/applications" element={<CurrentPath />} />
      </Routes>
    </MemoryRouter>,
  );
}

describe("NotificationsPage", () => {
  beforeEach(() => vi.clearAllMocks());

  it("shows loading while notifications are being fetched", () => {
    vi.mocked(notificationService.getNotifications).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.getByText(/loading notifications/i)).toBeInTheDocument();
  });

  it("shows the empty state when the feed is empty", async () => {
    vi.mocked(notificationService.getNotifications).mockResolvedValue({ items: [], unreadCount: 0 });
    renderPage();
    expect(await screen.findByText(/no notifications to show/i)).toBeInTheDocument();
  });

  it("renders notification details and marks one as read before following its link", async () => {
    vi.mocked(notificationService.getNotifications).mockResolvedValue(feed);
    vi.mocked(notificationService.markNotificationRead).mockResolvedValue();
    renderPage();
    fireEvent.click(await screen.findByRole("button", { name: "View" }));
    await waitFor(() => expect(notificationService.markNotificationRead).toHaveBeenCalledWith("n-1"));
    expect(await screen.findByLabelText("current-path")).toHaveTextContent("/employee/applications");
  });

  it("marks all notifications as read and resets the unread count", async () => {
    vi.mocked(notificationService.getNotifications).mockResolvedValue(feed);
    vi.mocked(notificationService.markAllNotificationsRead).mockResolvedValue();
    renderPage();
    fireEvent.click(await screen.findByRole("button", { name: /mark all as read/i }));
    await waitFor(() => expect(notificationService.markAllNotificationsRead).toHaveBeenCalledOnce());
    expect(screen.getByRole("button", { name: /mark all as read \(0\)/i })).toBeDisabled();
  });

  it("shows a useful error when the feed cannot be loaded", async () => {
    vi.mocked(notificationService.getNotifications).mockRejectedValue(new Error("Service unavailable"));
    renderPage();
    expect(await screen.findByRole("alert")).toHaveTextContent("Service unavailable");
  });
});
