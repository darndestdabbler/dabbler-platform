using System.ComponentModel.DataAnnotations;

namespace AcmeCoffeeOnline.Shared.DTOs;

public class SupplierDto
{
    public int Id { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string City { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Country { get; set; } = string.Empty;

    [StringLength(255)]
    [EmailAddress]
    public string? ContactEmail { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
