import { useEffect, useState } from "react";
import "../styles/AdminEmployeesPage.css"

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ??
  "https://localhost:7001/api/v1";

interface AdminEmployee {
  id: string;
  employeeCode: string;
  fullName: string;
  email: string;
  departmentName: string | null;
  designation: string | null;
  dateOfJoining: string;
  monthlySalary: number;
  phoneNumber: string | null;
  isActive: boolean;
}

function formatSalary(value: number) {
  return new Intl.NumberFormat("en-IN", {
    style: "currency",
    currency: "INR",
    maximumFractionDigits: 0,
  }).format(value);
}

function formatDate(value: string) {
  if (!value) return "-";

  return new Date(value).toLocaleDateString("en-IN", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  });
}

export default function AdminEmployeesPage() {
  const [employees, setEmployees] = useState<AdminEmployee[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  async function loadEmployees() {
    try {
      setLoading(true);
      setError("");

      const token = sessionStorage.getItem(
        "eduloan_access_token"
      );

      const response = await fetch(
        `${API_BASE_URL}/admin/employees`,
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );

      const text = await response.text();

      if (!response.ok) {
        throw new Error(
          text || `Request failed with status ${response.status}`
        );
      }

      if (!text) {
        throw new Error("Empty response received from server.");
      }

      const result = JSON.parse(text);

      if (!result.success) {
        throw new Error(
          result.message || "Failed to load employees."
        );
      }

      setEmployees(result.data ?? []);
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Failed to load employees."
      );
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadEmployees();
  }, []);

  return (
    <div className="admin-employees-page">
      <div className="admin-employees-inner">

        <div className="admin-employees-header">
          <div>
            <h1>Employees</h1>
            <p>
              View employee information and education loan details.
            </p>
          </div>

          <div className="admin-employees-count">
            {employees.length} Employees
          </div>
        </div>

        {loading && (
          <div className="admin-employees-state">
            Loading employees...
          </div>
        )}

        {!loading && error && (
          <div className="admin-employees-error">
            <strong>Unable to load employees</strong>

            <p>{error}</p>

            <button
              type="button"
              onClick={loadEmployees}
            >
              Retry
            </button>
          </div>
        )}

        {!loading &&
          !error &&
          employees.length === 0 && (
            <div className="admin-employees-state">
              No employees found.
            </div>
          )}

        {!loading &&
          !error &&
          employees.length > 0 && (
            <div className="admin-employees-table-card">
              <table className="admin-employees-table">

                <thead>
                  <tr>
                    <th>Employee Code</th>
                    <th>Employee</th>
                    <th>Department</th>
                    <th>Designation</th>
                    <th>Date of Joining</th>
                    <th>Monthly Salary</th>
                    <th>Status</th>
                  </tr>
                </thead>

                <tbody>
                  {employees.map((employee) => (
                    <tr key={employee.id}>

                      <td>
                        <span className="admin-employee-code">
                          {employee.employeeCode}
                        </span>
                      </td>

                      <td>
                        <div className="admin-employee-name">
                          {employee.fullName}
                        </div>

                        <div className="admin-employee-email">
                          {employee.email}
                        </div>
                      </td>

                      <td>
                        {employee.departmentName || "-"}
                      </td>

                      <td>
                        {employee.designation || "-"}
                      </td>

                      <td>
                        {formatDate(employee.dateOfJoining)}
                      </td>

                      <td>
                        {formatSalary(employee.monthlySalary)}
                      </td>

                      <td>
                        <span
                          className={
                            employee.isActive
                              ? "admin-employee-status active"
                              : "admin-employee-status inactive"
                          }
                        >
                          {employee.isActive
                            ? "Active"
                            : "Inactive"}
                        </span>
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