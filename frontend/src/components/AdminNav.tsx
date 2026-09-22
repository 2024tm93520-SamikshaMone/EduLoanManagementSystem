import { useEffect, useRef, useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import "./AdminNav.css";

type IconName =
  | "dashboard"
  | "employees"
  | "applications"
  | "masterData"
  | "rules"
  | "reports"
  | "help";

function NavIcon({ name }: { name: IconName }) {
  const common = {
    width: 21,
    height: 21,
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: 1.8,
    strokeLinecap: "round" as const,
    strokeLinejoin: "round" as const,
    "aria-hidden": true,
  };

  switch (name) {
    case "dashboard":
      return (
        <svg {...common}>
          <rect x="3" y="3" width="7" height="7" rx="1" />
          <rect x="14" y="3" width="7" height="7" rx="1" />
          <rect x="3" y="14" width="7" height="7" rx="1" />
          <rect x="14" y="14" width="7" height="7" rx="1" />
        </svg>
      );

    case "employees":
      return (
        <svg {...common}>
          <circle cx="9" cy="8" r="3" />
          <path d="M3 20c0-3.3 2.7-5 6-5s6 1.7 6 5" />
          <path d="M16 11c2.5.2 4.5 1.7 5 4" />
          <path d="M16 5.5a3 3 0 0 1 0 5.5" />
        </svg>
      );

    case "applications":
      return (
        <svg {...common}>
          <rect x="5" y="3" width="14" height="18" rx="2" />
          <path d="M9 8h6" />
          <path d="M9 12h6" />
          <path d="M9 16h4" />
        </svg>
      );

    case "masterData":
      return (
        <svg {...common}>
          <rect x="4" y="4" width="16" height="16" rx="2" />
          <path d="M8 8h8" />
          <path d="M8 12h8" />
          <path d="M8 16h5" />
        </svg>
      );

    case "rules":
      return (
        <svg {...common}>
          <path d="M5 4h14v16H5z" />
          <path d="M8 9l2 2 5-5" />
          <path d="M8 15h8" />
        </svg>
      );

    case "reports":
      return (
        <svg {...common}>
          <path d="M5 20V10" />
          <path d="M12 20V4" />
          <path d="M19 20v-7" />
        </svg>
      );

    case "help":
      return (
        <svg {...common}>
          <circle cx="12" cy="12" r="9" />
          <path d="M9.7 9a2.4 2.4 0 1 1 4.2 1.6c-1.2 1.1-1.9 1.5-1.9 3" />
          <path d="M12 17h.01" />
        </svg>
      );

    default:
      return null;
  }
}

function DropdownIcon({
  type,
}: {
  type: "account" | "password" | "logout";
}) {
  const common = {
    width: 18,
    height: 18,
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: 1.8,
    strokeLinecap: "round" as const,
    strokeLinejoin: "round" as const,
    "aria-hidden": true,
  };

  if (type === "account") {
    return (
      <svg {...common}>
        <circle cx="12" cy="8" r="3.5" />
        <path d="M5 20c.8-4 3-6 7-6s6.2 2 7 6" />
      </svg>
    );
  }

  if (type === "password") {
    return (
      <svg {...common}>
        <rect x="5" y="10" width="14" height="10" rx="2" />
        <path d="M8 10V7a4 4 0 0 1 8 0v3" />
        <circle cx="12" cy="15" r="1" />
      </svg>
    );
  }

  return (
    <svg {...common}>
      <path d="M10 17l5-5-5-5" />
      <path d="M15 12H3" />
      <path d="M20 4v16" />
    </svg>
  );
}

function AccentureMark() {
  return (
    <div className="admin-accenture-mark" aria-hidden="true">
      <span />
      <span />
    </div>
  );
}

export default function AdminNav() {
  const location = useLocation();
  const navigate = useNavigate();

  const [isProfileMenuOpen, setIsProfileMenuOpen] = useState(false);

  const profileMenuRef = useRef<HTMLDivElement>(null);

  const isActive = (path: string) =>
    location.pathname === path ||
    location.pathname.startsWith(`${path}/`);

  // Close dropdown when clicking outside
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (
        profileMenuRef.current &&
        !profileMenuRef.current.contains(event.target as Node)
      ) {
        setIsProfileMenuOpen(false);
      }
    }

    document.addEventListener("mousedown", handleClickOutside);

    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, []);

  // Logout
  function handleLogout() {
    localStorage.removeItem("token");
    localStorage.removeItem("accessToken");

    setIsProfileMenuOpen(false);

    navigate("/login", { replace: true });
  }

  return (
    <>
      {/* ================= HEADER ================= */}

      <header className="admin-topbar">
        <div className="admin-brand">
          <Link
            to="/admin/dashboard"
            className="admin-brand-link"
            aria-label="Admin portal home"
          >
            <span className="admin-accenture-wordmark">
              accenture
            </span>

            <AccentureMark />
          </Link>

          <span className="admin-brand-divider" />

          <span className="admin-portal-title">
            Employee Education Loan Portal
          </span>
        </div>

        <div className="admin-topbar-right">
          {/* Notification */}
          <button
            type="button"
            className="admin-notification-button"
            aria-label="Notifications"
          >
            <svg
              width="22"
              height="22"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="1.8"
              strokeLinecap="round"
              strokeLinejoin="round"
              aria-hidden="true"
            >
              <path d="M18 9a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9" />
              <path d="M10 21h4" />
            </svg>

            <span className="admin-notification-dot" />
          </button>

          {/* ADMIN PROFILE */}
          <div
            className="admin-profile-menu-wrapper"
            ref={profileMenuRef}
          >
            <button
              type="button"
              className="admin-user-menu"
              onClick={() =>
                setIsProfileMenuOpen((prev) => !prev)
              }
              aria-expanded={isProfileMenuOpen}
              aria-haspopup="menu"
            >
              <span className="admin-user-avatar">
                AU
              </span>

              <span className="admin-user-name">
                Admin User
              </span>

              <span className="admin-user-chevron">
                <svg
                  width="16"
                  height="16"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="2.5"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                >
                  <path
                    d={
                      isProfileMenuOpen
                        ? "M6 15l6-6 6 6"
                        : "M6 9l6 6 6-6"
                    }
                  />
                </svg>
              </span>
            </button>

            {/* DROPDOWN */}
            {isProfileMenuOpen && (
              <div
                className="admin-profile-dropdown"
                role="menu"
              >
                <div className="admin-dropdown-user-info">
                  <strong>Admin User</strong>
                  <span>Administrator</span>
                </div>

                <div className="admin-dropdown-divider" />

                <Link
                  to="/admin/profile"
                  className="admin-dropdown-item"
                  role="menuitem"
                  onClick={() =>
                    setIsProfileMenuOpen(false)
                  }
                >
                  <span className="admin-dropdown-icon">
                    <DropdownIcon type="account" />
                  </span>

                  <span>Account Information</span>
                </Link>

                <Link
                  to="/admin/change-password"
                  className="admin-dropdown-item"
                  role="menuitem"
                  onClick={() =>
                    setIsProfileMenuOpen(false)
                  }
                >
                  <span className="admin-dropdown-icon">
                    <DropdownIcon type="password" />
                  </span>

                  <span>Change Password</span>
                </Link>

                <div className="admin-dropdown-divider" />

                <button
                  type="button"
                  className="admin-dropdown-item admin-logout-item"
                  onClick={handleLogout}
                  role="menuitem"
                >
                  <span className="admin-dropdown-icon">
                    <DropdownIcon type="logout" />
                  </span>

                  <span>Logout</span>
                </button>
              </div>
            )}
          </div>
        </div>
      </header>

      {/* ================= SIDEBAR ================= */}

      <aside
        className="admin-sidebar"
        aria-label="Admin navigation"
      >
        <nav className="admin-sidebar-nav">

          <Link
            to="/admin/dashboard"
            className={`admin-sidebar-link ${isActive("/admin/dashboard")
                ? "active"
                : ""
              }`}
          >
            <NavIcon name="dashboard" />
            <span>Dashboard</span>
          </Link>

          <Link
            to="/admin/employees"
            className={`admin-sidebar-link ${isActive("/admin/employees")
                ? "active"
                : ""
              }`}
          >
            <NavIcon name="employees" />
            <span>Employees</span>
          </Link>

          <Link
            to="/admin/applications"
            className={`admin-sidebar-link ${isActive("/admin/applications")
                ? "active"
                : ""
              }`}
          >
            <NavIcon name="applications" />
            <span>Loan Applications</span>
          </Link>

          <Link
            to="/admin/master-data"
            className={`admin-sidebar-link ${isActive("/admin/master-data")
                ? "active"
                : ""
              }`}
          >
            <NavIcon name="masterData" />
            <span>Master Data</span>
          </Link>

          <Link
            to="/admin/eligibility-rules"
            className={`admin-sidebar-link ${isActive("/admin/eligibility-rules")
                ? "active"
                : ""
              }`}
          >
            <NavIcon name="rules" />
            <span>Eligibility Rules</span>
          </Link>

          <Link
            to="/admin/reports"
            className={`admin-sidebar-link ${isActive("/admin/reports")
                ? "active"
                : ""
              }`}
          >
            <NavIcon name="reports" />
            <span>Reports</span>
          </Link>

          <div className="admin-sidebar-divider" />

          <Link
            to="/admin/help"
            className={`admin-sidebar-link ${isActive("/admin/help")
                ? "active"
                : ""
              }`}
          >
            <NavIcon name="help" />
            <span>Help &amp; Support</span>
          </Link>

        </nav>
      </aside>
    </>
  );
}
