import { useEffect, useRef, useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import "../styles/FinanceNav.css";
import { clearSession, getStoredUser } from "../../services/authService";
import { useUnreadNotificationCount } from "../../features/shared/hooks/useUnreadNotificationCount";

type IconName =
  | "dashboard"
  | "notifications"
  | "help"
  | "account"
  | "password"
  | "logout";

function Icon({ type }: { type: IconName }) {
  const common = {
    width: 20,
    height: 20,
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: 1.8,
    strokeLinecap: "round" as const,
    strokeLinejoin: "round" as const,
    "aria-hidden": true as const,
  };

  switch (type) {
    case "dashboard":
      return (
        <svg {...common}>
          <rect x="3" y="3" width="7" height="7" rx="1" />
          <rect x="14" y="3" width="7" height="7" rx="1" />
          <rect x="3" y="14" width="7" height="7" rx="1" />
          <rect x="14" y="14" width="7" height="7" rx="1" />
        </svg>
      );

    case "notifications":
      return (
        <svg {...common}>
          <path d="M18 9a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9" />
          <path d="M10 21h4" />
        </svg>
      );

    case "account":
      return (
        <svg {...common}>
          <circle cx="12" cy="8" r="3.5" />
          <path d="M5 20c.8-4 3-6 7-6s6.2 2 7 6" />
        </svg>
      );

    case "password":
      return (
        <svg {...common}>
          <rect x="5" y="10" width="14" height="10" rx="2" />
          <path d="M8 10V7a4 4 0 0 1 8 0v3" />
          <circle cx="12" cy="15" r="1" />
        </svg>
      );

    case "logout":
      return (
        <svg {...common}>
          <path d="M10 17l5-5-5-5" />
          <path d="M15 12H3" />
          <path d="M20 4v16" />
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

function Mark() {
  return (
    <span className="finance-shell-mark" aria-hidden="true">
      <span />
      <span />
    </span>
  );
}

export default function FinanceNav() {
  const { unreadCount } = useUnreadNotificationCount();
  const [isProfileMenuOpen, setIsProfileMenuOpen] = useState(false);
  const profileMenuRef = useRef<HTMLDivElement>(null);
  const location = useLocation();
  const navigate = useNavigate();

  const storedUser = getStoredUser() as {
    fullName?: string;
    name?: string;
    role?: string;
  } | null;

  const displayName =
    storedUser?.fullName || storedUser?.name || "Finance User";

  const initials =
    displayName
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0])
      .join("")
      .toUpperCase() || "FN";

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

  useEffect(() => {
    setIsProfileMenuOpen(false);
  }, [location.pathname]);

  function handleLogout() {
    clearSession();
    navigate("/login", { replace: true });
  }

  const active = (path: string) => location.pathname === path;

  return (
    <>
      <header className="finance-shell-topbar">
        <Link
          to="/finance/dashboard"
          className="finance-shell-brand"
          aria-label="EduLoan Finance home"
        >
          <span className="finance-shell-wordmark">accenture</span>
          <Mark />
          <span className="finance-shell-divider" aria-hidden="true" />
          <span className="finance-shell-title">
            Employee Education Loan Portal
          </span>
        </Link>

        <div className="finance-shell-user-area">
          <button
            type="button"
            className="notification-button"
            aria-label="Notifications"
            onClick={() => navigate("/finance/notifications")}
          >
            <Icon type="notifications" />
            {unreadCount > 0 && <span className="notification-count-badge">{unreadCount > 99 ? "99+" : unreadCount}</span>}
          </button>

          <div className="finance-profile-menu-wrapper" ref={profileMenuRef}>
            <button
              type="button"
              className="finance-profile-menu-trigger"
              onClick={() =>
                setIsProfileMenuOpen((previous) => !previous)
              }
              aria-expanded={isProfileMenuOpen}
              aria-haspopup="menu"
            >
              <span className="finance-shell-avatar">{initials}</span>
              <span className="finance-shell-user-name">{displayName}</span>
              <span className="finance-profile-chevron" aria-hidden="true">
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

            {isProfileMenuOpen && (
              <div className="finance-profile-dropdown" role="menu">
                <div className="finance-dropdown-user-info">
                  <strong>{displayName}</strong>
                  <span>Finance</span>
                </div>

                <div className="finance-dropdown-divider" />

                <Link
                  to="/finance/profile"
                  className="finance-dropdown-item"
                  role="menuitem"
                  onClick={() => setIsProfileMenuOpen(false)}
                >
                  <Icon type="account" />
                  <span>Account Information</span>
                </Link>

                <Link
                  to="/finance/change-password"
                  className="finance-dropdown-item"
                  role="menuitem"
                  onClick={() => setIsProfileMenuOpen(false)}
                >
                  <Icon type="password" />
                  <span>Change Password</span>
                </Link>

                <div className="finance-dropdown-divider" />

                <button
                  type="button"
                  className="finance-dropdown-item finance-logout-item"
                  role="menuitem"
                  onClick={handleLogout}
                >
                  <Icon type="logout" />
                  <span>Logout</span>
                </button>
              </div>
            )}
          </div>
        </div>
      </header>

      <aside className="finance-shell-sidebar" aria-label="Finance navigation">
        <nav>
          <Link
            to="/finance/dashboard"
            className={`finance-shell-link ${
              active("/finance/dashboard") ? "active" : ""
            }`}
          >
            <Icon type="dashboard" />
            <span>Dashboard</span>
          </Link>

          <Link
            to="/finance/repayment-simulation"
            className={`finance-shell-link ${active("/finance/repayment-simulation") ? "active" : ""}`}
          >
            <Icon type="dashboard" />
            <span>Repayment Simulation</span>
          </Link>

          <div className="finance-shell-separator" />

          <Link
            to="/finance/help"
            className={`finance-shell-link ${
              active("/finance/help") ? "active" : ""
            }`}
          >
            <Icon type="help" />
            <span>Help &amp; Support</span>
          </Link>
        </nav>

        <div className="finance-shell-sidebar-note">
          <strong>Finance workspace</strong>
          <span>Bank handoff and processing confirmation</span>
        </div>
      </aside>
    </>
  );
}