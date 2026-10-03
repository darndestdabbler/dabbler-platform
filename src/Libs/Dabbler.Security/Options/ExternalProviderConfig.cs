namespace Dabbler.Security.Options;

/// <summary>
/// Internal registration record for an external authentication provider configured
/// via <see cref="DabblerSecurityOptions.AddEntraId"/> or
/// <see cref="DabblerSecurityOptions.AddExternalOAuth"/>.
/// </summary>
internal sealed class ExternalProviderConfig
{
    /// <summary>
    /// The authentication scheme name for the temporary external auth cookie.
    /// OIDC/OAuth middleware writes here; the callback handler converts to the main app cookie.
    /// </summary>
    internal const string ExternalAuthScheme = "DabblerExternalAuth";

    /// <summary>The provider name (e.g., "EntraId", "Google").</summary>
    public required string ProviderName { get; init; }

    /// <summary>The authentication scheme name used to register the OIDC/OAuth handler.</summary>
    public required string AuthenticationScheme { get; init; }

    /// <summary>OIDC-specific configuration. Non-null when this is an OIDC provider.</summary>
    public OidcProviderOptions? OidcOptions { get; init; }

    /// <summary>OAuth-specific configuration. Non-null when this is an OAuth provider.</summary>
    public OAuthProviderOptions? OAuthOptions { get; init; }

    /// <summary>True when this provider uses the OIDC protocol.</summary>
    public bool IsOidc => OidcOptions is not null;
}
