import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import NewApplicationWizard from "../pages/NewApplicationWizard";
import * as loanService from "../../../services/loanApplicationService";
import * as eligibilityService from "../../../services/eligibilityPreviewService";
import * as employeeDocumentService from "../../../services/employeeDocumentService";
import type { LoanApplicationDetail } from "../../../types/loanApplication";

const COLLEGES = [{ id: 1, name: "BITS Pilani" }];
const COURSES = [{ id: 1, name: "M.Tech SE" }];

const DRAFT_APPLICATION: LoanApplicationDetail = {
  id: "app-1",
  applicationNumber: "EDL-2026-000001",
  employeeId: "emp-1",
  collegeId: 1,
  collegeName: "BITS Pilani",
  courseId: 1,
  courseName: "M.Tech SE",
  specialization: "AI/ML",
  courseDurationMonths: 24,
  totalEducationFees: 500000,
  requestedAmount: 400000,
  requestedTenureMonths: 36,
  educationPurpose: "MTech at BITS",
  status: "Draft",
  createdAt: "2026-09-01T00:00:00Z",
  submittedAt: null,
  documents: [],
};

async function fillEducationStep(user: ReturnType<typeof userEvent.setup>) {
  await user.selectOptions(screen.getByLabelText(/college/i), "1");
  await user.selectOptions(screen.getByLabelText(/^course$/i), "1");
  await user.type(screen.getByLabelText(/specialization/i), "AI/ML");
  await user.type(screen.getByLabelText(/course duration/i), "24");
  await user.type(screen.getByLabelText(/total education fees/i), "500000");
  await user.click(screen.getByRole("button", { name: /next/i }));
}

describe("NewApplicationWizard", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    vi.spyOn(loanService, "getColleges").mockResolvedValue(COLLEGES);
    vi.spyOn(loanService, "getCourses").mockResolvedValue(COURSES);
    vi.spyOn(employeeDocumentService, "getEmployeeDocuments").mockResolvedValue([{ id: "lib-1", documentType: "PAN", fileName: "pan.pdf", contentType: "application/pdf", fileSizeBytes: 1024, uploadedAtUtc: "2026-09-01T00:00:00Z" }]);
    vi.spyOn(loanService, "attachLibraryDocument").mockResolvedValue({ id: "app-doc-1", documentType: "PAN", fileName: "pan.pdf", fileSizeBytes: 1024, uploadedAt: "2026-09-01T00:00:00Z" });
    // The wizard now loads eligibility rules alongside college/course lookups.
    // Mock these APIs so the component tests stay isolated from the real backend.
    vi.spyOn(eligibilityService, "getEligibilityGuidelines").mockResolvedValue([]);
    vi.spyOn(eligibilityService, "previewEligibility").mockResolvedValue({
      monthlySalary: 65000,
      tenureMonths: 36,
      maximumEligibleAmount: 500000,
      hasBlockingFailures: false,
      rules: [],
      results: [],
    });
  });

  it("blocks moving to step 2 when required education fields are missing", async () => {
    const user = userEvent.setup();
    render(<NewApplicationWizard />);
    await screen.findByLabelText(/college/i);

    await user.click(screen.getByRole("button", { name: /next/i }));

    expect(await screen.findByText(/please select a college/i)).toBeInTheDocument();
  });

  it("creates the draft application when moving from Loan Details to Documents", async () => {
    const createSpy = vi.spyOn(loanService, "createApplication").mockResolvedValue(DRAFT_APPLICATION);
    const user = userEvent.setup();
    render(<NewApplicationWizard />);
    await screen.findByLabelText(/college/i);

    await fillEducationStep(user);
    await user.type(screen.getByLabelText(/requested loan amount/i), "400000");
    await user.type(screen.getByLabelText(/repayment tenure/i), "36");
    await user.type(screen.getByLabelText(/purpose/i), "MTech at BITS");
    await user.click(screen.getByRole("button", { name: /next/i }));

    await waitFor(() => expect(createSpy).toHaveBeenCalled());
    expect(await screen.findByText(/at least one supporting document/i)).toBeInTheDocument();
  });

  it("rejects requested amount greater than total education fees", async () => {
    const user = userEvent.setup();
    render(<NewApplicationWizard />);
    await screen.findByLabelText(/college/i);

    await fillEducationStep(user);
    await user.type(screen.getByLabelText(/requested loan amount/i), "999999");
    await user.type(screen.getByLabelText(/repayment tenure/i), "36");
    await user.type(screen.getByLabelText(/purpose/i), "x");
    await user.click(screen.getByRole("button", { name: /next/i }));

    expect(await screen.findByText(/cannot exceed total education fees/i)).toBeInTheDocument();
  });

  it("adds and removes a document, and disables Next until one exists", async () => {
    vi.spyOn(loanService, "createApplication").mockResolvedValue(DRAFT_APPLICATION);
    const addedDoc = { id: "doc-1", documentType: "Admission Letter", fileName: "admit.pdf", fileSizeBytes: 1024, uploadedAt: "2026-09-01" };
    vi.spyOn(loanService, "addDocument").mockResolvedValue(addedDoc);
    vi.spyOn(loanService, "removeDocument").mockResolvedValue(undefined);

    const user = userEvent.setup();
    render(<NewApplicationWizard />);
    await screen.findByLabelText(/college/i);
    await fillEducationStep(user);
    await user.type(screen.getByLabelText(/requested loan amount/i), "400000");
    await user.type(screen.getByLabelText(/repayment tenure/i), "36");
    await user.type(screen.getByLabelText(/purpose/i), "x");
    await user.click(screen.getByRole("button", { name: /next/i }));
    await screen.findByText(/at least one supporting document/i);

    const nextButton = screen.getByRole("button", { name: /^next$/i });
    expect(nextButton).toBeDisabled();

    const fakeFile = new File(["dummy content"], "admit.pdf", { type: "application/pdf" });
    await user.type(screen.getByLabelText(/document type/i), "Admission Letter");
    await user.upload(screen.getByLabelText(/^file$/i), fakeFile);
    await user.click(screen.getByRole("button", { name: /^add$/i }));

    expect(await screen.findByText("admit.pdf", { exact: false })).toBeInTheDocument();
    expect(nextButton).not.toBeDisabled();

    await user.click(screen.getByRole("button", { name: /remove/i }));
    await waitFor(() => expect(screen.queryByText("admit.pdf", { exact: false })).not.toBeInTheDocument());
  });


  it("attaches an existing document from My Documents to the draft application", async () => {
    vi.spyOn(loanService, "createApplication").mockResolvedValue(DRAFT_APPLICATION);
    const attachSpy = vi.spyOn(loanService, "attachLibraryDocument");
    const user = userEvent.setup();
    render(<NewApplicationWizard />);
    await screen.findByLabelText(/college/i);
    await fillEducationStep(user);
    await user.type(screen.getByLabelText(/requested loan amount/i), "400000");
    await user.type(screen.getByLabelText(/repayment tenure/i), "36");
    await user.type(screen.getByLabelText(/purpose/i), "MTech at BITS");
    await user.click(screen.getByRole("button", { name: /^next$/i }));
    await screen.findByText(/use a document from my documents/i);
    await user.selectOptions(screen.getByLabelText(/saved document/i), "lib-1");
    await user.click(screen.getByRole("button", { name: /attach selected/i }));
    await waitFor(() => expect(attachSpy).toHaveBeenCalledWith("app-1", "lib-1"));
    expect(await screen.findByText("pan.pdf", { exact: false })).toBeInTheDocument();
  });

  it("submits the application from the Review step and calls onDone", async () => {
    vi.spyOn(loanService, "createApplication").mockResolvedValue(DRAFT_APPLICATION);
    vi.spyOn(loanService, "addDocument").mockResolvedValue({
      id: "doc-1", documentType: "Admission Letter", fileName: "admit.pdf", fileSizeBytes: 1024, uploadedAt: "2026-09-01",
    });
    const submitSpy = vi.spyOn(loanService, "submitApplication").mockResolvedValue({
      ...DRAFT_APPLICATION, status: "Submitted",
    });

    const onDone = vi.fn();
    const user = userEvent.setup();
    render(<NewApplicationWizard onDone={onDone} />);
    await screen.findByLabelText(/college/i);
    await fillEducationStep(user);
    await user.type(screen.getByLabelText(/requested loan amount/i), "400000");
    await user.type(screen.getByLabelText(/repayment tenure/i), "36");
    await user.type(screen.getByLabelText(/purpose/i), "x");
    await user.click(screen.getByRole("button", { name: /next/i }));
    await screen.findByText(/at least one supporting document/i);

    const fakeFile = new File(["dummy content"], "admit.pdf", { type: "application/pdf" });
    await user.type(screen.getByLabelText(/document type/i), "Admission Letter");
    await user.upload(screen.getByLabelText(/^file$/i), fakeFile);
    await user.click(screen.getByRole("button", { name: /^add$/i }));
    await screen.findByText("admit.pdf", { exact: false });

    await user.click(screen.getByRole("button", { name: /^next$/i }));
    await screen.findByText(/EDL-2026-000001/);

    await user.click(screen.getByRole("button", { name: /submit application/i }));

    await waitFor(() => {
      expect(submitSpy).toHaveBeenCalledWith("app-1");
      expect(onDone).toHaveBeenCalled();
    });
  });
});
