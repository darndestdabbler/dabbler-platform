using Dabbler.Security.Services;

namespace Dabbler.Security.Tests.Services;

public class RowRestrictionExpressionBuilderTests
{
    private sealed class Order
    {
        public int CustomerId { get; set; }
        public int OwnerId { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    [Fact]
    public void IntegerEquality_CompilesAndFiltersCorrectly()
    {
        var expr = RowRestrictionExpressionBuilder.TryBuild<Order>("OwnerId == 42", tokens: null);

        expr.Should().NotBeNull();
        var compiled = expr!.Compile();
        compiled(new Order { OwnerId = 42 }).Should().BeTrue();
        compiled(new Order { OwnerId = 99 }).Should().BeFalse();
    }

    [Fact]
    public void StringLiteralEquality_CompilesAndFiltersCorrectly()
    {
        var expr = RowRestrictionExpressionBuilder.TryBuild<Order>("Status == 'Active'", tokens: null);

        expr.Should().NotBeNull();
        var compiled = expr!.Compile();
        compiled(new Order { Status = "Active" }).Should().BeTrue();
        compiled(new Order { Status = "Closed" }).Should().BeFalse();
    }

    [Fact]
    public void TokenEquality_ResolvesFromDictionaryCaseInsensitive()
    {
        var tokens = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["CurrentUserCustomerId"] = 7
        };

        var expr = RowRestrictionExpressionBuilder.TryBuild<Order>(
            "CustomerId == @currentusercustomerid",
            tokens);

        expr.Should().NotBeNull();
        var compiled = expr!.Compile();
        compiled(new Order { CustomerId = 7 }).Should().BeTrue();
        compiled(new Order { CustomerId = 8 }).Should().BeFalse();
    }

    [Fact]
    public void MissingToken_ReturnsNull()
    {
        var expr = RowRestrictionExpressionBuilder.TryBuild<Order>(
            "CustomerId == @CurrentUserCustomerId",
            tokens: new Dictionary<string, object?>());

        expr.Should().BeNull();
    }

    [Fact]
    public void NullToken_ReturnsNull()
    {
        var tokens = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["CurrentUserCustomerId"] = null
        };

        var expr = RowRestrictionExpressionBuilder.TryBuild<Order>(
            "CustomerId == @CurrentUserCustomerId",
            tokens);

        expr.Should().BeNull();
    }

    [Fact]
    public void UnknownProperty_ReturnsNull()
    {
        var expr = RowRestrictionExpressionBuilder.TryBuild<Order>("NoSuchColumn == 1", tokens: null);

        expr.Should().BeNull();
    }

    [Fact]
    public void UnparseableExpression_ReturnsNull()
    {
        var expr = RowRestrictionExpressionBuilder.TryBuild<Order>("garbage", tokens: null);

        expr.Should().BeNull();
    }

    [Fact]
    public void EmptyExpression_ReturnsNull()
    {
        var expr = RowRestrictionExpressionBuilder.TryBuild<Order>("   ", tokens: null);

        expr.Should().BeNull();
    }
}
