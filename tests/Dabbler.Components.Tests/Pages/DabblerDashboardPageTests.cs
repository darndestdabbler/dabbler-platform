using Bunit;
using Dabbler.Components.Data.Metadata;
using Dabbler.Components.Tests.TestHelpers;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace Dabbler.Components.Tests.Pages;

public class DabblerDashboardPageTests
{
    [Fact]
    public void CoreNavigationWidgets_RenderWithoutUsageTracking()
    {
        using var ctx = new TestContext().SetupForDabblerComponents();

        var cut = ctx.RenderComponent<DabblerDashboardPage>(parameters => parameters
            .Add(x => x.Configuration, BuildNavigationOnlyDashboard()));

        cut.Markup.Should().Contain("Orders");
        cut.Markup.Should().Contain("Reports");
        cut.FindAll("[data-compatibility-widget]").Should().BeEmpty();
    }

    [Fact]
    public void CompatibilityWidgets_RenderPlaceholderWithoutUsageTracking()
    {
        using var ctx = new TestContext().SetupForDabblerComponents();

        var cut = ctx.RenderComponent<DabblerDashboardPage>(parameters => parameters
            .Add(x => x.Configuration, BuildCompatibilityWidgetDashboard()));

        cut.Find("[data-compatibility-widget='RecentItems']").TextContent
            .Should().Contain("compatibility-only dashboard widget");
        cut.Find("[data-compatibility-widget='Favorites']").TextContent
            .Should().Contain("requires usage tracking");
    }

    private static DashboardConfiguration BuildNavigationOnlyDashboard() =>
        new()
        {
            DashboardId = "core-dashboard",
            Name = "Core Dashboard",
            ColumnCount = 2,
            Widgets =
            [
                new WidgetPlacement
                {
                    PlacementId = "orders",
                    DashboardId = "core-dashboard",
                    GridRow = 1,
                    GridColumn = 1,
                    WidgetType = WidgetType.NavigationCard,
                    ConfigJson = JsonSerializer.Serialize(new
                    {
                        Label = "Orders",
                        Icon = "ShoppingCart",
                        NavigateTo = new { Type = "Query", EntityId = "Order" }
                    })
                },
                new WidgetPlacement
                {
                    PlacementId = "reports",
                    DashboardId = "core-dashboard",
                    GridRow = 1,
                    GridColumn = 2,
                    WidgetType = WidgetType.QuickLinksGroup,
                    ConfigJson = JsonSerializer.Serialize(new
                    {
                        Title = "Reports",
                        Links = new[]
                        {
                            new { Label = "Orders by Customer", Type = "Report", ArtifactId = "orders-by-customer" }
                        }
                    })
                }
            ]
        };

    private static DashboardConfiguration BuildCompatibilityWidgetDashboard() =>
        new()
        {
            DashboardId = "compat-dashboard",
            Name = "Compatibility Dashboard",
            ColumnCount = 2,
            Widgets =
            [
                new WidgetPlacement
                {
                    PlacementId = "recent",
                    DashboardId = "compat-dashboard",
                    GridRow = 1,
                    GridColumn = 1,
                    WidgetType = WidgetType.RecentItems
                },
                new WidgetPlacement
                {
                    PlacementId = "favorites",
                    DashboardId = "compat-dashboard",
                    GridRow = 1,
                    GridColumn = 2,
                    WidgetType = WidgetType.Favorites
                }
            ]
        };
}
