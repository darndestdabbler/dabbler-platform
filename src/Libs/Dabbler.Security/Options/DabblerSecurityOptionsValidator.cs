using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dabbler.Security.Options;

/// <summary>
/// Validates the configuration settings for <see cref="DabblerSecurityOptions"/> at application startup.
/// Ensures critical security-related options are correctly configured to prevent runtime errors or vulnerabilities.
/// </summary>
public class DabblerSecurityOptionsValidator : IValidateOptions<DabblerSecurityOptions>
{
    private readonly ILogger<DabblerSecurityOptionsValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DabblerSecurityOptionsValidator"/> class.
    /// </summary>
    /// <param name="logger">The logger used to report validation advisories and errors.</param>
    public DabblerSecurityOptionsValidator(ILogger<DabblerSecurityOptionsValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates the provided <see cref="DabblerSecurityOptions"/> instance.
    /// </summary>
    /// <param name="name">The name of the options instance being validated. Typically <see cref="global::Microsoft.Extensions.Options.Options.DefaultName"/>.</param>
    /// <param name="options">The <see cref="DabblerSecurityOptions"/> instance to validate.</param>
    /// <returns>A <see cref="ValidateOptionsResult"/> indicating whether the options are valid or contain failures.</returns>
    public ValidateOptionsResult Validate(string? name, DabblerSecurityOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
            failures.Add("DabblerSecurity:ConnectionString is required.");

        if (string.IsNullOrWhiteSpace(options.InternalApiKey))
            failures.Add("DabblerSecurity:InternalApiKey is required.");

        if ((options.RequireMfa || options.Mfa.RequireMfaForRoles.Count > 0)
            && string.IsNullOrWhiteSpace(options.Mfa.EncryptionKey))
        {
            failures.Add("DabblerSecurity:Mfa:EncryptionKey is required when MFA is enabled globally or for any role.");
        }

        if (options.Mfa.LockoutThreshold <= 0)
            failures.Add("DabblerSecurity:Mfa:LockoutThreshold must be greater than 0.");

        if (options.Email.SenderType == EmailSenderType.Smtp)
        {
            if (string.IsNullOrWhiteSpace(options.Email.SmtpHost))
                failures.Add("DabblerSecurity:Email:SmtpHost is required for SMTP.");

            if (options.Email.SmtpPort <= 0 || options.Email.SmtpPort > 65535)
                failures.Add("DabblerSecurity:Email:SmtpPort must be between 1 and 65535 for SMTP.");

            if (string.IsNullOrWhiteSpace(options.Email.FromAddress))
                failures.Add("DabblerSecurity:Email:FromAddress is required for SMTP.");
        }

        // Validate external provider configurations.
        foreach (var provider in options.ExternalProviders)
        {
            if (provider.IsOidc && provider.OidcOptions is { } oidcOpts)
            {
                if (string.IsNullOrWhiteSpace(oidcOpts.Authority))
                    failures.Add($"DabblerSecurity:ExternalProviders:{provider.ProviderName}:Authority is required.");

                if (string.IsNullOrWhiteSpace(oidcOpts.ClientId))
                    failures.Add($"DabblerSecurity:ExternalProviders:{provider.ProviderName}:ClientId is required.");
            }
            else if (provider.OAuthOptions is { } oauthOpts)
            {
                if (string.IsNullOrWhiteSpace(oauthOpts.ClientId))
                    failures.Add($"DabblerSecurity:ExternalProviders:{provider.ProviderName}:ClientId is required.");
            }
        }

        // Advisory: remind consuming apps to seed the UserAdministrator role.
        // Only warn when MFA roles or resource registrations indicate a full
        // Dabbler security setup (avoids false positives for API-only projects
        // that use Dabbler only for authentication, not authorization).
        if (options.Mfa.RequireMfaForRoles.Count > 0
            || options.RegisteredPages.Count > 0
            || options.RegisteredEntities.Count > 0
            || options.RegisteredEndpoints.Count > 0
            || options.RegisteredControls.Count > 0)
        {
            _logger.LogInformation(
                "Dabbler.Security authorization is configured. " +
                "Ensure the UserAdministrator role has been seeded with at least one admin user.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
