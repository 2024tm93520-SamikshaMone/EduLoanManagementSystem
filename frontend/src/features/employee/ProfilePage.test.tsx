import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import ProfilePage from "./ProfilePage";
import * as profileService from "../../services/profileService";
import type { EmployeeProfile } from "../../types/employee";

const SAMPLE_PROFILE: EmployeeProfile = {
  id: "1",
  employeeCode: "EMP1001",
  fullName: "Samiksha Mone",
  email: "samiksha@acc.com",
  role: "Employee",
  departmentName: "Engineering",
  designation: "Software Engineer",
  dateOfJoining: "2022-01-10",
  tenureMonths: 56,
  monthlySalary: 65000,
  phoneNumber: "9876543210",
};

describe("ProfilePage", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it("shows a loading state, then renders the profile once loaded", async () => {
    vi.spyOn(profileService, "getMyProfile").mockResolvedValue(SAMPLE_PROFILE);
    render(<ProfilePage />);

    expect(screen.getByRole("status")).toHaveTextContent(/loading/i);

    expect(await screen.findByRole("heading", { name: "Samiksha Mone" })).toBeInTheDocument();
    expect(screen.getAllByText("EMP1001").length).toBeGreaterThan(0); // shown in badge and details
    expect(screen.getByText("Engineering")).toBeInTheDocument();
    expect(screen.getByText("4 yr 8 mo")).toBeInTheDocument();
  });

  it("shows an error message if the profile fails to load", async () => {
    vi.spyOn(profileService, "getMyProfile").mockRejectedValue(
      new profileService.ProfileError("Could not load your profile.")
    );
    render(<ProfilePage />);

    expect(await screen.findByText(/could not load your profile/i)).toBeInTheDocument();
  });

  it("lets the user edit and save a valid phone number", async () => {
    vi.spyOn(profileService, "getMyProfile").mockResolvedValue(SAMPLE_PROFILE);
    vi.spyOn(profileService, "updateMyProfile").mockResolvedValue({
      ...SAMPLE_PROFILE,
      phoneNumber: "9998887770",
    });

    const user = userEvent.setup();
    render(<ProfilePage />);

    await screen.findByRole("heading", { name: "Samiksha Mone" });
    await user.click(screen.getByRole("button", { name: /edit/i }));

    const phoneField = screen.getByRole("textbox", { name: /phone number/i });
    await user.clear(phoneField);
    await user.type(phoneField, "9998887770");
    await user.click(screen.getByRole("button", { name: /save/i }));

    await waitFor(() => {
      expect(screen.getByText(/profile updated successfully/i)).toBeInTheDocument();
    });
    expect(screen.getByText(/9998887770/)).toBeInTheDocument();
  });

  it("shows a validation error for an invalid phone number and does not call the API", async () => {
    vi.spyOn(profileService, "getMyProfile").mockResolvedValue(SAMPLE_PROFILE);
    const updateSpy = vi.spyOn(profileService, "updateMyProfile");

    const user = userEvent.setup();
    render(<ProfilePage />);

    await screen.findByRole("heading", { name: "Samiksha Mone" });
    await user.click(screen.getByRole("button", { name: /edit/i }));

    const phoneField = screen.getByRole("textbox", { name: /phone number/i });
    await user.clear(phoneField);
    await user.type(phoneField, "123");
    await user.click(screen.getByRole("button", { name: /save/i }));

    expect(await screen.findByText(/exactly 10 digits/i)).toBeInTheDocument();
    expect(updateSpy).not.toHaveBeenCalled();
  });

  it("shows a form-level error if saving fails on the server", async () => {
    vi.spyOn(profileService, "getMyProfile").mockResolvedValue(SAMPLE_PROFILE);
    vi.spyOn(profileService, "updateMyProfile").mockRejectedValue(
      new profileService.ProfileError("Could not update your profile.")
    );

    const user = userEvent.setup();
    render(<ProfilePage />);

    await screen.findByRole("heading", { name: "Samiksha Mone" });
    await user.click(screen.getByRole("button", { name: /edit/i }));

    const phoneField = screen.getByRole("textbox", { name: /phone number/i });
    await user.clear(phoneField);
    await user.type(phoneField, "9998887770");
    await user.click(screen.getByRole("button", { name: /save/i }));

    expect(await screen.findByText(/could not update your profile/i)).toBeInTheDocument();
  });

  it("cancel button discards the edit and restores the original value", async () => {
    vi.spyOn(profileService, "getMyProfile").mockResolvedValue(SAMPLE_PROFILE);
    const user = userEvent.setup();
    render(<ProfilePage />);

    await screen.findByRole("heading", { name: "Samiksha Mone" });
    await user.click(screen.getByRole("button", { name: /edit/i }));

    const phoneField = screen.getByRole("textbox", { name: /phone number/i });
    await user.clear(phoneField);
    await user.type(phoneField, "0000000000");
    await user.click(screen.getByRole("button", { name: /cancel/i }));

    expect(screen.getByText(/9876543210/)).toBeInTheDocument();
  });
});
