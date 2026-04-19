using Bunit;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Authorization;
using Dabbler.Security.Blazor.Components;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Dabbler.Security.Configuration;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Dabbler.Security.Blazor.Tests.Components;

public class DabblerSecuredTests
{
    private static SecurityConfigurationManager BuildManager(SecurityConfiguration config)
    {
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(config);
        return new SecurityConfigurationManager(store.Object);
    }

    private static void RegisterSecuredServices(
        IServiceCollection svc,
        SecurityConfiguration config)
    {
        svc.AddSingleton(BuildManager(config));
        svc.AddSingleton<IBakedInRoleProvider, PlatformBakedInRoleProvider>();
    }

    [Fact]
    public void UserTypeRole_WithHiddenUiRestriction_RendersNothing()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            UserTypeRoles =
            [
                new RoleDefinition
                {
                    RoleName = "Restricted",
                    UiRestrictions =
                    [
                        new UiRestriction { PageKey = "dashboard", ControlKey = "secret-btn", Kind = UiRestrictionKind.Hidden }
                    ],
                },
            ],
        };
        ctx.SetupForDabbler(
            roles: ["Restricted"],
            configureServices: svc => RegisterSecuredServices(svc, config));

        var cut = ctx.RenderComponent<DabblerSecured>(p => p
            .Add(c => c.PageKey, "dashboard")
            .Add(c => c.ControlKey, "secret-btn")
            .Add(c => c.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<span>Secret</span>"))));

        cut.Markup.Should().NotContain("Secret");
    }

    [Fact]
    public void UserTypeRole_WithDisabledUiRestriction_RendersWithDisabledWrapper()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            UserTypeRoles =
            [
                new RoleDefinition
                {
                    RoleName = "Viewer",
                    UiRestrictions =
                    [
                        new UiRestriction { PageKey = "orders", ControlKey = "delete-btn", Kind = UiRestrictionKind.Disabled }
                    ],
                },
            ],
        };
        ctx.SetupForDabbler(
            roles: ["Viewer"],
            configureServices: svc => RegisterSecuredServices(svc, config));

        var cut = ctx.RenderComponent<DabblerSecured>(p => p
            .Add(c => c.PageKey, "orders")
            .Add(c => c.ControlKey, "delete-btn")
            .Add(c => c.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<button>Delete</button>"))));

        cut.Markup.Should().Contain("Delete");
        cut.Markup.Should().Contain("aria-disabled");
    }

    [Fact]
    public void UserTypeRole_WithNoRestriction_RendersNormally()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            UserTypeRoles = [new RoleDefinition { RoleName = "Editor" }],
        };
        ctx.SetupForDabbler(
            roles: ["Editor"],
            configureServices: svc => RegisterSecuredServices(svc, config));

        var cut = ctx.RenderComponent<DabblerSecured>(p => p
            .Add(c => c.PageKey, "admin")
            .Add(c => c.ControlKey, "create-btn")
            .Add(c => c.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<button>Create</button>"))));

        cut.Markup.Should().Contain("Create");
        cut.Markup.Should().NotContain("aria-disabled");
    }

    [Fact]
    public void BakedInRole_AlwaysRendersEnabled()
    {
        using var ctx = new TestContext();
        // Even with a UiRestriction on the SystemAdmin name, baked-in roles bypass
        // user-type restrictions (they are not stored in UserTypeRoles).
        var config = new SecurityConfiguration();
        ctx.SetupForDabbler(
            roles: [BakedInRoles.SystemAdmin],
            configureServices: svc => RegisterSecuredServices(svc, config));

        var cut = ctx.RenderComponent<DabblerSecured>(p => p
            .Add(c => c.PageKey, "admin")
            .Add(c => c.ControlKey, "danger-btn")
            .Add(c => c.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<button>Danger</button>"))));

        cut.Markup.Should().Contain("Danger");
        cut.Markup.Should().NotContain("aria-disabled");
    }

    [Fact]
    public void EntirePageHiddenRestriction_HidesEveryControl()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            UserTypeRoles =
            [
                new RoleDefinition
                {
                    RoleName = "Banned",
                    UiRestrictions =
                    [
                        new UiRestriction { PageKey = "secret", ControlKey = "*", Kind = UiRestrictionKind.Hidden }
                    ],
                },
            ],
        };
        ctx.SetupForDabbler(
            roles: ["Banned"],
            configureServices: svc => RegisterSecuredServices(svc, config));

        var cut = ctx.RenderComponent<DabblerSecured>(p => p
            .Add(c => c.PageKey, "secret")
            .Add(c => c.ControlKey, "any-control-at-all")
            .Add(c => c.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<span>Visible?</span>"))));

        cut.Markup.Should().NotContain("Visible?");
    }

    [Fact]
    public void UnauthenticatedUser_RendersNothing()
    {
        using var ctx = new TestContext();
        var config = new SecurityConfiguration();

        ctx.SetupForDabbler(
            configureServices: svc => RegisterSecuredServices(svc, config));

        var cut = ctx.RenderComponent<DabblerSecured>(p => p
            .Add(c => c.PageKey, "page")
            .Add(c => c.ControlKey, "btn")
            .Add(c => c.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<span>Secured</span>"))));

        cut.Markup.Should().NotContain("Secured");
    }
}
