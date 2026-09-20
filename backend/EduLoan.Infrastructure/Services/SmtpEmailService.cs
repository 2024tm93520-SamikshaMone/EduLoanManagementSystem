using System.Net;
using System.Net.Mail;
using EduLoan.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace EduLoan.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public SmtpEmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendAsync(string recipientEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        var host = _configuration["Smtp:Host"];
        var portText = _configuration["Smtp:Port"];
        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];
        var from = _configuration["Smtp:From"] ?? username;

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(from))
        {
            throw new InvalidOperationException("SMTP settings are not configured.");
        }

        var port = int.TryParse(portText, out var parsedPort) ? parsedPort : 587;

        using var message = new MailMessage(from, recipientEmail)
        {
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(username, password)
        };

        ct.ThrowIfCancellationRequested();
        await client.SendMailAsync(message);
    }
}
