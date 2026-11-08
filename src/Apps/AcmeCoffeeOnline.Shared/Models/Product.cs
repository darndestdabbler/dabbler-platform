using AcmeCoffeeOnline.Shared.Enums;

namespace AcmeCoffeeOnline.Shared.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public RoastLevel RoastLevel { get; set; }
    public CaffeineContent CaffeineContent { get; set; }
    public GrindType GrindType { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsAvailable { get; set; } = true;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
