using Bunit;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Blazor.Components.Admin;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Dabbler.Security.Configuration;
using Dabbler.Security.Registration;
using Dabbler.Security.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Dabbler.Security.Blazor.Tests.Components.Admin;

public class DabblerControlPermissionTabTests
{
    private static (SecurityConfigurationManager manager, Mock<ISecurityConfigurationStore> store)
        BuildManager(SecurityConfiguration config)
    {
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(config);
        store.Setup(s => s.SaveAsync(It.IsAny<SecurityConfiguration>(), default))
             .Returns(Task.CompletedTask);
        return (new SecurityConfigurationManager(store.Object), store);
    }

    private static ResourceRegistry BuildRegistry(string pageKey, string pageLabel, params string[] controlKeys) =>
        new ResourceRegistry(
            [new RegisteredPage { Key = pageKey, DisplayName = pageLabel }],
            [], [],
            controlKeys.Select(k => new RegisteredControl { Key = k, PageKey = pageKey, DisplayName = k }).ToList());

    [Fact]
    public void StateDropdown_RendersIconAndText_ForEachControlState()
    {
        // Three roles seeded with one of each ControlState so every state's
        // selected-display template renders in the same CUT markup.
        using var ctx = new TestContext();
        var config = new SecurityConfiguration
        {
            Roles =
            [
                new RoleDefinition { RoleName = "RoleEnabled" },
                new RoleDefinition { RoleName = "RoleDisabled" },
                new RoleDefinition { RoleName = "RoleHidden" }
            ],
            ControlPermissions =
            [
                new ControlPermission
                {
                    PageKey = "orders",
                    ControlKey = "delete-btn",
                    RolePermissions =
                    [
                        new ControlRolePermission { RoleName = "RoleEnabled",  State = ControlState.Enabled },
                        new ControlRolePermission { RoleName = "RoleDisabled", State = ControlState.Disabled },
                        new ControlRolePermission { RoleName = "RoleHidden",   State = ControlState.Hidden }
                    ]
                }
            ]
        };
        var (manager, _) = BuildManager(config);
        ctx.SetupForDabbler(
            roles: ["SystemAdministrator"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton(BuildRegistry("orders", "Orders", "delete-btn"));
                svc.AddSingleton<AuthorizationStateNotifier>();
            });

        var cut = ctx.RenderComponent<DabblerControlPermissionTab>();

        cut.Markup.Should().Contain("_content/Dabbler.Security.Blazor/enabled.svg");
        cut.Markup.Should().Contain("_content/Dabbler.Security.Blazor/disabled.svg");
        cut.Markup.Should().Contain("_content/Dabbler.Security.Blazor/hidden.svg");
        cut.Markup.Should().Contain("Enabled");
        cut.Markup.Should().Contain("Disabled");
        cut.Markup.Should().Contain("Hidden");

        var iconImgs = cut.FindAll("img[src*='_content/Dabbler.Security.Blazor/']");
        iconImgs.Should().NotBeEmpty();
        foreach (var img in iconImgs)
        {
            img.GetAttribute("aria-hidden").Should().Be("true");
            img.GetAttribute("alt").Should().Be(string.Empty);
        }
    }
}
