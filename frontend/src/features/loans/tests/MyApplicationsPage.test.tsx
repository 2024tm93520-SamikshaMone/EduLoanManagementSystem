import { render, screen, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as loanService from "../../../services/loanApplicationService";
import type { LoanApplicationSummary } from "../../../types/loanApplication";
import MyApplicationsPage from "../pages/MyApplicationsPage"


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

  it("shows college, course, formatted amount and status for each application", async () => {
    vi.spyOn(loanService, "getMyApplications").mockResolvedValue(SAMPLE_APPLICATIONS);
    render(<MyApplicationsPage />);

    const row = (await screen.findByText("EDL-2026-000001")).closest("tr") as HTMLElement;
    expect(within(row).getByText("BITS Pilani")).toBeInTheDocument();
    expect(within(row).getByText("M.Tech SE")).toBeInTheDocument();
    expect(within(row).getByText("\u20B94,00,000")).toBeInTheDocument();
    expect(within(row).getByText("Submitted")).toBeInTheDocument();

    for (const header of ["Application No.", "College", "Course", "Amount", "Status"]) {
      expect(screen.getByRole("columnheader", { name: header })).toBeInTheDocument();
    }
  });
});
