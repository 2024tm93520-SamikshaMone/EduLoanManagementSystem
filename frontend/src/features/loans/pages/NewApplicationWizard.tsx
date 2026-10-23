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
} from "../../../services/loanApplicationService";
import type { LoanApplicationDetail, LookupItem } from "../../../types/loanApplication";
import { getEmployeeDocuments, type EmployeeDocument } from "../../../services/employeeDocumentService";
import { attachLibraryDocument } from "../../../services/loanApplicationService";
import { getEligibilityGuidelines, getDocumentRequirements, previewEligibility } from "../../../services/eligibilityPreviewService";
import type { DocumentRequirement, EligibilityPreview, EligibilityRuleGuideline } from "../../../services/eligibilityPreviewService";

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
  const [guidelines, setGuidelines] = useState<EligibilityRuleGuideline[]>([]);
  const [eligibilityPreview, setEligibilityPreview] = useState<EligibilityPreview | null>(null);
  const [eligibilityError, setEligibilityError] = useState<string | null>(null);
  const [documentRequirements, setDocumentRequirements] = useState<DocumentRequirement[]>([]);

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
  const [libraryDocuments, setLibraryDocuments] = useState<EmployeeDocument[]>([]);
  const [selectedLibraryDocumentId, setSelectedLibraryDocumentId] = useState("");
  const [isAttachingLibraryDocument, setIsAttachingLibraryDocument] = useState(false);

  const [finalError, setFinalError] = useState<string | null>(null);
  const [isFinalSubmitting, setIsFinalSubmitting] = useState(false);

  useEffect(() => {
    let active = true;

    // Core lookup data must not be blocked by an optional eligibility-guidance API.
    Promise.all([getColleges(), getCourses()])
      .then(([collegeItems, courseItems]) => {
        if (!active) return;
        setColleges(collegeItems);
        setCourses(courseItems);
        setLookupsError(null);
      })
      .catch((err: unknown) => {
        if (!active) return;
        setLookupsError(
          err instanceof LoanApplicationError
            ? err.message
            : "Could not load colleges/courses."
        );
      });

    getEligibilityGuidelines()
      .then((rules) => {
        if (active) setGuidelines(rules);
      })
      .catch(() => {
        // Keep the application wizard usable if guidance is temporarily unavailable.
        if (active) setGuidelines([]);
      });

    getEmployeeDocuments().then(items => { if (active) setLibraryDocuments(items); }).catch(() => { if (active) setLibraryDocuments([]); });

    return () => { active = false; };
  }, []);

  useEffect(() => {
    if (!requestedAmount || Number(requestedAmount) <= 0 || !requestedTenureMonths || Number(requestedTenureMonths) <= 0 || !totalEducationFees || Number(totalEducationFees) <= 0) {
      setEligibilityPreview(null);
      return;
    }
    let active = true;
    previewEligibility({ requestedAmount: Number(requestedAmount), requestedTenureMonths: Number(requestedTenureMonths), totalEducationFees: Number(totalEducationFees) })
      .then(result => { if (active) { setEligibilityPreview(result); setEligibilityError(null); } })
      .catch(err => { if (active) setEligibilityError(err instanceof Error ? err.message : "Eligibility preview is unavailable; the server will validate before submission."); });
    return () => { active = false; };
  }, [requestedAmount, requestedTenureMonths, totalEducationFees]);

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

  async function handleAttachLibraryDocument() {
    if (!application || !selectedLibraryDocumentId) return;
    setDocError(null); setIsAttachingLibraryDocument(true);
    try {
      const doc = await attachLibraryDocument(application.id, selectedLibraryDocumentId);
      setApplication(current => current ? { ...current, documents: [...current.documents, doc] } : current);
      setLibraryDocuments(current => current.filter(item => item.id !== selectedLibraryDocumentId));
      setSelectedLibraryDocumentId("");
    } catch (err) {
      setDocError(err instanceof LoanApplicationError ? err.message : "Could not attach library document.");
    } finally { setIsAttachingLibraryDocument(false); }
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
      if (err instanceof LoanApplicationError) {
        // Eligibility failures come back with multiple messages (one per failed rule) —
        // join them into one string so this stays a simple single-message display,
        // same shape as every other error banner in this component.
        setFinalError(err.errors.length > 0 ? err.errors.join(" ") : err.message);
      } else {
        setFinalError("Could not submit application.");
      }
    } finally {
      setIsFinalSubmitting(false);
    }
  }

  return (
    <div className="wizard">
      <div className="wizard-steps">
        {STEPS.map((stepName, index) => (
          <div key={stepName} className="step-wrapper">
            <div
              className={`wizard-step ${stepIndex === index ? "active" : ""
                } ${stepIndex > index ? "completed" : ""}`}
            >
              <div className="step-number">
                {stepIndex > index ? "✓" : index + 1}
              </div>

              <div className="step-content">
                <span className="step-title">{stepName}</span>

                <span className="step-description">
                  {index === 0 && "Education details"}
                  {index === 1 && "Loan information"}
                  {index === 2 && "Upload documents"}
                  {index === 3 && "Verify application"}
                </span>
              </div>
            </div>

            {index < STEPS.length - 1 && (
              <div
                className={`step-connector ${stepIndex > index ? "completed" : ""
                  }`}
              ></div>
            )}
          </div>
        ))}
      </div>

      {lookupsError && <div role="alert" className="form-error">{lookupsError}</div>}

      {currentStep === "Education" && (
        <div className="wizard-panel">
          <section className="eligibility-guidelines" aria-label="Eligibility and loan guidelines">
            <h2>Eligibility &amp; Loan Guidelines</h2>
            <p>These rules are maintained by your administrator. Your final eligibility is validated by the server before submission.</p>
            {guidelines.length === 0 ? <p>Configured rules are currently unavailable. You can continue; eligibility will be checked before submission.</p> : <ul>{guidelines.map(rule => <li key={rule.id}><strong>{rule.ruleName}</strong> — {rule.conditionField} {rule.operator} {rule.conditionValue.toLocaleString("en-IN")} ({rule.severity.toLowerCase()}). {rule.errorMessage}</li>)}</ul>}
          </section>
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
          <section className="eligibility-guidelines" aria-label="Live eligibility check">
            <h2>Eligibility Check</h2>
            {eligibilityError && <p role="status">{eligibilityError}</p>}
            {eligibilityPreview ? <>
              <p>Monthly salary: <strong>₹{eligibilityPreview.monthlySalary.toLocaleString("en-IN")}</strong> · Service: <strong>{eligibilityPreview.tenureMonths} months</strong></p>
              {eligibilityPreview.maximumEligibleAmount !== null && <p>Configured maximum amount: <strong>₹{eligibilityPreview.maximumEligibleAmount.toLocaleString("en-IN")}</strong></p>}
              <ul>{eligibilityPreview.results.map(result => <li key={result.ruleId}>{result.passed ? "✓" : result.severity === "Warning" ? "⚠" : "✕"} <strong>{result.ruleName}</strong>: {result.passed ? "Passed" : (result.failureMessage || "Needs review")}</li>)}</ul>
              {eligibilityPreview.hasBlockingFailures && <p role="alert" className="form-error">One or more blocking rules currently fail. Adjust your details before submitting.</p>}
            </> : <p>Enter the amount, tenure and education fees to check the configured eligibility rules.</p>}
          </section>
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
          {documentRequirements.length > 0 && <section className="document-requirements-panel" aria-label="Configured document requirements">
            <h3>Document checklist</h3>
            <p>Requirements are maintained by administrators. Additional documents may be requested during review.</p>
            {Object.entries(documentRequirements.reduce<Record<string, DocumentRequirement[]>>((groups, item) => {
              (groups[item.category] ||= []).push(item); return groups;
            }, {})).map(([category, items]) => <div key={category} className="document-requirement-group"><h4>{category}</h4><ul>{items.map(item => <li key={item.id}><strong>{item.displayName}</strong> — {item.isRequired ? "Required" : "Optional"}{item.requiresCollateral ? " · Security/collateral related" : ""}</li>)}</ul></div>)}
          </section>}
          <p className="wizard-hint">Add at least one supporting document (e.g. Admission Letter, Fee Receipt). PDF, JPG, or PNG, up to 5 MB.</p>
          <section className="document-library-picker" aria-labelledby="saved-document-picker-heading">
            <h3 id="saved-document-picker-heading">Use a document from My Documents</h3>
            <p>Choose a document you uploaded earlier. A separate application copy is attached, so your library original remains safe.</p>
            {libraryDocuments.length === 0 ? <p>No saved documents found. <a href="/employee/documents">Upload documents to My Documents</a>.</p> : <div className="document-library-picker-row">
              <label htmlFor="saved-document-select" className="sr-only">Saved document</label>
              <select id="saved-document-select" aria-label="Saved document" value={selectedLibraryDocumentId} onChange={e => setSelectedLibraryDocumentId(e.target.value)}>
                <option value="">Select a saved document</option>
                {libraryDocuments.map(doc => <option key={doc.id} value={doc.id}>{doc.documentType} — {doc.fileName}</option>)}
              </select>
              <button type="button" onClick={() => void handleAttachLibraryDocument()} disabled={!selectedLibraryDocumentId || isAttachingLibraryDocument}>{isAttachingLibraryDocument ? "Attaching…" : "Attach selected"}</button>
            </div>}
          </section>
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
