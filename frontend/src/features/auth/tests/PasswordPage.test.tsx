import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes, useLocation } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import PasswordPage from "../pages/PasswordPage";
import * as authService from "../../../services/authService";

vi.mock("../../../services/authService", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../../../services/authService")>();
  return { ...actual, changePassword: vi.fn(), sendForgotPasswordOtp: vi.fn(), verifyForgotPasswordOtp: vi.fn(), resetForgottenPassword: vi.fn() };
});

function Path() {
  const location = useLocation();
  return <div aria-label="path">{location.pathname}</div>;
}

function renderPassword(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route path="*" element={<><PasswordPage /><Path /></>} />
      </Routes>
    </MemoryRouter>
  );
}

describe("PasswordPage", () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it("renders the change password form outside the forgot-password route", () => {
    renderPassword("/employee/change-password");
    expect(screen.getByRole("heading", { name: /change password/i })).toBeInTheDocument();
    expect(screen.getByLabelText(/current password/i)).toBeInTheDocument();
  });

  it("rejects mismatched new passwords without calling the API", async () => {
    const user = userEvent.setup();
    renderPassword("/employee/change-password");
    await user.type(screen.getByLabelText(/current password/i), "old-pass");
    await user.type(screen.getByLabelText(/^new password/i), "new-pass-1");
    await user.type(screen.getByLabelText(/confirm new password/i), "different");
    await user.click(screen.getByRole("button", { name: /update password/i }));
    expect(await screen.findByRole("alert")).toHaveTextContent(/must match/i);
    expect(authService.changePassword).not.toHaveBeenCalled();
  });

  it("submits a valid password change and shows success", async () => {
    vi.mocked(authService.changePassword).mockResolvedValue("Password changed successfully.");
    const user = userEvent.setup();
    renderPassword("/employee/change-password");
    await user.type(screen.getByLabelText(/current password/i), "old-pass");
    await user.type(screen.getByLabelText(/^new password/i), "new-pass-1");
    await user.type(screen.getByLabelText(/confirm new password/i), "new-pass-1");
    await user.click(screen.getByRole("button", { name: /update password/i }));
    expect(await screen.findByRole("status")).toHaveTextContent(/changed successfully/i);
    expect(authService.changePassword).toHaveBeenCalledWith({ currentPassword: "old-pass", newPassword: "new-pass-1", confirmPassword: "new-pass-1" });
  });

  it("requires a registered email before requesting a reset OTP", async () => {
    renderPassword("/forgot-password");
    fireEvent.click(screen.getByRole("button", { name: /send otp|continue|reset password/i }));
    expect(await screen.findByRole("alert")).toHaveTextContent(/email is required/i);
    expect(authService.sendForgotPasswordOtp).not.toHaveBeenCalled();
  });
});
