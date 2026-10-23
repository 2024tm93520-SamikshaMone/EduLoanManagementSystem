import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen } from "@testing-library/react";
import SimilarApplicationsPanel from "../pages/SimilarApplicationsPanel";
import { getSimilarApplications, type SimilarApplicationsSummary } from "../../../services/similarApplicationsService";

vi.mock("../../../services/similarApplicationsService", () => ({
  getSimilarApplications: vi.fn(),
}));

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

describe("SimilarApplicationsPanel", () => {
  it("shows a loading state while similarity summary is being fetched", () => {
    vi.mocked(getSimilarApplications).mockReturnValue(new Promise<SimilarApplicationsSummary>(() => undefined));

    render(<SimilarApplicationsPanel applicationId="app-1" />);

    expect(screen.getByRole("status")).toHaveTextContent("Comparing with historical applications");
  });

  it("shows historical neighbors and decision-support disclaimer", async () => {
    vi.mocked(getSimilarApplications).mockResolvedValue({
      hasSufficientHistory: true,
      historicalCaseCount: 10,
      k: 5,
      approvedWeightedPercent: 80,
      rejectedWeightedPercent: 20,
      summary: "4 of 5 nearest historical cases were approved. This is context only, not an approval recommendation.",
      eligibilityEvaluations: [{ ruleName: "Minimum Tenure", ruleCategory: "Tenure", severity: "Blocking", passed: true, evaluatedValue: "24", failureMessage: null, evaluatedAt: "2026-10-01T00:00:00Z" }],
      neighbors: [{
        applicationId: "case-1",
        applicationNumber: "EDL-2026-001",
        outcome: "Approved",
        collegeName: "Example College",
        courseName: "M.Tech",
        totalEducationFees: 400000,
        requestedAmount: 300000,
        courseDurationMonths: 24,
        similarityPercent: 100,
      }],
    });

    render(<SimilarApplicationsPanel applicationId="app-1" />);

    expect(await screen.findByText(/4 of 5 nearest historical cases/)).toBeInTheDocument();
    expect(screen.getByText(/EDL-2026-001/)).toBeInTheDocument();
    expect(screen.getByText(/does not override blocking eligibility rules/)).toBeInTheDocument();
  });

  it("explains that manual review is needed when historical data is insufficient", async () => {
    vi.mocked(getSimilarApplications).mockResolvedValue({
      hasSufficientHistory: false,
      historicalCaseCount: 1,
      k: 5,
      approvedWeightedPercent: null,
      rejectedWeightedPercent: null,
      summary: "Only 1 historical approved/rejected application(s) are available. At least 3 are required. Review this application manually.",
      eligibilityEvaluations: [],
      neighbors: [],
    });

    render(<SimilarApplicationsPanel applicationId="app-2" />);

    expect(await screen.findByText(/Insufficient historical data/)).toBeInTheDocument();
    expect(screen.getByText(/Review this application manually/)).toBeInTheDocument();
  });

  it("shows a recoverable error when the API fails", async () => {
    vi.mocked(getSimilarApplications).mockRejectedValue(new Error("Service unavailable"));

    render(<SimilarApplicationsPanel applicationId="app-3" />);

    expect(await screen.findByRole("alert")).toHaveTextContent("Service unavailable");
  });
});
