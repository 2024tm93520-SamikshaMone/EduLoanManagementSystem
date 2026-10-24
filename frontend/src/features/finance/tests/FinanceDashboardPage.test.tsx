import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import FinanceDashboardPage from "../FinanceDashboardPage";

const row = {
  id: "app-1", applicationNumber: "EDU-2026-001", employeeName: "Asha Rao", employeeCode: "EMP001",
  requestedAmount: 50000, totalEducationFees: 60000, status: "PendingFinance", submittedAt: null, updatedAt: null, finance: null,
};

function mockFetch(body: unknown, ok = true) {
  vi.stubGlobal("fetch", vi.fn().mockResolvedValue({ ok, json: async () => ({ success: ok, data: body, message: ok ? "" : "Service unavailable" }) }));
}

describe("FinanceDashboardPage", () => {
  beforeEach(() => { vi.unstubAllGlobals(); sessionStorage.clear(); });

  it("shows loading then renders the Finance application queue", async () => {
    mockFetch([row]);
    render(<FinanceDashboardPage />);
    expect(screen.getByText(/loading finance applications/i)).toBeInTheDocument();
    expect(await screen.findByText("EDU-2026-001")).toBeInTheDocument();
    expect(screen.getByText("Asha Rao")).toBeInTheDocument();
  });

  it("shows an empty state when no applications need Finance processing", async () => {
    mockFetch([]);
    render(<FinanceDashboardPage />);
    expect(await screen.findByText(/you're all caught up/i)).toBeInTheDocument();
  });

  it("filters the queue by employee or application search text", async () => {
    mockFetch([row, { ...row, id: "app-2", applicationNumber: "EDU-2026-002", employeeName: "Dev Shah" }]);
    const user = userEvent.setup();
    render(<FinanceDashboardPage />);
    await screen.findByText("EDU-2026-001");
    await user.type(screen.getByPlaceholderText(/application number, employee, bank reference/i), "Dev Shah");
    expect(screen.getByText("EDU-2026-002")).toBeInTheDocument();
    expect(screen.queryByText("EDU-2026-001")).not.toBeInTheDocument();
  });

  it("shows a recoverable error when the Finance API fails", async () => {
    mockFetch([], false);
    render(<FinanceDashboardPage />);
    expect(await screen.findByRole("alert")).toHaveTextContent(/service unavailable/i);
    expect(screen.getByRole("button", { name: /try again/i })).toBeInTheDocument();
  });

  it("prevents recording a bank submission before required reference and date are entered", async () => {
    mockFetch([{ ...row, finance: { status: "Pending Finance Review", receivedAtUtc: "2026-10-01T00:00:00Z" } }]);
    render(<FinanceDashboardPage />);
    await screen.findByText("EDU-2026-001");
    await waitFor(() => expect(screen.getByRole("button", { name: /record send to bank/i })).toBeDisabled());
  });

  it("records Finance receipt only after the API confirms success and refreshes the queue", async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce({ ok: true, json: async () => ({ success: true, data: [row], message: "" }) })
      .mockResolvedValueOnce({ ok: true, json: async () => ({ success: true, data: { status: "Pending Finance Review" }, message: "received" }) })
      .mockResolvedValueOnce({ ok: true, json: async () => ({ success: true, data: [{ ...row, finance: { status: "Pending Finance Review", receivedAtUtc: "2026-10-24T00:00:00Z" } }], message: "" }) });
    vi.stubGlobal("fetch", fetchMock);
    const user = userEvent.setup();
    render(<FinanceDashboardPage />);
    await screen.findByText("EDU-2026-001");
    await user.click(screen.getByRole("button", { name: /mark received by finance/i }));
    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(3));
    expect(fetchMock.mock.calls[1][0]).toContain("/module6/finance/applications/app-1/received");
    expect(await screen.findByLabelText(/bank reference/i)).toBeInTheDocument();
  });

  it("surfaces a Finance action failure without pretending the action succeeded", async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce({ ok: true, json: async () => ({ success: true, data: [row], message: "" }) })
      .mockResolvedValueOnce({ ok: false, json: async () => ({ success: false, data: null, message: "Conflict: already received" }) });
    vi.stubGlobal("fetch", fetchMock);
    const user = userEvent.setup();
    render(<FinanceDashboardPage />);
    await screen.findByText("EDU-2026-001");
    await user.click(screen.getByRole("button", { name: /mark received by finance/i }));
    expect(await screen.findByRole("alert")).toHaveTextContent(/already received/i);
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });

});
