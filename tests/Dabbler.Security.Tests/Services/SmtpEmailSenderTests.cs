using Dabbler.Security.Options;
using Dabbler.Security.Services;
using Microsoft.Extensions.Logging.Abstractions;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;

namespace Dabbler.Security.Tests.Services;

/// <summary>
/// Integration tests for SmtpEmailSender. These tests require a running Mailpit instance.
/// Start with: docker run -d --name mailpit -p 1025:1025 -p 8025:8025 axllent/mailpit
///
/// Tests are skipped automatically when Mailpit is not reachable, so they are safe
/// to include in the standard test run and will pass in CI without Docker.
/// </summary>
public class SmtpEmailSenderTests : IAsyncLifetime
{
    private const string MailpitSmtpHost = "localhost";
    private const int MailpitSmtpPort = 1025;
    private const string MailpitApiBase = "http://localhost:8025";
    private static readonly HttpClient _http = new();

    private bool _mailpitAvailable;

    public async Task InitializeAsync()
    {
        try
        {
            var response = await _http.GetAsync($"{MailpitApiBase}/api/v1/messages", new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token);
            _mailpitAvailable = response.IsSuccessStatusCode;
        }
        catch
        {
            _mailpitAvailable = false;
        }

        if (_mailpitAvailable)
        {
            // Purge all messages before each test run so assertions are clean
            await _http.DeleteAsync($"{MailpitApiBase}/api/v1/messages");
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private SmtpEmailSender CreateSender(TimeSpan? magicLinkExpiry = null, TimeSpan? lockoutDuration = null)
    {
        var opts = new DabblerSecurityOptions
        {
            MagicLinkExpiry = magicLinkExpiry ?? TimeSpan.FromMinutes(10),
            Email = new EmailOptions
            {
                SenderType = EmailSenderType.Smtp,
                SmtpHost = MailpitSmtpHost,
                SmtpPort = MailpitSmtpPort,
                SmtpUser = string.Empty,
                SmtpPassword = string.Empty,
                FromAddress = "noreply@test.local",
                FromName = "Test App",
                SmtpEnableSsl = false   // Mailpit does not use SSL
            },
            Mfa = new MfaOptions { LockoutDuration = lockoutDuration ?? TimeSpan.FromMinutes(30) }
        };

        return new SmtpEmailSender(MicrosoftOptions.Create(opts), NullLogger<SmtpEmailSender>.Instance);
    }

    private async Task<List<MailpitMessage>> GetMessagesAsync()
    {
        var json = await _http.GetStringAsync($"{MailpitApiBase}/api/v1/messages");
        var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        using var document = System.Text.Json.JsonDocument.Parse(json);

        if (document.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<MailpitMessage>>(json, options) ?? [];
        }

        var envelope = System.Text.Json.JsonSerializer.Deserialize<MailpitEnvelope>(json, options);
        return envelope?.Messages ?? [];
    }

    [Fact]
    public async Task SendMagicLinkAsync_DeliveredToMailpit()
    {
        if (!_mailpitAvailable)
            return; // Skip when Docker is not running

        var sender = CreateSender();
        await sender.SendMagicLinkAsync("recipient@test.local", "123456", "https://app/login?token=abc");

        await Task.Delay(300); // brief settle time
        var messages = await GetMessagesAsync();

        messages.Should().ContainSingle();
        var msg = messages[0];
        msg.To.Should().Contain(t => t.Address == "recipient@test.local");
        msg.Subject.Should().Contain("Login");
    }

    [Fact]
    public async Task SendMfaEnrollmentAsync_DeliveredToMailpit()
    {
        if (!_mailpitAvailable)
            return;

        var sender = CreateSender();
        await sender.SendMfaEnrollmentAsync("mfa@test.local", "data:image/png;base64,AAAA");

        await Task.Delay(300);
        var messages = await GetMessagesAsync();

        messages.Should().ContainSingle();
        messages[0].Subject.Should().Contain("MFA");
    }

    [Fact]
    public async Task SendAccountLockedAsync_DeliveredToMailpit()
    {
        if (!_mailpitAvailable)
            return;

        var sender = CreateSender(lockoutDuration: TimeSpan.FromMinutes(30));
        await sender.SendAccountLockedAsync("locked@test.local");

        await Task.Delay(300);
        var messages = await GetMessagesAsync();

        messages.Should().ContainSingle();
        messages[0].Subject.Should().Contain("Locked");
    }

    [Fact]
    public async Task SendMagicLinkAsync_EmailBodyContainsCodeAndLink()
    {
        if (!_mailpitAvailable)
            return;

        var sender = CreateSender(magicLinkExpiry: TimeSpan.FromMinutes(10));
        await sender.SendMagicLinkAsync("body@test.local", "654321", "https://app/login?token=xyz");

        await Task.Delay(300);
        var messages = await GetMessagesAsync();
        messages.Should().ContainSingle();

        // Fetch full message to inspect body
        var msgId = messages[0].ID;
        var bodyJson = await _http.GetStringAsync($"{MailpitApiBase}/api/v1/messages/{msgId}");
        bodyJson.Should().Contain("654321");
        bodyJson.Should().Contain("https://app/login?token");
        bodyJson.Should().Contain("xyz");
        bodyJson.Should().Contain("10"); // expiry minutes
    }

    [Fact]
    public async Task SendMagicLinkAsync_UnreachableHost_ThrowsException()
    {
        var opts = new DabblerSecurityOptions
        {
            MagicLinkExpiry = TimeSpan.FromMinutes(10),
            Email = new EmailOptions
            {
                SmtpHost = "invalid.nonexistent.host.local",
                SmtpPort = 1025,
                FromAddress = "noreply@test.local",
                FromName = "Test",
                SmtpEnableSsl = false
            },
            Mfa = new MfaOptions()
        };

        var sender = new SmtpEmailSender(MicrosoftOptions.Create(opts), NullLogger<SmtpEmailSender>.Instance);

        var act = async () => await sender.SendMagicLinkAsync("user@test.local", "000000", "https://app");
        await act.Should().ThrowAsync<Exception>();
    }

    // Minimal DTO for deserialising Mailpit /api/v1/messages response
    private class MailpitEnvelope
    {
        public List<MailpitMessage> Messages { get; set; } = [];
    }

    private class MailpitMessage
    {
        private string _subject = string.Empty;

        public string ID { get; set; } = string.Empty;
        public string Subject
        {
            get => !string.IsNullOrWhiteSpace(_subject)
                ? _subject
                : Content?.Headers?.TryGetValue("Subject", out var values) == true
                    ? values.FirstOrDefault() ?? string.Empty
                    : string.Empty;
            set => _subject = value;
        }

        public List<MailpitAddress> To { get; set; } = [];
        public MailpitContent? Content { get; set; }
    }

    private class MailpitAddress
    {
        private string _address = string.Empty;

        public string Address
        {
            get => !string.IsNullOrWhiteSpace(_address)
                ? _address
                : string.IsNullOrWhiteSpace(Mailbox) || string.IsNullOrWhiteSpace(Domain)
                    ? string.Empty
                    : $"{Mailbox}@{Domain}";
            set => _address = value;
        }

        public string Mailbox { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
    }

    private class MailpitContent
    {
        public Dictionary<string, string[]> Headers { get; set; } = [];
    }
}
