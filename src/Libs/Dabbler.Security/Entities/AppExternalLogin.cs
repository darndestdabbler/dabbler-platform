namespace Dabbler.Security.Entities;

/// <summary>
/// Represents a link between an external identity provider (IdP) account and an internal
/// <c>AppUser</c> record. This allows users to authenticate using external services
/// (e.g., Google, Microsoft Entra ID) and associate those logins with their Dabbler application profile.
/// </summary>
public class AppExternalLogin
{
    /// <summary>
    /// The unique identifier for this external login record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The foreign key to the internal <c>AppUser</c> record.
    /// This links the external login to a specific user within the application.
    /// <para>
    /// **Security Note:** This ID is critical for mapping external identities to internal users.
    /// Its integrity must be protected to prevent identity spoofing or incorrect user associations.
    /// </para>
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// The name of the external identity provider (e.g., "Google", "Microsoft", "Auth0").
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// The unique subject identifier provided by the external identity provider for the user.
    /// This value is typically opaque and provider-specific.
    /// <para>
    /// **Security Note:** This value is used to uniquely identify a user from a specific external IdP.
    /// It must be securely stored and used only for authentication and linking purposes to prevent
    /// account takeovers or mistaken identity.
    /// </para>
    /// </summary>
    public string ProviderSubjectId { get; set; } = string.Empty;

    /// <summary>
    /// The email address provided by the external identity provider, if available.
    /// This can be used for initial user provisioning or display purposes.
    /// </summary>
    public string? ProviderEmail { get; set; }

    /// <summary>
    /// The UTC timestamp when this external login was first linked to the <c>AppUser</c> account.
    /// </summary>
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The UTC timestamp when this external login was last used for authentication.
    /// This can be used for auditing or identifying stale login credentials.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// The navigation property for the associated <c>AppUser</c>.
    /// </summary>
    public AppUser? User { get; set; }
}
