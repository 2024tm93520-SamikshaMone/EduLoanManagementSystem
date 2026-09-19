import { useEffect, useState } from "react";
import {
  LoanApplicationError,
  getMyApplications,
} from "../../../services/loanApplicationService";
import type { LoanApplicationSummary } from "../../../types/loanApplication";
import "../styles/MyApplicationsPage.css";

export default function MyApplicationsPage() {
  const [applications, setApplications] = useState<LoanApplicationSummary[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  function loadApplications() {
    setIsLoading(true);
    setLoadError(null);

    getMyApplications()
      .then(setApplications)
      .catch((err) =>
        setLoadError(
          err instanceof LoanApplicationError
            ? err.message
            : "Could not load your applications."
        )
      )
      .finally(() => setIsLoading(false));
  }

  useEffect(() => {
    loadApplications();
  }, []);

  return (
    <div className="admin-page">
      <div className="admin-card">
        <div className="master-data-header">
          <h1>My Loan Applications</h1>
        </div>

        {isLoading ? (
          <p role="status">Loading your applications...</p>
        ) : loadError ? (
          <div role="alert" className="form-error">
            {loadError}
          </div>
        ) : applications.length === 0 ? (
          <p className="master-data-empty">
            You haven't submitted any applications yet.
          </p>
        ) : (
          <table className="master-data-table">
            <thead>
              <tr>
                <th>Application No.</th>
                <th>College</th>
                <th>Course</th>
                <th>Amount</th>
                <th>Status</th>
              </tr>
            </thead>

            <tbody>
              {applications.map((app) => (
                <tr key={app.id}>
                  <td>{app.applicationNumber}</td>
                  <td>{app.collegeName}</td>
                  <td>{app.courseName}</td>
                  <td>
                    ₹{app.requestedAmount.toLocaleString("en-IN")}
                  </td>
                  <td>
                    <span
                      className={`status-badge status-${app.status.toLowerCase()}`}
                    >
                      {app.status}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}