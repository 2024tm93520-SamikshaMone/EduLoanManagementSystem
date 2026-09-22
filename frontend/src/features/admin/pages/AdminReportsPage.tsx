import { useEffect, useState } from "react";

import {
  getAdminReports,
  type AdminReports,
  type AdminReportFilters,
} from "../../../services/adminReportService";

import {
  getReportDepartments,
  getReportCourses,
  getReportColleges,
  type ReportDepartment,
  type ReportCourse,
  type ReportCollege,
} from "../../../services/adminReportMasterDataService";

import "../styles/AdminReportsPage.css";

const currentYear = new Date().getFullYear();

const years = Array.from(
  { length: 5 },
  (_, index) => currentYear - index
);

const formatCurrency = (amount: number) =>
  new Intl.NumberFormat("en-IN", {
    style: "currency",
    currency: "INR",
    maximumFractionDigits: 0,
  }).format(amount);

const formatNumber = (value: number) =>
  new Intl.NumberFormat("en-IN").format(value);

const formatStatus = (status: string) =>
  status
    .replace(/([A-Z])/g, " $1")
    .replace(/^./, (char) => char.toUpperCase());

export default function AdminReportsPage() {
  const [reports, setReports] =
    useState<AdminReports | null>(null);

  const [departments, setDepartments] =
    useState<ReportDepartment[]>([]);

  const [courses, setCourses] =
    useState<ReportCourse[]>([]);

  const [colleges, setColleges] =
    useState<ReportCollege[]>([]);

  const [filters, setFilters] =
    useState<AdminReportFilters>({
      year: currentYear,
    });

  const [selectedYear, setSelectedYear] =
    useState(currentYear.toString());

  const [selectedDepartment, setSelectedDepartment] =
    useState("");

  const [selectedCourse, setSelectedCourse] =
    useState("");

  const [selectedCollege, setSelectedCollege] =
    useState("");

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  /* =========================================================
     LOAD REPORTS
     ========================================================= */

  const loadReports = async (
    appliedFilters: AdminReportFilters = filters
  ) => {
    try {
      setLoading(true);
      setError("");

      const data = await getAdminReports(
        appliedFilters
      );

      setReports(data);
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Failed to load reports."
      );
    } finally {
      setLoading(false);
    }
  };

  /* =========================================================
     LOAD MASTER DATA
     ========================================================= */

  const loadMasterData = async () => {
    try {
      const [
        departmentData,
        courseData,
        collegeData,
      ] = await Promise.all([
        getReportDepartments(),
        getReportCourses(),
        getReportColleges(),
      ]);

      setDepartments(departmentData);
      setCourses(courseData);
      setColleges(collegeData);
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Failed to load report filters."
      );
    }
  };

  /* =========================================================
     INITIAL LOAD
     ========================================================= */

  useEffect(() => {
    const initializePage = async () => {
      await Promise.all([
        loadReports({
          year: currentYear,
        }),
        loadMasterData(),
      ]);
    };

    initializePage();

    // Initial page load only.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  /* =========================================================
     APPLY FILTERS
     ========================================================= */

  const handleApplyFilters = () => {
    const appliedFilters: AdminReportFilters = {};

    if (selectedYear) {
      appliedFilters.year =
        Number(selectedYear);
    }

    if (selectedDepartment) {
      appliedFilters.departmentId =
        Number(selectedDepartment);
    }

    if (selectedCourse) {
      appliedFilters.courseId =
        Number(selectedCourse);
    }

    if (selectedCollege) {
      appliedFilters.collegeId =
        Number(selectedCollege);
    }

    setFilters(appliedFilters);

    loadReports(appliedFilters);
  };

  /* =========================================================
     CLEAR FILTERS
     ========================================================= */

  const handleClearFilters = () => {
    const defaultFilters: AdminReportFilters = {
      year: currentYear,
    };

    setSelectedYear(
      currentYear.toString()
    );

    setSelectedDepartment("");
    setSelectedCourse("");
    setSelectedCollege("");

    setFilters(defaultFilters);

    loadReports(defaultFilters);
  };

  return (
    <main className="admin-reports-page">
      <div className="admin-reports-container">

        {/* ===================================================
            HEADER
        =================================================== */}

        <div className="admin-reports-header">
          <div>
            <h1>Reports</h1>

            <p>
              View education loan application and
              employee education insights.
            </p>
          </div>
        </div>

        {/* ===================================================
            FILTERS
        =================================================== */}

        <section className="admin-reports-filter-card">

          <div className="admin-reports-filter-title">
            Report Filters
          </div>

          <div className="admin-reports-filters">

            {/* Year */}

            <div className="admin-report-filter-group">

              <label>Year</label>

              <select
                value={selectedYear}
                onChange={(e) =>
                  setSelectedYear(
                    e.target.value
                  )
                }
              >
                <option value="">
                  All Years
                </option>

                {years.map((year) => (
                  <option
                    key={year}
                    value={year}
                  >
                    {year}
                  </option>
                ))}
              </select>

            </div>

            {/* Department */}

            <div className="admin-report-filter-group">

              <label>Department</label>

              <select
                value={selectedDepartment}
                onChange={(e) =>
                  setSelectedDepartment(
                    e.target.value
                  )
                }
              >
                <option value="">
                  All Departments
                </option>

                {departments.map(
                  (department) => (
                    <option
                      key={department.id}
                      value={department.id}
                    >
                      {department.name}
                    </option>
                  )
                )}
              </select>

            </div>

            {/* Course */}

            <div className="admin-report-filter-group">

              <label>Course</label>

              <select
                value={selectedCourse}
                onChange={(e) =>
                  setSelectedCourse(
                    e.target.value
                  )
                }
              >
                <option value="">
                  All Courses
                </option>

                {courses.map((course) => (
                  <option
                    key={course.id}
                    value={course.id}
                  >
                    {course.name}
                  </option>
                ))}
              </select>

            </div>

            {/* College */}

            <div className="admin-report-filter-group">

              <label>College</label>

              <select
                value={selectedCollege}
                onChange={(e) =>
                  setSelectedCollege(
                    e.target.value
                  )
                }
              >
                <option value="">
                  All Colleges
                </option>

                {colleges.map(
                  (college) => (
                    <option
                      key={college.id}
                      value={college.id}
                    >
                      {college.name}
                    </option>
                  )
                )}
              </select>

            </div>

            {/* Actions */}

            <div className="admin-reports-filter-actions">

              <button
                className="admin-reports-apply-btn"
                onClick={handleApplyFilters}
              >
                Apply
              </button>

              <button
                className="admin-reports-clear-btn"
                onClick={handleClearFilters}
              >
                Clear
              </button>

            </div>

          </div>
        </section>

        {/* ===================================================
            LOADING
        =================================================== */}

        {loading && (
          <div className="admin-reports-state">
            Loading reports...
          </div>
        )}

        {/* ===================================================
            ERROR
        =================================================== */}

        {!loading && error && (
          <div className="admin-reports-error">
            {error}
          </div>
        )}

        {/* ===================================================
            REPORT DATA
        =================================================== */}

        {!loading &&
          !error &&
          reports && (
            <>

              {/* Summary */}

              <section className="admin-reports-summary-grid">

                <div className="admin-report-summary-card">
                  <span>
                    Total Applications
                  </span>

                  <strong>
                    {formatNumber(
                      reports.summary
                        .totalApplications
                    )}
                  </strong>
                </div>

                <div className="admin-report-summary-card">
                  <span>Approved</span>

                  <strong>
                    {formatNumber(
                      reports.summary
                        .approvedApplications
                    )}
                  </strong>
                </div>

                <div className="admin-report-summary-card">
                  <span>Rejected</span>

                  <strong>
                    {formatNumber(
                      reports.summary
                        .rejectedApplications
                    )}
                  </strong>
                </div>

                <div className="admin-report-summary-card">
                  <span>Submitted</span>

                  <strong>
                    {formatNumber(
                      reports.summary
                        .submittedApplications
                    )}
                  </strong>
                </div>

                <div className="admin-report-summary-card">
                  <span>Total Employees</span>

                  <strong>
                    {formatNumber(
                      reports.summary
                        .totalEmployees
                    )}
                  </strong>
                </div>

                <div className="admin-report-summary-card">
                  <span>
                    Requested Amount
                  </span>

                  <strong>
                    {formatCurrency(
                      reports.summary
                        .totalRequestedAmount
                    )}
                  </strong>
                </div>

              </section>

              {/* =================================================
                  STATUS OVERVIEW
              ================================================= */}

              <section className="admin-report-section">

                <div className="admin-report-section-header">
                  <h2>
                    Application Status
                  </h2>
                </div>

                {reports.statusOverview.length ===
                  0 ? (
                  <div className="admin-report-empty">
                    No application data
                    available.
                  </div>
                ) : (
                  <div className="admin-report-status-grid">

                    {reports.statusOverview.map(
                      (item) => (
                        <div
                          className="admin-report-status-card"
                          key={item.status}
                        >
                          <span>
                            {formatStatus(
                              item.status
                            )}
                          </span>

                          <strong>
                            {formatNumber(
                              item.count
                            )}
                          </strong>
                        </div>
                      )
                    )}

                  </div>
                )}

              </section>

              {/* =================================================
                  DEPARTMENT
              ================================================= */}

              <section className="admin-report-section">

                <div className="admin-report-section-header">
                  <h2>
                    Department-wise Applications
                  </h2>
                </div>

                <div className="admin-report-table-card">

                  {reports.departmentOverview.length ===
                    0 ? (
                    <div className="admin-report-empty">
                      No department data
                      available.
                    </div>
                  ) : (
                    <table>

                      <thead>
                        <tr>
                          <th>
                            Department
                          </th>

                          <th>
                            Applications
                          </th>

                          <th>
                            Requested Amount
                          </th>
                        </tr>
                      </thead>

                      <tbody>

                        {reports.departmentOverview.map(
                          (item) => (
                            <tr
                              key={
                                item.departmentId
                              }
                            >
                              <td>
                                {
                                  item.departmentName
                                }
                              </td>

                              <td>
                                {formatNumber(
                                  item.applicationCount
                                )}
                              </td>

                              <td>
                                {formatCurrency(
                                  item.requestedAmount
                                )}
                              </td>
                            </tr>
                          )
                        )}

                      </tbody>

                    </table>
                  )}

                </div>

              </section>

              {/* =================================================
                  COURSE
              ================================================= */}

              <section className="admin-report-section">

                <div className="admin-report-section-header">
                  <h2>
                    Course-wise Applications
                  </h2>
                </div>

                <div className="admin-report-table-card">

                  {reports.courseOverview.length ===
                    0 ? (
                    <div className="admin-report-empty">
                      No course data
                      available.
                    </div>
                  ) : (
                    <table>

                      <thead>
                        <tr>
                          <th>Course</th>

                          <th>
                            Applications
                          </th>

                          <th>
                            Requested Amount
                          </th>
                        </tr>
                      </thead>

                      <tbody>

                        {reports.courseOverview.map(
                          (item) => (
                            <tr
                              key={
                                item.courseId
                              }
                            >
                              <td>
                                {item.courseName}
                              </td>

                              <td>
                                {formatNumber(
                                  item.applicationCount
                                )}
                              </td>

                              <td>
                                {formatCurrency(
                                  item.requestedAmount
                                )}
                              </td>
                            </tr>
                          )
                        )}

                      </tbody>

                    </table>
                  )}

                </div>

              </section>

              {/* =================================================
                  COLLEGE
              ================================================= */}

              <section className="admin-report-section">

                <div className="admin-report-section-header">
                  <h2>
                    College-wise Applications
                  </h2>
                </div>

                <div className="admin-report-table-card">

                  {reports.collegeOverview.length ===
                    0 ? (
                    <div className="admin-report-empty">
                      No college data
                      available.
                    </div>
                  ) : (
                    <table>

                      <thead>
                        <tr>
                          <th>College</th>

                          <th>
                            Applications
                          </th>

                          <th>
                            Requested Amount
                          </th>
                        </tr>
                      </thead>

                      <tbody>

                        {reports.collegeOverview.map(
                          (item) => (
                            <tr
                              key={
                                item.collegeId
                              }
                            >
                              <td>
                                {item.collegeName}
                              </td>

                              <td>
                                {formatNumber(
                                  item.applicationCount
                                )}
                              </td>

                              <td>
                                {formatCurrency(
                                  item.requestedAmount
                                )}
                              </td>
                            </tr>
                          )
                        )}

                      </tbody>

                    </table>
                  )}

                </div>

              </section>

            </>
          )}

      </div>
    </main>
  );
}