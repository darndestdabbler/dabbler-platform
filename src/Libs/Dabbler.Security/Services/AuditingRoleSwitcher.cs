using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

namespace Dabbler.Security.Services;

/// <summary>
/// Decorator around <see cref="IRoleSwitcher"/> that emits a
/// <see cref="Models.SensitiveAccessEventKind.RoleSwitch"/> audit row each time
/// the wrapped switcher's state mutates. Both transitions audit:
/// <list type="bullet">
///   <item><see cref="SwitchToRole"/> — switching into a non-natural role.</item>
///   <item><see cref="ClearSwitch"/> — reverting to the user's natural role.</item>
/// </list>
/// The decorator resolves the active principal lazily (Blazor or ASP.NET Core)
/// because <see cref="IRoleSwitcher.SwitchToRole"/> is a synchronous void —
/// fire-and-forget the audit work so the calling component does not have to
/// await an EF round-trip.
/// </summary>
public sealed class AuditingRoleSwitcher : IRoleSwitcher
{
    private readonly IRoleSwitcher _inner;
    private readonly ISensitiveAccessAuditor _auditor;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditingRoleSwitcher"/> class.
    /// </summary>
    public AuditingRoleSwitcher(
        IRoleSwitcher inner,
        ISensitiveAccessAuditor auditor,
        IServiceProvider serviceProvider)
    {
        _inner = inner;
        _auditor = auditor;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public bool IsRoleSwitched => _inner.IsRoleSwitched;

    /// <inheritdoc />
    public IEnumerable<string> EffectiveRoles(ClaimsPrincipal user) => _inner.EffectiveRoles(user);

    /// <inheritdoc />
    public void SwitchToRole(string roleName)
    {
        _inner.SwitchToRole(roleName);
        _ = EmitAuditAsync(roleName, "switch");
    }

    /// <inheritdoc />
    public void ClearSwitch()
    {
        // Capture the switched-from role BEFORE clearing so the revert event
        // names the role the user was operating under at the moment of revert.
        // EffectiveRoles against an empty principal returns just the switched
        // role when one is active, or an empty enumerable otherwise — this
        // depends on SessionRoleSwitcher's contract that EffectiveRoles
        // returns [_switchedRole] regardless of the principal argument when
        // a switch is active. If a future IRoleSwitcher implementation
        // unions the principal's claim roles with the switched role, this
        // probe should be replaced with a dedicated read-only accessor on
        // IRoleSwitcher (e.g., CurrentSwitchedRole).
        var roleBeforeClear = _inner.IsRoleSwitched
            ? _inner.EffectiveRoles(new ClaimsPrincipal()).FirstOrDefault() ?? string.Empty
            : string.Empty;
        _inner.ClearSwitch();
        _ = EmitAuditAsync(roleBeforeClear, "revert");
    }

    private async Task EmitAuditAsync(string roleName, string action)
    {
        try
        {
            var user = await ResolvePrincipalAsync();
            if (user is null) return;
            await _auditor.AuditRoleSwitchAsync(user, roleName, action);
        }
        catch
        {
            // Auditing is best-effort by design (see the spec's Risk Notes).
            // The decorator must never crash the role-switch caller.
        }
    }

    private async Task<ClaimsPrincipal?> ResolvePrincipalAsync()
    {
        // Blazor circuits expose AuthenticationStateProvider; ASP.NET Core
        // controllers / minimal APIs expose IHttpContextAccessor. Try Blazor
        // first — when both are present the circuit's principal is the one a
        // user-initiated role switch is happening under.
        var stateProvider = (AuthenticationStateProvider?)_serviceProvider.GetService(typeof(AuthenticationStateProvider));
        if (stateProvider is not null)
        {
            var state = await stateProvider.GetAuthenticationStateAsync();
            if (state.User.Identity?.IsAuthenticated == true)
                return state.User;
        }

        var httpAccessor = (IHttpContextAccessor?)_serviceProvider.GetService(typeof(IHttpContextAccessor));
        var ctxUser = httpAccessor?.HttpContext?.User;
        if (ctxUser?.Identity?.IsAuthenticated == true)
            return ctxUser;

        return null;
    }

}
