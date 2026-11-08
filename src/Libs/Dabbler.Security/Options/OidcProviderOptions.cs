namespace Dabbler.Security.Options;

/// <summary>
/// Configuration options for an OpenID Connect (OIDC) authentication provider (e.g., Microsoft Entra ID).
/// </summary>
public sealed class OidcProviderOptions
{
    /// <summary>
    /// The OIDC authority URL (e.g., <c>https://login.microsoftonline.com/{tenantId}/v2.0</c>).
    /// This URL is used by the OIDC middleware to discover the provider's metadata.
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// The client (application) ID registered with the identity provider.
    /// </summary>
    /// <remarks>
    /// This identifies your application to the OIDC provider.
    /// </remarks>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// The client secret for confidential client flows.
    /// </summary>
    /// <remarks>
    /// This is a highly sensitive credential and must be protected.
    /// Do not hardcode; use secure configuration providers (e.g., Azure Key Vault, environment variables).
    /// </remarks>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Whether to automatically create an AppUser when a user logs in via OIDC for the first time.
    /// Default: <see langword="true"/>.
    /// </summary>
    public bool AutoProvisionUsers { get; set; } = true;

    /// <summary>
    /// Roles assigned to auto-provisioned users.
    /// These roles will be granted to new users created via this provider upon their first login.
    /// </summary>
    public string[] DefaultRoles { get; set; } = [];

    /// <summary>
    /// The callback path for the OIDC middleware.
    /// When <see langword="null"/>, defaults to <c>/signin-{scheme}</c> (e.g., <c>/signin-EntraId</c>)
    /// to avoid collisions when multiple OIDC providers are configured.
    /// Ensure this path is registered with the OIDC identity provider.
    /// </summary>
    public string? CallbackPath { get; set; }

    /// <summary>
    /// The signed-out callback path.
    /// When <see langword="null"/>, defaults to <c>/signout-callback-{scheme}</c>.
    /// Ensure this path is registered with the OIDC identity provider if handling sign-out.
    /// </summary>
    public string? SignedOutCallbackPath { get; set; }

    /// <summary>
    /// Whether OIDC metadata must be served over HTTPS.
    /// Default: <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// Keep this enabled by default in production environments to prevent man-in-the-middle
    /// attacks on the OIDC discovery document. Only disable it for trusted local development.
    /// </remarks>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// Display name shown on the login page button.
    /// Default: "Sign in with your organization account".
    /// </summary>
    public string DisplayName { get; set; } = "Sign in with your organization account";
}
