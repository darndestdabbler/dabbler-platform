using System.Globalization;
using System.Security.Claims;
using Dabbler.Security.Authorization;
using Dabbler.Security.Configuration;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace Dabbler.Security.Blazor.Components.Restriction;

/// <summary>
/// Default <see cref="IRestrictionBinder"/> implementation. Resolves the
/// active <see cref="ClaimsPrincipal"/> via the injected
/// <see cref="AuthenticationStateProvider"/> and the user's effective roles
/// via <see cref="IRoleSwitcher"/>, then evaluates the column against the
/// current <see cref="SecurityConfiguration"/> snapshot.
/// </summary>
public sealed class RestrictionBinder : IRestrictionBinder
{
    private readonly SecurityConfigurationManager _configManager;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IRoleSwitcher _roleSwitcher;
    private readonly IBakedInRoleProvider _bakedInRoleProvider;
    private readonly ISensitiveAccessAuditor? _auditor;
    private readonly IRenderCorrelationScope? _correlationScope;

    // Per-scope set of exemption keys that have already produced an
    // ExemptionExpired audit row. Resolves are cheap and high-frequency
    // (a 50-row grid with 3 sensitive columns invokes them 150x per render);
    // this set caps emission at one row per (entity, column, role, expiry)
    // per scope, per the spec's "once per exemption" rule.
    private readonly HashSet<string> _emittedExpiredExemptionKeys = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new <see cref="RestrictionBinder"/>.
    /// </summary>
    /// <param name="configManager">The security configuration manager.</param>
    /// <param name="authStateProvider">The authentication state provider.</param>
    /// <param name="roleSwitcher">The active role switcher.</param>
    /// <param name="bakedInRoleProvider">The baked-in role provider.</param>
    /// <param name="auditor">
    /// Optional auditor for emitting <c>ExemptionExpired</c> /
    /// <c>FieldUnmaskedRead</c> rows. Tests that do not care about audit
    /// emission may omit it.
    /// </param>
    /// <param name="correlationScope">
    /// Optional correlation scope used to batch <c>FieldUnmaskedRead</c>
    /// emissions per <c>(CorrelationId, EntityKey, EntityId, ColumnKey)</c>.
    /// When omitted (or null), the binder falls back to a per-instance
    /// dedup set so existing call sites keep their previous behavior.
    /// </param>
    public RestrictionBinder(
        SecurityConfigurationManager configManager,
        AuthenticationStateProvider authStateProvider,
        IRoleSwitcher roleSwitcher,
        IBakedInRoleProvider bakedInRoleProvider,
        ISensitiveAccessAuditor? auditor = null,
        IRenderCorrelationScope? correlationScope = null)
    {
        _configManager = configManager;
        _authStateProvider = authStateProvider;
        _roleSwitcher = roleSwitcher;
        _bakedInRoleProvider = bakedInRoleProvider;
        _auditor = auditor;
        _correlationScope = correlationScope;
    }

    /// <inheritdoc />
    public Task<RestrictionResolution> ResolveAsync(string entityKey, string columnKey)
        => ResolveAsync(entityKey, columnKey, entityId: null);

    /// <inheritdoc />
    public async Task<RestrictionResolution> ResolveAsync(string entityKey, string columnKey, string? entityId)
    {
        await _configManager.EnsureLoadedAsync();
        var config = _configManager.Current;

        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated != true)
            return RestrictionResolution.Unrestricted(entityKey, columnKey);

        var effectiveRoles = _roleSwitcher.EffectiveRoles(user).ToList();
        var evaluatedAt = DateTimeOffset.UtcNow;

        await EmitExpiredExemptionAuditsAsync(user, entityKey, columnKey, config, effectiveRoles, evaluatedAt);

        var resolution = Resolve(entityKey, columnKey, config, effectiveRoles, evaluatedAt);

        await EmitFieldUnmaskedReadIfApplicableAsync(user, entityKey, columnKey, entityId, config, resolution);

        return resolution;
    }

    private async Task EmitFieldUnmaskedReadIfApplicableAsync(
        ClaimsPrincipal user,
        string entityKey,
        string columnKey,
        string? entityId,
        SecurityConfiguration configuration,
        RestrictionResolution resolution)
    {
        if (_auditor is null) return;
        if (string.IsNullOrEmpty(entityId)) return;

        // "Exempted → unmasked": the column carries a default mask, but the
        // user's effective roles include an active exemption, so the binder
        // resolved IsMasked=false. A column without a default mask is not
        // sensitive in the platform's model and produces no audit signal.
        var hasDefaultMask = configuration.DefaultMasks.Any(m =>
            string.Equals(m.EntityKey, entityKey, StringComparison.OrdinalIgnoreCase)
            && string.Equals(m.ColumnKey, columnKey, StringComparison.OrdinalIgnoreCase));
        if (!hasDefaultMask) return;
        if (resolution.IsMasked) return;

        var batchKey = string.Create(
            CultureInfo.InvariantCulture,
            $"FUR|{CorrelationKey()}|{entityKey}|{entityId}|{columnKey}");

        if (!TryRecordOnceForCurrentScope(batchKey)) return;

        await _auditor.AuditFieldUnmaskedReadAsync(user, entityKey, entityId, columnKey);
    }

    // Scope-key helpers — when a correlation scope is registered the binder
    // delegates dedup to it (so navigation regenerates the batch); otherwise
    // a per-binder set keeps existing tests deterministic.
    private readonly HashSet<string> _localUnmaskedBatch = new(StringComparer.Ordinal);

    private string CorrelationKey() =>
        _correlationScope?.Current.ToString("N") ?? "no-scope";

    private bool TryRecordOnceForCurrentScope(string key)
    {
        if (_correlationScope is not null)
            return _correlationScope.TryRecordOnce(key);

        lock (_localUnmaskedBatch)
            return _localUnmaskedBatch.Add(key);
    }

    private async Task EmitExpiredExemptionAuditsAsync(
        ClaimsPrincipal user,
        string entityKey,
        string columnKey,
        SecurityConfiguration configuration,
        IReadOnlyList<string> effectiveRoles,
        DateTimeOffset evaluatedAt)
    {
        if (_auditor is null) return;
        if (effectiveRoles.Count == 0) return;

        foreach (var roleName in effectiveRoles)
        {
            foreach (var exemption in configuration.MaskExemptions)
            {
                if (!string.Equals(exemption.EntityKey, entityKey, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.Equals(exemption.ColumnKey, columnKey, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.Equals(exemption.TargetRoleName, roleName, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (exemption.ExpiresAt is not { } expiry || expiry > evaluatedAt)
                    continue;

                var key = string.Create(
                    CultureInfo.InvariantCulture,
                    $"{exemption.EntityKey}|{exemption.ColumnKey}|{exemption.TargetRoleName}|{exemption.GrantedAt.UtcTicks}|{expiry.UtcTicks}");
                if (!_emittedExpiredExemptionKeys.Add(key))
                    continue;

                await _auditor.AuditExemptionExpiredAsync(
                    user,
                    exemption.EntityKey,
                    exemption.ColumnKey,
                    exemption.TargetRoleName,
                    expiry);
            }
        }
    }

    /// <inheritdoc />
    public RestrictionResolution Resolve(
        string entityKey,
        string columnKey,
        SecurityConfiguration configuration,
        IReadOnlyList<string> effectiveRoles,
        DateTimeOffset? evaluatedAt = null)
    {
        return Evaluate(
            entityKey,
            columnKey,
            configuration,
            effectiveRoles,
            _bakedInRoleProvider,
            evaluatedAt ?? DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Pure static evaluator. The instance and unit tests both delegate
    /// here so the resolution rules live in exactly one place.
    /// </summary>
    /// <remarks>
    /// Aggregation rules (most-permissive across roles):
    /// <list type="bullet">
    /// <item>Visibility = Visible if any role can see the column.</item>
    /// <item>Editability = Editable if any role can edit the column.</item>
    /// <item>IsMasked = false if any role has an active mask exemption.</item>
    /// </list>
    /// A baked-in role short-circuits to fully unrestricted — baked-in
    /// roles do not consult the configuration's restriction lists.
    /// </remarks>
    internal static RestrictionResolution Evaluate(
        string entityKey,
        string columnKey,
        SecurityConfiguration configuration,
        IReadOnlyList<string> effectiveRoles,
        IBakedInRoleProvider bakedInRoleProvider,
        DateTimeOffset evaluatedAt)
    {
        if (effectiveRoles is null || effectiveRoles.Count == 0)
            return RestrictionResolution.Unrestricted(entityKey, columnKey);

        var defaultMask = configuration.DefaultMasks.FirstOrDefault(m =>
            string.Equals(m.EntityKey, entityKey, StringComparison.OrdinalIgnoreCase)
            && string.Equals(m.ColumnKey, columnKey, StringComparison.OrdinalIgnoreCase));
        var hasDefaultMask = defaultMask is not null;

        var visibility = ColumnVisibility.Hidden;
        var editability = ColumnEditability.ReadOnly;
        var maskExemptedByAnyRole = false;
        var anyRoleEvaluated = false;

        foreach (var roleName in effectiveRoles)
        {
            anyRoleEvaluated = true;

            // Baked-in roles are unrestricted: visible + editable + (always
            // see unmasked, since they bypass DefaultMasks at the binder layer).
            // Mask-exemption rows targeting baked-in roles still flow through
            // the per-role check below, but a baked-in role is never made
            // hidden/read-only/masked by ColumnRestriction or DefaultMasks.
            if (bakedInRoleProvider.TryGetBakedInRole(roleName) is not null)
            {
                visibility = ColumnVisibility.Visible;
                editability = ColumnEditability.Editable;
                maskExemptedByAnyRole = true;
                continue;
            }

            var userTypeRole = configuration.UserTypeRoles.FirstOrDefault(r =>
                string.Equals(r.RoleName, roleName, StringComparison.OrdinalIgnoreCase));

            // Role not found in config: treat as default-permissive — visible + editable.
            if (userTypeRole is null)
            {
                visibility = ColumnVisibility.Visible;
                editability = ColumnEditability.Editable;
            }
            else
            {
                var restriction = userTypeRole.ColumnRestrictions.FirstOrDefault(r =>
                    string.Equals(r.EntityKey, entityKey, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(r.ColumnKey, columnKey, StringComparison.OrdinalIgnoreCase));

                if (restriction is null)
                {
                    visibility = ColumnVisibility.Visible;
                    editability = ColumnEditability.Editable;
                }
                else if (restriction.RestrictionKind == ColumnRestrictionKind.ViewOnly)
                {
                    // Visible but not editable. Most-permissive editability
                    // is preserved if some other role is editable.
                    visibility = ColumnVisibility.Visible;
                }
                // ColumnRestrictionKind.Hidden: no upgrade — but another
                // role in the loop may still flip visibility to Visible.
            }

            if (hasDefaultMask && !maskExemptedByAnyRole)
            {
                var hasActiveExemption = configuration.MaskExemptions.Any(ex =>
                    string.Equals(ex.EntityKey, entityKey, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(ex.ColumnKey, columnKey, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(ex.TargetRoleName, roleName, StringComparison.OrdinalIgnoreCase)
                    && (ex.ExpiresAt is null || ex.ExpiresAt > evaluatedAt));

                if (hasActiveExemption)
                    maskExemptedByAnyRole = true;
            }
        }

        if (!anyRoleEvaluated)
            return RestrictionResolution.Unrestricted(entityKey, columnKey);

        var isMasked = hasDefaultMask && !maskExemptedByAnyRole;
        var maskFormat = isMasked ? defaultMask!.MaskFormat : string.Empty;

        return new RestrictionResolution(entityKey, columnKey, visibility, editability, isMasked, maskFormat);
    }
}
