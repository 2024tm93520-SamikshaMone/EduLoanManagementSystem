import {
  BrowserRouter,
  Navigate,
  Route,
  Routes,
  useNavigate,
} from "react-router-dom";

import LoginPage from "./features/auth/LoginPage";
import ProfilePage from "./features/employee/ProfilePage";

import AdminMasterDataPage from "./features/admin/pages/AdminMasterDataPage";
import AdminDashboardPage from "./features/admin/pages/AdminDashboardPage";
import EligibilityRulesPage from "./features/admin/pages/EligibilityRulesPage";

import MyApplicationsPage from "./features/loans/pages/MyApplicationsPage";
import NewApplicationPage from "./features/loans/pages/NewApplicationPage";
import AdminLoanApplicationsPage from "./features/admin/pages/AdminLoanApplicationsPage";
import AdminEmployeesPage from "./features/admin/pages/AdminEmployeesPage";
import AdminReportsPage from "./features/admin/pages/AdminReportsPage";
import EmployeeNav from "./components/EmployeeNav";
import AdminNav from "./components/AdminNav";

import PasswordPage from "./features/auth/PasswordPage";

function LoginRoute() {
  const navigate = useNavigate();

  return (
    <LoginPage
      onLoginSuccess={(route) => navigate(route)}
    />
  );
}

/* =========================================================
   EMPLOYEE ROUTES
   ========================================================= */

function EmployeeProfileRoute() {
  return (
    <>
      <EmployeeNav />
      <ProfilePage />
    </>
  );
}

function EmployeeApplicationsRoute() {
  return (
    <>
      <EmployeeNav />
      <MyApplicationsPage />
    </>
  );
}

function NewApplicationRoute() {
  return (
    <>
      <EmployeeNav />
      <NewApplicationPage />
    </>
  );
}

/* =========================================================
   ADMIN ROUTES
   ========================================================= */

function AdminDashboardRoute() {
  return (
    <>
      <AdminNav />
      <AdminDashboardPage />
    </>
  );
}

function AdminMasterDataRoute() {
  return (
    <>
      <AdminNav />
      <AdminMasterDataPage />
    </>
  );
}

function AdminEligibilityRulesRoute() {
  return (
    <>
      <AdminNav />
      <EligibilityRulesPage />
    </>
  );
}

function AdminLoanApplicationsRoute() {
  return (
    <>
      <AdminNav />
      <AdminLoanApplicationsPage />
    </>
  );
}

function AdminReportsRoute() {
  return (
    <>
      <AdminNav />
      <AdminReportsPage />
    </>
  );
}

/* =========================================================
   APP
   ========================================================= */

export default function App() {
  return (
    <BrowserRouter>
      <Routes>

        {/* ==================== AUTH ==================== */}

        <Route
          path="/login"
          element={<LoginRoute />}
        />

        <Route
          path="/forgot-password"
          element={<PasswordPage />}
        />

        <Route
          path="/employee/change-password"
          element={<PasswordPage />}
        />

        {/* ==================== EMPLOYEE ==================== */}

        <Route
          path="/employee/dashboard"
          element={<EmployeeProfileRoute />}
        />

        <Route
          path="/employee/profile"
          element={<EmployeeProfileRoute />}
        />

        <Route
          path="/employee/applications"
          element={<EmployeeApplicationsRoute />}
        />

        <Route
          path="/employee/applications/new"
          element={<NewApplicationRoute />}
        />

        {/* ==================== ADMIN ==================== */}

        <Route
          path="/admin/dashboard"
          element={<AdminDashboardRoute />}
        />

        <Route
          path="/admin/master-data"
          element={<AdminMasterDataRoute />}
        />

        <Route
          path="/admin/eligibility-rules"
          element={<AdminEligibilityRulesRoute />}
        />

        <Route
          path="/admin/applications"
          element={<AdminLoanApplicationsRoute />}
        />

        <Route
          path="/admin/employees"
          element={
            <>
              <AdminNav />
              <AdminEmployeesPage />
            </>
          }
        />

        <Route
          path="/admin/reports"
          element={<AdminReportsRoute />}
        />

        {/* ==================== FALLBACK ==================== */}

        <Route
          path="*"
          element={<Navigate to="/login" replace />}
        />

      </Routes>
    </BrowserRouter>
  );
}