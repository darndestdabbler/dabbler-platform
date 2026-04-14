using System.Collections.Concurrent;

namespace Dabbler.Security.Services;

/// <summary>
/// Stores development-only email payloads so local and automated tests can
/// inspect magic links without relying on external mail infrastructure.
/// </summary>
public class DevEmailInbox
{
    private readonly ConcurrentQueue<DevEmailMessage> _messages = new();

    public void Add(DevEmailMessage message)
    {
        _messages.Enqueue(message);
    }

    public IReadOnlyList<DevEmailMessage> GetAll()
    {
        return _messages.ToArray();
    }

    public void Clear()
    {
        while (_messages.TryDequeue(out _))
        {
        }
    }
}

public sealed record DevEmailMessage(
    string EmailType,
    string Email,
    string? Code,
    string? LinkUrl,
    string? QrCodeDataUri,
    DateTimeOffset CreatedAtUtc);
