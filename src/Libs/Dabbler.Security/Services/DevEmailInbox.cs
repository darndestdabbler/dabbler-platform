using System.Collections.Concurrent;

namespace Dabbler.Security.Services;

/// <summary>
/// Stores development-only email payloads so local and automated tests can
/// inspect magic links without relying on external mail infrastructure.
/// </summary>
public class DevEmailInbox
{
    private readonly ConcurrentQueue<DevEmailMessage> _messages = new();

    /// <summary>Adds a development-only email payload to the inbox.</summary>
    public void Add(DevEmailMessage message)
    {
        _messages.Enqueue(message);
    }

    /// <summary>Returns every message currently held by the inbox, in arrival order.</summary>
    public IReadOnlyList<DevEmailMessage> GetAll()
    {
        return _messages.ToArray();
    }

    /// <summary>Removes all queued messages.</summary>
    public void Clear()
    {
        while (_messages.TryDequeue(out _))
        {
        }
    }
}

/// <summary>A captured development-only email payload (magic link, verification code, etc.).</summary>
/// <param name="EmailType">Discriminator identifying the email template (e.g. "magic-link", "verification").</param>
/// <param name="Email">The recipient email address.</param>
/// <param name="Code">The one-time code embedded in the email, when applicable.</param>
/// <param name="LinkUrl">The absolute link surfaced in the email body, when applicable.</param>
/// <param name="QrCodeDataUri">A data-URI PNG of the link encoded as a QR code, when applicable.</param>
/// <param name="CreatedAtUtc">Timestamp marking when the message was queued.</param>
public sealed record DevEmailMessage(
    string EmailType,
    string Email,
    string? Code,
    string? LinkUrl,
    string? QrCodeDataUri,
    DateTimeOffset CreatedAtUtc);
