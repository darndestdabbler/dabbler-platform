using Dabbler.Api.Querying;
using System.Text.Json;

namespace AcmeCoffeeOnline.Api.Tests;

public class QueryableExtensionsTests
{
    private sealed record Widget(int Id, string? Name, decimal Price, bool Active, decimal? DiscountedPrice = null);

    private static IQueryable<Widget> Widgets() => new List<Widget>
    {
        new(1, "Alpha Widget", 10.00m, true, 8.00m),
        new(2, "beta product", 25.50m, false, null),
        new(3, "Gamma Widget", 5.00m, true, null),
        new(4, null, 99.99m, false, 75.00m),
    }.AsQueryable();

    [Fact]
    public void ApplyGridCompositeFilter_NullFilter_ReturnsAll()
    {
        var result = Widgets().ApplyGridCompositeFilter(null).ToList();
        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void ApplyGridCompositeFilter_Contains_FiltersCorrectly()
    {
        var filter = new GridCompositeFilter
        {
            Logic = GridFilterLogic.And,
            Filters = [new GridQueryFilter("Name", "Contains", "Widget")],
        };
        var result = Widgets().ApplyGridCompositeFilter(filter).ToList();
        Assert.Equal(2, result.Count); // Alpha Widget, Gamma Widget
        Assert.All(result, w => Assert.Contains("Widget", w.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ApplyGridCompositeFilter_Equals_FiltersDecimal()
    {
        var filter = new GridCompositeFilter
        {
            Filters = [new GridQueryFilter("Price", "Equals", 25.50m)],
        };
        var result = Widgets().ApplyGridCompositeFilter(filter).ToList();
        Assert.Single(result);
        Assert.Equal(2, result[0].Id);
    }

    [Fact]
    public void ApplyGridCompositeFilter_GreaterThan_FiltersCorrectly()
    {
        var filter = new GridCompositeFilter
        {
            Filters = [new GridQueryFilter("Price", "GreaterThan", 20.00m)],
        };
        var result = Widgets().ApplyGridCompositeFilter(filter).ToList();
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ApplyGridCompositeFilter_IsEmpty_NullProperty()
    {
        var filter = new GridCompositeFilter
        {
            Filters = [new GridQueryFilter("Name", "IsEmpty", null)],
        };
        var result = Widgets().ApplyGridCompositeFilter(filter).ToList();
        Assert.Single(result);
        Assert.Equal(4, result[0].Id);
    }

    [Fact]
    public void ApplyGridCompositeFilter_OrLogic_CombinesCorrectly()
    {
        var filter = new GridCompositeFilter
        {
            Logic = GridFilterLogic.Or,
            Filters =
            [
                new GridQueryFilter("Id", "Equals", 1),
                new GridQueryFilter("Id", "Equals", 3),
            ],
        };
        var result = Widgets().ApplyGridCompositeFilter(filter).OrderBy(w => w.Id).ToList();
        Assert.Equal(2, result.Count);
        Assert.Equal([1, 3], result.Select(w => w.Id));
    }

    [Fact]
    public void ApplyGridQuerySorts_SingleSort_OrdersAscending()
    {
        var sorts = new List<GridQuerySort> { new("Price") };
        var result = Widgets().ApplyGridQuerySorts(sorts).ToList();
        Assert.Equal([5.00m, 10.00m, 25.50m, 99.99m], result.Select(w => w.Price));
    }

    [Fact]
    public void ApplyGridQuerySorts_MultiSort_UsesOrderByThenBy()
    {
        var sorts = new List<GridQuerySort>
        {
            new("Active", Descending: true),  // true first
            new("Price"),                      // then price asc within group
        };
        var result = Widgets().ApplyGridQuerySorts(sorts).ToList();
        // Active=true: Gamma(5), Alpha(10). Active=false: beta(25.5), Widget4(99.99)
        Assert.True(result[0].Active);
        Assert.True(result[1].Active);
        Assert.False(result[2].Active);
        Assert.Equal(5.00m, result[0].Price);
        Assert.Equal(10.00m, result[1].Price);
    }

    [Fact]
    public void ApplyGridCompositeFilter_InvalidPropertyName_ReturnsAll()
    {
        var filter = new GridCompositeFilter
        {
            Filters = [new GridQueryFilter("NonExistent", "Contains", "x")],
        };
        var result = Widgets().ApplyGridCompositeFilter(filter).ToList();
        // Invalid property → filter skipped silently → returns all
        Assert.Equal(4, result.Count);
    }

    // Proves that JsonElement values (as System.Text.Json deserialises object? from POST body)
    // are correctly unwrapped before expression-tree building.
    [Fact]
    public void ApplyGridCompositeFilter_JsonElement_StringValue_FiltersCorrectly()
    {
        var json = """{"PropertyName":"Name","Operator":"Contains","Value":"Widget"}""";
        var filter_dto = JsonSerializer.Deserialize<GridQueryFilter>(json)!;
        // Value is now a JsonElement(String), not a string
        Assert.IsType<JsonElement>(filter_dto.Value);

        var filter = new GridCompositeFilter { Filters = [filter_dto] };
        var result = Widgets().ApplyGridCompositeFilter(filter).ToList();
        Assert.Equal(2, result.Count);
        Assert.All(result, w => Assert.Contains("Widget", w.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ApplyGridCompositeFilter_NullableDecimal_Equals_FiltersCorrectly()
    {
        // DiscountedPrice is decimal? — validates nullable member constant lift
        var filter = new GridCompositeFilter
        {
            Filters = [new GridQueryFilter("DiscountedPrice", "Equals", 8.00m)],
        };
        var result = Widgets().ApplyGridCompositeFilter(filter).ToList();
        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public void ApplyGridCompositeFilter_NullableDecimal_GreaterThan_FiltersCorrectly()
    {
        var filter = new GridCompositeFilter
        {
            Filters = [new GridQueryFilter("DiscountedPrice", "GreaterThan", 10.00m)],
        };
        var result = Widgets().ApplyGridCompositeFilter(filter).ToList();
        Assert.Single(result);
        Assert.Equal(4, result[0].Id); // 75.00m
    }

    [Fact]
    public void ApplyGridCompositeFilter_JsonElement_NumberValue_FiltersCorrectly()
    {
        var json = """{"PropertyName":"Price","Operator":"GreaterThan","Value":20.0}""";
        var filter_dto = JsonSerializer.Deserialize<GridQueryFilter>(json)!;
        Assert.IsType<JsonElement>(filter_dto.Value);

        var filter = new GridCompositeFilter { Filters = [filter_dto] };
        var result = Widgets().ApplyGridCompositeFilter(filter).ToList();
        Assert.Equal(2, result.Count); // 25.50 and 99.99
    }
}
