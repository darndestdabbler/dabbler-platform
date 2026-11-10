using AcmeCoffeeOnline.Web.Components.Shared;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace AcmeCoffeeOnline.Web.Tests.Components.Shared;

public sealed class AccessDeniedCardTests : TestContext
{
    public AccessDeniedCardTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
    }

    [Fact]
    public void Renders_ApprovedCopy_HeadingAndBody()
    {
        var cut = RenderComponent<AccessDeniedCard>();

        var card = cut.Find("[data-testid='access-denied-card']");
        Assert.Contains("Access denied", card.TextContent);
        Assert.Contains(
            "You don't have access to this page. If you need access, please request it from your administrator.",
            card.TextContent);
    }

    [Fact]
    public void Renders_BackToDashboardButton()
    {
        var cut = RenderComponent<AccessDeniedCard>();

        var button = cut.Find("[data-testid='access-denied-back-button']");
        Assert.Contains("Back to dashboard", button.TextContent);
    }

    [Fact]
    public void BackButton_Click_NavigatesToDashboard()
    {
        var cut = RenderComponent<AccessDeniedCard>();
        var navigationManager = Services.GetRequiredService<NavigationManager>();

        cut.Find("[data-testid='access-denied-back-button']").Click();

        Assert.EndsWith("/dashboard", navigationManager.Uri);
    }

    [Fact]
    public void Wrapper_Has_AlertRole_ForScreenReaders()
    {
        var cut = RenderComponent<AccessDeniedCard>();

        var card = cut.Find("[data-testid='access-denied-card']");
        Assert.Equal("alert", card.GetAttribute("role"));
        Assert.Equal("assertive", card.GetAttribute("aria-live"));
    }
}
