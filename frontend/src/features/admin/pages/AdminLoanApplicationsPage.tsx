import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  getAdminLoanApplications,
  type AdminLoanApplication,
} from "../../../services/adminLoanApplicationService";
import "./../styles/AdminLoanApplicationsPage.css";

function formatAmount(amount: number) {
  return new Intl.NumberFormat("en-IN", {
    style: "currency",
    currency: "INR",
    maximumFractionDigits: 0,
  }).format(amount);
}

function formatDate(value: string | null) {
  if (!value) return "-";

  return new Date(value).toLocaleDateString("en-IN", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  });
}

function getStatusClass(status: string) {
  return `admin-loan-status ${status.toLowerCase()}`;
}

export default function AdminLoanApplicationsPage() {
  const navigate = useNavigate();

  const [applications, setApplications] = useState<
    AdminLoanApplication[]
  >([]);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  async function loadApplications() {
    try {
      setLoading(true);
      setError("");

      const data = await getAdminLoanApplications();

      setApplications(data);
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Failed to load loan applications."
      );
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadApplications();
  }, []);

  return (
    <div className="admin-loan-applications-page">
      <div className="admin-loan-applications-inner">

        <div className="admin-loan-applications-header">
          <div>
            <h1>Loan Applications</h1>
            <p>
              View and manage employee education loan applications.
            </p>
          </div>
        </div>

        {loading && (
          <div className="admin-loan-state">
            Loading loan applications...
          </div>
        )}

        {!loading && error && (
          <div className="admin-loan-error">
            <strong>Unable to load applications</strong>
            <p>{error}</p>

            <button
              type="button"
              onClick={loadApplications}
            >
              Retry
            </button>
          </div>
        )}

        {!loading && !error && applications.length === 0 && (
          <div className="admin-loan-state">
            No loan applications found.
          </div>
        )}

        {!loading && !error && applications.length > 0 && (
          <div className="admin-loan-table-card">

            <table className="admin-loan-table">
              <thead>
                <tr>
                  <th>Application No.</th>
                  <th>Employee</th>
                  <th>Department</th>
                  <th>College</th>
                  <th>Course</th>
                  <th>Requested Amount</th>
                  <th>Submitted Date</th>
                  <th>Status</th>
                  <th>Action</th>
                </tr>
              </thead>

              <tbody>
                {applications.map((application) => (
                  <tr key={application.id}>

                    <td className="application-number">
                      {application.applicationNumber}
                    </td>

                    <td>
                      <div className="employee-name">
                        {application.employeeName}
                      </div>

                      <div className="employee-code">
                        {application.employeeCode}
                      </div>
                    </td>

                    <td>
                      {application.departmentName}
                    </td>

                    <td>
                      {application.collegeName}
                    </td>

                    <td>
                      {application.courseName}
                    </td>

                    <td>
                      {formatAmount(
                        application.requestedAmount
                      )}
                    </td>

                    <td>
                      {formatDate(
                        application.submittedAt
                      )}
                    </td>

                    <td>
                      <span
                        className={getStatusClass(
                          application.status
                        )}
                      >
                        {application.status}
                      </span>
                    </td>

                    <td>
                      <button
                        type="button"
                        className="admin-loan-view-button"
                        onClick={() =>
                          navigate(
                            `/admin/applications/${application.id}`
                          )
                        }
                      >
                        View
                      </button>
                    </td>

                  </tr>
                ))}
              </tbody>
            </table>

          </div>
        )}
      </div>
    </div>
  );
}