import { beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { StrictMode } from "react";
import ConsistencyFlagReviewPanel from "../pages/ConsistencyFlagReviewPanel";
import * as service from "../../../services/applicationConsistencyService";

vi.mock("../../../services/applicationConsistencyService", async () => {
  const actual = await vi.importActual<typeof import("../../../services/applicationConsistencyService")>("../../../services/applicationConsistencyService");
  return { ...actual, getPersistedConsistencyFlags: vi.fn(), updateConsistencyFlagStatus: vi.fn() };
});

const openFlag = {
  id: "flag-1", applicationId: "app-1", applicationNumber: "EDU-001", ruleCode: "AMOUNT_EXCEEDS_FEES",
  title: "Requested amount exceeds education fees", category: "Consistency", severity: "High",
  description: "Verify amount.", detectedAtUtc: "2026-10-20T10:00:00Z", status: "Open" as const,
};

describe("ConsistencyFlagReviewPanel", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(service.getPersistedConsistencyFlags).mockResolvedValue({ items: [openFlag], page: 1, pageSize: 10, totalCount: 1, totalPages: 1 });
  });

  it("renders persisted flags and refreshes after a successful review action", async () => {
    vi.mocked(service.updateConsistencyFlagStatus).mockResolvedValue({});
    render(<ConsistencyFlagReviewPanel applicationId="app-1" />);
    expect(await screen.findByText(/Requested amount exceeds education fees/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: /Acknowledge \/ Investigate/i }));
    await waitFor(() => expect(service.updateConsistencyFlagStatus).toHaveBeenCalledWith("flag-1", "UnderInvestigation", undefined));
    await waitFor(() => expect(service.getPersistedConsistencyFlags).toHaveBeenCalledTimes(2));
  });

  it("clears loading state when rendered inside StrictMode", async () => {
    render(
      <StrictMode>
        <ConsistencyFlagReviewPanel applicationId="app-1" />
      </StrictMode>
    );

    expect(await screen.findByText(/Requested amount exceeds education fees/)).toBeInTheDocument();
    expect(screen.queryByText(/Loading persisted flags/i)).not.toBeInTheDocument();
  });

  it("requests subsequent pages and exposes pagination controls", async () => {
    vi.mocked(service.getPersistedConsistencyFlags).mockImplementation(async (_applicationId, filters) => ({
      items: [openFlag], page: filters?.page ?? 1, pageSize: 10, totalCount: 21, totalPages: 3,
    }));
    render(<ConsistencyFlagReviewPanel applicationId="app-1" />);
    expect(await screen.findByText(/Page 1 of 3 · 21 flags/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Next page" }));
    await waitFor(() => expect(service.getPersistedConsistencyFlags).toHaveBeenLastCalledWith("app-1", { status: "All", severity: "All", page: 2, pageSize: 10 }));
  });

  it("requires notes before resolving a flag", async () => {
    render(<ConsistencyFlagReviewPanel applicationId="app-1" />);
    fireEvent.click(await screen.findByRole("button", { name: "Resolve" }));
    expect(await screen.findByRole("alert")).toHaveTextContent(/notes are required/i);
    expect(service.updateConsistencyFlagStatus).not.toHaveBeenCalled();
  });

  it("shows API failures", async () => {
    vi.mocked(service.getPersistedConsistencyFlags).mockRejectedValue(new Error("Forbidden"));
    render(<ConsistencyFlagReviewPanel applicationId="app-1" />);
    expect(await screen.findByRole("alert")).toHaveTextContent("Forbidden");
  });

  it("resets pagination when a filter changes", async () => {
    vi.mocked(service.getPersistedConsistencyFlags).mockImplementation(async (_applicationId, filters) => ({
      items: [openFlag], page: filters?.page ?? 1, pageSize: 10, totalCount: 21, totalPages: 3,
    }));
    render(<ConsistencyFlagReviewPanel applicationId="app-1" />);
    fireEvent.click(await screen.findByRole("button", { name: "Next page" }));
    await waitFor(() => expect(service.getPersistedConsistencyFlags).toHaveBeenLastCalledWith("app-1", { status: "All", severity: "All", page: 2, pageSize: 10 }));
    fireEvent.change(screen.getByLabelText("Status"), { target: { value: "Open" } });
    await waitFor(() => expect(service.getPersistedConsistencyFlags).toHaveBeenLastCalledWith("app-1", { status: "Open", severity: "All", page: 1, pageSize: 10 }));
  });

  it("moves back to the last valid page if filtering reduces page count", async () => {
    let calls = 0;
    vi.mocked(service.getPersistedConsistencyFlags).mockImplementation(async (_applicationId, filters) => {
      calls += 1;
      if (filters?.status === "Resolved") return { items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 };
      return { items: [openFlag], page: filters?.page ?? 1, pageSize: 10, totalCount: 21, totalPages: 3 };
    });
    render(<ConsistencyFlagReviewPanel applicationId="app-1" />);
    fireEvent.click(await screen.findByRole("button", { name: "Next page" }));
    await waitFor(() => expect(service.getPersistedConsistencyFlags).toHaveBeenLastCalledWith("app-1", { status: "All", severity: "All", page: 2, pageSize: 10 }));
    fireEvent.change(screen.getByLabelText("Status"), { target: { value: "Resolved" } });
    expect(await screen.findByText(/No persisted flags match/)).toBeInTheDocument();
    expect(calls).toBeGreaterThanOrEqual(3);
  });

  it("requires notes to dismiss and reports mutation failures without refreshing as success", async () => {
    render(<ConsistencyFlagReviewPanel applicationId="app-1" />);
    fireEvent.click(await screen.findByRole("button", { name: /Dismiss as false positive/i }));
    expect(await screen.findByRole("alert")).toHaveTextContent(/notes are required/i);
    expect(service.updateConsistencyFlagStatus).not.toHaveBeenCalled();

    fireEvent.change(screen.getByLabelText(/Review \/ resolution notes/i), { target: { value: "Reviewed evidence" } });
    vi.mocked(service.updateConsistencyFlagStatus).mockRejectedValue(new Error("Forbidden"));
    fireEvent.click(screen.getByRole("button", { name: /Dismiss as false positive/i }));
    expect(await screen.findByRole("alert")).toHaveTextContent("Forbidden");
    expect(service.updateConsistencyFlagStatus).toHaveBeenCalledWith("flag-1", "Dismissed", "Reviewed evidence");
  });

  it("shows a clear empty state and disables unavailable pagination", async () => {
    vi.mocked(service.getPersistedConsistencyFlags).mockResolvedValue({ items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 });
    render(<ConsistencyFlagReviewPanel applicationId="app-1" />);
    expect(await screen.findByText(/No persisted flags match/)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Previous page" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Next page" })).toBeDisabled();
    expect(screen.getByText(/Page 0 of 0 · 0 flags/)).toBeInTheDocument();
  });
});

