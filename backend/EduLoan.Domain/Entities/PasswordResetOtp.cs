namespace EduLoan.Domain.Entities;

public class PasswordResetOtp
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Associated user
    public Guid UserId { get; set; }

    // Email address for which the OTP was generated
    public string Email { get; set; } = string.Empty;

    // Store the hashed OTP, never the plain OTP
    public string OtpHash { get; set; } = string.Empty;

    // OTP expiry time
    public DateTime ExpiresAtUtc { get; set; }

    // Number of verification attempts
    public int Attempts { get; set; }

    // Whether the OTP has already been used
    public bool IsUsed { get; set; }

    // When the OTP was generated
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // When the OTP was successfully verified
    public DateTime? VerifiedAtUtc { get; set; }

    // Used to authorize the password reset after OTP verification
    public string? ResetTokenHash { get; set; }

    public DateTime? ResetTokenExpiresAtUtc { get; set; }

    // Navigation property
    public User? User { get; set; }
}