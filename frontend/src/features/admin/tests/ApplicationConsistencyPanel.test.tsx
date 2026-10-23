import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import ApplicationConsistencyPanel from "../pages/ApplicationConsistencyPanel";
import * as service from "../../../services/applicationConsistencyService";

vi.mock("../../../services/applicationConsistencyService", () => ({ getApplicationConsistencyChecks: vi.fn() }));

describe("ApplicationConsistencyPanel", () => {
  beforeEach(() => vi.clearAllMocks());

  it("shows consistency flags and potential duplicate applications", async () => {
    vi.mocked(service.getApplicationConsistencyChecks).mockResolvedValue({
      applicationId: "app-1",
      flags: [{ code: "AMOUNT_EXCEEDS_FEES", title: "Requested amount exceeds education fees", severity: "High", description: "Verify the amount." }],
      potentialDuplicates: [{ applicationId: "app-2", applicationNumber: "EDU-002", status: "UnderReview" }],
      disclaimer: "Admin review only.",
    });

    render(<ApplicationConsistencyPanel applicationId="app-1" />);

    expect(await screen.findByText(/Requested amount exceeds education fees/i)).toBeInTheDocument();
    expect(screen.getByText(/EDU-002/)).toBeInTheDocument();
    expect(screen.getByText(/Admin review only/)).toBeInTheDocument();
  });

  it("shows an empty-state message when no flags are found", async () => {
    vi.mocked(service.getApplicationConsistencyChecks).mockResolvedValue({
      applicationId: "app-1", flags: [], potentialDuplicates: [], disclaimer: "Admin review only.",
    });

    render(<ApplicationConsistencyPanel applicationId="app-1" />);
    expect(await screen.findByText(/No consistency flags were found/i)).toBeInTheDocument();
  });

  it("shows API errors", async () => {
    vi.mocked(service.getApplicationConsistencyChecks).mockRejectedValue(new Error("Service unavailable"));
    render(<ApplicationConsistencyPanel applicationId="app-1" />);
    expect(await screen.findByRole("alert")).toHaveTextContent("Service unavailable");
  });
});
