using System.Security.Claims;
using AcmeCoffeeOnline.Shared.Enums;
using Dabbler.Security.Entities;

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

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
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

            if (context?.Items["AppUser"] is AppUser appUser)
                return appUser.DisplayName;

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
}
