namespace SmokeApp.Shared.Models;

/// <summary>
/// Minimal EF entity used to exercise the dabbler-crud-page + dabbler-crud-service
/// templates end-to-end (Session 3 smoke).
/// </summary>
public class Smoke
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
