using System.Security.Cryptography;
using System.Text;
using EduLoan.Application.Common;
using EduLoan.Application.Interfaces;
using EduLoan.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduLoan.Application.Features.Auth;

public record RequestPasswordResetOtpCommand(string Email) : IRequest<MessageResponseDto>;
public record VerifyPasswordResetOtpCommand(string Email, string Otp) : IRequest<VerifyOtpResponseDto>;
public record ResetPasswordCommand(string Email, string ResetToken, string NewPassword, string ConfirmPassword) : IRequest<MessageResponseDto>;
public record ChangePasswordCommand(string CurrentPassword, string NewPassword, string ConfirmPassword) : IRequest<MessageResponseDto>;

public class RequestPasswordResetOtpHandler : IRequestHandler<RequestPasswordResetOtpCommand, MessageResponseDto>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IEmailService _email;

    public RequestPasswordResetOtpHandler(IAppDbContext db, IPasswordHasher hasher, IEmailService email)
    {
        _db = db; _hasher = hasher; _email = email;
    }

    public async Task<MessageResponseDto> Handle(RequestPasswordResetOtpCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == email && x.IsActive, ct);

        // Do not reveal whether an email exists.
        if (user is null)
            return new MessageResponseDto("If the email is registered, an OTP has been sent.");

        var previous = await _db.PasswordResetOtps
            .Where(x => x.UserId == user.Id && !x.IsUsed)
            .ToListAsync(ct);
        foreach (var item in previous) item.IsUsed = true;

        var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var record = new PasswordResetOtp
        {
            UserId = user.Id,
            Email = email,
            OtpHash = _hasher.Hash(otp),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
            Attempts = 0,
            IsUsed = false
        };
        _db.AddPasswordResetOtp(record);
        await _db.SaveChangesAsync(ct);

        var body = $"<p>Hello {System.Net.WebUtility.HtmlEncode(user.FullName)},</p>" +
                   $"<p>Your EduLoan password reset OTP is <strong>{otp}</strong>.</p>" +
                   "<p>This OTP expires in 5 minutes. If you did not request this, ignore this email.</p>";
        await _email.SendAsync(email, "EduLoan Password Reset OTP", body, ct);

        return new MessageResponseDto("If the email is registered, an OTP has been sent.");
    }
}

public class VerifyPasswordResetOtpHandler : IRequestHandler<VerifyPasswordResetOtpCommand, VerifyOtpResponseDto>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;

    public VerifyPasswordResetOtpHandler(IAppDbContext db, IPasswordHasher hasher)
    { _db = db; _hasher = hasher; }

    public async Task<VerifyOtpResponseDto> Handle(VerifyPasswordResetOtpCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var record = await _db.PasswordResetOtps
            .Where(x => x.Email == email && !x.IsUsed)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (record is null || record.ExpiresAtUtc <= DateTime.UtcNow || record.Attempts >= 5)
            throw new AuthenticationFailedException("Invalid or expired OTP.");

        if (!_hasher.Verify(request.Otp.Trim(), record.OtpHash))
        {
            record.Attempts++;
            await _db.SaveChangesAsync(ct);
            throw new AuthenticationFailedException("Invalid or expired OTP.");
        }

        record.IsUsed = true;
        record.VerifiedAtUtc = DateTime.UtcNow;
        var resetToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        record.ResetTokenHash = Sha256(resetToken);
        record.ResetTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(10);
        await _db.SaveChangesAsync(ct);

        return new VerifyOtpResponseDto(resetToken);
    }

    internal static string Sha256(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}

public class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand, MessageResponseDto>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;

    public ResetPasswordHandler(IAppDbContext db, IPasswordHasher hasher)
    { _db = db; _hasher = hasher; }

    public async Task<MessageResponseDto> Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        if (request.NewPassword != request.ConfirmPassword)
            throw new AuthenticationFailedException("Passwords do not match.");

        var email = request.Email.Trim().ToLowerInvariant();
        var record = await _db.PasswordResetOtps
            .Where(x => x.Email == email && x.VerifiedAtUtc != null)
            .OrderByDescending(x => x.VerifiedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (record is null || record.ResetTokenExpiresAtUtc <= DateTime.UtcNow ||
            string.IsNullOrWhiteSpace(record.ResetTokenHash) ||
            !string.Equals(record.ResetTokenHash, VerifyPasswordResetOtpHandler.Sha256(request.ResetToken), StringComparison.OrdinalIgnoreCase))
            throw new AuthenticationFailedException("Invalid or expired reset session.");

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == record.UserId && x.IsActive, ct);
        if (user is null) throw new AuthenticationFailedException("Unable to reset password.");

        user.PasswordHash = _hasher.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        record.ResetTokenHash = null;
        record.ResetTokenExpiresAtUtc = null;
        await _db.SaveChangesAsync(ct);

        return new MessageResponseDto("Password reset successful. You can now log in.");
    }
}

public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, MessageResponseDto>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ICurrentUserService _currentUser;

    public ChangePasswordHandler(IAppDbContext db, IPasswordHasher hasher, ICurrentUserService currentUser)
    { _db = db; _hasher = hasher; _currentUser = currentUser; }

    public async Task<MessageResponseDto> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null) throw new AuthenticationFailedException("Login required.");
        if (request.NewPassword != request.ConfirmPassword)
            throw new AuthenticationFailedException("Passwords do not match.");

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == _currentUser.UserId.Value && x.IsActive, ct);
        if (user is null || !_hasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new AuthenticationFailedException("Current password is incorrect.");

        user.PasswordHash = _hasher.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new MessageResponseDto("Password changed successfully.");
    }
}
