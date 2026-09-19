import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import MyApplicationsPage from "./pages/MyApplicationsPage";
import * as loanService from "../../services/loanApplicationService";
import type { LoanApplicationSummary } from "../../types/loanApplication";

const SAMPLE_APPLICATIONS: LoanApplicationSummary[] = [
  {
    id: "1", applicationNumber: "EDL-2026-000001", collegeName: "BITS Pilani", courseName: "M.Tech SE",
    requestedAmount: 400000, status: "Submitted", createdAt: "2026-09-01", submittedAt: "2026-09-02",
  },
];

describe("MyApplicationsPage", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it("shows loading, then renders the applications table", async () => {
    vi.spyOn(loanService, "getMyApplications").mockResolvedValue(SAMPLE_APPLICATIONS);
    render(<MyApplicationsPage />);

    expect(screen.getByRole("status")).toHaveTextContent(/loading/i);

    expect(await screen.findByText("EDL-2026-000001")).toBeInTheDocument();
    expect(screen.getByText("Submitted")).toBeInTheDocument();
  });

  it("shows an empty state when there are no applications", async () => {
    vi.spyOn(loanService, "getMyApplications").mockResolvedValue([]);
    render(<MyApplicationsPage />);

    expect(await screen.findByText(/haven't submitted any applications/i)).toBeInTheDocument();
  });

  it("shows an error message if loading fails", async () => {
    vi.spyOn(loanService, "getMyApplications").mockRejectedValue(
      new loanService.LoanApplicationError("Could not load your applications.")
    );
    render(<MyApplicationsPage />);

    expect(await screen.findByText(/could not load your applications/i)).toBeInTheDocument();
  });

  it("opens the new application wizard and can cancel back to the list", async () => {
    vi.spyOn(loanService, "getMyApplications").mockResolvedValue(SAMPLE_APPLICATIONS);
    vi.spyOn(loanService, "getColleges").mockResolvedValue([{ id: 1, name: "BITS Pilani" }]);
    vi.spyOn(loanService, "getCourses").mockResolvedValue([{ id: 1, name: "M.Tech SE" }]);

    const user = userEvent.setup();
    render(<MyApplicationsPage />);
    await screen.findByText("EDL-2026-000001");

    await user.click(screen.getByRole("button", { name: /new application/i }));
    expect(await screen.findByText(/1\. education/i)).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /cancel and go back to list/i }));
    expect(await screen.findByText("EDL-2026-000001")).toBeInTheDocument();
  });
});
