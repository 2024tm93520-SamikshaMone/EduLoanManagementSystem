import { BrowserRouter, Navigate, Route, Routes, useNavigate } from "react-router-dom";
import LoginPage from "./features/auth/LoginPage";
import ProfilePage from "./features/employee/ProfilePage";
import AdminMasterDataPage from "./features/admin/AdminMasterDataPage";
import MyApplicationsPage from "./features/loans/MyApplicationsPage";
import EmployeeNav from "./components/EmployeeNav";

function LoginRoute() {
  const navigate = useNavigate();
  return <LoginPage onLoginSuccess={(route) => navigate(route)} />;
}

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

// Note: full Admin/HR/Finance dashboards aren't built yet (upcoming modules).
// Admin "dashboard" temporarily points at Master Data management, since that's
// the only admin-facing feature built so far.
export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginRoute />} />
        <Route path="/employee/dashboard" element={<EmployeeProfileRoute />} />
        <Route path="/employee/profile" element={<EmployeeProfileRoute />} />
        <Route path="/employee/applications" element={<EmployeeApplicationsRoute />} />
        <Route path="/admin/dashboard" element={<AdminMasterDataPage />} />
        <Route path="/admin/master-data" element={<AdminMasterDataPage />} />
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
