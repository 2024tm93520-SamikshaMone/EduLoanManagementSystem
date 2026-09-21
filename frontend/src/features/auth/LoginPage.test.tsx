import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import LoginPage from "./LoginPage";
import * as authService from "../../services/authService";

describe("LoginPage", () => {
  beforeEach(() => {
    sessionStorage.clear();
    vi.restoreAllMocks();
  });

  it("renders email and password fields and a submit button", () => {
    render(<LoginPage />);
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    expect(screen.getByLabelText("Password")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /sign in/i })).toBeInTheDocument();
  });

  it("shows validation errors when submitted empty", async () => {
    const user = userEvent.setup();
    render(<LoginPage />);

    await user.click(screen.getByRole("button", { name: /sign in/i }));

    expect(await screen.findByText(/email is required/i)).toBeInTheDocument();
    expect(screen.getByText(/password is required/i)).toBeInTheDocument();
  });

  it("shows an error for an invalid email format", async () => {
    const user = userEvent.setup();
    render(<LoginPage />);

    await user.type(screen.getByLabelText(/email/i), "not-an-email");
    await user.type(screen.getByLabelText("Password"), "somepassword");
    await user.click(screen.getByRole("button", { name: /sign in/i }));

    expect(await screen.findByText(/enter a valid email address/i)).toBeInTheDocument();
  });

  it("calls onLoginSuccess with the role-based route on successful login", async () => {
    vi.spyOn(authService, "login").mockResolvedValue({
      accessToken: "token123",
      refreshToken: "refresh123",
      user: { id: "1", fullName: "Samiksha Mone", email: "samiksha@acc.com", role: "Employee" },
    });
    vi.spyOn(authService, "storeSession").mockImplementation(() => {});

    const onLoginSuccess = vi.fn();
    const user = userEvent.setup();
    render(<LoginPage onLoginSuccess={onLoginSuccess} />);

    await user.type(screen.getByLabelText(/email/i), "samiksha@acc.com");
    await user.type(screen.getByLabelText("Password"), "password1");
    await user.click(screen.getByRole("button", { name: /sign in/i }));

    await waitFor(() => {
      expect(onLoginSuccess).toHaveBeenCalledWith("/employee/dashboard");
    });
  });

  it("shows a form-level error message when the API rejects the credentials", async () => {
    vi.spyOn(authService, "login").mockRejectedValue(
      new authService.AuthError("Invalid email or password.")
    );

    const user = userEvent.setup();
    render(<LoginPage />);

    await user.type(screen.getByLabelText(/email/i), "samiksha@acc.com");
    await user.type(screen.getByLabelText("Password"), "wrongpassword");
    await user.click(screen.getByRole("button", { name: /sign in/i }));

    expect(await screen.findByText(/invalid email or password/i)).toBeInTheDocument();
  });

  it("disables the submit button while the request is in flight", async () => {
    let resolveLogin: (value: any) => void = () => {};
    vi.spyOn(authService, "login").mockImplementation(
      () => new Promise((resolve) => { resolveLogin = resolve; })
    );
    vi.spyOn(authService, "storeSession").mockImplementation(() => {});

    const user = userEvent.setup();
    render(<LoginPage />);

    await user.type(screen.getByLabelText(/email/i), "samiksha@acc.com");
    await user.type(screen.getByLabelText("Password"), "password1");
    await user.click(screen.getByRole("button", { name: /sign in/i }));

    expect(screen.getByRole("button", { name: /signing in/i })).toBeDisabled();
    resolveLogin({ accessToken: "t", refreshToken: "r", user: { id: "1", fullName: "x", email: "x", role: "Employee" } });
  });
});
