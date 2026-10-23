import { fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes, useLocation } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import FinanceNav from "../pages/FinanceNav";

vi.mock("../../services/authService", () => ({
  getStoredUser: () => ({ fullName: "Finance Test User", role: "Finance" }),
  clearSession: vi.fn(),
}));

function LocationDisplay() {
  const location = useLocation();
  return <output aria-label="current-path">{location.pathname}</output>;
}

function renderFinanceNav(path = "/finance/dashboard") {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <FinanceNav />
      <Routes>
        <Route path="*" element={<LocationDisplay />} />
      </Routes>
    </MemoryRouter>,
  );
}

describe("FinanceNav", () => {
  beforeEach(() => localStorage.clear());

  it("shows the notification icon in the header without a duplicate sidebar item", () => {
    renderFinanceNav();
    expect(screen.getByRole("link", { name: "Notifications" })).toBeInTheDocument();
    expect(screen.queryByText("Notifications")).not.toBeInTheDocument();
  });

  it("shows Help & Support in the sidebar", () => {
    renderFinanceNav();
    expect(screen.getByRole("link", { name: /Help & Support/i })).toHaveAttribute("href", "/finance/help");
  });

  it("provides account, password and logout actions in the profile menu", () => {
    renderFinanceNav();
    fireEvent.click(screen.getByRole("button", { name: /Finance Test User/i }));
    expect(screen.getByRole("menuitem", { name: /Account Information/i })).toHaveAttribute("href", "/finance/profile");
    expect(screen.getByRole("menuitem", { name: /Change Password/i })).toHaveAttribute("href", "/finance/change-password");
    expect(screen.getByRole("menuitem", { name: /Logout/i })).toBeInTheDocument();
  });

  it("clears the session and navigates to login when logout is selected", async () => {
    const auth = await import("../../services/authService");
    renderFinanceNav();
    fireEvent.click(screen.getByRole("button", { name: /Finance Test User/i }));
    fireEvent.click(screen.getByRole("menuitem", { name: /Logout/i }));
    expect(auth.clearSession).toHaveBeenCalledOnce();
    expect(screen.getByLabelText("current-path")).toHaveTextContent("/login");
  });
});
