using System.Net;
using System.Net.Mail;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dabbler.Security.Services;

/// <summary>
/// An implementation of <see cref="IEmailSender"/> that sends emails using an SMTP server.
/// This is intended for use in production environments.
/// </summary>
/// <param name="options">The security options containing SMTP configuration.</param>
/// <param name="logger">The logger for recording email sending activity and errors.</param>
public class SmtpEmailSender(
    IOptions<DabblerSecurityOptions> options,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    /// <inheritdoc/>
    public async Task SendMagicLinkAsync(string email, string code, string linkUrl)
    {
        var encodedUrl = WebUtility.HtmlEncode(linkUrl);
        var encodedCode = WebUtility.HtmlEncode(code);

        var subject = "Your Login Link";
        var body = $@"
<p>Click the link below to log in:</p>
<p><a href=""{encodedUrl}"">{encodedUrl}</a></p>
<p>Or enter this code: <strong>{encodedCode}</strong></p>
<p>This link expires in {options.Value.MagicLinkExpiry.TotalMinutes} minutes.</p>";

        await SendEmailAsync(email, subject, body);
    }

    /// <inheritdoc/>
    public async Task SendMfaEnrollmentAsync(string email, string qrCodeDataUri)
    {
        var encodedUri = WebUtility.HtmlEncode(qrCodeDataUri);

        var subject = "MFA Enrollment";
        var body = $@"
<p>Scan the QR code below with your authenticator app to enroll in MFA:</p>
<p><img src=""{encodedUri}"" alt=""QR Code"" /></p>";

        await SendEmailAsync(email, subject, body);
    }

    /// <inheritdoc/>
    public async Task SendAccountLockedAsync(string email)
    {
        var subject = "Account Locked";
        var body = $@"
<p>Your account has been locked due to too many failed login attempts.</p>
<p>It will be unlocked in {options.Value.Mfa.LockoutDuration.TotalMinutes} minutes.</p>";

        await SendEmailAsync(email, subject, body);
    }

    private async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        try
        {
            var emailOptions = options.Value.Email;
            using var client = new SmtpClient(emailOptions.SmtpHost, emailOptions.SmtpPort)
            {
                Credentials = new NetworkCredential(emailOptions.SmtpUser, emailOptions.SmtpPassword),
                EnableSsl = emailOptions.SmtpEnableSsl
            };

            var message = new MailMessage
            {
                From = new MailAddress(emailOptions.FromAddress, emailOptions.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(to);

            await client.SendMailAsync(message);
            logger.LogInformation("Email sent to {Email} with subject {Subject}", Mask(to), subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {Email}", Mask(to));
            throw;
        }
    }

    private static string Mask(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1) return "***";
        return $"{email[0]}***{email[(at - 1)..]}";
    }
}
