import { FormEvent, useEffect, useState } from "react";
import { ProfileError, getMyProfile, updateMyProfile } from "../../services/profileService";
import type { EmployeeProfile } from "../../types/employee";

function formatCurrency(amount: number): string {
  return new Intl.NumberFormat("en-IN", { style: "currency", currency: "INR", maximumFractionDigits: 0 }).format(amount);
}

function formatTenure(months: number): string {
  const years = Math.floor(months / 12);
  const remMonths = months % 12;
  if (years === 0) return `${remMonths} mo`;
  return remMonths === 0 ? `${years} yr` : `${years} yr ${remMonths} mo`;
}

export default function ProfilePage() {
  const [profile, setProfile] = useState<EmployeeProfile | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const [isEditing, setIsEditing] = useState(false);
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
        setPhoneInput(data.phoneNumber ?? "");
      })
      .catch((err) => {
        if (cancelled) return;
        setLoadError(err instanceof ProfileError ? err.message : "Something went wrong. Please try again.");
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
  }

  function cancelEditing() {
    setIsEditing(false);
    setPhoneInput(profile?.phoneNumber ?? "");
    setPhoneFieldError(null);
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
      const updated = await updateMyProfile(phoneInput);
      setProfile(updated);
      setIsEditing(false);
      setSaveSuccess(true);
    } catch (err) {
      setSaveError(err instanceof ProfileError ? err.message : "Something went wrong. Please try again.");
    } finally {
      setIsSaving(false);
    }
  }

  if (isLoading) {
    return (
      <div className="profile-page">
        <p role="status">Loading your profile...</p>
      </div>
    );
  }

  if (loadError || !profile) {
    return (
      <div className="profile-page">
        <div role="alert" className="form-error">
          {loadError ?? "Profile unavailable."}
        </div>
      </div>
    );
  }

  return (
    <div className="profile-page">
      <div className="profile-card">
        <h1>My Profile</h1>

        <div className="profile-grid">
          <div className="profile-field">
            <span className="profile-label">Employee Code</span>
            <span className="profile-value">{profile.employeeCode}</span>
          </div>
          <div className="profile-field">
            <span className="profile-label">Full Name</span>
            <span className="profile-value">{profile.fullName}</span>
          </div>
          <div className="profile-field">
            <span className="profile-label">Email</span>
            <span className="profile-value">{profile.email}</span>
          </div>
          <div className="profile-field">
            <span className="profile-label">Department</span>
            <span className="profile-value">{profile.departmentName ?? "—"}</span>
          </div>
          <div className="profile-field">
            <span className="profile-label">Designation</span>
            <span className="profile-value">{profile.designation ?? "—"}</span>
          </div>
          <div className="profile-field">
            <span className="profile-label">Tenure</span>
            <span className="profile-value">{formatTenure(profile.tenureMonths)}</span>
          </div>
          <div className="profile-field">
            <span className="profile-label">Monthly Salary</span>
            <span className="profile-value">{formatCurrency(profile.monthlySalary)}</span>
          </div>
        </div>

        <div className="profile-phone-section">
          <span className="profile-label">Phone Number</span>

          {!isEditing ? (
            <div className="profile-phone-view">
              <span className="profile-value">{profile.phoneNumber ?? "Not set"}</span>
              <button type="button" className="link-button" onClick={startEditing}>
                Edit
              </button>
            </div>
          ) : (
            <form onSubmit={handleSave} noValidate>
              <input
                id="phoneNumber"
                aria-label="Phone Number"
                type="tel"
                value={phoneInput}
                onChange={(e) => setPhoneInput(e.target.value)}
                aria-invalid={!!phoneFieldError}
              />
              {phoneFieldError && (
                <span role="alert" className="field-error">
                  {phoneFieldError}
                </span>
              )}
              <div className="profile-phone-actions">
                <button type="submit" disabled={isSaving}>
                  {isSaving ? "Saving..." : "Save"}
                </button>
                <button type="button" className="link-button" onClick={cancelEditing} disabled={isSaving}>
                  Cancel
                </button>
              </div>
            </form>
          )}

          {saveError && (
            <div role="alert" className="form-error">
              {saveError}
            </div>
          )}
          {saveSuccess && (
            <div role="status" className="form-success">
              Profile updated successfully.
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
