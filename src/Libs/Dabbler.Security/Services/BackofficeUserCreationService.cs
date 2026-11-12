using System.Globalization;
using System.Security.Claims;
using Dabbler.Security.Authorization;
using Dabbler.Security.Data;
using Dabbler.Security.Entities;
using Dabbler.Security.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dabbler.Security.Services;

/// <summary>
/// Default <see cref="IBackofficeUserCreationService"/> implementation.
/// Provisions a new backoffice user inside a single EF transaction, emitting a
/// <see cref="SensitiveAccessEventKind.UserCreated"/> audit row before flipping
/// <c>IsProvisioned=true</c>, then enqueues the onboarding magic link.
/// </summary>
public sealed class BackofficeUserCreationService(
    DabblerSecurityDbContext db,
    IRoleSwitcher roleSwitcher,
    IBakedInRoleProvider bakedInRoleProvider,
    IMagicLinkService magicLinkService,
    ILogger<BackofficeUserCreationService> logger) : IBackofficeUserCreationService
{
    /// <inheritdoc />
    public async Task<BackofficeUserCreationResult> CreateAsync(
        CreateBackofficeUserRequest request,
        ClaimsPrincipal granter,
        string baseVerifyUrl,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(granter);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseVerifyUrl);

        var granterUserId = ResolveUserId(granter);
        var granterActiveRole = ResolveActiveAdminRole(granter);

        if (string.IsNullOrEmpty(granterActiveRole))
        {
            throw new UnauthorizedAccessException("The current user does not have an active admin role.");
        }

        var granterOrgIds = await db.AppUserOrganizationMemberships
            .AsNoTracking()
            .Where(m => m.UserId == granterUserId && m.IsActive && m.DeletedAt == null)
            .Select(m => m.OrganizationId)
            .ToListAsync(ct);

        var authorityResult = RoleAssignmentAuthority.Evaluate(
            granterRoleName: granterActiveRole,
            granterUserId: granterUserId.ToString(CultureInfo.InvariantCulture),
            granterAssignedOrganizationIds: granterOrgIds,
            targetUserId: "new-user",
            targetAssignedOrganizationIds: new[] { request.InitialOrganizationId },
            targetCurrentRoles: Array.Empty<string>(),
            proposedRoleName: request.InitialRoleName);

        if (!authorityResult.IsAllowed)
        {
            throw new UnauthorizedAccessException(authorityResult.Message);
        }

        var org = await db.AppOrganizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.InitialOrganizationId && o.IsActive, ct);

        if (org is null)
        {
            throw new InvalidOperationException($"Organization {request.InitialOrganizationId} does not exist or is not active.");
        }

        var role = await db.AppRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name == request.InitialRoleName && r.DeletedAt == null, ct);

        if (role is null)
        {
            throw new InvalidOperationException($"Role '{request.InitialRoleName}' does not exist.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        await using var txn = await db.Database.BeginTransactionAsync(ct);

        try
        {
            // INSERT AppUser with IsProvisioned=false — the provisioning gate
            // prevents magic-link issuance before the transaction commits.
            var user = new AppUser
            {
                Email = normalizedEmail,
                DisplayName = request.DisplayName,
                IsActive = true,
                IsProvisioned = false,
                CreatedBy = granterUserId,
                CreatedAt = DateTime.UtcNow,
            };

            db.AppUsers.Add(user);

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                await txn.RollbackAsync(ct);
                throw new EmailAlreadyExistsException(normalizedEmail);
            }

            // INSERT AppUserOrganizationMembership
            var membership = new AppUserOrganizationMembership
            {
                UserId = user.Id,
                OrganizationId = request.InitialOrganizationId,
                IsDefault = true,
                IsActive = true,
                JoinedAt = DateTime.UtcNow,
                JoinedBy = granterUserId,
            };

            db.AppUserOrganizationMemberships.Add(membership);
            await db.SaveChangesAsync(ct);

            // INSERT AppUserRole + audit row + IsProvisioned flip — all in one SaveChanges
            db.AppUserRoles.Add(new AppUserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                OrganizationMembershipId = membership.Id,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = granterUserId,
            });

            // Audit emission inside the transaction — cannot lose an unaudited creation.
            db.AppSensitiveAccessLogs.Add(new AppSensitiveAccessLog
            {
                EventKind = SensitiveAccessEventKind.UserCreated,
                Timestamp = DateTime.UtcNow,
                CorrelationId = Guid.NewGuid(),
                UserId = granterUserId,
                RoleName = granterActiveRole,
                OrganizationId = request.InitialOrganizationId,
                EntityKey = "AppUser",
                EntityId = user.Id.ToString(CultureInfo.InvariantCulture),
                GranterUserId = granterUserId,
                TargetRoleName = request.InitialRoleName,
                Details = Truncate($"target_email={normalizedEmail}"),
            });

            user.IsProvisioned = true;

            await db.SaveChangesAsync(ct);
            await txn.CommitAsync(ct);
        }
        catch (EmailAlreadyExistsException)
        {
            throw;
        }
        catch
        {
            await txn.RollbackAsync(ct);
            throw;
        }

        logger.LogInformation(
            "Backoffice user created. GranterUserId={GranterUserId} NewEmail={Email} Role={Role} OrgId={OrgId}",
            granterUserId, Mask(normalizedEmail), request.InitialRoleName, request.InitialOrganizationId);

        // Re-query to build the AdminUserRow projection (consistent with AdminUserQueryService).
        var userRow = await BuildAdminUserRowAsync(normalizedEmail, request, org, role, ct);

        // Enqueue magic link AFTER the transaction has committed. If this fails,
        // the user exists with IsProvisioned=true but has no onboarding email.
        // The controller surfaces a non-fatal warning in that case.
        bool magicLinkSent;
        string? magicLinkError;

        try
        {
            await magicLinkService.IssueAsync(normalizedEmail, baseVerifyUrl, ipAddress: null, ct);
            magicLinkSent = true;
            magicLinkError = null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Magic link send failed for newly created user {Email}. User exists with IsProvisioned=true.",
                Mask(normalizedEmail));
            magicLinkSent = false;
            magicLinkError = ex.Message;
        }

        return new BackofficeUserCreationResult(userRow, magicLinkSent, magicLinkError);
    }

    private async Task<AdminUserRow> BuildAdminUserRowAsync(
        string normalizedEmail,
        CreateBackofficeUserRequest request,
        Entities.AppOrganization org,
        Entities.AppRole role,
        CancellationToken ct)
    {
        var user = await db.AppUsers
            .AsNoTracking()
            .Where(u => u.Email == normalizedEmail)
            .Select(u => new { u.Id, u.DisplayName, u.Email, u.IsActive })
            .FirstAsync(ct);

        return new AdminUserRow(
            UserId: user.Id.ToString(CultureInfo.InvariantCulture),
            DisplayName: user.DisplayName,
            Email: user.Email,
            IsActive: user.IsActive,
            Organizations: new[]
            {
                new AdminUserOrganization(
                    OrganizationId: org.Id,
                    OrganizationKey: org.Key,
                    OrganizationName: org.Name,
                    IsDefault: true,
                    IsInternal: org.IsInternal)
            },
            Roles: new[] { role.Name });
    }

    private int ResolveUserId(ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : 0;
    }

    private string? ResolveActiveAdminRole(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        string? fallback = null;
        foreach (var roleName in roleSwitcher.EffectiveRoles(principal))
        {
            var bakedIn = bakedInRoleProvider.TryGetBakedInRole(roleName);
            if (bakedIn is null) continue;

            if (bakedIn.Privileges.CanAssignUsersToRoles)
            {
                return bakedIn.Name;
            }

            fallback ??= bakedIn.Privileges.CanSeeAllOrganizations ? bakedIn.Name : null;
        }

        return fallback;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        // Per the spec, email uniqueness is enforced via the existing IX_AppUsers_Email
        // unique index rather than a pre-check SELECT, to avoid the TOCTOU race under
        // concurrent admin requests. Message-string matching is the EF Core provider-
        // agnostic mechanism; SQL Server also exposes SqlException.Number 2601/2627
        // but that requires a platform-specific cast unavailable in Dabbler.Security.
        // Check both the outer message and the inner exception message.
        // SQL Server: "Violation of UNIQUE KEY constraint 'IX_AppUsers_Email'"
        // EF Core in-memory: "The index 'IX_AppUsers_Email' contains duplicate values"
        var outer = ex.Message ?? string.Empty;
        var inner = ex.InnerException?.Message ?? string.Empty;
        return outer.Contains("IX_AppUsers_Email", StringComparison.OrdinalIgnoreCase)
            || inner.Contains("IX_AppUsers_Email", StringComparison.OrdinalIgnoreCase)
            || inner.Contains("UNIQUE KEY constraint", StringComparison.OrdinalIgnoreCase)
            || inner.Contains("unique constraint", StringComparison.OrdinalIgnoreCase)
            || inner.Contains("Cannot insert duplicate key", StringComparison.OrdinalIgnoreCase)
            || outer.Contains("duplicate", StringComparison.OrdinalIgnoreCase);
    }

    private static string? Truncate(string? value)
        => value is null || value.Length <= 1024 ? value : value[..1024];

    private static string Mask(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1) return "***";
        return $"{email[0]}***{email[(at - 1)..]}";
    }
}
