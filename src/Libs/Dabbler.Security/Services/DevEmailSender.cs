using Dabbler.Security.Abstractions;
using Microsoft.Extensions.Logging;

namespace Dabbler.Security.Services;

/// <summary>
/// An implementation of <see cref="IEmailSender"/> for development environments.
/// Instead of sending real emails, it logs the email content to the console or other configured logging providers.
/// </summary>
/// <param name="logger">The logger to write email information to.</param>
public class DevEmailSender(ILogger<DevEmailSender> logger) : IEmailSender
{
    /// <inheritdoc/>
    public Task SendMagicLinkAsync(string email, string code, string linkUrl)
    {
        logger.LogInformation("DEV EMAIL MagicLink -> {Email} | Code: {Code} | Link: {Link}", Mask(email), code, linkUrl);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SendMfaEnrollmentAsync(string email, string qrCodeDataUri)
    {
        logger.LogInformation("DEV EMAIL MFA -> {Email} | QR: {Qr}", Mask(email), qrCodeDataUri);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SendAccountLockedAsync(string email)
    {
        logger.LogWarning("DEV EMAIL AccountLocked -> {Email}", Mask(email));
        return Task.CompletedTask;
    }

    private static string Mask(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1) return "***";
        return $"{email[0]}***{email[(at - 1)..]}";
    }
}
