import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";

import {
  getAdminDashboard,
  type AdminDashboardData,
} from "../../../services/adminDashboardService";


import "./../styles/AdminDashboardPage.css";

export default function AdminDashboardPage() {
  const navigate = useNavigate();

  const [dashboard, setDashboard] =
    useState<AdminDashboardData | null>(null);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    loadDashboard();
  }, []);

  async function loadDashboard() {
    try {
      setLoading(true);
      setError("");

      const data = await getAdminDashboard();

      setDashboard(data);
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Unable to load dashboard."
      );
    } finally {
      setLoading(false);
    }
  }

  /* =========================================================
     LOADING
     ========================================================= */

  if (loading) {
    return (
      <main className="admin-dashboard-page">
        <div className="admin-dashboard-inner">
          <div className="admin-dashboard-state">
            Loading dashboard...
          </div>
        </div>
      </main>
    );
  }

  /* =========================================================
     ERROR
     ========================================================= */

  if (error) {
    return (
      <main className="admin-dashboard-page">
        <div className="admin-dashboard-inner">
          <div className="admin-dashboard-error">
            <strong>Unable to load dashboard</strong>

            <p>{error}</p>

            <button
              type="button"
              className="admin-dashboard-retry-button"
              onClick={loadDashboard}
            >
              Retry
            </button>
          </div>
        </div>
      </main>
    );
  }

  if (!dashboard) {
    return (
      <main className="admin-dashboard-page">
        <div className="admin-dashboard-inner">
          <div className="admin-dashboard-state">
            No dashboard data available.
          </div>
        </div>
      </main>
    );
  }

  return (
    <main className="admin-dashboard-page">
      <div className="admin-dashboard-inner">

        {/* =====================================================
            HEADER
            ===================================================== */}

        <section className="admin-dashboard-heading">
          <div>
            <span className="admin-dashboard-welcome">
              Welcome back,
            </span>

            <span className="admin-dashboard-accent" />

            <h1>Admin Dashboard</h1>

            <p>
              Manage employee education loans and monitor
              application processing.
            </p>
          </div>

          <div className="admin-dashboard-decoration">
            Empower People.
            <br />
            Build Futures.
          </div>
        </section>

        {/* =====================================================
            SUMMARY CARDS
            ===================================================== */}

        <section className="admin-summary-grid">

          <SummaryCard
            icon="♧"
            title="Total Employees"
            value={dashboard.summary.totalEmployees.toLocaleString(
              "en-IN"
            )}
            description="Active employees in system"
            type="purple"
          />

          <SummaryCard
            icon="▱"
            title="Pending Applications"
            value={dashboard.summary.pendingApplications.toString()}
            description="Awaiting review"
            type="orange"
          />

          <SummaryCard
            icon="✓"
            title="Approved Applications"
            value={dashboard.summary.approvedApplications.toString()}
            description="Approved applications"
            type="green"
          />

          <SummaryCard
            icon="⌛"
            title="Pending Finance Confirmation"
            value={dashboard.summary.pendingFinanceConfirmation.toString()}
            description="Approved, pending finance confirmation"
            type="purple"
          />

        </section>

        {/* =====================================================
            LOWER CONTENT
            ===================================================== */}

        <section className="admin-dashboard-content">

          {/* ===================================================
              RECENT APPLICATIONS
              =================================================== */}

          <div className="admin-dashboard-card applications-card">

            <div className="admin-dashboard-card-header">

              <div className="admin-card-title">
                <span className="admin-card-title-icon">
                  ▣
                </span>

                <h2>Recent Loan Applications</h2>
              </div>

              <button
                type="button"
                className="admin-view-all"
                onClick={() =>
                  navigate("/admin/applications")
                }
              >
                View All
              </button>

            </div>

            <div className="admin-dashboard-table-wrapper">

              {dashboard.recentApplications.length === 0 ? (
                <div className="admin-dashboard-empty">
                  No recent loan applications available.
                </div>
              ) : (
                <table className="admin-dashboard-table">

                  <thead>
                    <tr>
                      <th>Application No.</th>
                      <th>Employee</th>
                      <th>College</th>
                      <th>Amount</th>
                      <th>Status</th>
                      <th>Actions</th>
                    </tr>
                  </thead>

                  <tbody>
                    {dashboard.recentApplications.map(
                      (application: any) => (
                        <tr key={application.id}>

                          <td>
                            <strong>
                              {application.applicationNumber}
                            </strong>
                          </td>

                          <td>
                            <div className="admin-employee-cell">

                              <span>
                                {application.employeeName}
                              </span>

                              <small>
                                {application.employeeCode}
                              </small>

                            </div>
                          </td>

                          <td>
                            {application.collegeName}
                          </td>

                          <td>
                            <strong>
                              ₹
                              {application.requestedAmount.toLocaleString(
                                "en-IN"
                              )}
                            </strong>
                          </td>

                          <td>
                            <span
                              className={`admin-status-badge ${getStatusClass(
                                application.status
                              )}`}
                            >
                              {formatStatus(
                                application.status
                              )}
                            </span>
                          </td>

                          <td>
                            <button
                              type="button"
                              className="admin-view-button"
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
                      )
                    )}
                  </tbody>

                </table>
              )}

            </div>

          </div>

          {/* ===================================================
              STATUS OVERVIEW
              =================================================== */}

          <div className="admin-dashboard-card status-card">

            <div className="admin-dashboard-card-header">

              <div className="admin-card-title">

                <span className="admin-card-title-icon">
                  ▥
                </span>

                <h2>
                  Application Status Overview
                </h2>

              </div>

            </div>

            <div className="admin-status-list">

              {dashboard.statusOverview.length === 0 ? (
                <div className="admin-dashboard-empty">
                  No application status data available.
                </div>
              ) : (
                dashboard.statusOverview.map((item: any) => (

                  <div
                    className="admin-status-row"
                    key={item.status}
                  >

                    <span className="admin-status-label">
                      {formatStatus(item.status)}
                    </span>

                    <div className="admin-status-progress">

                      <div
                        className={`admin-status-progress-bar ${getProgressClass(
                          item.status
                        )}`}
                        style={{
                          width: `${item.percentage}%`,
                        }}
                      />

                    </div>

                    <strong className="admin-status-count">
                      {item.count}
                    </strong>

                    <span className="admin-status-percentage">
                      {item.percentage}%
                    </span>

                  </div>

                ))
              )}

            </div>

            <div className="admin-dashboard-message">

              <span className="admin-dashboard-message-icon">
                ♧
              </span>

              <div>

                <strong>
                  Streamline Education Support
                </strong>

                <p>
                  Ensure a seamless loan process for
                  our employees and invest in their
                  future.
                </p>

              </div>

            </div>

          </div>

        </section>

      </div>
    </main>
  );
}

/* =========================================================
   STATUS HELPERS
   ========================================================= */

function normalizeStatus(status: string): string {
  return status
    .trim()
    .toLowerCase()
    .replace(/[\s_-]+/g, "");
}

function formatStatus(status: string): string {
  const normalized = normalizeStatus(status);

  switch (normalized) {
    case "underreview":
      return "Under Review";

    case "pendingfinance":
      return "Pending Finance";

    case "submitted":
      return "Submitted";

    case "approved":
      return "Approved";

    case "rejected":
      return "Rejected";

    case "draft":
      return "Draft";

    case "cancelled":
      return "Cancelled";

    default:
      return status;
  }
}

function getStatusClass(status: string): string {
  const normalized = normalizeStatus(status);

  switch (normalized) {
    case "submitted":
      return "submitted";

    case "underreview":
      return "under-review";

    case "approved":
      return "approved";

    case "pendingfinance":
      return "pending-finance";

    case "rejected":
      return "rejected";

    case "draft":
      return "draft";

    case "cancelled":
      return "cancelled";

    default:
      return "";
  }
}

function getProgressClass(status: string): string {
  const normalized = normalizeStatus(status);

  switch (normalized) {
    case "submitted":
      return "submitted";

    case "underreview":
      return "review";

    case "approved":
      return "approved";

    case "pendingfinance":
      return "finance";

    case "rejected":
      return "rejected";

    default:
      return "";
  }
}

/* =========================================================
   SUMMARY CARD
   ========================================================= */

interface SummaryCardProps {
  icon: string;
  title: string;
  value: string;
  description: string;
  type: "purple" | "orange" | "green";
}

function SummaryCard({
  icon,
  title,
  value,
  description,
  type,
}: SummaryCardProps) {
  return (
    <div className="admin-summary-card">

      <div
        className={`admin-summary-icon ${type}`}
      >
        {icon}
      </div>

      <div className="admin-summary-content">

        <span className="admin-summary-title">
          {title}
        </span>

        <strong className="admin-summary-value">
          {value}
        </strong>

        <span className="admin-summary-description">
          {description}
        </span>

      </div>

    </div>
  );
}