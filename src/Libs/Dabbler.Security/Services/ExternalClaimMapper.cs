using System.Security.Claims;

namespace Dabbler.Security.Services;

/// <summary>
/// Extracts user information from external identity provider claims.
/// Handles provider-specific claim types (e.g., Entra ID uses "oid" for subject ID).
/// </summary>
public static class ExternalClaimMapper
{
    /// <summary>
    /// User info extracted from external provider claims.
    /// </summary>
    /// <param name="SubjectId">The unique, stable identifier for the user from the external provider.</param>
    /// <param name="Email">The user's email address, if available.</param>
    /// <param name="DisplayName">The user's display name.</param>
    public record ExternalUserInfo(string SubjectId, string? Email, string DisplayName);

    /// <summary>
    /// Extracts subject ID, email, and display name from the external principal's claims,
    /// using provider-specific claim type mappings.
    /// </summary>
    /// <param name="providerName">The provider name (e.g., "EntraId", "Google").</param>
    /// <param name="principal">The claims principal from the external authentication result.</param>
    /// <returns>The extracted user information.</returns>
    /// <exception cref="InvalidOperationException">When a required claim (subject ID) is missing.</exception>
    public static ExternalUserInfo ExtractUserInfo(string providerName, ClaimsPrincipal principal)
    {
        var subjectId = ExtractSubjectId(providerName, principal);
        var email = ExtractEmail(providerName, principal);
        var displayName = ExtractDisplayName(principal, email);
        return new ExternalUserInfo(subjectId, email, displayName);
    }

    private static string ExtractSubjectId(string providerName, ClaimsPrincipal principal)
    {
        // Entra ID uses the "oid" claim (Object Identifier) as the stable user identifier.
        // The "sub" claim in Entra ID is per-application, while "oid" is tenant-wide.
        if (providerName == "EntraId")
        {
            return principal.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
                   ?? principal.FindFirstValue("oid")
                   ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? throw new InvalidOperationException("No subject ID claim found in Entra ID token");
        }

        // Generic OAuth/OIDC: prefer "sub", fall back to NameIdentifier
        return principal.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? principal.FindFirstValue("sub")
               ?? throw new InvalidOperationException("No subject ID claim found in external token");
    }

    private static string? ExtractEmail(string providerName, ClaimsPrincipal principal)
    {
        if (providerName == "EntraId")
        {
            // Entra ID may put email in different claims depending on tenant configuration
            return principal.FindFirstValue(ClaimTypes.Email)
                   ?? principal.FindFirstValue("preferred_username")
                   ?? principal.FindFirstValue("email");
        }

        return principal.FindFirstValue(ClaimTypes.Email)
               ?? principal.FindFirstValue("email");
    }

    private static string ExtractDisplayName(ClaimsPrincipal principal, string? email)
    {
        return principal.FindFirstValue(ClaimTypes.Name)
               ?? principal.FindFirstValue("name")
               ?? email
               ?? "Unknown";
    }
}
