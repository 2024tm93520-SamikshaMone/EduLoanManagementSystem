import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import DocumentsPage from "./DocumentsPage";
import * as documentService from "../../services/employeeDocumentService";

const requirements: documentService.EmployeeDocumentRequirement[] = [
  { id: 1, documentType: "PAN", displayName: "PAN Card", category: "Identity", isRequired: true, isActive: true, displayOrder: 1 },
];
const documents: documentService.EmployeeDocument[] = [
  { id: "doc-1", documentType: "PAN", fileName: "pan.pdf", contentType: "application/pdf", fileSizeBytes: 1024, uploadedAtUtc: "2026-10-01T00:00:00Z" },
];

describe("Employee Documents page", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    vi.spyOn(documentService, "getEmployeeDocuments").mockResolvedValue(documents);
    vi.spyOn(documentService, "getEmployeeDocumentRequirements").mockResolvedValue(requirements);
    vi.spyOn(documentService, "uploadEmployeeDocument").mockResolvedValue({ ...documents[0], id: "doc-2", fileName: "new-pan.pdf" });
    vi.spyOn(documentService, "downloadEmployeeDocument").mockResolvedValue(undefined);
  });

  it("lists saved documents and available admin-configured types", async () => {
    render(<MemoryRouter><DocumentsPage /></MemoryRouter>);
    expect(await screen.findByText("pan.pdf")).toBeInTheDocument();
    expect(screen.getByRole("option", { name: "PAN Card (Required)" })).toBeInTheDocument();
  });

  it("uploads a selected document and adds it to the visible list", async () => {
    const user = userEvent.setup();
    render(<MemoryRouter><DocumentsPage /></MemoryRouter>);
    await screen.findByText("pan.pdf");
    await user.selectOptions(screen.getByLabelText(/document type/i), "PAN");
    await user.upload(screen.getByLabelText(/choose file/i), new File(["pdf bytes"], "new-pan.pdf", { type: "application/pdf" }));
    await user.click(screen.getByRole("button", { name: /upload to my documents/i }));
    expect(await screen.findByText(/uploaded to my documents/i)).toBeInTheDocument();
    await waitFor(() => expect(screen.getByText("new-pan.pdf")).toBeInTheDocument());
    expect(documentService.uploadEmployeeDocument).toHaveBeenCalledWith("PAN", expect.any(File));
  });

  it("shows an empty state when the employee has no saved documents", async () => {
    vi.spyOn(documentService, "getEmployeeDocuments").mockResolvedValue([]);
    render(<MemoryRouter><DocumentsPage /></MemoryRouter>);
    expect(await screen.findByText(/no documents uploaded yet/i)).toBeInTheDocument();
  });
});
