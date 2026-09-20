using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using EduLoan.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using EduLoan.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace EduLoan.Application.Features.Auth;

public sealed class RequestPasswordResetOtpCommandHandler
    : IRequestHandler<RequestPasswordResetOtpCommand, MessageResponseDto>
{
    private readonly IAppDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public RequestPasswordResetOtpCommandHandler(
        IAppDbContext dbContext,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public async Task<MessageResponseDto> Handle(
        RequestPasswordResetOtpCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(
                user => user.Email.ToLower() == email &&
                        user.IsActive,
                cancellationToken);

        // Do not reveal whether the email exists.
        if (user is null)
        {
            return new MessageResponseDto(
                "If the email is registered, an OTP has been sent.");
        }

        var otp = RandomNumberGenerator
            .GetInt32(100000, 1000000)
            .ToString();

        var otpHash = CreateSha256Hash(otp);

        var previousOtps = await _dbContext.PasswordResetOtps
            .Where(x => x.UserId == user.Id && !x.IsUsed)
            .ToListAsync(cancellationToken);

        foreach (var previousOtp in previousOtps)
        {
            previousOtp.IsUsed = true;
        }

        var otpRecord = new PasswordResetOtp
        {
            UserId = user.Id,
            Email = email,
            OtpHash = otpHash,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
            Attempts = 0,
            IsUsed = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.AddPasswordResetOtp(otpRecord);

        _dbContext.AddPasswordResetOtp(otpRecord);

        await _dbContext.SaveChangesAsync(cancellationToken);

        Console.WriteLine($"OTP generated for: {email}");
        Console.WriteLine($"OTP value for testing: {otp}");

        try
        {
            await SendOtpEmailAsync(email, otp, cancellationToken);
            Console.WriteLine("OTP email sent successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OTP email failed: {ex}");
            throw;
        }

        return new MessageResponseDto(
            "If the email is registered, an OTP has been sent.");
    }

    private async Task SendOtpEmailAsync(
        string recipientEmail,
        string otp,
        CancellationToken cancellationToken)
    {
        var smtpHost = _configuration["Smtp:Host"]
            ?? throw new InvalidOperationException("SMTP host is missing.");

        var smtpPort = int.Parse(
            _configuration["Smtp:Port"] ?? "587");

        var smtpUsername = _configuration["Smtp:Username"]
            ?? throw new InvalidOperationException("SMTP username is missing.");

        var smtpPassword = _configuration["Smtp:Password"]
            ?? throw new InvalidOperationException("SMTP password is missing.");

        var fromEmail = _configuration["Smtp:From"] ?? smtpUsername;

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(fromEmail),
            Subject = "EduLoan Portal - Password Reset OTP",
            Body =
                $"Your EduLoan password reset OTP is: {otp}\n\n" +
                "This OTP is valid for 10 minutes.\n" +
                "If you did not request a password reset, please ignore this email.",
            IsBodyHtml = false
        };

        mailMessage.To.Add(recipientEmail);

        using var smtpClient = new SmtpClient(smtpHost, smtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(
                smtpUsername,
                smtpPassword)
        };

        cancellationToken.ThrowIfCancellationRequested();

        await smtpClient.SendMailAsync(mailMessage);
    }

    private static string CreateSha256Hash(string value)
    {
        var hashBytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(value));

        return Convert.ToHexString(hashBytes);
    }
}