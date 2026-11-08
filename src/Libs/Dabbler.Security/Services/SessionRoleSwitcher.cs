using System.Security.Claims;

namespace Dabbler.Security.Services;

/// <summary>
/// Scoped implementation of <see cref="IRoleSwitcher"/> that stores state in a plain field.
/// Works for both DI scenarios:
/// <list type="bullet">
///   <item>ASP.NET Core Web API — scoped = per HTTP request lifetime</item>
///   <item>Blazor Server — scoped = per circuit lifetime (persists across renders)</item>
/// </list>
/// No <c>IHttpContextAccessor</c> is needed because a plain field covers both models.
/// (<c>HttpContext.Items</c> would be null in Blazor Server after the initial HTTP handshake.)
/// </summary>
public sealed class SessionRoleSwitcher : IRoleSwitcher
{
    private string? _switchedRole;

    /// <inheritdoc />
    public bool IsRoleSwitched => _switchedRole is not null;

    /// <inheritdoc />
    public IEnumerable<string> EffectiveRoles(ClaimsPrincipal user)
    {
        if (_switchedRole is not null)
            return [_switchedRole];

        return user.FindAll(ClaimTypes.Role).Select(c => c.Value);
    }

    /// <inheritdoc />
    public void SwitchToRole(string roleName)
    {
        ArgumentNullException.ThrowIfNull(roleName);
        _switchedRole = roleName;
    }

    /// <inheritdoc />
    public void ClearSwitch() => _switchedRole = null;
}
