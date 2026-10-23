import { useEffect, useState } from "react";
import { getSimilarApplications, type SimilarApplicationsSummary } from "../../../services/similarApplicationsService";

interface Props {
  applicationId: string;
}

export default function SimilarApplicationsPanel({ applicationId }: Props) {
  const [summary, setSummary] = useState<SimilarApplicationsSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError("");
    getSimilarApplications(applicationId)
      .then(value => { if (active) setSummary(value); })
      .catch(reason => { if (active) setError(reason instanceof Error ? reason.message : "Could not load similar historical applications."); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [applicationId]);

  return (
    <section className="admin-detail-history" aria-labelledby="similar-cases-heading">
      <h2 id="similar-cases-heading">Similar Historical Applications (KNN)</h2>
      {loading && <p role="status">Comparing with historical applications...</p>}
      {error && <p role="alert" className="form-error">{error}</p>}
      {!loading && !error && summary && (
        <>
          <h3>Eligibility rule results</h3>
          {summary.eligibilityEvaluations.length === 0
            ? <p className="master-data-empty">No saved rule evaluation is available yet. Eligibility rules are evaluated by the server during submission.</p>
            : <ul>{summary.eligibilityEvaluations.map((rule, index) => (
                <li key={`${rule.ruleName}-${rule.evaluatedAt}-${index}`}>
                  <strong>{rule.ruleName}</strong> ({rule.ruleCategory}) — {rule.passed ? "Passed" : "Failed"}; evaluated value: {rule.evaluatedValue}; severity: {rule.severity}.
                  {!rule.passed && rule.failureMessage && <> Reason: {rule.failureMessage}</>}
                </li>
              ))}</ul>}
          <h3>Historical similarity (KNN)</h3>
          <p>{summary.summary}</p>
          <p>
            Historical final outcomes available: <strong>{summary.historicalCaseCount}</strong>.
            {summary.hasSufficientHistory && summary.approvedWeightedPercent !== null && summary.rejectedWeightedPercent !== null && (
              <> Similar-case weighted outcomes: <strong>{summary.approvedWeightedPercent}% approved</strong> and <strong>{summary.rejectedWeightedPercent}% rejected</strong>.</>
            )}
          </p>
          {!summary.hasSufficientHistory && <p className="master-data-empty">Insufficient historical data. Complete the normal manual review; KNN does not make the decision.</p>}
          {summary.neighbors.length > 0 && (
            <ul>
              {summary.neighbors.map(item => (
                <li key={item.applicationId}>
                  <strong>{item.applicationNumber}</strong> — {item.outcome}; {item.collegeName}, {item.courseName}; fees ₹{item.totalEducationFees.toLocaleString("en-IN")}; requested ₹{item.requestedAmount.toLocaleString("en-IN")}; {item.courseDurationMonths} months; similarity {item.similarityPercent}%.
                </li>
              ))}
            </ul>
          )}
          <p className="history-comment">KNN is decision support only. It uses education/fee attributes and past Approved/Rejected outcomes; it does not override blocking eligibility rules or replace the Admin's decision.</p>
        </>
      )}
    </section>
  );
}
