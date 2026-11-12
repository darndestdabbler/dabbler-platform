namespace AcmeCoffeeOnline.Shared.DTOs;

/// <summary>
/// Request body for <c>POST /api/admin/users</c>.
/// </summary>
public sealed record CreateBackofficeUserDto(
    string DisplayName,
    string Email,
    string InitialRoleName,
    int InitialOrganizationId);

/// <summary>
/// Response body for a successful <c>POST /api/admin/users</c>.
/// </summary>
public sealed record BackofficeUserCreationResponseDto(
    string UserId,
    string DisplayName,
    string Email,
    bool IsActive,
    bool MagicLinkSent,
    string? MagicLinkError = null);
