using System.Security.Claims;

namespace Dabbler.Security.Services;

/// <summary>
/// Provisions a new backoffice user account inside a single EF transaction:
/// AppUser (IsProvisioned=false) → membership → role → audit row → IsProvisioned=true flip.
/// </summary>
public interface IBackofficeUserCreationService
{
    /// <summary>
    /// Creates a new backoffice user and sends the onboarding magic link.
    /// </summary>
    /// <param name="request">The creation request (display name, email, role, org).</param>
    /// <param name="granter">The signed-in admin performing the creation.</param>
    /// <param name="baseVerifyUrl">Base URL used by IMagicLinkService to build the sign-in link.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The projection for the newly created user. The magic-link send result is returned
    /// separately so a post-commit email failure does not roll back the user creation.
    /// </returns>
    /// <exception cref="UnauthorizedAccessException">
    /// The granter's effective role is not authorized to assign the requested role to the
    /// requested organization, per <see cref="RoleAssignmentAuthority.Evaluate"/>.
    /// </exception>
    /// <exception cref="EmailAlreadyExistsException">
    /// A user with the requested email already exists.
    /// </exception>
    Task<BackofficeUserCreationResult> CreateAsync(
        CreateBackofficeUserRequest request,
        ClaimsPrincipal granter,
        string baseVerifyUrl,
        CancellationToken ct = default);
}

/// <summary>
/// Result returned by <see cref="IBackofficeUserCreationService.CreateAsync"/>.
/// </summary>
/// <param name="User">The admin-grid projection for the new user.</param>
/// <param name="MagicLinkSent">
/// True when the onboarding email was enqueued successfully. False when the user was
/// created but the email transport failed (post-commit) — the admin should be informed
/// so they can re-send manually.
/// </param>
/// <param name="MagicLinkError">
/// Non-null when <see cref="MagicLinkSent"/> is false. Contains the error message from
/// the email transport so the UI can surface a non-fatal warning.
/// </param>
public sealed record BackofficeUserCreationResult(
    AdminUserRow User,
    bool MagicLinkSent,
    string? MagicLinkError = null);
