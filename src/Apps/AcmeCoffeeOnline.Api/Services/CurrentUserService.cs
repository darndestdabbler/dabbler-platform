using System.Security.Claims;
using AcmeCoffeeOnline.Api.Data;
using AcmeCoffeeOnline.Shared.Enums;
using Dabbler.Security.Services;
using Microsoft.EntityFrameworkCore;

namespace AcmeCoffeeOnline.Api.Services;

/// <summary>
/// Resolves the current user from Dabbler.Security's identity pipeline.
/// Checks HttpContext.Items (populated by InternalUserMiddleware when wired)
/// then HttpContext.User claims, with fallback to configuration for development.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly AcmeCoffeeDbContext _dbContext;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        AcmeCoffeeDbContext dbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _dbContext = dbContext;
    }

    public string UserId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;

            // InternalUserMiddleware sets AppUserId when API key + user header are valid
            if (context?.Items["AppUserId"] is int appUserId)
                return appUserId.ToString();

            // Direct cookie/OIDC authentication
            var claim = context?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null)
                return claim.Value;

            // Development fallback
            return _configuration["TestUser:UserId"] ?? "test-user-1";
        }
    }

    public string UserName
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;

            var claim = context?.User.FindFirst(ClaimTypes.Name);
            if (claim != null)
                return claim.Value;

            return _configuration["TestUser:UserName"] ?? "Test User";
        }
    }

    public UserRole Role
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;

            // Roles set by InternalUserMiddleware
            if (context?.Items["AppUserRoles"] is List<string?> roleNames)
            {
                return MapHighestRole(roleNames);
            }

            // Roles from claims
            var roleClaims = context?.User.FindAll(ClaimTypes.Role);
            if (roleClaims?.Any() == true)
            {
                return MapHighestRole(roleClaims.Select(c => (string?)c.Value));
            }

            // Development fallback
            var roleStr = _configuration["TestUser:Role"];
            return Enum.TryParse<UserRole>(roleStr, ignoreCase: true, out var role)
                ? role
                : UserRole.BackofficeUser;
        }
    }

    public int? ActiveOrganizationId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;

            if (context?.Items["ActiveOrganizationId"] is int activeOrganizationId)
            {
                return activeOrganizationId;
            }

            var activeOrganizationClaim = context?.User.FindFirst(UserSessionContextConstants.ActiveOrganizationIdClaimType)?.Value;
            return int.TryParse(activeOrganizationClaim, out var parsedOrganizationId)
                ? parsedOrganizationId
                : null;
        }
    }

    public int? CustomerId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;

            // If the user has a linked CustomerId stored as a claim or item
            if (context?.Items["AppUserCustomerId"] is int customerId)
                return customerId;

            var customerIdClaim = context?.User.FindFirst("CustomerId");
            if (customerIdClaim != null && int.TryParse(customerIdClaim.Value, out var id))
                return id;

            var activeOrganizationKey = context?.User.FindFirst(UserSessionContextConstants.ActiveOrganizationKeyClaimType)?.Value;
            if (TryResolveCustomerIdFromOrganizationKey(activeOrganizationKey, out var bridgedCustomerId))
            {
                return bridgedCustomerId;
            }

            if (context?.Items.TryGetValue("ResolvedCustomerId", out var cachedCustomerId) == true)
            {
                return cachedCustomerId as int?;
            }

            var email = context?.User.FindFirst(ClaimTypes.Email)?.Value;

            if (!string.IsNullOrWhiteSpace(email))
            {
                var normalizedEmail = email.ToLowerInvariant();
                var resolvedCustomerId = _dbContext.Customers
                    .AsNoTracking()
                    .Where(customer => customer.Email == normalizedEmail)
                    .Select(customer => (int?)customer.Id)
                    .FirstOrDefault();

                context?.Items.TryAdd("ResolvedCustomerId", resolvedCustomerId);

                if (resolvedCustomerId.HasValue)
                {
                    return resolvedCustomerId.Value;
                }
            }

            // Development fallback
            var idStr = _configuration["TestUser:CustomerId"];
            return int.TryParse(idStr, out var configId) ? configId : null;
        }
    }

    public bool IsInRole(UserRole minimumRole) => Role >= minimumRole;

    private static UserRole MapHighestRole(IEnumerable<string?> roleNames)
    {
        var highest = UserRole.Customer;

        foreach (var name in roleNames)
        {
            if (string.IsNullOrEmpty(name)) continue;

            if (Enum.TryParse<UserRole>(name, ignoreCase: true, out var parsed) && parsed > highest)
                highest = parsed;
        }

        return highest;
    }

    private static bool TryResolveCustomerIdFromOrganizationKey(string? organizationKey, out int customerId)
    {
        const string customerPrefix = "customer-";

        if (!string.IsNullOrWhiteSpace(organizationKey)
            && organizationKey.StartsWith(customerPrefix, StringComparison.OrdinalIgnoreCase)
            && int.TryParse(organizationKey[customerPrefix.Length..], out customerId))
        {
            return true;
        }

        customerId = 0;
        return false;
    }
}
