import { FormEvent, useEffect, useState } from "react";
import {
  LoanApplicationError,
  addDocument,
  createApplication,
  downloadDocument,
  getColleges,
  getCourses,
  removeDocument,
  submitApplication,
} from "../../services/loanApplicationService";
import type { LoanApplicationDetail, LookupItem } from "../../types/loanApplication";

const STEPS = ["Education", "Loan Details", "Documents", "Review & Submit"] as const;
type Step = (typeof STEPS)[number];

interface NewApplicationWizardProps {
  onDone?: () => void;
}

export default function NewApplicationWizard({ onDone }: NewApplicationWizardProps) {
  const [stepIndex, setStepIndex] = useState(0);
  const currentStep: Step = STEPS[stepIndex];

  const [colleges, setColleges] = useState<LookupItem[]>([]);
  const [courses, setCourses] = useState<LookupItem[]>([]);
  const [lookupsError, setLookupsError] = useState<string | null>(null);

  // Education step
  const [collegeId, setCollegeId] = useState<number | "">("");
  const [courseId, setCourseId] = useState<number | "">("");
  const [specialization, setSpecialization] = useState("");
  const [courseDurationMonths, setCourseDurationMonths] = useState<number | "">("");
  const [totalEducationFees, setTotalEducationFees] = useState<number | "">("");

  // Loan step
  const [requestedAmount, setRequestedAmount] = useState<number | "">("");
  const [requestedTenureMonths, setRequestedTenureMonths] = useState<number | "">("");
  const [educationPurpose, setEducationPurpose] = useState("");

  const [stepError, setStepError] = useState<string | null>(null);
  const [isSubmittingStep, setIsSubmittingStep] = useState(false);

  // Once the draft is created (after step 1->2 transition we actually create it
  // on the server so documents can attach to a real ApplicationId), we track it here.
  const [application, setApplication] = useState<LoanApplicationDetail | null>(null);

  const [docType, setDocType] = useState("");
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [docError, setDocError] = useState<string | null>(null);
  const [isUploadingDoc, setIsUploadingDoc] = useState(false);

  const [finalError, setFinalError] = useState<string | null>(null);
  const [isFinalSubmitting, setIsFinalSubmitting] = useState(false);

  useEffect(() => {
    Promise.all([getColleges(), getCourses()])
      .then(([c, co]) => {
        setColleges(c);
        setCourses(co);
      })
      .catch((err) =>
        setLookupsError(err instanceof LoanApplicationError ? err.message : "Could not load colleges/courses.")
      );
  }, []);

  function validateEducationStep(): string | null {
    if (!collegeId) return "Please select a college.";
    if (!courseId) return "Please select a course.";
    if (!specialization.trim()) return "Specialization is required.";
    if (!courseDurationMonths || Number(courseDurationMonths) <= 0) return "Enter a valid course duration.";
    if (!totalEducationFees || Number(totalEducationFees) <= 0) return "Enter valid total education fees.";
    return null;
  }

  function validateLoanStep(): string | null {
    if (!requestedAmount || Number(requestedAmount) <= 0) return "Enter a valid requested amount.";
    if (Number(requestedAmount) > Number(totalEducationFees))
      return "Requested amount cannot exceed total education fees.";
    if (!requestedTenureMonths || Number(requestedTenureMonths) <= 0) return "Enter a valid repayment tenure.";
    if (!educationPurpose.trim()) return "Please describe the purpose of this loan.";
    return null;
  }

  async function goNext() {
    setStepError(null);

    if (currentStep === "Education") {
      const err = validateEducationStep();
      if (err) return setStepError(err);
      setStepIndex(1);
      return;
    }

    if (currentStep === "Loan Details") {
      const err = validateLoanStep();
      if (err) return setStepError(err);

      setIsSubmittingStep(true);
      try {
        const created = await createApplication({
          collegeId: Number(collegeId),
          courseId: Number(courseId),
          specialization,
          courseDurationMonths: Number(courseDurationMonths),
          totalEducationFees: Number(totalEducationFees),
          requestedAmount: Number(requestedAmount),
          requestedTenureMonths: Number(requestedTenureMonths),
          educationPurpose,
        });
        setApplication(created);
        setStepIndex(2);
      } catch (err) {
        setStepError(err instanceof LoanApplicationError ? err.message : "Could not save application draft.");
      } finally {
        setIsSubmittingStep(false);
      }
      return;
    }

    if (currentStep === "Documents") {
      setStepIndex(3);
    }
  }

  function goBack() {
    setStepError(null);
    setStepIndex((i) => Math.max(0, i - 1));
  }

  async function handleAddDocument(e: FormEvent) {
    e.preventDefault();
    setDocError(null);
    if (!application) return;
    if (!docType.trim()) {
      setDocError("Document type is required.");
      return;
    }
    if (!selectedFile) {
      setDocError("Please choose a file to upload.");
      return;
    }
    setIsUploadingDoc(true);
    try {
      const doc = await addDocument(application.id, docType, selectedFile);
      setApplication({ ...application, documents: [...application.documents, doc] });
      setDocType("");
      setSelectedFile(null);
    } catch (err) {
      setDocError(err instanceof LoanApplicationError ? err.message : "Could not add document.");
    } finally {
      setIsUploadingDoc(false);
    }
  }

  async function handleRemoveDocument(documentId: string) {
    if (!application) return;
    try {
      await removeDocument(application.id, documentId);
      setApplication({
        ...application,
        documents: application.documents.filter((d) => d.id !== documentId),
      });
    } catch (err) {
      setDocError(err instanceof LoanApplicationError ? err.message : "Could not remove document.");
    }
  }

  async function handleDownloadDocument(documentId: string, fileName: string) {
    if (!application) return;
    try {
      await downloadDocument(application.id, documentId, fileName);
    } catch (err) {
      setDocError(err instanceof LoanApplicationError ? err.message : "Could not download this document.");
    }
  }

  async function handleFinalSubmit() {
    if (!application) return;
    setFinalError(null);
    setIsFinalSubmitting(true);
    try {
      await submitApplication(application.id);
      onDone?.();
    } catch (err) {
      setFinalError(err instanceof LoanApplicationError ? err.message : "Could not submit application.");
    } finally {
      setIsFinalSubmitting(false);
    }
  }

  return (
    <div className="wizard">
      <div className="wizard-steps" role="tablist">
        {STEPS.map((step, i) => (
          <div key={step} className={`wizard-step ${i === stepIndex ? "active" : ""} ${i < stepIndex ? "done" : ""}`}>
            {i + 1}. {step}
          </div>
        ))}
      </div>

      {lookupsError && <div role="alert" className="form-error">{lookupsError}</div>}

      {currentStep === "Education" && (
        <div className="wizard-panel">
          <div className="form-group">
            <label htmlFor="college">College</label>
            <select id="college" value={collegeId} onChange={(e) => setCollegeId(Number(e.target.value) || "")}>
              <option value="">Select a college</option>
              {colleges.map((c) => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
          </div>
          <div className="form-group">
            <label htmlFor="course">Course</label>
            <select id="course" value={courseId} onChange={(e) => setCourseId(Number(e.target.value) || "")}>
              <option value="">Select a course</option>
              {courses.map((c) => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
          </div>
          <div className="form-group">
            <label htmlFor="specialization">Specialization</label>
            <input id="specialization" value={specialization} onChange={(e) => setSpecialization(e.target.value)} />
          </div>
          <div className="form-group">
            <label htmlFor="duration">Course Duration (months)</label>
            <input
              id="duration"
              type="number"
              value={courseDurationMonths}
              onChange={(e) => setCourseDurationMonths(Number(e.target.value) || "")}
            />
          </div>
          <div className="form-group">
            <label htmlFor="fees">Total Education Fees (₹)</label>
            <input
              id="fees"
              type="number"
              value={totalEducationFees}
              onChange={(e) => setTotalEducationFees(Number(e.target.value) || "")}
            />
          </div>
        </div>
      )}

      {currentStep === "Loan Details" && (
        <div className="wizard-panel">
          <div className="form-group">
            <label htmlFor="requestedAmount">Requested Loan Amount (₹)</label>
            <input
              id="requestedAmount"
              type="number"
              value={requestedAmount}
              onChange={(e) => setRequestedAmount(Number(e.target.value) || "")}
            />
          </div>
          <div className="form-group">
            <label htmlFor="tenure">Repayment Tenure (months)</label>
            <input
              id="tenure"
              type="number"
              value={requestedTenureMonths}
              onChange={(e) => setRequestedTenureMonths(Number(e.target.value) || "")}
            />
          </div>
          <div className="form-group">
            <label htmlFor="purpose">Purpose</label>
            <textarea id="purpose" value={educationPurpose} onChange={(e) => setEducationPurpose(e.target.value)} />
          </div>
        </div>
      )}

      {currentStep === "Documents" && application && (
        <div className="wizard-panel">
          <p className="wizard-hint">Add at least one supporting document (e.g. Admission Letter, Fee Receipt). PDF, JPG, or PNG, up to 5 MB.</p>
          <form onSubmit={handleAddDocument} className="document-form">
            <input
              aria-label="Document Type"
              placeholder="Document type (e.g. Admission Letter)"
              value={docType}
              onChange={(e) => setDocType(e.target.value)}
            />
            <input
              aria-label="File"
              type="file"
              accept=".pdf,.jpg,.jpeg,.png"
              onChange={(e) => setSelectedFile(e.target.files?.[0] ?? null)}
            />
            <button type="submit" disabled={isUploadingDoc}>
              {isUploadingDoc ? "Uploading..." : "Add"}
            </button>
          </form>
          {docError && <div role="alert" className="form-error">{docError}</div>}

          {application.documents.length === 0 ? (
            <p className="master-data-empty">No documents added yet.</p>
          ) : (
            <ul className="document-list">
              {application.documents.map((d) => (
                <li key={d.id}>
                  <span>
                    <strong>{d.documentType}</strong> — {d.fileName}
                    {d.fileSizeBytes > 0 && ` (${(d.fileSizeBytes / 1024).toFixed(0)} KB)`}
                  </span>
                  <span>
                    <button type="button" className="link-button" onClick={() => handleDownloadDocument(d.id, d.fileName)}>
                      Download
                    </button>{" "}
                    <button type="button" className="link-button danger" onClick={() => handleRemoveDocument(d.id)}>
                      Remove
                    </button>
                  </span>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}

      {currentStep === "Review & Submit" && application && (
        <div className="wizard-panel">
          <div className="profile-grid">
            <div className="profile-field"><span className="profile-label">Application No.</span><span className="profile-value">{application.applicationNumber}</span></div>
            <div className="profile-field"><span className="profile-label">College</span><span className="profile-value">{application.collegeName}</span></div>
            <div className="profile-field"><span className="profile-label">Course</span><span className="profile-value">{application.courseName}</span></div>
            <div className="profile-field"><span className="profile-label">Requested Amount</span><span className="profile-value">₹{application.requestedAmount.toLocaleString("en-IN")}</span></div>
            <div className="profile-field"><span className="profile-label">Tenure</span><span className="profile-value">{application.requestedTenureMonths} months</span></div>
            <div className="profile-field"><span className="profile-label">Documents</span><span className="profile-value">{application.documents.length} attached</span></div>
          </div>

          {finalError && <div role="alert" className="form-error">{finalError}</div>}

          <button type="button" onClick={handleFinalSubmit} disabled={isFinalSubmitting}>
            {isFinalSubmitting ? "Submitting..." : "Submit Application"}
          </button>
        </div>
      )}

      {stepError && <div role="alert" className="form-error">{stepError}</div>}

      <div className="wizard-nav">
        {stepIndex > 0 && stepIndex < 3 && (
          <button type="button" className="link-button" onClick={goBack}>Back</button>
        )}
        {stepIndex < 2 && (
          <button type="button" onClick={goNext} disabled={isSubmittingStep}>
            {isSubmittingStep ? "Saving..." : "Next"}
          </button>
        )}
        {stepIndex === 2 && (
          <button type="button" onClick={goNext} disabled={!application || application.documents.length === 0}>
            Next
          </button>
        )}
      </div>
    </div>
  );
}
