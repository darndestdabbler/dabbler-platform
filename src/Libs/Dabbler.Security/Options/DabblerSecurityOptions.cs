using Dabbler.Security.Registration;

namespace Dabbler.Security.Options;

/// <summary>
/// Represents the central configuration options for the Dabbler Security framework.
/// </summary>
public sealed class DabblerSecurityOptions
{
    /// <summary>
    /// The name of the configuration section for Dabbler Security settings.
    /// </summary>
    public const string SectionName = "DabblerSecurity";

    /// <summary>
    /// The connection string for the Dabbler Security database (e.g., for user management, audit logs).
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// The duration for which magic links (e.g., for passwordless login or MFA setup) remain valid.
    /// </summary>
    public TimeSpan MagicLinkExpiry { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Determines if Multi-Factor Authentication (MFA) is globally required for all users.
    /// If true, users will be prompted to set up MFA upon first login.
    /// </summary>
    public bool RequireMfa { get; set; }

    /// <summary>
    /// A secret key used for internal service-to-service authentication within the Dabbler application.
    /// **Security Critical:** Keep this key secure and use strong, randomly generated values. Do not expose publicly.
    /// </summary>
    public string InternalApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Configuration options for sending emails, primarily for security-related notifications like magic links,
    /// MFA codes, or password resets.
    /// </summary>
    public EmailOptions Email { get; set; } = new();

    /// <summary>
    /// Configuration options specific to Multi-Factor Authentication (MFA).
    /// </summary>
    public MfaOptions Mfa { get; set; } = new();

    /// <summary>
    /// Configuration options for API and request rate limiting.
    /// </summary>
    public RateLimitingOptions RateLimiting { get; set; } = new();

    /// <summary>
    /// Controls what happens when an authorization check targets a resource that was not
    /// registered via <see cref="RegisterPages"/> / <see cref="RegisterEntities"/> / <see cref="RegisterEndpoints"/> / <see cref="RegisterControls"/>.
    /// Defaults to <see cref="UnregisteredResourcePolicy.FailOpen"/> (allow access).
    /// </summary>
    public UnregisteredResourcePolicy UnregisteredResourcePolicy { get; set; } =
        UnregisteredResourcePolicy.FailOpen;

    // Internal collections populated by the fluent registration methods below.
    internal List<RegisteredPage> RegisteredPages { get; } = [];
    internal List<RegisteredEntity> RegisteredEntities { get; } = [];
    internal List<RegisteredEndpoint> RegisteredEndpoints { get; } = [];
    internal List<RegisteredControl> RegisteredControls { get; } = [];

    // External authentication providers configured via AddEntraId() / AddExternalOAuth().
    internal List<ExternalProviderConfig> ExternalProviders { get; } = [];

    /// <summary>
    /// Returns display information for all configured external authentication providers.
    /// Used by the login page to render provider sign-in buttons.
    /// </summary>
    /// <returns>A read-only list of configured external providers' display information.</returns>
    public IReadOnlyList<ExternalProviderDisplayInfo> GetConfiguredProviders()
    {
        return ExternalProviders.Select(p => new ExternalProviderDisplayInfo(
            p.ProviderName,
            p.AuthenticationScheme,
            p.IsOidc ? p.OidcOptions!.DisplayName : p.OAuthOptions!.DisplayName,
            p.IsOidc
        )).ToList().AsReadOnly();
    }

    /// <summary>
    /// Declares the pages that can have role-based access configured.
    /// </summary>
    /// <param name="configure">An action to configure a <see cref="PageRegistrar"/>, where individual pages and their permissions are defined.</param>
    /// <returns>The current <see cref="DabblerSecurityOptions"/> instance for chaining.</returns>
    public DabblerSecurityOptions RegisterPages(Action<PageRegistrar> configure)
    {
        var registrar = new PageRegistrar();
        configure(registrar);
        RegisteredPages.AddRange(registrar.Build());
        return this;
    }

    /// <summary>
    /// Declares the entities that can have CRUD and field-level permissions configured.
    /// </summary>
    /// <param name="configure">An action to configure an <see cref="EntityRegistrar"/>, where entities and their permissions are defined.</param>
    /// <returns>The current <see cref="DabblerSecurityOptions"/> instance for chaining.</returns>
    public DabblerSecurityOptions RegisterEntities(Action<EntityRegistrar> configure)
    {
        var registrar = new EntityRegistrar();
        configure(registrar);
        RegisteredEntities.AddRange(registrar.Build());
        return this;
    }

    /// <summary>
    /// Declares the API endpoints that can have role-based access configured.
    /// </summary>
    /// <param name="configure">An action to configure an <see cref="EndpointRegistrar"/>, where endpoints and their permissions are defined.</param>
    /// <returns>The current <see cref="DabblerSecurityOptions"/> instance for chaining.</returns>
    public DabblerSecurityOptions RegisterEndpoints(Action<EndpointRegistrar> configure)
    {
        var registrar = new EndpointRegistrar();
        configure(registrar);
        RegisteredEndpoints.AddRange(registrar.Build());
        return this;
    }

    /// <summary>
    /// Declares the UI controls that can have per-role state configured.
    /// </summary>
    /// <param name="configure">An action to configure a <see cref="ControlRegistrar"/>, where UI controls and their permissions are defined.</param>
    /// <returns>The current <see cref="DabblerSecurityOptions"/> instance for chaining.</returns>
    public DabblerSecurityOptions RegisterControls(Action<ControlRegistrar> configure)
    {
        var registrar = new ControlRegistrar();
        configure(registrar);
        RegisteredControls.AddRange(registrar.Build());
        return this;
    }

    /// <summary>
    /// Registers Microsoft Entra ID (OpenID Connect) as an external authentication provider.
    /// When configured, the login page will show an OIDC sign-in button and the OIDC
    /// authentication scheme will be registered alongside the existing cookie scheme.
    /// </summary>
    /// <param name="configure">An action to configure <see cref="OidcProviderOptions"/> specific to Entra ID.</param>
    /// <returns>The current <see cref="DabblerSecurityOptions"/> instance for chaining.</returns>
    public DabblerSecurityOptions AddEntraId(Action<OidcProviderOptions> configure)
    {
        var oidcOptions = new OidcProviderOptions();
        configure(oidcOptions);
        ExternalProviders.Add(new ExternalProviderConfig
        {
            ProviderName = "EntraId",
            AuthenticationScheme = "EntraId",
            OidcOptions = oidcOptions
        });
        return this;
    }

    /// <summary>
    /// Registers a generic external OAuth provider (e.g., Google, GitHub).
    /// Reserved for future use.
    /// </summary>
    /// <param name="providerName">A unique name for the OAuth provider.</param>
    /// <param name="configure">An action to configure <see cref="OAuthProviderOptions"/> specific to the provider.</param>
    /// <returns>The current <see cref="DabblerSecurityOptions"/> instance for chaining.</returns>
    public DabblerSecurityOptions AddExternalOAuth(string providerName, Action<OAuthProviderOptions> configure)
    {
        var oauthOptions = new OAuthProviderOptions();
        configure(oauthOptions);
        ExternalProviders.Add(new ExternalProviderConfig
        {
            ProviderName = providerName,
            AuthenticationScheme = providerName,
            OAuthOptions = oauthOptions
        });
        return this;
    }
}

/// <summary>
/// Configuration options for the email sender used by Dabbler Security.
/// </summary>
public sealed class EmailOptions
{
    /// <summary>
    /// Specifies the type of email sender to use.
    /// </summary>
    public EmailSenderType SenderType { get; set; } = EmailSenderType.DevMailHog;

    /// <summary>
    /// The hostname or IP address of the SMTP server.
    /// </summary>
    public string SmtpHost { get; set; } = string.Empty;

    /// <summary>
    /// The port number for the SMTP server.
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// The username for SMTP authentication.
    /// </summary>
    public string SmtpUser { get; set; } = string.Empty;

    /// <summary>
    /// The password for SMTP authentication. **Security Critical:** Keep this secure. Use environment variables or a secrets manager.
    /// </summary>
    public string SmtpPassword { get; set; } = string.Empty;

    /// <summary>
    /// The email address from which security-related emails will be sent.
    /// </summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>
    /// The display name for the email sender.
    /// </summary>
    public string FromName { get; set; } = string.Empty;

    /// <summary>
    /// Whether to use SSL/TLS for SMTP. Set to false when testing against Mailpit or MailHog (default: true).
    /// </summary>
    public bool SmtpEnableSsl { get; set; } = true;
}

/// <summary>
/// Configuration options for Multi-Factor Authentication (MFA).
/// </summary>
public sealed class MfaOptions
{
    /// <summary>
    /// The maximum number of failed MFA attempts before a user's account is temporarily locked out.
    /// </summary>
    public int LockoutThreshold { get; set; } = 5;

    /// <summary>
    /// The duration for which a user's account remains locked out after exceeding the <see cref="LockoutThreshold"/>.
    /// </summary>
    public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// The symmetric encryption key used to encrypt and decrypt MFA setup data (e.g., TOTP secrets) stored in the database.
    /// **Security Critical:** This key must be strong, unique, and kept highly secure. Losing this key will make all existing MFA setups unusable. Use environment variables or a secrets manager.
    /// </summary>
    public string EncryptionKey { get; set; } = string.Empty;

    /// <summary>
    /// A list of role names for which MFA is always required, regardless of the global <see cref="DabblerSecurityOptions.RequireMfa"/> setting.
    /// </summary>
    public List<string> RequireMfaForRoles { get; set; } = [];
}

/// <summary>
/// Specifies different types of email senders supported by Dabbler Security.
/// </summary>
public enum EmailSenderType
{
    /// <summary>
    /// Uses a development-friendly email catcher like MailHog or Mailpit for local testing.
    /// </summary>
    DevMailHog = 0,
    /// <summary>
    /// Uses a standard SMTP server for sending emails.
    /// </summary>
    Smtp = 1
}

/// <summary>
/// Configuration options for rate limiting within Dabbler Security.
/// </summary>
public sealed class RateLimitingOptions
{
    /// <summary>
    /// When true, uses <c>IDistributedCache</c> for rate limit state instead of
    /// in-memory storage. Requires a distributed cache provider (Redis, SQL Server, etc.)
    /// to be registered in the service collection. Default is false (in-memory).
    /// </summary>
    public bool UseDistributedCache { get; set; }
}

/// <summary>
/// Controls what happens when an authorization check targets a resource that was not
/// registered via <see cref="DabblerSecurityOptions.RegisterPages"/> / <see cref="DabblerSecurityOptions.RegisterEntities"/> / <see cref="DabblerSecurityOptions.RegisterEndpoints"/> / <see cref="DabblerSecurityOptions.RegisterControls"/>.
/// </summary>
public enum UnregisteredResourcePolicy
{
    /// <summary>
    /// Access is granted when the resource has not been registered. Suitable for brownfield
    /// adoption where not every resource is registered up-front.
    /// </summary>
    FailOpen = 0,

    /// <summary>
    /// Access is denied when the resource has not been registered. Use for zero-trust
    /// deployments where every resource must be explicitly declared.
    /// </summary>
    FailClosed = 1
}
