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

    /// <summary>
    /// Initializes a new <see cref="RestrictionBinder"/>.
    /// </summary>
    public RestrictionBinder(
        SecurityConfigurationManager configManager,
        AuthenticationStateProvider authStateProvider,
        IRoleSwitcher roleSwitcher,
        IBakedInRoleProvider bakedInRoleProvider)
    {
        _configManager = configManager;
        _authStateProvider = authStateProvider;
        _roleSwitcher = roleSwitcher;
        _bakedInRoleProvider = bakedInRoleProvider;
    }

    /// <inheritdoc />
    public async Task<RestrictionResolution> ResolveAsync(string entityKey, string columnKey)
    {
        await _configManager.EnsureLoadedAsync();
        var config = _configManager.Current;

        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated != true)
            return RestrictionResolution.Unrestricted(entityKey, columnKey);

        var effectiveRoles = _roleSwitcher.EffectiveRoles(user).ToList();
        return Resolve(entityKey, columnKey, config, effectiveRoles);
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
