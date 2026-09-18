import { Link, useLocation } from "react-router-dom";

export default function EmployeeNav() {
  const location = useLocation();

  return (
    <div className="employee-nav">
      <Link to="/employee/dashboard" className={location.pathname.includes("dashboard") ? "active" : ""}>
        My Profile
      </Link>
      <Link to="/employee/applications" className={location.pathname.includes("applications") ? "active" : ""}>
        My Applications
      </Link>
    </div>
  );
}
