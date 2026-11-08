namespace Dabbler.Security.Options;

/// <summary>
/// Public read-only descriptor of a configured external authentication provider.
/// Used by the login page UI to render provider buttons.
/// </summary>
/// <param name="ProviderName">A unique, internal name for the provider (e.g., "EntraId", "Google").</param>
/// <param name="AuthenticationScheme">The authentication scheme name used to register the provider's handler.</param>
/// <param name="DisplayName">The user-friendly name to display on the login button (e.g., "Sign in with Microsoft").</param>
/// <param name="IsOidc"><see langword="true"/> if the provider uses the OpenID Connect protocol; <see langword="false"/> otherwise (e.g., generic OAuth).</param>
public sealed record ExternalProviderDisplayInfo(
    string ProviderName,
    string AuthenticationScheme,
    string DisplayName,
    bool IsOidc);
