import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import NewApplicationWizard from "./NewApplicationWizard";
import * as loanService from "../../services/loanApplicationService";
import type { LoanApplicationDetail } from "../../types/loanApplication";

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
