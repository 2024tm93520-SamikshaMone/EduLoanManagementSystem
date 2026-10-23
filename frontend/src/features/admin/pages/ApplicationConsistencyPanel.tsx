import { useEffect, useState } from "react";
import { getApplicationConsistencyChecks, type ApplicationConsistencySummary } from "../../../services/applicationConsistencyService";

interface Props { applicationId: string; }

export default function ApplicationConsistencyPanel({ applicationId }: Props) {
  const [summary, setSummary] = useState<ApplicationConsistencySummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError("");
    getApplicationConsistencyChecks(applicationId)
      .then(value => { if (active) setSummary(value); })
      .catch(reason => { if (active) setError(reason instanceof Error ? reason.message : "Could not load consistency checks."); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [applicationId]);

  return (
    <section className="admin-detail-history" aria-labelledby="consistency-checks-heading">
      <h2 id="consistency-checks-heading">Application Consistency Checks</h2>
      {loading && <p role="status">Checking application data and required education documents...</p>}
      {error && <p role="alert" className="form-error">{error}</p>}
      {!loading && !error && summary && (
        <>
          {summary.flags.length === 0
            ? <p role="status">No consistency flags were found by the current checks.</p>
            : <ul>{summary.flags.map(flag => (
                <li key={flag.code}>
                  <strong>{flag.title}</strong> <span>({flag.severity})</span> — {flag.description}
                </li>
              ))}</ul>}
          {summary.potentialDuplicates.length > 0 && (
            <>
              <h3>Potential matching applications</h3>
              <ul>{summary.potentialDuplicates.map(item => (
                <li key={item.applicationId}><strong>{item.applicationNumber}</strong> — {item.status}</li>
              ))}</ul>
            </>
          )}
          <p className="history-comment">{summary.disclaimer}</p>
        </>
      )}
    </section>
  );
}
