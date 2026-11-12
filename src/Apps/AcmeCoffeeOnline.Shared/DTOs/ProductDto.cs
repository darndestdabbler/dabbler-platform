using System.ComponentModel.DataAnnotations;
using AcmeCoffeeOnline.Shared.Enums;

namespace AcmeCoffeeOnline.Shared.DTOs;

public class ProductDto
{
    public int Id { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    public RoastLevel RoastLevel { get; set; }
    public CaffeineContent CaffeineContent { get; set; }
    public GrindType GrindType { get; set; }

    [Range(0.01, 9999.99)]
    public decimal UnitPrice { get; set; }

    public bool IsAvailable { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(500)]
    [Url]
    public string? ImageUrl { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
