using System.Linq.Expressions;
using System.Security.Claims;
using Dabbler.Security.Authorization;
using Dabbler.Security.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Dabbler.Security.Services;

/// <summary>
/// Default <see cref="IRowRestrictionBroker"/> that resolves the active user's
/// row restrictions from <see cref="SecurityConfigurationManager.Current"/> and
/// compiles them through <see cref="RowRestrictionExpressionBuilder"/>.
/// </summary>
/// <remarks>
/// <para>
/// Baked-in roles are governed by compiled privilege profiles, so the broker
/// short-circuits to <c>null</c> as soon as it sees any baked-in role in the
/// user's effective role set.
/// </para>
/// <para>
/// For user-type roles, each effective role's
/// <see cref="RoleDefinition.RowRestrictions"/> list is searched for an entry
/// whose <see cref="RowRestriction.EntityKey"/> matches the supplied key
/// (case-insensitive). A user-type role with no matching restriction for the
/// entity is default-permissive for that entity, so the broker returns
/// <c>null</c> — the most permissive active role wins, mirroring how multiple
/// role claims normally resolve. If every matched user-type role carries a
/// restriction, the broker OR-combines them into a single expression.
/// </para>
/// <para>
/// When a <see cref="RowRestriction.FilterExpression"/> fails to parse, the
/// broker logs a warning and treats that role as deny-all for the entity
/// (fail-closed), matching the prior <c>RowFilterService</c> behaviour.
/// </para>
/// </remarks>
public sealed class RowRestrictionBroker : IRowRestrictionBroker
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRoleSwitcher _roleSwitcher;
    private readonly SecurityConfigurationManager _configuration;
    private readonly IBakedInRoleProvider _bakedInRoles;
    private readonly ILogger<RowRestrictionBroker> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RowRestrictionBroker"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">Accessor for the current HTTP context (claims + items).</param>
    /// <param name="roleSwitcher">Scoped role switcher for the active role set.</param>
    /// <param name="configuration">The singleton security-configuration holder.</param>
    /// <param name="bakedInRoles">The DI-provided list of baked-in role canonical names.</param>
    /// <param name="logger">A logger for parse-failure diagnostics.</param>
    public RowRestrictionBroker(
        IHttpContextAccessor httpContextAccessor,
        IRoleSwitcher roleSwitcher,
        SecurityConfigurationManager configuration,
        IBakedInRoleProvider bakedInRoles,
        ILogger<RowRestrictionBroker> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _roleSwitcher = roleSwitcher;
        _configuration = configuration;
        _bakedInRoles = bakedInRoles;
        _logger = logger;
    }

    /// <inheritdoc />
    public Expression<Func<T, bool>>? GetRestriction<T>(string entityKey)
    {
        if (string.IsNullOrWhiteSpace(entityKey))
        {
            return null;
        }

        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal is null || principal.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var effectiveRoles = _roleSwitcher.EffectiveRoles(principal).ToList();
        if (effectiveRoles.Count == 0)
        {
            return null;
        }

        var bakedInNames = _bakedInRoles.GetBakedInRoles()
            .Select(role => role.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (effectiveRoles.Any(role => bakedInNames.Contains(role)))
        {
            return null;
        }

        var userTypeRoles = _configuration.Current.UserTypeRoles;
        if (userTypeRoles.Count == 0)
        {
            return null;
        }

        var tokens = BuildTokenMap(principal);

        Expression<Func<T, bool>>? combined = null;
        foreach (var roleName in effectiveRoles)
        {
            var role = userTypeRoles.FirstOrDefault(r =>
                string.Equals(r.RoleName, roleName, StringComparison.OrdinalIgnoreCase));

            if (role is null)
            {
                continue;
            }

            var restriction = role.RowRestrictions.FirstOrDefault(r =>
                string.Equals(r.EntityKey, entityKey, StringComparison.OrdinalIgnoreCase));

            if (restriction is null)
            {
                return null;
            }

            var expr = RowRestrictionExpressionBuilder.TryBuild<T>(restriction.FilterExpression, tokens)
                      ?? DenyAll<T>();

            if (ReferenceEqualsDenyAll(expr))
            {
                _logger.LogWarning(
                    "Failed to parse RowRestriction expression '{Expression}' for role '{Role}' on entity '{Entity}'. Denying access for that role.",
                    restriction.FilterExpression, role.RoleName, entityKey);
            }

            combined = combined is null ? expr : OrElse(combined, expr);
        }

        return combined;
    }

    private static IReadOnlyDictionary<string, object?> BuildTokenMap(ClaimsPrincipal principal)
    {
        var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            map["CurrentUserId"] = userId;
        }

        var customerIdClaim = principal.FindFirst("CustomerId")?.Value;
        if (int.TryParse(customerIdClaim, out var customerId))
        {
            map["CurrentUserCustomerId"] = customerId;
        }

        var orgIdClaim = principal.FindFirst(UserSessionContextConstants.ActiveOrganizationIdClaimType)?.Value;
        if (int.TryParse(orgIdClaim, out var orgId))
        {
            map["CurrentOrganizationId"] = orgId;
        }

        return map;
    }

    private static Expression<Func<T, bool>> DenyAll<T>() => _ => false;

    private static bool ReferenceEqualsDenyAll<T>(Expression<Func<T, bool>> expr)
    {
        // A parse failure produces the literal `_ => false` lambda above.
        // Detect that shape cheaply by body kind so we don't double-log.
        return expr.Body is ConstantExpression constant && constant.Value is false;
    }

    private static Expression<Func<T, bool>> OrElse<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var param = Expression.Parameter(typeof(T), "x");
        var leftBody = new ReplaceParameterVisitor(left.Parameters[0], param).Visit(left.Body)!;
        var rightBody = new ReplaceParameterVisitor(right.Parameters[0], param).Visit(right.Body)!;
        return Expression.Lambda<Func<T, bool>>(Expression.OrElse(leftBody, rightBody), param);
    }

    private sealed class ReplaceParameterVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _source;
        private readonly ParameterExpression _target;

        public ReplaceParameterVisitor(ParameterExpression source, ParameterExpression target)
        {
            _source = source;
            _target = target;
        }

        protected override Expression VisitParameter(ParameterExpression node) =>
            node == _source ? _target : base.VisitParameter(node);
    }
}
