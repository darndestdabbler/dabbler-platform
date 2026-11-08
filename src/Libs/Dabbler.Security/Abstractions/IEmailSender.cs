namespace Dabbler.Security.Abstractions;

/// <summary>
/// Defines an interface for services responsible for sending various security-related emails.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends a magic link email for passwordless authentication or account verification.
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="code">The one-time code or token to be included in the link.</param>
    /// <param name="linkUrl">The base URL for the magic link, to which the code will be appended or used to construct the full link.</param>
    /// <returns>A task representing the asynchronous email sending operation.</returns>
    Task SendMagicLinkAsync(string email, string code, string linkUrl);

    /// <summary>
    /// Sends an email informing the user about MFA enrollment, often including QR code data.
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="qrCodeDataUri">The data URI for the QR code image, typically for scanning with an authenticator app.</param>
    /// <returns>A task representing the asynchronous email sending operation.</returns>
    Task SendMfaEnrollmentAsync(string email, string qrCodeDataUri);

    /// <summary>
    /// Sends an email notification to the user indicating their account has been locked.
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <returns>A task representing the asynchronous email sending operation.</returns>
    Task SendAccountLockedAsync(string email);
}
