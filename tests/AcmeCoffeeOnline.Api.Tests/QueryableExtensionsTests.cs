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

    // ── Nothing is dropped: each rule throws GridQueryException ─────────────

    [Fact]
    public void ApplyGridCompositeFilter_UnknownProperty_ThrowsGridQueryException()
    {
        var ex = Assert.Throws<GridQueryException>(() => Widgets().ApplyGridCompositeFilter(Where("NonExistent", "Contains", "x")));
        Assert.Equal("NonExistent", ex.PropertyName);
        Assert.Equal("Contains", ex.Operator);
        Assert.Contains("'NonExistent'", ex.Message);
        Assert.Contains("'Contains'", ex.Message);
    }

    [Fact]
    public void ApplyGridQuerySorts_UnknownProperty_ThrowsGridQueryException()
    {
        var ex = Assert.Throws<GridQueryException>(() => Widgets().ApplyGridQuerySorts([new GridQuerySort("NonExistent")]));
        Assert.Equal("NonExistent", ex.PropertyName);
        Assert.Null(ex.Operator);
        Assert.Contains("'NonExistent'", ex.Message);
    }

    [Fact]
    public void ApplyGridCompositeFilter_UnknownOperator_ThrowsGridQueryException()
    {
        var ex = Assert.Throws<GridQueryException>(() => Widgets().ApplyGridCompositeFilter(Where("Name", "contains", "x")));
        Assert.Equal("Name", ex.PropertyName);
        Assert.Equal("contains", ex.Operator);
    }

    [Theory]
    [InlineData("Price", "Contains", "1")]          // string operator on a decimal
    [InlineData("Active", "GreaterThan", true)]     // comparison on a type without ordering
    [InlineData("Price", "IsEmpty", null)]          // a non-nullable value type is never empty
    [InlineData("Price", "IsNotEmpty", null)]
    [InlineData("Price", "Equals", null)]           // null against a non-nullable value type
    [InlineData("Price", "GreaterThan", null)]      // a comparison needs a value
    public void ApplyGridCompositeFilter_OperatorThePropertyTypeDoesNotSupport_ThrowsGridQueryException(
        string property, string op, object? value)
    {
        var ex = Assert.Throws<GridQueryException>(() => Widgets().ApplyGridCompositeFilter(Where(property, op, value)));
        Assert.Equal(property, ex.PropertyName);
        Assert.Equal(op, ex.Operator);
    }

    [Fact]
    public void ApplyGridCompositeFilter_InWithoutArray_ThrowsGridQueryException()
    {
        var ex = Assert.Throws<GridQueryException>(() => Widgets().ApplyGridCompositeFilter(Where("Id", "In", 1)));
        Assert.Equal("In", ex.Operator);
    }

    [Theory]
    [InlineData("Id", "abc")]
    [InlineData("Made", "not-a-date")]
    [InlineData("Key", "not-a-guid")]
    [InlineData("Hue", "Purple")]
    [InlineData("Hue", "99")]
    public void ApplyGridCompositeFilter_ValueThatCannotBeConverted_ThrowsGridQueryException(string property, string text)
    {
        foreach (var value in new object[] { text, Json($"\"{text}\"") })
        {
            var ex = Assert.Throws<GridQueryException>(() => Gadgets().ApplyGridCompositeFilter(Where(property, "Equals", value)));
            Assert.Equal(property, ex.PropertyName);
            Assert.Equal("Equals", ex.Operator);
        }
    }

    [Fact]
    public void GridQueryException_IsAnArgumentException()
    {
        Assert.IsAssignableFrom<ArgumentException>(new GridQueryException("Name", "Equals", "reason"));
    }

    // ── Conversions, from a string and from a JsonElement ───────────────────

    [Fact]
    public void ApplyGridCompositeFilter_DateOnly_ConvertsIsoDate()
    {
        AssertMatchesFromStringAndJson("Made", "Equals", "2025-03-01", expectedId: 2);
        AssertMatchesFromStringAndJson("Made", "GreaterThanOrEqual", "2025-03-01T00:00:00", expectedIds: [2, 3]);
    }

    [Fact]
    public void ApplyGridCompositeFilter_TimeOnly_ConvertsIsoTime()
    {
        AssertMatchesFromStringAndJson("OpensAt", "Equals", "07:30:00", expectedId: 2);
    }

    [Fact]
    public void ApplyGridCompositeFilter_DateTime_ConvertsIsoDateTime()
    {
        AssertMatchesFromStringAndJson("CreatedAt", "LessThan", "2025-02-01T00:00:00", expectedId: 1);
    }

    [Fact]
    public void ApplyGridCompositeFilter_DateTimeOffset_ConvertsIsoDateTimeOffset()
    {
        AssertMatchesFromStringAndJson("ShippedAt", "Equals", "2025-03-01T12:00:00+02:00", expectedId: 2);
    }

    [Fact]
    public void ApplyGridCompositeFilter_Guid_ConvertsText()
    {
        AssertMatchesFromStringAndJson("Key", "Equals", GadgetKey3.ToString(), expectedId: 3);
    }

    [Fact]
    public void ApplyGridCompositeFilter_Enum_ConvertsMemberName()
    {
        AssertMatchesFromStringAndJson("Hue", "Equals", "Blue", expectedId: 2);
    }

    [Fact]
    public void ApplyGridCompositeFilter_Enum_ConvertsMemberNumber()
    {
        AssertMatchesFromStringAndJson("Hue", "Equals", "2", expectedId: 3);
        var result = Gadgets().ApplyGridCompositeFilter(Where("Hue", "Equals", Json("2"))).ToList();
        Assert.Equal([3], result.Select(g => g.Id));
    }

    [Fact]
    public void ApplyGridCompositeFilter_Decimal_ConvertsInvariantText()
    {
        var result = Widgets().ApplyGridCompositeFilter(Where("Price", "Equals", "25.50")).ToList();
        Assert.Equal([2], result.Select(w => w.Id));
    }

    // ── In ──────────────────────────────────────────────────────────────────

    [Fact]
    public void ApplyGridCompositeFilter_In_AppliesArrayValue()
    {
        var fromClr = Widgets().ApplyGridCompositeFilter(Where("Id", "In", new[] { 1, 3 })).OrderBy(w => w.Id).ToList();
        Assert.Equal([1, 3], fromClr.Select(w => w.Id));

        var fromJson = Gadgets().ApplyGridCompositeFilter(Where("Hue", "In", Json("""["Red", 2]"""))).OrderBy(g => g.Id).ToList();
        Assert.Equal([1, 3], fromJson.Select(g => g.Id));
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private enum Hue { Red, Blue, Green }

    private sealed record Gadget(int Id, DateOnly Made, TimeOnly OpensAt, DateTime CreatedAt, DateTimeOffset ShippedAt, Guid Key, Hue Hue);

    private static readonly Guid GadgetKey3 = Guid.Parse("8f3b2a1c-0d4e-4f5a-9b6c-7d8e9f0a1b2c");

    private static IQueryable<Gadget> Gadgets() => new List<Gadget>
    {
        new(1, new DateOnly(2025, 1, 15), new TimeOnly(6, 0), new DateTime(2025, 1, 15, 9, 0, 0),
            new DateTimeOffset(2025, 1, 15, 12, 0, 0, TimeSpan.Zero), Guid.NewGuid(), Hue.Red),
        new(2, new DateOnly(2025, 3, 1), new TimeOnly(7, 30), new DateTime(2025, 3, 1, 9, 0, 0),
            new DateTimeOffset(2025, 3, 1, 10, 0, 0, TimeSpan.Zero), Guid.NewGuid(), Hue.Blue),
        new(3, new DateOnly(2025, 6, 30), new TimeOnly(8, 0), new DateTime(2025, 6, 30, 9, 0, 0),
            new DateTimeOffset(2025, 6, 30, 12, 0, 0, TimeSpan.Zero), GadgetKey3, Hue.Green),
    }.AsQueryable();

    private static GridCompositeFilter Where(string property, string op, object? value) =>
        new() { Filters = [new GridQueryFilter(property, op, value)] };

    private static JsonElement Json(string json) => JsonSerializer.Deserialize<JsonElement>(json);

    private static void AssertMatchesFromStringAndJson(string property, string op, string text, int expectedId) =>
        AssertMatchesFromStringAndJson(property, op, text, expectedIds: [expectedId]);

    private static void AssertMatchesFromStringAndJson(string property, string op, string text, int[] expectedIds)
    {
        foreach (var value in new object[] { text, Json($"\"{text}\"") })
        {
            var result = Gadgets().ApplyGridCompositeFilter(Where(property, op, value)).OrderBy(g => g.Id).ToList();
            Assert.Equal(expectedIds, result.Select(g => g.Id));
        }
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
