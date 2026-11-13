using Microsoft.Extensions.Logging;

namespace AcmeCoffeeOnline.Web.Diagnostics;

internal static partial class AuthObservabilityLog
{
    [LoggerMessage(
        EventId = 4100,
        Level = LogLevel.Information,
        Message = "Magic link diagnostics requested for {EmailFilter} clearInbox={ClearInbox} capturedCount={CapturedCount}")]
    internal static partial void MagicLinkDiagnosticsRequested(
        ILogger logger,
        string EmailFilter,
        bool ClearInbox,
        int CapturedCount);

    [LoggerMessage(
        EventId = 4101,
        Level = LogLevel.Information,
        Message = "Seeded session issued for {MaskedEmail} userId={ResolvedUserId} roles={Roles} matchedExistingUser={MatchedExistingUser}")]
    internal static partial void SeededSessionIssued(
        ILogger logger,
        string MaskedEmail,
        string ResolvedUserId,
        string Roles,
        bool MatchedExistingUser);
}
