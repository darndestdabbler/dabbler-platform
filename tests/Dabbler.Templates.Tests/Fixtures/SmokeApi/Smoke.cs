namespace SmokeApi.Shared.Models;

/// <summary>
/// Minimal EF entity used to exercise the dabbler-crud-service template smoke test.
/// </summary>
public class Smoke
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
