import { useCallback, useEffect, useState } from "react";
import { LoanApplicationError, addDocument, getMyApplications } from "../../../services/loanApplicationService";
import type { LoanApplicationSummary } from "../../../types/loanApplication";
import "../styles/MyApplicationsPage.css";

type Rfi = { id: string; applicationId?: string; question: string; requiredDocumentType?: string | null; reason: string; dueDateUtc?: string | null; instructions?: string | null; status: string; createdAtUtc: string; responses: { id: string; responseText: string; documentFileName?: string | null; submittedAtUtc: string }[] };
type AppDocument = { id: string; documentType: string; fileName: string; status?: string };
const API = (import.meta as ImportMeta & { env?: { VITE_API_BASE_URL?: string } }).env?.VITE_API_BASE_URL ?? "https://localhost:7001/api/v1";
async function api<T>(path: string, method = "GET", body?: unknown): Promise<T> {
  const token = sessionStorage.getItem("eduloan_access_token");
  const response = await fetch(`${API}${path}`, { method, headers: { ...(token ? { Authorization: `Bearer ${token}` } : {}), ...(body ? { "Content-Type": "application/json" } : {}) }, ...(body ? { body: JSON.stringify(body) } : {}) });
  const result = await response.json();
  if (!response.ok || !result.success) throw new Error(result.message || "Request failed.");
  return result.data as T;
}

export default function MyApplicationsPage() {
  const [applications, setApplications] = useState<LoanApplicationSummary[]>([]);
  const [rfis, setRfis] = useState<Record<string, Rfi[]>>({});
  const [responses, setResponses] = useState<Record<string, string>>({});
  const [responseDocuments, setResponseDocuments] = useState<Record<string, string>>({});
  const [responseFiles, setResponseFiles] = useState<Record<string, File | null>>({});
  const [responseDocumentTypes, setResponseDocumentTypes] = useState<Record<string, string>>({});
  const [documents, setDocuments] = useState<Record<string, AppDocument[]>>({});
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [actionError, setActionError] = useState("");
  const [busy, setBusy] = useState("");

  const loadApplications = useCallback(async () => {
    setIsLoading(true); setLoadError(null);
    try {
      const apps = await getMyApplications(); setApplications(apps);
      const pairs = await Promise.all(apps.map(async app => {
        const [rfiItems, documentItems] = await Promise.all([
          api<Rfi[]>(`/module6/applications/${app.id}/rfis`).catch(() => [] as Rfi[]),
          api<AppDocument[]>(`/module6/applications/${app.id}/documents`).catch(() => [] as AppDocument[]),
        ]);
        return { id: app.id, rfiItems, documentItems };
      }));
      setRfis(Object.fromEntries(pairs.map(x => [x.id, x.rfiItems])));
      setDocuments(Object.fromEntries(pairs.map(x => [x.id, x.documentItems])));
    } catch (err) {
      setLoadError(err instanceof LoanApplicationError ? err.message : "Could not load your applications.");
    } finally { setIsLoading(false); }
  }, []);
  useEffect(() => { void loadApplications(); }, [loadApplications]);

  async function submitResponse(rfi: Rfi, applicationId: string) {
    const text = (responses[rfi.id] || "").trim();
    if (!text) { setActionError("Enter a response before submitting."); return; }
    setBusy(rfi.id); setActionError("");
    try {
      let documentId = responseDocuments[rfi.id] || null;
      const file = responseFiles[rfi.id];
      if (file) {
        const docType = (responseDocumentTypes[rfi.id] || rfi.requiredDocumentType || "Additional Document").trim();
        if (!docType) throw new Error("Enter a document type before uploading.");
        const uploaded = await addDocument(applicationId, docType, file);
        documentId = uploaded.id;
      }
      await api(`/module6/rfis/${rfi.id}/responses`, "POST", { responseText: text, documentId });
      setResponses(v => ({ ...v, [rfi.id]: "" })); setResponseDocuments(v => ({ ...v, [rfi.id]: "" })); setResponseFiles(v => ({ ...v, [rfi.id]: null })); await loadApplications();
    }
    catch (e) { setActionError(e instanceof Error ? e.message : "Could not submit response."); }
    finally { setBusy(""); }
  }

  return <div className="admin-page"><div className="admin-card"><div className="master-data-header"><h1>My Loan Applications</h1><button type="button" onClick={() => void loadApplications()}>Refresh</button></div>
    {actionError && <div role="alert" className="form-error">{actionError}</div>}
    {isLoading ? <p role="status">Loading your applications...</p> : loadError ? <div role="alert" className="form-error">{loadError}<button type="button" onClick={() => void loadApplications()}>Retry</button></div> : applications.length === 0 ? <p className="master-data-empty">You haven't submitted any applications yet.</p> : <table className="master-data-table"><thead><tr><th>Application No.</th><th>College</th><th>Course</th><th>Amount</th><th>Status</th></tr></thead><tbody>{applications.map(app => <tr key={app.id}><td>{app.applicationNumber}</td><td>{app.collegeName}</td><td>{app.courseName}</td><td>₹{app.requestedAmount.toLocaleString("en-IN")}</td><td><span className={`status-badge status-${app.status.toLowerCase()}`}>{app.status}</span><div className="employee-status-tracker" aria-label={`Progress for ${app.applicationNumber}`}>{(["Submitted", "Eligibility Checked", "Documents Verified", "Admin Review", "Approved", "Finance/Bank Processing", "Processing Confirmed"] as const).map((label, index) => { const current = String(app.status) === "Processed" ? 6 : String(app.status) === "PendingFinance" ? 5 : String(app.status) === "Approved" ? 4 : ["Submitted", "UnderReview", "ManualReview", "InfoRequested", "Rejected"].includes(String(app.status)) ? 3 : String(app.status) === "Draft" ? -1 : 0; const completed = index < current; const active = index === current; return <span key={label} className={`employee-status-step ${completed ? "done" : ""} ${active ? "current" : ""}`}>{label}</span>; })}</div></td></tr>)}</tbody></table>}
    {applications.map(app => { const items = rfis[app.id] || []; if (!items.length) return null; return <section key={`rfi-${app.id}`} className="employee-rfi-section"><h2>Information Requests — {app.applicationNumber}</h2>{items.map(rfi => <article key={rfi.id} className="employee-rfi-card"><div className="employee-rfi-title"><strong>{rfi.question}</strong><span className={`status-badge status-${rfi.status.toLowerCase()}`}>{rfi.status}</span></div><p><strong>Reason:</strong> {rfi.reason}</p>{rfi.requiredDocumentType && <p><strong>Requested document:</strong> {rfi.requiredDocumentType}. You can upload it directly below or attach a document already uploaded to this application.</p>}{rfi.instructions && <p>{rfi.instructions}</p>}{rfi.dueDateUtc && <p><strong>Due date:</strong> {new Date(rfi.dueDateUtc).toLocaleDateString()}</p>}<div className="employee-rfi-history">{rfi.responses.map(r => <p key={r.id}><strong>Response ({new Date(r.submittedAtUtc).toLocaleString()}):</strong> {r.responseText}{r.documentFileName && <> · Attached document: {r.documentFileName}</>}</p>)}</div>{rfi.status === "Pending" && <><label className="employee-rfi-label">Your response<textarea value={responses[rfi.id] || ""} onChange={e => setResponses(v => ({ ...v, [rfi.id]: e.target.value }))} rows={3} placeholder="Provide the requested information" /></label>
          {(documents[app.id] || []).length > 0 && <label className="employee-rfi-label">Attach an uploaded document (optional)<select value={responseDocuments[rfi.id] || ""} onChange={e => setResponseDocuments(v => ({ ...v, [rfi.id]: e.target.value }))}><option value="">No attachment</option>{(documents[app.id] || []).map(doc => <option key={doc.id} value={doc.id}>{doc.documentType} — {doc.fileName}</option>)}</select></label>}
          <label className="employee-rfi-label">Upload a new document (optional)<input type="text" value={responseDocumentTypes[rfi.id] ?? rfi.requiredDocumentType ?? ""} onChange={e => setResponseDocumentTypes(v => ({ ...v, [rfi.id]: e.target.value }))} placeholder="Document type" /><input type="file" accept=".pdf,.jpg,.jpeg,.png" onChange={e => setResponseFiles(v => ({ ...v, [rfi.id]: e.target.files?.[0] ?? null }))} /></label>
          <button type="button" disabled={busy === rfi.id} onClick={() => void submitResponse(rfi, app.id)}>{busy === rfi.id ? "Submitting…" : "Submit Response"}</button></>}</article>)}</section>; })}
  </div><style>{`.employee-status-tracker{display:flex;flex-wrap:wrap;gap:4px;margin-top:8px;max-width:380px}.employee-status-step{font-size:10px;padding:3px 6px;border:1px solid #d3dbe1;border-radius:10px;color:#687780}.employee-status-step.done{background:#e8f5ec;border-color:#a7d7b5;color:#206a38}.employee-status-step.current{background:#e8f3fb;border-color:#82b8dc;color:#005d91;font-weight:700}.employee-rfi-section{margin-top:28px}.employee-rfi-section h2{font-size:20px}.employee-rfi-card{border:1px solid #d9dee3;border-radius:8px;padding:16px;margin:12px 0}.employee-rfi-title{display:flex;justify-content:space-between;gap:12px;align-items:center}.employee-rfi-card p{margin:10px 0}.employee-rfi-label{display:flex;flex-direction:column;gap:6px;margin:12px 0}.employee-rfi-label textarea,.employee-rfi-label select{padding:10px;border:1px solid #c7cdd2;border-radius:6px;font:inherit}.employee-rfi-card button{padding:9px 14px;border:0;border-radius:5px;background:#006eaf;color:white;cursor:pointer}.employee-rfi-history{background:#f6f8f9;padding:8px 12px;border-radius:6px}`}</style></div>;
}
