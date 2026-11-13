namespace Dabbler.Security.Entities;

/// <summary>
/// Defines the various authentication schemes supported by the Dabbler application.
/// These schemes represent different ways users can log in or authenticate their identity.
/// </summary>
public enum DabblerAuthScheme
{
    /// <summary>
    /// Authentication via a secure, time-limited link sent to a user's email address.
    /// This typically does not require a password.
    /// </summary>
    MagicLink,
    /// <summary>
    /// Authentication via Microsoft Entra ID (formerly Azure Active Directory).
    /// </summary>
    EntraId,
    /// <summary>
    /// Authentication via a generic external OAuth 2.0 provider (e.g., Google, GitHub).
    /// </summary>
    ExternalOAuth
}
