namespace Dabbler.Security.Services;

/// <summary>
/// Scoped service that exposes a stable <see cref="Current"/> correlation
/// identifier shared by every audit row emitted as part of a single Blazor
/// circuit invocation or HTTP request. The same scope also de-duplicates
/// per-correlation events through <see cref="TryRecordOnce"/>, which the
/// restriction binder uses to batch <c>FieldUnmaskedRead</c> emissions so
/// repainting a 50-row grid with three sensitive columns produces at most
/// one row per <c>(CorrelationId, EntityKey, EntityId, ColumnKey)</c>.
/// </summary>
public interface IRenderCorrelationScope
{
    /// <summary>
    /// The active correlation identifier for the current scope. Stable for
    /// the duration of a single Blazor render cycle / HTTP request and
    /// regenerated on navigation (Blazor) or via <see cref="ResetForRequest"/>
    /// (HTTP API contexts).
    /// </summary>
    Guid Current { get; }

    /// <summary>
    /// Resets the active correlation identifier and clears the
    /// <see cref="TryRecordOnce"/> de-duplication set. Called by the HTTP
    /// pipeline at the start of each request so handlers that resolve
    /// restrictions in the API layer share a per-request correlation rather
    /// than a per-process one.
    /// </summary>
    void ResetForRequest();

    /// <summary>
    /// Records that an event keyed by <paramref name="key"/> has been
    /// emitted under the current correlation. Returns <c>true</c> the first
    /// time a key is presented under a given correlation (so the caller
    /// proceeds with the emission); returns <c>false</c> on subsequent
    /// presentations of the same key (so the caller skips it). The set is
    /// cleared whenever <see cref="Current"/> changes (navigation, request
    /// boundary, explicit reset).
    /// </summary>
    /// <param name="key">
    /// An opaque string the caller composes from the event's identity. The
    /// binder uses
    /// <c>"{CorrelationId}|{EntityKey}|{EntityId}|{ColumnKey}"</c>; other
    /// callers may compose their own as long as the format is stable for
    /// the same logical event.
    /// </param>
    bool TryRecordOnce(string key);
}
