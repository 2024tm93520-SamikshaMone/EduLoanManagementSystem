import {
  BrowserRouter,
  Link,
  Navigate,
  Route,
  Routes,
  useNavigate,
} from "react-router-dom";

import LoginPage from "./features/auth/pages/LoginPage";
import ProfilePage from "./features/employee/pages/ProfilePage";
import DocumentsPage from "./features/employee/pages/DocumentsPage";
import HelpSupportPage from "./HelpSupportPage";

import AdminMasterDataPage from "./features/admin/pages/AdminMasterDataPage";
import AdminDashboardPage from "./features/admin/pages/AdminDashboardPage";
import EligibilityRulesPage from "./features/admin/pages/EligibilityRulesPage";
import AdminLoanApplicationsPage from "./features/admin/pages/AdminLoanApplicationsPage";
import AdminLoanApplicationDetailPage from "./features/admin/pages/AdminLoanApplicationDetailPage";
import AdminEmployeesPage from "./features/admin/pages/AdminEmployeesPage";
import AdminReportsPage from "./features/admin/pages/AdminReportsPage";

import MyApplicationsPage from "./features/loans/pages/MyApplicationsPage";
import NewApplicationPage from "./features/loans/pages/NewApplicationPage";

import EmployeeNav from "./components/pages/EmployeeNav";
import AdminNav from "./components/pages/AdminNav";

import PasswordPage from "./features/auth/pages/PasswordPage";
import FinanceDashboardPage from "./features/finance/FinanceDashboardPage";
import RepaymentSimulationPage from "./features/finance/RepaymentSimulationPage";
import FinanceNav from "./components/pages/FinanceNav";
import DocumentRequirementsPage from "./features/admin/pages/DocumentRequirementsPage";
import NotificationsPage from "./features/shared/NotificationsPage";
import RoleDashboardPage from "./features/shared/RoleDashboardPage";
import AuditTrailPage from "./features/admin/pages/AuditTrailPage";


/* =========================================================
   LOGIN
   ========================================================= */

function LoginRoute() {
  const navigate = useNavigate();

  return (
    <LoginPage
      onLoginSuccess={(route: string) => navigate(route)}
    />
  );
}

/* =========================================================
   EMPLOYEE ROUTES
   ========================================================= */

function EmployeeDashboardRoute() { return <><EmployeeNav /><RoleDashboardPage /></>; }

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

function EmployeeDocumentsRoute() {
  return (<>
    <EmployeeNav />
    <DocumentsPage />
  </>);
}

function EmployeeHelpSupportRoute() {
  return (
    <>
      <EmployeeNav />
      <HelpSupportPage />
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

function AdminLoanApplicationDetailRoute() {
  return (
    <>
      <AdminNav />
      <AdminLoanApplicationDetailPage />
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

function AdminHelpSupportRoute() {
  return (
    <>
      <AdminNav />
      <HelpSupportPage />
    </>
  );
}

function FinanceNotificationsRoute() {
  return (
    <div className="finance-notifications-shell">
      <FinanceNav />
      <NotificationsPage />
    </div>
  );
}

function FinanceHelpSupportRoute() {
  return (
    <div className="finance-help-shell">
      <FinanceNav />
      <HelpSupportPage />
    </div>
  );
}

/* =========================================================
   APP
   ========================================================= */

export default function App() {
  return (
    <BrowserRouter>
      <Routes>

        {/* =====================================================
           AUTH
           ===================================================== */}

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

        {/* =====================================================
           EMPLOYEE
           ===================================================== */}

        <Route
          path="/employee/dashboard"
          element={<EmployeeDashboardRoute />}
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

        <Route path="/employee/documents" element={<EmployeeDocumentsRoute />} />

        <Route path="/employee/notifications" element={<><EmployeeNav /><NotificationsPage /></>} />

        <Route
          path="/employee/help"
          element={<EmployeeHelpSupportRoute />}
        />

        <Route path="/hr/dashboard" element={<><AdminNav /><RoleDashboardPage /></>} />
        <Route path="/finance/dashboard" element={<><FinanceNav /><FinanceDashboardPage /></>} />
        <Route path="/finance/repayment-simulation" element={<><FinanceNav /><RepaymentSimulationPage /></>} />
        <Route path="/finance/notifications" element={<FinanceNotificationsRoute />} />
        <Route path="/finance/help" element={<FinanceHelpSupportRoute />} />
        <Route path="/finance/profile" element={<><FinanceNav /><ProfilePage /></>} />
        <Route path="/finance/change-password" element={<><FinanceNav /><PasswordPage /></>} />

        {/* =====================================================
           ADMIN
           ===================================================== */}

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
          path="/admin/document-requirements"
          element={<><AdminNav /><DocumentRequirementsPage /></>}
        />

        <Route
          path="/admin/applications"
          element={<AdminLoanApplicationsRoute />}
        />

        <Route
          path="/admin/applications/:id"
          element={<AdminLoanApplicationDetailRoute />}
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

        <Route path="/admin/notifications" element={<><AdminNav /><NotificationsPage /></>} />
        <Route path="/admin/audit" element={<><AdminNav /><AuditTrailPage /></>} />

        <Route
          path="/admin/help"
          element={<AdminHelpSupportRoute />}
        />

        {/* =====================================================
           FALLBACK
           ===================================================== */}

        <Route
          path="*"
          element={<Navigate to="/login" replace />}
        />

      </Routes>
    </BrowserRouter>
  );
}