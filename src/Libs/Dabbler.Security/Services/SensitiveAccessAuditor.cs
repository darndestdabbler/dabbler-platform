using System.Globalization;
using System.Security.Claims;
using Dabbler.Security.Data;
using Dabbler.Security.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dabbler.Security.Services;

/// <summary>
/// Default <see cref="ISensitiveAccessAuditor"/> implementation. Resolves the
/// active user / role / correlation from the supplied principal and ambient
/// scope, builds an <see cref="AppSensitiveAccessLog"/> row, and writes it
/// through a fresh <see cref="DabblerSecurityDbContext"/> obtained from the
/// scoped <see cref="IDbContextFactory{TContext}"/>. Using the factory keeps
/// the auditor safe to call from concurrent Blazor component initializers
/// (per the AdminUserQueryService precedent in
/// <c>ServiceCollectionExtensions</c>).
/// </summary>
public sealed class SensitiveAccessAuditor : ISensitiveAccessAuditor
{
    private readonly IDbContextFactory<DabblerSecurityDbContext> _dbContextFactory;
    private readonly IRenderCorrelationScope _correlationScope;
    private readonly ILogger<SensitiveAccessAuditor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SensitiveAccessAuditor"/> class.
    /// </summary>
    public SensitiveAccessAuditor(
        IDbContextFactory<DabblerSecurityDbContext> dbContextFactory,
        IRenderCorrelationScope correlationScope,
        ILogger<SensitiveAccessAuditor> logger)
    {
        _dbContextFactory = dbContextFactory;
        _correlationScope = correlationScope;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task AuditRoleSwitchAsync(
        ClaimsPrincipal user,
        string roleName,
        string? details = null,
        CancellationToken ct = default)
    {
        var row = new AppSensitiveAccessLog
        {
            EventKind = SensitiveAccessEventKind.RoleSwitch,
            Timestamp = DateTime.UtcNow,
            CorrelationId = _correlationScope.Current,
            UserId = ResolveUserId(user),
            RoleName = roleName,
            OrganizationId = ResolveOrganizationId(user),
            Details = Truncate(details),
        };

        return AuditAsync(row, ct);
    }

    /// <inheritdoc />
    public Task AuditExemptionGrantedAsync(
        ClaimsPrincipal user,
        string entityKey,
        string columnKey,
        string targetRoleName,
        int? granterUserId,
        DateTimeOffset? expiresAt,
        string? details = null,
        CancellationToken ct = default)
    {
        var userId = ResolveUserId(user);
        var row = new AppSensitiveAccessLog
        {
            EventKind = SensitiveAccessEventKind.ExemptionGranted,
            Timestamp = DateTime.UtcNow,
            CorrelationId = _correlationScope.Current,
            UserId = userId,
            RoleName = ResolveActiveRole(user),
            OrganizationId = ResolveOrganizationId(user),
            EntityKey = entityKey,
            ColumnKey = columnKey,
            TargetRoleName = targetRoleName,
            GranterUserId = granterUserId ?? (userId == 0 ? null : userId),
            ExpiresAt = expiresAt,
            Details = Truncate(details),
        };

        return AuditAsync(row, ct);
    }

    /// <inheritdoc />
    public Task AuditExemptionExpiredAsync(
        ClaimsPrincipal user,
        string entityKey,
        string columnKey,
        string targetRoleName,
        DateTimeOffset expiresAt,
        CancellationToken ct = default)
    {
        var row = new AppSensitiveAccessLog
        {
            EventKind = SensitiveAccessEventKind.ExemptionExpired,
            Timestamp = DateTime.UtcNow,
            CorrelationId = _correlationScope.Current,
            UserId = ResolveUserId(user),
            RoleName = ResolveActiveRole(user),
            OrganizationId = ResolveOrganizationId(user),
            EntityKey = entityKey,
            ColumnKey = columnKey,
            TargetRoleName = targetRoleName,
            ExpiresAt = expiresAt,
        };

        return AuditAsync(row, ct);
    }

    /// <inheritdoc />
    public Task AuditFieldUnmaskedReadAsync(
        ClaimsPrincipal user,
        string entityKey,
        string entityId,
        string columnKey,
        CancellationToken ct = default)
    {
        var row = new AppSensitiveAccessLog
        {
            EventKind = SensitiveAccessEventKind.FieldUnmaskedRead,
            Timestamp = DateTime.UtcNow,
            CorrelationId = _correlationScope.Current,
            UserId = ResolveUserId(user),
            RoleName = ResolveActiveRole(user),
            OrganizationId = ResolveOrganizationId(user),
            EntityKey = entityKey,
            EntityId = entityId,
            ColumnKey = columnKey,
        };

        return AuditAsync(row, ct);
    }

    /// <inheritdoc />
    public async Task AuditAsync(AppSensitiveAccessLog row, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(row);

        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync(ct);
            db.AppSensitiveAccessLogs.Add(row);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // The audit log is append-only at the app layer (see Risk Notes in
            // sensitive-field-masking-audit/spec.md). A failure here must not
            // bubble out of an in-progress UI action — losing one audit row
            // is preferable to crashing a role-switch or detail-page render —
            // but the failure is logged with full structured context so the
            // operator dashboard surfaces it.
            _logger.LogError(
                ex,
                "Failed to write sensitive access audit row. EventKind={EventKind} UserId={UserId} EntityKey={EntityKey} ColumnKey={ColumnKey}",
                row.EventKind,
                row.UserId,
                row.EntityKey,
                row.ColumnKey);
        }
    }

    private static int ResolveUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            ? id
            : 0;
    }

    private static int? ResolveOrganizationId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(UserSessionContextConstants.ActiveOrganizationIdClaimType);
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            ? id
            : null;
    }

    private static string ResolveActiveRole(ClaimsPrincipal user)
    {
        // The first ClaimTypes.Role on the principal is what the rest of the
        // platform treats as the user's active role for audit-row purposes.
        // Role-switch handling is covered separately by AuditingRoleSwitcher.
        return user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    }

    private static string? Truncate(string? value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= 1024 ? value : value[..1024];
    }
}
