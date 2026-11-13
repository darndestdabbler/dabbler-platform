namespace SmokeApi.Shared.DTOs;

/// <summary>
/// DTO for the Smoke aggregate. Mirrors the entity for the smoke-test path; real consumers
/// typically diverge (omit server-side audit columns, expose computed surfaces, etc.).
/// </summary>
public class SmokeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
