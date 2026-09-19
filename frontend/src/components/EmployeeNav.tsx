import { useState, useRef, useEffect } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import "./EmployeeNav.css";

type IconName =
  | "profile"
  | "applications"
  | "new"
  | "documents"
  | "notifications"
  | "help";

function NavIcon({ name }: { name: IconName }) {
  const common = {
    width: 20,
    height: 20,
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: 1.8,
    strokeLinecap: "round" as const,
    strokeLinejoin: "round" as const,
    "aria-hidden": true,
  };

  switch (name) {
    case "profile":
      return (
        <svg {...common}>
          <circle cx="12" cy="8" r="3.2" />
          <path d="M5.5 20c.7-4 2.8-6 6.5-6s5.8 2 6.5 6" />
        </svg>
      );

    case "applications":
      return (
        <svg {...common}>
          <rect x="4" y="3" width="16" height="18" rx="1.5" />
          <path d="M8 8h8M8 12h8M8 16h8" />
        </svg>
      );

    case "new":
      return (
        <svg {...common}>
          <circle cx="12" cy="12" r="8.5" />
          <path d="M12 8v8M8 12h8" />
        </svg>
      );

    case "documents":
      return (
        <svg {...common}>
          <path d="M7 3h7l4 4v14H7z" />
          <path d="M14 3v5h4M10 12h5M10 16h5" />
        </svg>
      );

    case "notifications":
      return (
        <svg {...common}>
          <path d="M18 9a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9" />
          <path d="M10 21h4" />
        </svg>
      );

    case "help":
      return (
        <svg {...common}>
          <circle cx="12" cy="12" r="9" />
          <path d="M9.7 9a2.5 2.5 0 1 1 4.2 1.8c-1.2 1-1.9 1.4-1.9 3" />
          <path d="M12 17h.01" />
        </svg>
      );
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
    <span className="accenture-mark" aria-hidden="true">
      <span />
      <span />
    </span>
  );
}

function isProfileRoute(pathname: string): boolean {
  return (
    pathname === "/employee/profile" ||
    pathname === "/employee/dashboard"
  );
}

export default function EmployeeNav() {
  const location = useLocation();
  const navigate = useNavigate();

  const [isProfileMenuOpen, setIsProfileMenuOpen] = useState(false);
  const profileMenuRef = useRef<HTMLDivElement>(null);

  const profileActive = isProfileRoute(location.pathname);
  const applicationsActive =
    location.pathname === "/employee/applications";

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

  // Logout functionality
  function handleLogout() {
    // Clear authentication data
    localStorage.removeItem("token");
    localStorage.removeItem("accessToken");

    // Redirect to Sign In page
    navigate("/login", { replace: true });
  }

  return (
    <>
      {/* HEADER */}
      <header className="employee-topbar">
        <Link
          to="/employee/dashboard"
          className="employee-brand"
          aria-label="Employee portal home"
        >
          <span className="accenture-wordmark">accenture</span>
          <AccentureMark />
        </Link>

        <span className="topbar-divider" aria-hidden="true" />

        <span className="portal-title">
          Employee Education Loan Portal
        </span>

        <span className="topbar-spacer" />

        <button
          type="button"
          className="notification-button"
          aria-label="Notifications"
        >
          <NavIcon name="notifications" />
          <span className="notification-dot" />
        </button>

        {/* PROFILE SECTION */}
        <div className="profile-menu-wrapper" ref={profileMenuRef}>
          <button
            type="button"
            className="user-summary profile-menu-trigger"
            onClick={() => setIsProfileMenuOpen((prev) => !prev)}
            aria-expanded={isProfileMenuOpen}
            aria-haspopup="menu"
          >
            <span className="user-avatar">SM</span>

            <span className="user-name">Samiksha Mone</span>

            <span className="user-chevron" aria-hidden="true">
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
                  d={isProfileMenuOpen ? "M6 15l6-6 6 6" : "M6 9l6 6 6-6"}
                />
              </svg>
            </span>
          </button>

          {/* DROPDOWN */}
          {isProfileMenuOpen && (
            <div className="profile-dropdown" role="menu">
              <div className="dropdown-user-info">
                <strong>Samiksha Mone</strong>
                <span>Employee</span>
              </div>

              <div className="dropdown-divider" />

              <Link
                to="/employee/profile"
                className="dropdown-item"
                role="menuitem"
                onClick={() => setIsProfileMenuOpen(false)}
              >
                <span className="dropdown-icon">
                  <DropdownIcon type="account" />
                </span>
                <span>Account Information</span>
              </Link>

              <Link
                to="/employee/change-password"
                className="dropdown-item"
                role="menuitem"
                onClick={() => setIsProfileMenuOpen(false)}
              >
                <span className="dropdown-icon">
                  <DropdownIcon type="password" />
                </span>
                <span>Change Password</span>
              </Link>

              <div className="dropdown-divider" />

              <button
                type="button"
                className="dropdown-item logout-item"
                onClick={handleLogout}
                role="menuitem"
              >
                <span className="dropdown-icon">
                  <DropdownIcon type="logout" />
                </span>
                <span>Logout</span>
              </button>
            </div>
          )}
        </div>
      </header>

      {/* SIDEBAR */}
      <aside className="employee-sidebar" aria-label="Employee navigation">
        <nav className="employee-nav">
          <Link
            to="/employee/profile"
            className={`employee-nav-link ${
              profileActive ? "active" : ""
            }`}
          >
            <span className="nav-icon">
              <NavIcon name="profile" />
            </span>
            <span>My Profile</span>
          </Link>

          <Link
            to="/employee/applications"
            className={`employee-nav-link ${
              applicationsActive ? "active" : ""
            }`}
          >
            <span className="nav-icon">
              <NavIcon name="applications" />
            </span>
            <span>My Applications</span>
          </Link>

          <Link
            to="/employee/applications/new"
            className="employee-nav-link"
          >
            <span className="nav-icon">
              <NavIcon name="new" />
            </span>
            <span>New Application</span>
          </Link>

          <Link to="/employee/documents" className="employee-nav-link">
            <span className="nav-icon">
              <NavIcon name="documents" />
            </span>
            <span>Documents</span>
          </Link>

          <Link
            to="/employee/notifications"
            className="employee-nav-link"
          >
            <span className="nav-icon">
              <NavIcon name="notifications" />
            </span>
            <span>Notifications</span>
          </Link>

          <div className="nav-divider" />

          <Link to="/employee/help" className="employee-nav-link">
            <span className="nav-icon">
              <NavIcon name="help" />
            </span>
            <span>Help &amp; Support</span>
          </Link>
        </nav>
      </aside>
    </>
  );
}