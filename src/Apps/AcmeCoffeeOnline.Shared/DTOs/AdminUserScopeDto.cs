namespace AcmeCoffeeOnline.Shared.DTOs;

public sealed class AdminUserScopeDto
{
    public bool IsSystemAdmin { get; set; }

    public bool CanManageUsers { get; set; }

    public int? ActiveOrganizationId { get; set; }

    public string? ActiveOrganizationKey { get; set; }

    public string? ActiveOrganizationName { get; set; }

    public IReadOnlyList<AdminUserScopeUserDto> Users { get; set; } = [];
}

public sealed class AdminUserScopeUserDto
{
    public int UserId { get; set; }

    public int OrganizationMembershipId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public IReadOnlyList<string> ScopedRoles { get; set; } = [];

    public IReadOnlyList<string> OtherOrganizations { get; set; } = [];

    public bool HasScopedUserAdmin { get; set; }

    public bool CanGrantUserAdmin { get; set; }

    public bool CanRevokeUserAdmin { get; set; }
}
