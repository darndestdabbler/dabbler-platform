using Microsoft.Extensions.Logging;

namespace Dabbler.Security.Services;

internal static partial class DevEmailSenderLog
{
    [LoggerMessage(
        EventId = 7100,
        Level = LogLevel.Information,
        Message = "Captured {EmailType} dev email for {MaskedEmail} hasCode={HasCode} hasLink={HasLink}")]
    internal static partial void DevEmailCaptured(
        ILogger logger,
        string EmailType,
        string MaskedEmail,
        bool HasCode,
        bool HasLink);
}
