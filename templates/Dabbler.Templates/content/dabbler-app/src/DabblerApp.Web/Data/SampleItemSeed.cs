namespace DabblerApp.Web.Data;

/// <summary>
/// In-memory seed data for the sample query page (A13).
/// </summary>
public static class SampleItemSeed
{
    public static IEnumerable<SampleItem> GetItems() =>
    [
        new() { Name = "Alpha Widget",    Description = "First sample item",  Value = 12.50m,  IsActive = true },
        new() { Name = "Beta Gadget",     Description = "Second sample item", Value = 99.00m,  IsActive = true },
        new() { Name = "Gamma Thingamajig", Description = "Third sample item", Value = 3.75m,  IsActive = false },
        new() { Name = "Delta Doohickey",  Description = "Fourth sample item", Value = 255.00m, IsActive = true },
        new() { Name = "Epsilon Gizmo",   Description = "Fifth sample item",  Value = 18.99m,  IsActive = true },
    ];
}
