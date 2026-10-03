using Dabbler.Security.Abstractions;
using Dabbler.Security.Configuration;
using Dabbler.Security.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dabbler.Security.Tests.Services;

public class RowFilterServiceTests
{
    private static RowFilterService CreateService(SecurityConfiguration config)
    {
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(config);
        var manager = new SecurityConfigurationManager(store.Object);
        // Pre-load so Current is available synchronously
        manager.EnsureLoadedAsync().GetAwaiter().GetResult();
        return new RowFilterService(manager, NullLogger<RowFilterService>.Instance);
    }

    private sealed class Order
    {
        public int OwnerId { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    [Fact]
    public void NoFilterConfigured_ReturnsPassthrough()
    {
        var service = CreateService(new SecurityConfiguration());

        var expr = service.GetFilter<Order>("Order", "Viewer");
        var compiled = expr.Compile();

        compiled(new Order { OwnerId = 99 }).Should().BeTrue();
    }

    [Fact]
    public void IntegerEqualityFilter_FiltersCorrectly()
    {
        var config = new SecurityConfiguration
        {
            RowFilters =
            [
                new RowFilter { EntityKey = "Order", RoleName = "Owner", FilterExpression = "OwnerId == 42" }
            ]
        };
        var service = CreateService(config);

        var expr = service.GetFilter<Order>("Order", "Owner");
        var compiled = expr.Compile();

        compiled(new Order { OwnerId = 42 }).Should().BeTrue();
        compiled(new Order { OwnerId = 99 }).Should().BeFalse();
    }

    [Fact]
    public void StringEqualityFilter_FiltersCorrectly()
    {
        var config = new SecurityConfiguration
        {
            RowFilters =
            [
                new RowFilter { EntityKey = "Order", RoleName = "Reviewer", FilterExpression = "Status == 'Active'" }
            ]
        };
        var service = CreateService(config);

        var expr = service.GetFilter<Order>("Order", "Reviewer");
        var compiled = expr.Compile();

        compiled(new Order { Status = "Active" }).Should().BeTrue();
        compiled(new Order { Status = "Closed" }).Should().BeFalse();
    }

    [Fact]
    public void UnparsableExpression_ReturnsDenyAll()
    {
        var config = new SecurityConfiguration
        {
            RowFilters =
            [
                new RowFilter { EntityKey = "Order", RoleName = "Admin", FilterExpression = "UNSUPPORTED EXPR" }
            ]
        };
        var service = CreateService(config);

        var expr = service.GetFilter<Order>("Order", "Admin");
        var compiled = expr.Compile();

        compiled(new Order()).Should().BeFalse();
    }

    [Fact]
    public void WrongRole_ReturnsPassthrough()
    {
        var config = new SecurityConfiguration
        {
            RowFilters =
            [
                new RowFilter { EntityKey = "Order", RoleName = "Owner", FilterExpression = "OwnerId == 1" }
            ]
        };
        var service = CreateService(config);

        // "Admin" role has no filter — should get passthrough
        var expr = service.GetFilter<Order>("Order", "Admin");
        var compiled = expr.Compile();

        compiled(new Order { OwnerId = 99 }).Should().BeTrue();
    }

    [Fact]
    public void CurrentUserIdToken_WithUserId_FiltersToThatUser()
    {
        var config = new SecurityConfiguration
        {
            RowFilters =
            [
                new RowFilter { EntityKey = "Order", RoleName = "Owner", FilterExpression = "OwnerId == @CurrentUserId" }
            ]
        };
        var service = CreateService(config);

        var expr = service.GetFilter<Order>("Order", "Owner", currentUserId: 7);
        var compiled = expr.Compile();

        compiled(new Order { OwnerId = 7 }).Should().BeTrue();
        compiled(new Order { OwnerId = 99 }).Should().BeFalse();
    }

    [Fact]
    public void CurrentUserIdToken_TokenCaseInsensitive_FiltersCorrectly()
    {
        var config = new SecurityConfiguration
        {
            RowFilters =
            [
                new RowFilter { EntityKey = "Order", RoleName = "Owner", FilterExpression = "OwnerId == @currentuserid" }
            ]
        };
        var service = CreateService(config);

        var expr = service.GetFilter<Order>("Order", "Owner", currentUserId: 42);
        var compiled = expr.Compile();

        compiled(new Order { OwnerId = 42 }).Should().BeTrue();
        compiled(new Order { OwnerId = 1 }).Should().BeFalse();
    }

    [Fact]
    public void CurrentUserIdToken_WithoutUserId_ReturnsDenyAll()
    {
        var config = new SecurityConfiguration
        {
            RowFilters =
            [
                new RowFilter { EntityKey = "Order", RoleName = "Owner", FilterExpression = "OwnerId == @CurrentUserId" }
            ]
        };
        var service = CreateService(config);

        // No currentUserId supplied — must deny access (fail closed), not pass through
        var expr = service.GetFilter<Order>("Order", "Owner");
        var compiled = expr.Compile();

        compiled(new Order { OwnerId = 99 }).Should().BeFalse();
    }
}
