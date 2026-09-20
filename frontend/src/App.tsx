import {
  BrowserRouter,
  Navigate,
  Route,
  Routes,
  useNavigate,
} from "react-router-dom";

import LoginPage from "./features/auth/LoginPage";
import ProfilePage from "./features/employee/ProfilePage";
import AdminMasterDataPage from "./features/admin/AdminMasterDataPage";
import MyApplicationsPage from "./features/loans/pages/MyApplicationsPage";

import EmployeeNav from "./components/EmployeeNav";
import NewApplicationPage from "./features/loans/pages/NewApplicationPage";
import PasswordPage from "./features/auth/PasswordPage";

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

<Route
  path="/employee/applications/new"
  element={
    <>
      <EmployeeNav />
      <NewApplicationPage />
    </>
  }
/>

function NewApplicationRoute() {
  return (
    <>
      <EmployeeNav />
      <NewApplicationPage />
    </>
  );
}

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginRoute />} />
        
        <Route path="/forgot-password" element={<PasswordPage />} />

        <Route path="/employee/change-password" element={<PasswordPage />} />

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

        <Route
          path="/admin/dashboard"
          element={<AdminMasterDataPage />}
        />

        <Route
          path="/admin/master-data"
          element={<AdminMasterDataPage />}
        />

        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </BrowserRouter>
  );
}