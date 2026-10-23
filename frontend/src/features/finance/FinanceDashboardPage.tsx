import { useCallback, useEffect, useState } from "react";
import "./FinanceDashboardPage.css";

type FinanceRow = {
  id: string; applicationNumber: string; employeeName: string; employeeCode: string;
  requestedAmount: number; totalEducationFees: number; status: string; submittedAt: string | null; updatedAt: string | null;
  finance: null | { status: string; receivedAtUtc?: string | null; bankReferenceNumber?: string; bankSubmissionDateUtc?: string; confirmationReference?: string; confirmationDateUtc?: string; processedAmount?: number; financeComments?: string; failureReason?: string };
};
type ActionPayload = { bankReferenceNumber?: string; bankSubmissionDateUtc?: string; processedAmount?: number; confirmationReference?: string; confirmationDateUtc?: string; comments?: string; failureReason?: string };
const API = (import.meta as ImportMeta & { env?: { VITE_API_BASE_URL?: string } }).env?.VITE_API_BASE_URL ?? "https://localhost:7001/api/v1";
const money = (n: number) => new Intl.NumberFormat("en-IN", { style: "currency", currency: "INR", maximumFractionDigits: 0 }).format(n);

async function request<T>(path: string, method = "GET", body?: unknown): Promise<T> {
  const token = sessionStorage.getItem("eduloan_access_token");
  const response = await fetch(`${API}${path}`, { method, headers: { ...(token ? { Authorization: `Bearer ${token}` } : {}), ...(body ? { "Content-Type": "application/json" } : {}) }, ...(body ? { body: JSON.stringify(body) } : {}) });
  const result = await response.json();
  if (!response.ok || !result.success) throw new Error(result.message || "Finance request failed.");
  return result.data as T;
}

export default function FinanceDashboardPage() {
  const [rows, setRows] = useState<FinanceRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [busyId, setBusyId] = useState("");
  const [comments, setComments] = useState<Record<string, string>>({});
  const [bankRefs, setBankRefs] = useState<Record<string, string>>({});
  const [bankDates, setBankDates] = useState<Record<string, string>>({});
  const [confirmationRefs, setConfirmationRefs] = useState<Record<string, string>>({});
  const [confirmationDates, setConfirmationDates] = useState<Record<string, string>>({});
  const [amounts, setAmounts] = useState<Record<string, string>>({});
  const [failures, setFailures] = useState<Record<string, string>>({});

  const load = useCallback(async () => {
    setLoading(true); setError("");
    try { setRows(await request<FinanceRow[]>("/module6/finance/applications")); }
    catch (e) { setError(e instanceof Error ? e.message : "Unable to load Finance applications."); }
    finally { setLoading(false); }
  }, []);
  useEffect(() => { void load(); }, [load]);

  async function action(row: FinanceRow, kind: "received" | "send-to-bank" | "confirm" | "failed") {
    setBusyId(row.id); setError("");
    const payload: ActionPayload = { comments: comments[row.id] || "" };
    if (kind === "send-to-bank") { payload.bankReferenceNumber = bankRefs[row.id] || ""; payload.bankSubmissionDateUtc = bankDates[row.id] || ""; }
    if (kind === "confirm") { payload.confirmationReference = confirmationRefs[row.id] || ""; payload.confirmationDateUtc = confirmationDates[row.id] || ""; payload.processedAmount = Number(amounts[row.id]); }
    if (kind === "failed") payload.failureReason = failures[row.id] || "";
    try { await request(`/module6/finance/applications/${row.id}/${kind}`, "POST", payload); await load(); }
    catch (e) { setError(e instanceof Error ? e.message : "Finance action failed."); }
    finally { setBusyId(""); }
  }

  const count = (status: string) => rows.filter(x => x.finance?.status === status).length;
  return <main className="finance-page"><header className="finance-header"><div><p className="finance-eyebrow">EMPLOYEE EDUCATION LOAN MANAGEMENT</p><h1>Finance Handoff</h1><p>Review approved applications and record external bank processing. This system does not transfer funds.</p></div><button onClick={() => void load()} disabled={loading}>Refresh</button></header>
    <section className="finance-cards"><article><span>Awaiting Finance Action</span><strong>{rows.filter(x => !x.finance || x.finance.status === "Pending Finance Review").length}</strong></article><article><span>Awaiting Bank Confirmation</span><strong>{count("Awaiting Bank Confirmation")}</strong></article><article><span>Processing Confirmed</span><strong>{count("Processing Confirmed")}</strong></article><article><span>Processing Failed</span><strong>{count("Processing Failed")}</strong></article></section>
    {error && <div className="finance-error" role="alert">{error}</div>}{loading && <p>Loading Finance applications…</p>}{!loading && !error && rows.length === 0 && <div className="finance-empty">No approved applications are currently awaiting or undergoing Finance processing.</div>}
    <section className="finance-list">{rows.map(row => { const status = row.finance?.status || "Pending Finance Review"; return <article className="finance-application" key={row.id}><div className="finance-application-head"><div><h2>{row.applicationNumber}</h2><p>{row.employeeName} · {row.employeeCode}</p></div><span className={`finance-status ${status.toLowerCase().replace(/ /g, "-")}`}>{status}</span></div><div className="finance-facts"><div><span>Approved/requested amount</span><strong>{money(row.requestedAmount)}</strong></div><div><span>Education fees</span><strong>{money(row.totalEducationFees)}</strong></div><div><span>Bank reference</span><strong>{row.finance?.bankReferenceNumber || "Not submitted"}</strong></div><div><span>Confirmation reference</span><strong>{row.finance?.confirmationReference || "Pending"}</strong></div></div>
      <label>Finance comments<textarea value={comments[row.id] || ""} onChange={e => setComments(v => ({ ...v, [row.id]: e.target.value }))} placeholder="Add an internal processing note" /></label>
      {(!row.finance || !row.finance.receivedAtUtc) && <div className="finance-action"><button disabled={busyId === row.id} onClick={() => void action(row, "received")}>Mark Received by Finance</button></div>}
      {status === "Pending Finance Review" && row.finance?.receivedAtUtc && <div className="finance-action"><div className="finance-inline"><label>Bank reference<input value={bankRefs[row.id] || ""} onChange={e => setBankRefs(v => ({ ...v, [row.id]: e.target.value }))} /></label><label>Submission date<input type="date" value={bankDates[row.id] || ""} onChange={e => setBankDates(v => ({ ...v, [row.id]: e.target.value }))} /></label><button disabled={busyId === row.id} onClick={() => void action(row, "send-to-bank")}>Record Send to Bank</button></div></div>}
      {status === "Awaiting Bank Confirmation" && <><div className="finance-inline"><label>Confirmation reference<input value={confirmationRefs[row.id] || ""} onChange={e => setConfirmationRefs(v => ({ ...v, [row.id]: e.target.value }))} /></label><label>Confirmation date<input type="date" value={confirmationDates[row.id] || ""} onChange={e => setConfirmationDates(v => ({ ...v, [row.id]: e.target.value }))} /></label><label>Processed amount (₹)<input type="number" min="1" max={row.requestedAmount} value={amounts[row.id] || ""} onChange={e => setAmounts(v => ({ ...v, [row.id]: e.target.value }))} /></label><button disabled={busyId === row.id} onClick={() => void action(row, "confirm")}>Record Processing Confirmation</button></div><label>Failure reason<textarea value={failures[row.id] || ""} onChange={e => setFailures(v => ({ ...v, [row.id]: e.target.value }))} placeholder="Required only if bank processing failed" /></label><button className="finance-danger" disabled={busyId === row.id} onClick={() => void action(row, "failed")}>Record Processing Failed</button></>}
      {status === "Processing Confirmed" && <p className="finance-success">External processing confirmation recorded on {row.finance?.confirmationDateUtc ? new Date(row.finance.confirmationDateUtc).toLocaleDateString() : "date unavailable"}. No disbursement or repayment is managed here.</p>}
      {status === "Processing Failed" && <p className="finance-error">{row.finance?.failureReason || "Bank processing failed."}</p>}
    </article>; })}</section>
  </main>;
}
