import { useCallback, useEffect, useRef, useState } from "react";
import { getPersistedConsistencyFlags, updateConsistencyFlagStatus, type PersistedConsistencyFlag, type ConsistencyFlagStatus } from "../../../services/applicationConsistencyService";

interface Props { applicationId: string; }
type StatusFilter = "All" | ConsistencyFlagStatus;

export default function ConsistencyFlagReviewPanel({ applicationId }: Props) {
  const [flags, setFlags] = useState<PersistedConsistencyFlag[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState("");
  const [savingId, setSavingId] = useState<string | null>(null);
  const [notes, setNotes] = useState<Record<string, string>>({});
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("All");
  const [severityFilter, setSeverityFilter] = useState("All");
  const [page, setPage] = useState(1);
  const [pageInfo, setPageInfo] = useState({ page: 1, pageSize: 10, totalCount: 0, totalPages: 0 });
  const requestSequence = useRef(0);
  const mounted = useRef(true);

  useEffect(() => {
    mounted.current = true;
    return () => {
      mounted.current = false;
      requestSequence.current += 1;
    };
  }, []);

  const refresh = useCallback(async (requestedPage = page, quiet = false) => {
    const sequence = ++requestSequence.current;
    if (quiet) setRefreshing(true); else setLoading(true);
    setError("");
    try {
      const result = await getPersistedConsistencyFlags(applicationId, {
        status: statusFilter, severity: severityFilter, page: requestedPage, pageSize: 10,
      });
      if (sequence !== requestSequence.current) return;
      // A filter can shrink the result set while the user is on a later page.
      if (result.totalPages > 0 && requestedPage > result.totalPages) {
        setPage(result.totalPages);
        return;
      }
      if (result.totalPages === 0 && requestedPage !== 1) {
        setPage(1);
        return;
      }
      setFlags(result.items);
      setPageInfo({ page: result.page, pageSize: result.pageSize, totalCount: result.totalCount, totalPages: result.totalPages });
    } catch (e) {
      if (sequence === requestSequence.current) {
        setError(e instanceof Error ? e.message : "Could not load persisted consistency flags.");
        setFlags([]);
      }
    } finally {
      if (sequence === requestSequence.current && mounted.current) {
        setLoading(false);
        setRefreshing(false);
      }
    }
  }, [applicationId, statusFilter, severityFilter, page]);

  useEffect(() => { void refresh(page); }, [refresh, page]);

  const changeStatus = async (flag: PersistedConsistencyFlag, status: ConsistencyFlagStatus) => {
    if (savingId) return;
    const note = notes[flag.id]?.trim();
    if ((status === "Resolved" || status === "Dismissed") && !note) {
      setError("Resolution or dismissal notes are required."); return;
    }
    setSavingId(flag.id); setError("");
    try {
      await updateConsistencyFlagStatus(flag.id, status, note);
      setNotes(current => ({ ...current, [flag.id]: "" }));
      await refresh(page, true);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Could not update flag status.");
    } finally { setSavingId(null); }
  };

  const changeStatusFilter = (value: StatusFilter) => { setPage(1); setStatusFilter(value); };
  const changeSeverityFilter = (value: string) => { setPage(1); setSeverityFilter(value); };

  return <section className="admin-detail-history consistency-flag-panel" aria-labelledby="persisted-consistency-heading">
    <div className="consistency-panel-heading">
      <div>
        <h2 id="persisted-consistency-heading">Persisted Consistency Flag Review</h2>
        <p className="consistency-panel-subtitle">Review deterministic data-consistency signals. Flags do not decide loan approval.</p>
      </div>
      <button type="button" className="consistency-refresh-button" onClick={() => void refresh(page, true)} disabled={loading || refreshing}>
        {refreshing ? "Refreshing…" : "Refresh flags"}
      </button>
    </div>

    <div className="consistency-filter-toolbar" aria-label="Filter consistency flags">
      <div className="consistency-filter-field">
        <label htmlFor="consistency-status-filter">Status</label>
        <select id="consistency-status-filter" value={statusFilter} onChange={e => changeStatusFilter(e.target.value as StatusFilter)} disabled={loading}>
          <option value="All">All statuses</option><option value="Open">Open</option><option value="UnderInvestigation">Under investigation</option><option value="Resolved">Resolved</option><option value="Dismissed">Dismissed</option>
        </select>
      </div>
      <div className="consistency-filter-field">
        <label htmlFor="consistency-severity-filter">Severity</label>
        <select id="consistency-severity-filter" value={severityFilter} onChange={e => changeSeverityFilter(e.target.value)} disabled={loading}>
          <option value="All">All severities</option><option value="High">High</option><option value="Medium">Medium</option><option value="Low">Low</option>
        </select>
      </div>
    </div>

    {error && <p role="alert" className="form-error consistency-error">{error}</p>}
    {loading && <p role="status" className="consistency-state">Loading persisted flags…</p>}
    {!loading && !error && flags.length === 0 && <p role="status" className="consistency-state">No persisted flags match the selected filters.</p>}

    {!loading && flags.map(flag => <article key={flag.id} className="consistency-flag-card">
      <div className="consistency-flag-card-heading">
        <h3>{flag.title}</h3>
        <span className={`consistency-severity severity-${flag.severity.toLowerCase()}`}>{flag.severity}</span>
      </div>
      <p className="consistency-flag-description">{flag.description}</p>
      <dl className="consistency-flag-metadata">
        <div><dt>Rule</dt><dd><code>{flag.ruleCode}</code></dd></div>
        <div><dt>Status</dt><dd><span className={`consistency-status status-${flag.status.toLowerCase()}`}>{flag.status === "UnderInvestigation" ? "Under investigation" : flag.status}</span></dd></div>
        <div><dt>Detected</dt><dd>{new Date(flag.detectedAtUtc).toLocaleString()}</dd></div>
      </dl>
      {flag.resolutionNotes && <p className="consistency-existing-notes"><strong>Reviewer notes:</strong> {flag.resolutionNotes}</p>}
      {(flag.status === "Open" || flag.status === "UnderInvestigation") && <>
        <div className="consistency-notes-field">
          <label htmlFor={`flag-notes-${flag.id}`}>Review / resolution notes</label>
          <textarea id={`flag-notes-${flag.id}`} value={notes[flag.id] ?? ""} onChange={e => setNotes(old => ({ ...old, [flag.id]: e.target.value }))} maxLength={1000} rows={3} placeholder="Add context for this review…" aria-describedby={`flag-notes-help-${flag.id}`} disabled={savingId !== null} />
          <span id={`flag-notes-help-${flag.id}`} className="consistency-field-help">Required to resolve or dismiss this flag.</span>
        </div>
        <div className="consistency-review-actions">
          {flag.status === "Open" && <button type="button" className="consistency-button secondary" disabled={savingId !== null} onClick={() => void changeStatus(flag, "UnderInvestigation")}>{savingId === flag.id ? "Saving…" : "Acknowledge / Investigate"}</button>}
          <button type="button" className="consistency-button primary" disabled={savingId !== null} onClick={() => void changeStatus(flag, "Resolved")}>{savingId === flag.id ? "Saving…" : "Resolve"}</button>
          <button type="button" className="consistency-button danger" disabled={savingId !== null} onClick={() => void changeStatus(flag, "Dismissed")}>{savingId === flag.id ? "Saving…" : "Dismiss as false positive"}</button>
        </div>
      </>}
    </article>)}

    {!loading && !error && <nav aria-label="Consistency flag pages" className="consistency-pagination">
      <span className="consistency-page-summary">
        {`Page ${pageInfo.totalPages === 0 ? 0 : pageInfo.page} of ${pageInfo.totalPages} · ${pageInfo.totalCount} ${pageInfo.totalCount === 1 ? "flag" : "flags"}`}
      </span>
      <div className="consistency-pagination-actions">
        <button type="button" aria-label="Previous page" className="consistency-button secondary" onClick={() => setPage(current => Math.max(1, current - 1))} disabled={loading || pageInfo.page <= 1}>Previous</button>
        <button type="button" aria-label="Next page" className="consistency-button secondary" onClick={() => setPage(current => Math.min(pageInfo.totalPages, current + 1))} disabled={loading || pageInfo.totalPages === 0 || pageInfo.page >= pageInfo.totalPages}>Next</button>
      </div>
    </nav>}
  </section>;
}
