using System.Security.Claims;

namespace Dabbler.Platform.Stubs;

/// <summary>
/// Describes the user identity surfaced by <see cref="StubAuthenticationStateProvider"/>.
/// </summary>
public sealed class StubUserContext
{
    /// <summary>
    /// Whether the user should be treated as authenticated.
    /// </summary>
    public bool IsAuthenticated { get; init; } = true;

    /// <summary>
    /// Stable user identifier claim value.
    /// </summary>
    public string UserId { get; init; } = "stub-user";

    /// <summary>
    /// Display name claim value.
    /// </summary>
    public string DisplayName { get; init; } = "Stub User";

    /// <summary>
    /// Email claim value.
    /// </summary>
    public string Email { get; init; } = "stub.user@example.test";

    /// <summary>
    /// Assigned role claims.
    /// </summary>
    public IReadOnlyList<string> Roles { get; init; } = [];

    /// <summary>
    /// Optional active-organization claim value.
    /// </summary>
    public string? OrganizationId { get; init; }

    /// <summary>
    /// Additional claims to append to the identity.
    /// </summary>
    public IReadOnlyList<Claim> AdditionalClaims { get; init; } = [];
}
