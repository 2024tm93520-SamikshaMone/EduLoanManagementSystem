import { FormEvent, useState } from "react";
import { AuthError, login, storeSession } from "../../services/authService";
import type { UserRole } from "../../types/auth";
import "./LoginPage.css";

const ROLE_HOME_ROUTE: Record<UserRole, string> = {
  Employee: "/employee/dashboard",
  Admin: "/admin/dashboard",
  HR: "/hr/dashboard",
  Finance: "/finance/dashboard",
};

interface LoginPageProps {
  onLoginSuccess?: (route: string) => void;
}

function isValidEmail(value: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
}

export default function LoginPage({ onLoginSuccess }: LoginPageProps) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [countryCode, setCountryCode] = useState("+91");
  const [phoneNumber, setPhoneNumber] = useState("");

  const [fieldErrors, setFieldErrors] = useState<{
    email?: string;
    password?: string;
  }>({});

  const [formError, setFormError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  function validate(): boolean {
    const errors: { email?: string; password?: string } = {};

    if (!email.trim()) {
      errors.email = "Email is required.";
    } else if (!isValidEmail(email)) {
      errors.email = "Enter a valid email address.";
    }

    if (!password) {
      errors.password = "Password is required.";
    } else if (password.length < 6) {
      errors.password = "Password must be at least 6 characters.";
    }

    setFieldErrors(errors);

    return Object.keys(errors).length === 0;
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setFormError(null);

    if (!validate()) return;

    setIsSubmitting(true);

    try {
      const result = await login({
        email: email.trim(),
        password,
      });

      storeSession(result);

      const route = ROLE_HOME_ROUTE[result.user.role];

      onLoginSuccess?.(route);
    } catch (err) {
      const message =
        err instanceof AuthError
          ? err.message
          : "Something went wrong. Please try again.";

      setFormError(message);
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="login-page">

      {/* Bottom-left decorative gradient triangle */}
      <div className="corner-triangle" aria-hidden="true"></div>

      {/* Top Branding */}
      <header className="login-header">
        <div className="accenture-brand">
          <span className="accenture-symbol">&gt;</span>
          <span className="accenture-name">accenture</span>
        </div>

        <div className="header-tagline">
          PEOPLE. TECHNOLOGY. GREATER POSSIBILITIES.
        </div>
      </header>

      {/* Main Content */}
      <main className="login-content">

        {/* Left Section */}
        <section className="login-intro">

          <div className="intro-accent"></div>

          <h2>
            Invest in Learning.
            <br />
            <span>Build the Future.</span>
          </h2>

          <p>
            Supporting employees in their
            <br />
            educational journey for a brighter
            <br />
            tomorrow.
          </p>

        </section>

        {/* Login Card */}
        <section className="login-card">

          <div className="card-accent"></div>

          <h1>Employee Education Loan Portal</h1>

          <p className="login-subtitle">
            Sign in to continue
          </p>

          <form onSubmit={handleSubmit} noValidate>

            {/* Email */}
            <div className="form-group">

              <label htmlFor="email">Email</label>

              <div className="input-wrapper">

                <span className="input-icon">
                  <svg
                    width="20"
                    height="20"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="1.8"
                  >
                    <rect
                      x="3"
                      y="5"
                      width="18"
                      height="14"
                      rx="2"
                    />
                    <path d="M3 7l9 6 9-6" />
                  </svg>
                </span>

                <input
                  id="email"
                  type="email"
                  placeholder="Enter your email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  aria-invalid={!!fieldErrors.email}
                  aria-describedby={
                    fieldErrors.email ? "email-error" : undefined
                  }
                />

              </div>

              {fieldErrors.email && (
                <span
                  id="email-error"
                  role="alert"
                  className="field-error"
                >
                  {fieldErrors.email}
                </span>
              )}

            </div>

            {/* Password */}
            <div className="form-group">

              <label htmlFor="password">Password</label>

              <div className="input-wrapper">

                <span className="input-icon">
                  <svg
                    width="20"
                    height="20"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="1.8"
                  >
                    <rect
                      x="5"
                      y="10"
                      width="14"
                      height="11"
                      rx="2"
                    />
                    <path d="M8 10V7a4 4 0 018 0v3" />
                  </svg>
                </span>

                <input
                  id="password"
                  type={showPassword ? "text" : "password"}
                  placeholder="Enter your password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  aria-invalid={!!fieldErrors.password}
                  aria-describedby={
                    fieldErrors.password
                      ? "password-error"
                      : undefined
                  }
                />

                <button
                  type="button"
                  className="password-toggle"
                  onClick={() => setShowPassword(!showPassword)}
                  aria-label={
                    showPassword
                      ? "Hide password"
                      : "Show password"
                  }
                >
                  {showPassword ? (
                    // Eye icon
                    <svg
                      width="18"
                      height="18"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="1.8"
                      strokeLinecap="round"
                      strokeLinejoin="round"
                    >
                      <path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12Z" />
                      <circle cx="12" cy="12" r="3" />
                    </svg>
                  ) : (
                    // Eye-slash icon
                    <svg
                      width="18"
                      height="18"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="1.8"
                      strokeLinecap="round"
                      strokeLinejoin="round"
                    >
                      <path d="M3 3l18 18" />
                      <path d="M10.6 5.1A10.8 10.8 0 0 1 12 5c6.5 0 10 7 10 7a18.3 18.3 0 0 1-3.2 4.1" />
                      <path d="M6.6 6.6C3.8 8.5 2 12 2 12s3.5 7 10 7a10.8 10.8 0 0 0 4.1-.8" />
                      <path d="M9.9 9.9a3 3 0 0 0 4.2 4.2" />
                    </svg>
                  )}
                </button>

              </div>

              {fieldErrors.password && (
                <span
                  id="password-error"
                  role="alert"
                  className="field-error"
                >
                  {fieldErrors.password}
                </span>
              )}

            </div>

            {/* Form Error */}
            {formError && (
              <div
                role="alert"
                className="form-error"
              >
                {formError}
              </div>
            )}

            {/* Sign In */}
            <button
              type="submit"
              disabled={isSubmitting}
            >
              {isSubmitting ? "Signing in..." : "Sign In"}
            </button>

            {/* Forgot Password */}
            <a
              href="/forgot-password"
              className="forgot-password"
            >
              Forgot password?
            </a>

          </form>

        </section>

        {/* Right Decorative Section */}
        <section className="login-decoration">

          {/* Folded-ribbon Accenture arrow, matching the logo mark */}
          <div className="purple-chevron" aria-hidden="true">
            <svg viewBox="0 0 300 300" xmlns="http://www.w3.org/2000/svg">
              <defs>
                <linearGradient id="chevronTop" x1="0%" y1="0%" x2="100%" y2="100%">
                  <stop offset="0%" stopColor="#ede1ff" />
                  <stop offset="100%" stopColor="#c39bff" />
                </linearGradient>
                <linearGradient id="chevronBottom" x1="0%" y1="0%" x2="100%" y2="100%">
                  <stop offset="0%" stopColor="#c39bff" />
                  <stop offset="100%" stopColor="#a566ef" />
                </linearGradient>
              </defs>

              {/* Upper wing */}
              <polygon
                points="0,0 300,150 192,150 0,96"
                fill="url(#chevronTop)"
              />

              {/* Lower wing */}
              <polygon
                points="0,300 300,150 192,150 0,204"
                fill="url(#chevronBottom)"
              />
            </svg>
          </div>

          <div className="decoration-text">
            <div className="decoration-line"></div>

            <p>
              SKILLS
              <br />
              PEOPLE
              <br />
              OPPORTUNITIES
              <br />
              A BRIGHTER TOMORROW
            </p>

            <div className="decoration-line"></div>
          </div>

        </section>

      </main>

      {/* Footer */}
      <footer className="login-footer">
        ACCENTURE
        <br />
        LET THERE BE CHANGE
      </footer>

    </div>
  );
}
