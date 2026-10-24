import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import RoleDashboardPage from "../RoleDashboardPage";

afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

describe("RoleDashboardPage", () => {
  it("renders data-backed summary and application statuses", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({ ok: true, json: async () => ({ success: true, data: { role: "Employee", summary: { totalApplications: 2, requiringAction: 1, unreadNotifications: 3 }, statusOverview: [{ status: "Submitted", count: 1 }, { status: "InfoRequested", count: 1 }], recentApplications: [{ id: "a1", applicationNumber: "EDL-001", status: "Submitted", requestedAmount: 12000, createdAt: "2026-10-01T00:00:00Z" }] } }) }));
    render(<MemoryRouter><RoleDashboardPage /></MemoryRouter>);
    expect(await screen.findByText("My education loan dashboard")).toBeInTheDocument();
    expect(screen.getByText("EDL-001")).toBeInTheDocument();
    expect(within(screen.getByRole("list")).getByText("Submitted")).toBeInTheDocument();
    const applicationRow = screen.getByRole("row", { name: /EDL-001 Submitted/ });
    expect(within(applicationRow).getByRole("cell", { name: "Submitted" })).toBeInTheDocument();
  });
  it("shows a retryable error when the API fails", async () => {
    vi.stubGlobal("fetch", vi.fn().mockRejectedValue(new Error("offline")));
    render(<MemoryRouter><RoleDashboardPage /></MemoryRouter>);
    expect(await screen.findByRole("alert")).toHaveTextContent("offline");
    await waitFor(() => expect(screen.getByRole("button", { name: "Retry" })).toBeInTheDocument());
  });
});
