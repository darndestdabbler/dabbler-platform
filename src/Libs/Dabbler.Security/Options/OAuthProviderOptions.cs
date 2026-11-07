namespace Dabbler.Security.Options;

/// <summary>
/// Configuration options for a generic OAuth authentication provider (e.g., Google, GitHub).
/// Reserved for future use.
/// </summary>
public sealed class OAuthProviderOptions
{
    /// <summary>
    /// The OAuth client ID registered with the provider.
    /// </summary>
    /// <remarks>
    /// This identifies your application to the OAuth provider.
    /// </remarks>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// The OAuth client secret for confidential client flows.
    /// </summary>
    /// <remarks>
    /// This is a highly sensitive credential and must be protected.
    /// Do not hardcode; use secure configuration providers (e.g., Azure Key Vault, environment variables).
    /// </remarks>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Whether to automatically create an AppUser on first OAuth login.
    /// Default: <see langword="true"/>.
    /// </summary>
    public bool AutoProvisionUsers { get; set; } = true;

    /// <summary>
    /// Roles assigned to auto-provisioned users.
    /// These roles will be granted to new users created via this provider upon their first login.
    /// </summary>
    public string[] DefaultRoles { get; set; } = [];

    /// <summary>
    /// Display name shown on the login page button (e.g., "Sign in with Google").
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// The callback path for the OAuth middleware.
    /// When <see langword="null"/>, defaults to <c>/signin-{providerName}</c> to avoid collisions
    /// when multiple OAuth providers are configured.
    /// Ensure this path is registered with the OAuth provider.
    /// </summary>
    public string? CallbackPath { get; set; }
}
