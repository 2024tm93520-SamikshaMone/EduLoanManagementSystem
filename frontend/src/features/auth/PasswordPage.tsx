import { FormEvent, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import {
    AuthError,
    changePassword,
    sendForgotPasswordOtp,
    resetForgottenPassword,
    verifyForgotPasswordOtp,
} from "../../services/authService";
import "./PasswordPage.css";

type ForgotStep = "email" | "otp" | "reset";

export default function PasswordPage() {
    const location = useLocation();
    const navigate = useNavigate();

    const isForgotFlow = location.pathname === "/forgot-password";

    const [email, setEmail] = useState("");
    const [otp, setOtp] = useState("");
    const [resetToken, setResetToken] = useState("");

    const [currentPassword, setCurrentPassword] = useState("");
    const [newPassword, setNewPassword] = useState("");
    const [confirmPassword, setConfirmPassword] = useState("");

    const [step, setStep] = useState<ForgotStep>("email");
    const [message, setMessage] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [busy, setBusy] = useState(false);

    function clearMessages() {
        setMessage(null);
        setError(null);
    }

    async function submit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        clearMessages();

        if (isForgotFlow && step === "email") {
            if (!email.trim()) {
                setError("Email is required.");
                return;
            }
        }

        if (isForgotFlow && step === "otp") {
            if (otp.length !== 6) {
                setError("Please enter a valid 6-digit OTP.");
                return;
            }
        }

        if (!isForgotFlow || step === "reset") {
            if (!newPassword || !confirmPassword) {
                setError("Please enter and confirm your new password.");
                return;
            }

            if (newPassword.length < 6) {
                setError("Password must be at least 6 characters.");
                return;
            }

            if (newPassword !== confirmPassword) {
                setError("New password and confirm password must match.");
                return;
            }
        }

        setBusy(true);

        try {
            // Change password from the profile page
            if (!isForgotFlow) {
                const response = await changePassword({
                    currentPassword,
                    newPassword,
                    confirmPassword,
                });

                setMessage(response);
                setCurrentPassword("");
                setNewPassword("");
                setConfirmPassword("");

                return;
            }

            // Step 1: Send OTP
            if (step === "email") {
                const response = await sendForgotPasswordOtp({
                    email: email.trim(),
                });

                setMessage(response);
                setStep("otp");

                return;
            }

            // Step 2: Verify OTP
            if (step === "otp") {
                const token = await verifyForgotPasswordOtp({
                    email: email.trim(),
                    otp: otp.trim(),
                });

                setResetToken(token);
                setOtp("");
                setMessage("OTP verified. Set your new password.");
                setStep("reset");

                return;
            }

            // Step 3: Reset password
            const response = await resetForgottenPassword({
                email: email.trim(),
                resetToken,
                newPassword,
                confirmPassword,
            });

            setMessage(response);

            setTimeout(() => {
                navigate("/login", { replace: true });
            }, 1200);
        } catch (err) {
            setError(
                err instanceof AuthError
                    ? err.message
                    : "Something went wrong. Please try again."
            );
        } finally {
            setBusy(false);
        }
    }

    function handleBack() {
        navigate(isForgotFlow ? "/login" : "/employee/profile");
    }

    return (
        <main className="password-page">
            <section className="password-card">
                <div className="password-brand">accenture</div>

                <p className="password-portal-title">
                    Employee Education Loan Portal
                </p>

                <h1>
                    {isForgotFlow ? "Reset Password" : "Change Password"}
                </h1>

                <p className="password-subtitle">
                    {isForgotFlow
                        ? "Verify your email and create a new password."
                        : "Update your account password securely."}
                </p>

                {message && (
                    <div
                        className="password-message success"
                        role="status"
                    >
                        {message}
                    </div>
                )}

                {error && (
                    <div
                        className="password-message error"
                        role="alert"
                    >
                        {error}
                    </div>
                )}

                <form onSubmit={submit} noValidate>
                    {isForgotFlow && step === "email" && (
                        <div className="password-form-group">
                            <label htmlFor="reset-email">
                                Registered Email
                            </label>

                            <input
                                id="reset-email"
                                type="email"
                                value={email}
                                onChange={(event) =>
                                    setEmail(event.target.value)
                                }
                                placeholder="Enter your registered email"
                                autoComplete="email"
                                required
                            />
                        </div>
                    )}

                    {isForgotFlow && step === "otp" && (
                        <>
                            <div className="password-form-group">
                                <label htmlFor="otp">
                                    6-digit OTP
                                </label>

                                <input
                                    id="otp"
                                    type="text"
                                    inputMode="numeric"
                                    maxLength={6}
                                    value={otp}
                                    onChange={(event) =>
                                        setOtp(
                                            event.target.value.replace(
                                                /\D/g,
                                                ""
                                            )
                                        )
                                    }
                                    placeholder="Enter OTP"
                                    autoComplete="one-time-code"
                                    required
                                />
                            </div>

                            <p className="password-help">
                                OTP sent to {email}
                            </p>
                        </>
                    )}

                    {(!isForgotFlow || step === "reset") && (
                        <>
                            {!isForgotFlow && (
                                <div className="password-form-group">
                                    <label htmlFor="current-password">
                                        Current Password
                                    </label>

                                    <input
                                        id="current-password"
                                        type="password"
                                        value={currentPassword}
                                        onChange={(event) =>
                                            setCurrentPassword(
                                                event.target.value
                                            )
                                        }
                                        autoComplete="current-password"
                                        required
                                    />
                                </div>
                            )}

                            <div className="password-form-group">
                                <label htmlFor="new-password">
                                    New Password
                                </label>

                                <input
                                    id="new-password"
                                    type="password"
                                    value={newPassword}
                                    onChange={(event) =>
                                        setNewPassword(event.target.value)
                                    }
                                    autoComplete="new-password"
                                    required
                                />
                            </div>

                            <div className="password-form-group">
                                <label htmlFor="confirm-password">
                                    Confirm New Password
                                </label>

                                <input
                                    id="confirm-password"
                                    type="password"
                                    value={confirmPassword}
                                    onChange={(event) =>
                                        setConfirmPassword(
                                            event.target.value
                                        )
                                    }
                                    autoComplete="new-password"
                                    required
                                />
                            </div>
                        </>
                    )}

                    <button
                        className="password-submit"
                        type="submit"
                        disabled={busy}
                    >
                        {busy
                            ? "Please wait..."
                            : !isForgotFlow
                                ? "Update Password"
                                : step === "email"
                                    ? "Send OTP"
                                    : step === "otp"
                                        ? "Verify OTP"
                                        : "Reset Password"}
                    </button>
                </form>

                <button
                    className="password-back"
                    type="button"
                    onClick={handleBack}
                >
                    Back
                </button>
            </section>
        </main>
    );
}