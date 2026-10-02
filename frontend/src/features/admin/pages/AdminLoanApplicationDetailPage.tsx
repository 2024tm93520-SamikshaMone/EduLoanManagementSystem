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
  requestInfo,
  sendToFinance,
  startReview,
} from "../../../services/approvalWorkflowService";
import { getStoredUser } from "../../../services/authService";
import "../styles/AdminLoanApplicationDetailPage.css";

type ActionKind = "approve" | "reject" | "requestInfo" | "manualReview" | "confirmProcessed" | null;

export default function AdminLoanApplicationDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const role: string | undefined = getStoredUser()?.role;

  const [application, setApplication] = useState<AdminLoanApplicationDetail | null>(null);
  const [history, setHistory] = useState<WorkflowStep[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const [activeAction, setActiveAction] = useState<ActionKind>(null);
  const [commentInput, setCommentInput] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  function loadAll() {
    if (!id) return;
    setIsLoading(true);
    setLoadError(null);
    Promise.all([getAdminLoanApplicationById(id), getWorkflowHistory(id)])
      .then(([app, hist]) => {
        setApplication(app);
        setHistory(hist);
      })
      .catch((err) => setLoadError(err instanceof Error ? err.message : "Could not load application."))
      .finally(() => setIsLoading(false));
  }

  useEffect(loadAll, [id]);

  function openAction(kind: ActionKind) {
    setActiveAction(kind);
    setCommentInput("");
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

    if ((activeAction === "reject" || activeAction === "requestInfo") && !commentInput.trim()) {
      setActionError(
        activeAction === "reject"
          ? "A reason is required to reject an application."
          : "Please describe what additional information is needed."
      );
      return;
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
      if (activeAction === "requestInfo") await requestInfo(id, commentInput);
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

  if (isLoading) return <div className="admin-detail-page"><p role="status">Loading application...</p></div>;
  if (loadError || !application)
    return <div className="admin-detail-page"><div role="alert" className="form-error">{loadError ?? "Not found."}</div></div>;

  const status = application.status;
  const isReviewer = role === "Admin" || role === "HR";
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
              <dt>Monthly Salary</dt><dd>₹{application.monthlySalary.toLocaleString("en-IN")}</dd>
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
            <label htmlFor="comment">
              {activeAction === "reject" && "Reason for rejection"}
              {activeAction === "requestInfo" && "What information is needed?"}
              {activeAction === "approve" && "Comments (optional)"}
              {activeAction === "manualReview" && "Reason for escalation (optional)"}
              {activeAction === "confirmProcessed" && "Finance confirmation reference"}
            </label>
            <textarea id="comment" value={commentInput} onChange={(e) => setCommentInput(e.target.value)} />
            {actionError && <div role="alert" className="form-error">{actionError}</div>}
            <div className="admin-detail-action-form-buttons">
              <button type="submit" disabled={isSubmitting}>{isSubmitting ? "Submitting..." : "Confirm"}</button>
              <button type="button" className="link-button" onClick={closeAction} disabled={isSubmitting}>reject</button>
            </div>
          </form>
        )}

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
