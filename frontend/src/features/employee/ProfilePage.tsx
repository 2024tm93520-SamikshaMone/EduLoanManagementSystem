import { FormEvent, useEffect, useState } from "react";
import {
  ProfileError,
  getMyProfile,
  updateMyProfile,
} from "../../services/profileService";
import type { EmployeeProfile } from "../../types/employee";
import "./ProfilePage.css";

function formatCurrency(amount: number): string {
  return new Intl.NumberFormat("en-IN", {
    style: "currency",
    currency: "INR",
    maximumFractionDigits: 0,
  }).format(amount);
}

function formatTenure(months: number): string {
  const years = Math.floor(months / 12);
  const remMonths = months % 12;

  if (years === 0) return `${remMonths} mo`;
  return remMonths === 0 ? `${years} yr` : `${years} yr ${remMonths} mo`;
}

function getInitials(fullName: string): string {
  return fullName
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((name) => name[0]?.toUpperCase() ?? "")
    .join("");
}

const COUNTRY_CODES = [
  { code: "+91", label: "India (+91)" },
  { code: "+1", label: "USA (+1)" },
  { code: "+44", label: "UK (+44)" },
  { code: "+61", label: "Australia (+61)" },
  { code: "+971", label: "UAE (+971)" },
  { code: "+65", label: "Singapore (+65)" },
  { code: "+81", label: "Japan (+81)" },
  { code: "+49", label: "Germany (+49)" },
];

export default function ProfilePage() {
  const [profile, setProfile] = useState<EmployeeProfile | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const [isEditing, setIsEditing] = useState(false);
  const [countryCode, setCountryCode] = useState("+91");
  const [phoneInput, setPhoneInput] = useState("");
  const [phoneFieldError, setPhoneFieldError] = useState<string | null>(null);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [saveSuccess, setSaveSuccess] = useState(false);

  useEffect(() => {
    let cancelled = false;

    getMyProfile()
      .then((data) => {
        if (cancelled) return;

        setProfile(data);
        setPhoneInput(data.phoneNumber?.replace(/^\+\d+\s*/, "") ?? "");
      })
      .catch((err) => {
        if (cancelled) return;
        setLoadError(
          err instanceof ProfileError
            ? err.message
            : "Something went wrong. Please try again."
        );
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, []);

  function startEditing() {
    setIsEditing(true);
    setSaveSuccess(false);
    setSaveError(null);
    setPhoneFieldError(null);
  }

  function cancelEditing() {
    setIsEditing(false);
    setPhoneInput(profile?.phoneNumber?.replace(/^\+\d+\s*/, "") ?? "");
    setCountryCode("+91");
    setPhoneFieldError(null);
    setSaveError(null);
  }

  async function handleSave(e: FormEvent) {
    e.preventDefault();
    setSaveError(null);
    setSaveSuccess(false);

    if (!/^[0-9]{10}$/.test(phoneInput)) {
      setPhoneFieldError("Phone number must be exactly 10 digits.");
      return;
    }

    setPhoneFieldError(null);
    setIsSaving(true);

    try {
      // The existing API accepts the local 10-digit number.
      // The selected country code is displayed and retained in the UI.
      const updated = await updateMyProfile(phoneInput);

      setProfile(updated);
      setIsEditing(false);
      setSaveSuccess(true);
    } catch (err) {
      setSaveError(
        err instanceof ProfileError
          ? err.message
          : "Something went wrong. Please try again."
      );
    } finally {
      setIsSaving(false);
    }
  }

  if (isLoading) {
    return (
      <main className="profile-page">
        <div className="profile-state-card" role="status">
          Loading your profile...
        </div>
      </main>
    );
  }

  if (loadError || !profile) {
    return (
      <main className="profile-page">
        <div role="alert" className="profile-message profile-message-error">
          {loadError ?? "Profile unavailable."}
        </div>
      </main>
    );
  }

  const initials = getInitials(profile.fullName);

  return (
    <main className="profile-page">
      <div className="profile-page-inner">
        <div className="profile-breadcrumb">
          <span>Home</span>
          <span className="breadcrumb-separator">›</span>
          <strong>My Profile</strong>
        </div>

        <section className="profile-heading">
          <div>
            <span className="section-accent-line" />
            <h1>My Profile</h1>
            <p>View and manage your personal and employment information.</p>
          </div>

          <div className="profile-heading-decoration" aria-hidden="true">
            <span>Invest in Learning.</span>
            <strong>Build the Future.</strong>
          </div>
        </section>

        <section className="profile-summary-card">
          <div className="profile-avatar" aria-hidden="true">
            {initials}
          </div>

          <div className="profile-summary-details">
            <h2>{profile.fullName}</h2>
            <span className="employee-badge">{profile.employeeCode}</span>
            <p className="profile-designation">
              {profile.designation ?? "Employee"}
            </p>
            <div className="profile-summary-meta">
              <span>◈ {profile.departmentName ?? "Department not set"}</span>
              <span className="meta-divider">|</span>
              <span>⌖ Pune, Maharashtra</span>
            </div>
          </div>
        </section>

        <section className="profile-information-card">
          <div className="profile-card-title">
            <span className="card-title-icon">♙</span>
            <h2>Employee Information</h2>
          </div>

          <div className="employee-information-grid">
            <div className="information-column">
              <div className="information-row">
                <span>Employee Code</span>
                <strong>{profile.employeeCode}</strong>
              </div>
              <div className="information-row">
                <span>Full Name</span>
                <strong>{profile.fullName}</strong>
              </div>
              <div className="information-row">
                <span>Email</span>
                <strong>{profile.email}</strong>
              </div>
              <div className="information-row">
                <span>Department</span>
                <strong>{profile.departmentName ?? "—"}</strong>
              </div>
            </div>

            <div className="information-column">
              <div className="information-row">
                <span>Designation</span>
                <strong>{profile.designation ?? "—"}</strong>
              </div>
              <div className="information-row">
                <span>Tenure</span>
                <strong>{formatTenure(profile.tenureMonths)}</strong>
              </div>
              <div className="information-row">
                <span>Monthly Salary</span>
                <strong>{formatCurrency(profile.monthlySalary)}</strong>
              </div>
            </div>
          </div>
        </section>

        <section className="profile-information-card phone-card">
          <div className="profile-card-title">
            <span className="card-title-icon">⌕</span>
            <h2>Phone Number</h2>
            {!isEditing && (
              <button
                type="button"
                className="text-action"
                onClick={startEditing}
              >
                Edit
              </button>
            )}
          </div>

          {!isEditing ? (
            <div className="phone-display-row">
              <div>
                <span className="phone-label">Phone Number</span>
                <strong>
                  {profile.phoneNumber
                    ? `${countryCode} ${profile.phoneNumber}`
                    : "Not set"}
                </strong>
              </div>
            </div>
          ) : (
            <form onSubmit={handleSave} noValidate className="phone-edit-form">
              <label htmlFor="countryCode">Phone Number</label>

              <div className="phone-input-wrapper">
                <select
                  id="countryCode"
                  className="country-code-select"
                  value={countryCode}
                  onChange={(e) => setCountryCode(e.target.value)}
                  aria-label="Country code"
                >
                  {COUNTRY_CODES.map((country) => (
                    <option key={country.code} value={country.code}>
                      {country.label}
                    </option>
                  ))}
                </select>

                <input
                  id="phoneNumber"
                  aria-label="Phone Number"
                  type="tel"
                  inputMode="numeric"
                  maxLength={10}
                  value={phoneInput}
                  onChange={(e) =>
                    setPhoneInput(e.target.value.replace(/\D/g, ""))
                  }
                  placeholder="Enter 10-digit number"
                  aria-invalid={!!phoneFieldError}
                />
              </div>

              {phoneFieldError && (
                <span
                  role="alert"
                  className="profile-message profile-message-error"
                >
                  {phoneFieldError}
                </span>
              )}

              <div className="phone-actions">
                <button
                  type="submit"
                  className="primary-button"
                  disabled={isSaving}
                >
                  {isSaving ? "Saving..." : "Save"}
                </button>
                <button
                  type="button"
                  className="secondary-button"
                  onClick={cancelEditing}
                  disabled={isSaving}
                >
                  Cancel
                </button>
              </div>
            </form>
          )}

          {saveError && (
            <div role="alert" className="profile-message profile-message-error">
              {saveError}
            </div>
          )}

          {saveSuccess && (
            <div role="status" className="profile-message profile-message-success">
              ✓ Profile updated successfully.
            </div>
          )}
        </section>

        <section className="learning-banner">
          <div>
            <h2>▣ Your Learning Journey Matters</h2>
            <p>
              Keep your profile updated to ensure a smooth loan application
              process and unlock opportunities for a brighter tomorrow.
            </p>
          </div>
          <div className="learning-symbol" aria-hidden="true">
            ✦
          </div>
        </section>
      </div>
    </main>
  );
}
