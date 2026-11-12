namespace SmokeApp.Shared.DTOs;

/// <summary>
/// DTO for the Smoke aggregate, consumed by the scaffolded page slice's
/// SmokeApiClient + SmokeGridDataProvider and by the scaffolded SmokeService's
/// QueryAsync / GetByIdAsync / CreateAsync / UpdateAsync / DeleteAsync.
/// </summary>
public class SmokeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
