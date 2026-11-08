namespace AcmeCoffeeOnline.Shared.DTOs;

public sealed class OrganizationOptionDto
{
    public int OrganizationId { get; set; }

    public string OrganizationKey { get; set; } = string.Empty;

    public string OrganizationName { get; set; } = string.Empty;

    public IReadOnlyList<string> RoleNames { get; set; } = [];

    public bool IsActive { get; set; }
}
