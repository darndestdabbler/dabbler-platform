namespace Dabbler.Security.Services;

/// <summary>
/// Scoped service that exposes a stable <see cref="Current"/> correlation
/// identifier shared by every audit row emitted as part of a single Blazor
/// circuit invocation or HTTP request. The full implementation (Blazor circuit
/// lifecycle hookup, regenerate on navigation, ASP.NET Core
/// <c>ResetForRequest</c> entry point) lands in Session 3 of the
/// sensitive-field-masking-audit set; Session 2 ships a placeholder that
/// generates a single Guid per scope so the auditor service has a real
/// dependency to consume now and the audit rows already share a CorrelationId
/// per scope.
/// </summary>
public interface IRenderCorrelationScope
{
    /// <summary>
    /// The active correlation identifier for the current scope.
    /// </summary>
    Guid Current { get; }

    /// <summary>
    /// Resets the active correlation identifier — used by the (forthcoming)
    /// HTTP entry point to start a fresh correlation per request.
    /// </summary>
    void ResetForRequest();
}

/// <summary>
/// Session 2 placeholder for <see cref="IRenderCorrelationScope"/>. Generates
/// a single Guid per scope; the Session 3 implementation extends this with
/// circuit-aware regeneration on navigation.
/// </summary>
public sealed class RenderCorrelationScope : IRenderCorrelationScope
{
    private Guid _current = Guid.NewGuid();

    /// <inheritdoc />
    public Guid Current => _current;

    /// <inheritdoc />
    public void ResetForRequest() => _current = Guid.NewGuid();
}
