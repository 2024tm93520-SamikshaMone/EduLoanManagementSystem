import { FormEvent, useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  AdminLoanApplicationDetail,
  getAdminLoanApplicationById,
} from "../../../services/adminLoanApplicationService";
import {
  WorkflowStep,
  approveApplication,
  confirmProcessed,
  escalateToManualReview,
  getWorkflowHistory,
  rejectApplication,
  sendToFinance,
  startReview,
} from "../../../services/approvalWorkflowService";
import { getStoredUser } from "../../../services/authService";
import SimilarApplicationsPanel from "./SimilarApplicationsPanel";
import ApplicationConsistencyPanel from "./ApplicationConsistencyPanel";
import "../styles/AdminLoanApplicationDetailPage.css";

type ActionKind = "approve" | "reject" | "requestInfo" | "manualReview" | "confirmProcessed" | null;
type ApplicationDocumentRow = { id: string; documentType: string; fileName: string; contentType: string; fileSizeBytes: number; uploadedAt: string; isRequired: boolean; status: string; verificationComments?: string | null; verifiedAtUtc?: string | null };

export default function AdminLoanApplicationDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const role: string | undefined = getStoredUser()?.role;

  const [application, setApplication] = useState<AdminLoanApplicationDetail | null>(null);
  const [history, setHistory] = useState<WorkflowStep[]>([]);
  const [documents, setDocuments] = useState<ApplicationDocumentRow[]>([]);
  const [documentComment, setDocumentComment] = useState<Record<string, string>>({});
  const [documentError, setDocumentError] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const [activeAction, setActiveAction] = useState<ActionKind>(null);
  const [commentInput, setCommentInput] = useState("");
  const [rfiReason, setRfiReason] = useState("");
  const [rfiDocumentType, setRfiDocumentType] = useState("");
  const [rfiDueDate, setRfiDueDate] = useState("");
  const [rfiInstructions, setRfiInstructions] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  function loadAll() {
    if (!id) return;
    setIsLoading(true);
    setLoadError(null);
    Promise.all([getAdminLoanApplicationById(id), getWorkflowHistory(id)])
      .then(async ([app, hist]) => {
        setApplication(app);
        setHistory(hist);
        const token = sessionStorage.getItem("eduloan_access_token");
        const base = (import.meta as ImportMeta & { env?: { VITE_API_BASE_URL?: string } }).env?.VITE_API_BASE_URL ?? "https://localhost:7001/api/v1";
        try {
          const response = await fetch(`${base}/module6/applications/${id}/documents`, { headers: token ? { Authorization: `Bearer ${token}` } : {} });
          const body = await response.json();
          if (response.ok && body.success) setDocuments(body.data as ApplicationDocumentRow[]);
        } catch { /* Keep the application detail usable if the additive document API is unavailable. */ }
      })
      .catch((err) => setLoadError(err instanceof Error ? err.message : "Could not load application."))
      .finally(() => setIsLoading(false));
  }

  useEffect(loadAll, [id]);

  function openAction(kind: ActionKind) {
    setActiveAction(kind);
    setCommentInput("");
    setRfiReason(""); setRfiDocumentType(""); setRfiDueDate(""); setRfiInstructions("");
    setActionError(null);
  }

  function closeAction() {
    setActiveAction(null);
    setActionError(null);
  }

  async function handleQuickAction(fn: () => Promise<unknown>) {
    setIsSubmitting(true);
    setActionError(null);
    try {
      await fn();
      loadAll();
    } catch (err) {
      setActionError(err instanceof Error ? err.message : "Action failed.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleCommentActionSubmit(e: FormEvent) {
    e.preventDefault();
    if (!id || !activeAction) return;

    if (activeAction === "reject" && !commentInput.trim()) {
      setActionError("A reason is required to reject an application."); return;
    }
    if (activeAction === "requestInfo" && (!commentInput.trim() || !rfiReason.trim())) {
      setActionError("Enter both the information requested and the reason for the request."); return;
    }
    if (activeAction === "confirmProcessed" && !commentInput.trim()) {
      setActionError("A finance confirmation reference is required.");
      return;
    }

    setIsSubmitting(true);
    setActionError(null);
    try {
      if (activeAction === "approve") await approveApplication(id, commentInput || undefined);
      if (activeAction === "reject") await rejectApplication(id, commentInput);
      if (activeAction === "requestInfo") {
        const token = sessionStorage.getItem("eduloan_access_token");
        const base = (import.meta as ImportMeta & { env?: { VITE_API_BASE_URL?: string } }).env?.VITE_API_BASE_URL ?? "https://localhost:7001/api/v1";
        const response = await fetch(`${base}/module6/applications/${id}/rfis`, {
          method: "POST",
          headers: { "Content-Type": "application/json", ...(token ? { Authorization: `Bearer ${token}` } : {}) },
          body: JSON.stringify({ question: commentInput.trim(), reason: rfiReason.trim(), requiredDocumentType: rfiDocumentType.trim() || null, dueDateUtc: rfiDueDate ? new Date(`${rfiDueDate}T23:59:59`).toISOString() : null, instructions: rfiInstructions.trim() || null }),
        });
        const body = await response.json();
        if (!response.ok || !body.success) throw new Error(body.message || "Could not create information request.");
      }
      if (activeAction === "manualReview") await escalateToManualReview(id, commentInput || undefined);
      if (activeAction === "confirmProcessed") await confirmProcessed(id, commentInput);
      closeAction();
      loadAll();
    } catch (err) {
      setActionError(err instanceof Error ? err.message : "Action failed.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function downloadAdminDocument(documentId: string, fileName: string) {
    setDocumentError("");
    const token = sessionStorage.getItem("eduloan_access_token");
    const base = (import.meta as ImportMeta & { env?: { VITE_API_BASE_URL?: string } }).env?.VITE_API_BASE_URL ?? "https://localhost:7001/api/v1";
    try {
      const response = await fetch(`${base}/module6/documents/${documentId}/download`, { headers: token ? { Authorization: `Bearer ${token}` } : {} });
      if (!response.ok) {
        const body = await response.json().catch(() => null);
        throw new Error(body?.message || "Could not download document.");
      }
      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement("a"); anchor.href = url; anchor.download = fileName; document.body.appendChild(anchor); anchor.click(); anchor.remove(); URL.revokeObjectURL(url);
    } catch (error) { setDocumentError(error instanceof Error ? error.message : "Could not download document."); }
  }

  async function reviewDocument(documentId: string, status: "Verified" | "Rejected") {
    setDocumentError("");
    const token = sessionStorage.getItem("eduloan_access_token");
    const base = (import.meta as ImportMeta & { env?: { VITE_API_BASE_URL?: string } }).env?.VITE_API_BASE_URL ?? "https://localhost:7001/api/v1";
    try {
      const response = await fetch(`${base}/module6/documents/${documentId}/review`, { method: "POST", headers: { "Content-Type": "application/json", ...(token ? { Authorization: `Bearer ${token}` } : {}) }, body: JSON.stringify({ status, comments: documentComment[documentId] || "" }) });
      const body = await response.json();
      if (!response.ok || !body.success) throw new Error(body.message || "Document review failed.");
      if (id) loadAll();
    } catch (error) { setDocumentError(error instanceof Error ? error.message : "Document review failed."); }
  }

  if (isLoading) return <div className="admin-detail-page"><p role="status">Loading application...</p></div>;
  if (loadError || !application)
    return <div className="admin-detail-page"><div role="alert" className="form-error">{loadError ?? "Not found."}</div></div>;

  const status = application.status;
  const isReviewer = role === "Admin";
  const isFinance = role === "Admin" || role === "Finance";

  const canStartReview = isReviewer && status === "Submitted";
  const canReviewActions = isReviewer && (status === "Submitted" || status === "UnderReview" || status === "ManualReview");
  const canSendToFinance = role === "Admin" && status === "Approved";
  const canConfirmProcessed = isFinance && status === "PendingFinance";

  return (
    <div className="admin-detail-page">
      <button type="button" className="link-button" onClick={() => navigate("/admin/applications")}>
        &larr; Back to all applications
      </button>

      <div className="admin-detail-card">
        <div className="admin-detail-header">
          <h1>{application.applicationNumber}</h1>
          <span className={`status-badge status-${status.toLowerCase()}`}>{status}</span>
        </div>

        <div className="admin-detail-grid">
          <section>
            <h2>Employee</h2>
            <dl>
              <dt>Name</dt><dd>{application.employeeName} ({application.employeeCode})</dd>
              <dt>Email</dt><dd>{application.email}</dd>
              <dt>Department</dt><dd>{application.departmentName}</dd>
              <dt>Designation</dt><dd>{application.designation}</dd>
            </dl>
          </section>

          <section>
            <h2>Education</h2>
            <dl>
              <dt>College</dt><dd>{application.collegeName}, {application.collegeCity}</dd>
              <dt>Course</dt><dd>{application.courseName} ({application.courseLevel})</dd>
              <dt>Specialization</dt><dd>{application.specialization}</dd>
              <dt>Duration</dt><dd>{application.courseDurationMonths} months</dd>
            </dl>
          </section>

          <section>
            <h2>Loan</h2>
            <dl>
              <dt>Total Fees</dt><dd>₹{application.totalEducationFees.toLocaleString("en-IN")}</dd>
              <dt>Requested Amount</dt><dd>₹{application.requestedAmount.toLocaleString("en-IN")}</dd>
              <dt>Tenure</dt><dd>{application.requestedTenureMonths} months</dd>
              <dt>Purpose</dt><dd>{application.educationPurpose}</dd>
            </dl>
          </section>
        </div>

        {id && <SimilarApplicationsPanel applicationId={id} />}
        {id && <ApplicationConsistencyPanel applicationId={id} />}

        {(canStartReview || canReviewActions || canSendToFinance || canConfirmProcessed) && (
          <div className="admin-detail-actions">
            <h2>Actions</h2>
            {canStartReview && (
              <button type="button" onClick={() => handleQuickAction(() => startReview(id!))} disabled={isSubmitting}>
                Start Review
              </button>
            )}
            {canReviewActions && (
              <>
                <button type="button" onClick={() => openAction("approve")} disabled={isSubmitting}>Approve</button>
                <button type="button" className="danger" onClick={() => openAction("reject")} disabled={isSubmitting}>Reject</button>
                <button type="button" onClick={() => openAction("requestInfo")} disabled={isSubmitting}>Request Info</button>
                {status !== "ManualReview" && (
                  <button type="button" onClick={() => openAction("manualReview")} disabled={isSubmitting}>
                    Escalate to Manual Review
                  </button>
                )}
              </>
            )}
            {canSendToFinance && (
              <button type="button" onClick={() => handleQuickAction(() => sendToFinance(id!))} disabled={isSubmitting}>
                Send to Finance
              </button>
            )}
            {canConfirmProcessed && (
              <button type="button" onClick={() => openAction("confirmProcessed")} disabled={isSubmitting}>
                Confirm Processed
              </button>
            )}
          </div>
        )}

        {activeAction && (
          <form onSubmit={handleCommentActionSubmit} className="admin-detail-action-form">
            {activeAction === "approve" && <section className="approval-confirmation" aria-label="Approval confirmation summary">
              <h3>Confirm application approval</h3>
              <p>Review this summary before approving. Approval records the decision; finance handoff remains a separate step.</p>
              <dl>
                <dt>Employee</dt><dd>{application.employeeName} ({application.employeeCode})</dd>
                <dt>Application</dt><dd>{application.applicationNumber}</dd>
                <dt>Requested amount</dt><dd>₹{application.requestedAmount.toLocaleString("en-IN")}</dd>
                <dt>Education fees</dt><dd>₹{application.totalEducationFees.toLocaleString("en-IN")}</dd>
                <dt>Eligibility rules evaluated</dt><dd>See the configured rule evaluation at submission; final approval remains subject to admin review.</dd>
              </dl>
            </section>}
            {activeAction === "requestInfo" ? <div className="rfi-request-fields">
              <label htmlFor="comment">Question / information required</label>
              <textarea id="comment" value={commentInput} onChange={(e) => setCommentInput(e.target.value)} required />
              <label htmlFor="rfi-reason">Reason for request</label>
              <textarea id="rfi-reason" value={rfiReason} onChange={e => setRfiReason(e.target.value)} required />
              <label htmlFor="rfi-document">Document required (optional)</label>
              <input id="rfi-document" value={rfiDocumentType} onChange={e => setRfiDocumentType(e.target.value)} placeholder="e.g. Admission letter or fee structure" />
              <label htmlFor="rfi-due-date">Due date (optional)</label>
              <input id="rfi-due-date" type="date" value={rfiDueDate} onChange={e => setRfiDueDate(e.target.value)} />
              <label htmlFor="rfi-instructions">Instructions (optional)</label>
              <textarea id="rfi-instructions" value={rfiInstructions} onChange={e => setRfiInstructions(e.target.value)} />
            </div> : <><label htmlFor="comment">
              {activeAction === "reject" && "Reason for rejection"}
              {activeAction === "approve" && "Comments (optional)"}
              {activeAction === "manualReview" && "Reason for escalation (optional)"}
              {activeAction === "confirmProcessed" && "Finance confirmation reference"}
            </label><textarea id="comment" value={commentInput} onChange={(e) => setCommentInput(e.target.value)} /></>}
            {actionError && <div role="alert" className="form-error">{actionError}</div>}
            <div className="admin-detail-action-form-buttons">
              <button type="submit" disabled={isSubmitting}>{isSubmitting ? "Submitting..." : "Confirm"}</button>
              <button type="button" className="link-button" onClick={closeAction} disabled={isSubmitting}>Cancel</button>
            </div>
          </form>
        )}

        <section className="admin-detail-history">
          <h2>Documents</h2>
          {documentError && <div role="alert" className="form-error">{documentError}</div>}
          {documents.length === 0 ? <p className="master-data-empty">No uploaded documents were returned for this application.</p> : <div className="admin-document-list">{documents.map(doc => <article key={doc.id} className="admin-document-row"><div><strong>{doc.documentType}</strong><div>{doc.fileName} · {(doc.fileSizeBytes / 1024).toFixed(1)} KB · {new Date(doc.uploadedAt).toLocaleString()}</div><small>{doc.isRequired ? "Required" : "Optional"} · Status: {doc.status}</small>{doc.verificationComments && <p>Reviewer comment: {doc.verificationComments}</p>}<button type="button" className="link-button" onClick={() => void downloadAdminDocument(doc.id, doc.fileName)}>View / Download</button></div><div className="admin-document-actions"><input aria-label={`Comment for ${doc.fileName}`} value={documentComment[doc.id] || ""} onChange={e => setDocumentComment(v => ({ ...v, [doc.id]: e.target.value }))} placeholder="Review comment" /><button type="button" onClick={() => void reviewDocument(doc.id, "Verified")}>Verify</button><button type="button" className="danger" onClick={() => void reviewDocument(doc.id, "Rejected")}>Reject</button></div></article>)}</div>}
        </section>

        <div className="admin-detail-history">
          <h2>Workflow History</h2>
          {history.length === 0 ? (
            <p className="master-data-empty">No actions taken yet.</p>
          ) : (
            <ul>
              {history.map((step) => (
                <li key={step.id}>
                  <strong>{step.action}</strong> by {step.actorName} ({step.actorRole}) —{" "}
                  {new Date(step.actionedAt).toLocaleString()}
                  {step.comments && <div className="history-comment">"{step.comments}"</div>}
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </div>
  );
}
