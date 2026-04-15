using AcmeCoffeeOnline.Shared.Enums;

namespace AcmeCoffeeOnline.Api.Services;

public interface ICurrentUserService
{
    string UserId { get; }
    string UserName { get; }
    UserRole Role { get; }
    int? ActiveOrganizationId { get; }
    int? CustomerId { get; }
    bool IsInRole(UserRole minimumRole);
}
