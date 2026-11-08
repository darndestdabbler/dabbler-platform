using Bunit;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Authorization;
using Dabbler.Security.Blazor.Components.Restriction;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Dabbler.Security.Configuration;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Dabbler.Security.Blazor.Tests.Components.Restriction;

/// <summary>
/// bUnit interaction tests for <see cref="RestrictedGridColumn"/>. The wrapper
/// is grid-agnostic — it conditionally invokes <see cref="ChildContent"/>
/// with a <see cref="RestrictionResolution"/>. These tests probe the wrapper
/// directly rather than mounting a real <c>MudDataGrid</c>; the column-omission
/// behaviour at the grid layer is the same as any other render-fragment
/// <c>@if</c> guard.
/// </summary>
public class RestrictedGridColumnTests
{
    private static SecurityConfigurationManager BuildManager(SecurityConfiguration config)
    {
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(config);
        store.Setup(s => s.SaveAsync(It.IsAny<SecurityConfiguration>(), default))
             .Returns(Task.CompletedTask);
        return new SecurityConfigurationManager(store.Object);
    }

    private static void Register(IServiceCollection svc, SecurityConfiguration config)
    {
        svc.AddSingleton(BuildManager(config));
        svc.AddSingleton<IBakedInRoleProvider, PlatformBakedInRoleProvider>();
        svc.AddScoped<IRestrictionBinder, RestrictionBinder>();
    }

    private static RenderFragment<RestrictionResolution> StateProbe() =>
        state => b =>
        {
            b.AddMarkupContent(0,
                $"<div data-testid=\"col-probe\" " +
                $"data-vis=\"{state.Visibility}\" " +
                $"data-edit=\"{state.Editability}\" " +
                $"data-masked=\"{state.IsMasked}\" " +
                $"data-fmt=\"{state.MaskFormat}\">column</div>");
        };

    [Fact]
    public void HiddenRestriction_DoesNotInvokeChildContent()
    {
        var config = new SecurityConfiguration
        {
            UserTypeRoles =
            [
                new RoleDefinition
                {
                    RoleName = "Warehouse",
                    ColumnRestrictions =
                    [
                        new ColumnRestriction { EntityKey = "Product", ColumnKey = "UnitPrice", RestrictionKind = ColumnRestrictionKind.Hidden }
                    ],
                },
            ],
        };
        using var ctx = new TestContext();
        ctx.SetupForDabbler(
            roles: ["Warehouse"],
            configureServices: svc => Register(svc, config));

        var cut = ctx.RenderComponent<RestrictedGridColumn>(p => p
            .Add(c => c.EntityKey, "Product")
            .Add(c => c.ColumnKey, "UnitPrice")
            .Add(c => c.ChildContent, StateProbe()));

        cut.FindAll("[data-testid='col-probe']").Should().BeEmpty();
        cut.Markup.Should().NotContain("column");
    }

    [Fact]
    public void ViewOnlyRestriction_PassesReadOnlyState()
    {
        var config = new SecurityConfiguration
        {
            UserTypeRoles =
            [
                new RoleDefinition
                {
                    RoleName = "Backoffice",
                    ColumnRestrictions =
                    [
                        new ColumnRestriction { EntityKey = "Product", ColumnKey = "UnitPrice", RestrictionKind = ColumnRestrictionKind.ViewOnly }
                    ],
                },
            ],
        };
        using var ctx = new TestContext();
        ctx.SetupForDabbler(
            roles: ["Backoffice"],
            configureServices: svc => Register(svc, config));

        var cut = ctx.RenderComponent<RestrictedGridColumn>(p => p
            .Add(c => c.EntityKey, "Product")
            .Add(c => c.ColumnKey, "UnitPrice")
            .Add(c => c.ChildContent, StateProbe()));

        var probe = cut.Find("[data-testid='col-probe']");
        probe.GetAttribute("data-vis").Should().Be("Visible");
        probe.GetAttribute("data-edit").Should().Be("ReadOnly");
        probe.GetAttribute("data-masked").Should().Be("False");
    }

    [Fact]
    public void NoRestriction_PassesEditableState()
    {
        var config = new SecurityConfiguration
        {
            UserTypeRoles = [new RoleDefinition { RoleName = "Open" }],
        };
        using var ctx = new TestContext();
        ctx.SetupForDabbler(
            roles: ["Open"],
            configureServices: svc => Register(svc, config));

        var cut = ctx.RenderComponent<RestrictedGridColumn>(p => p
            .Add(c => c.EntityKey, "Product")
            .Add(c => c.ColumnKey, "UnitPrice")
            .Add(c => c.ChildContent, StateProbe()));

        cut.Find("[data-testid='col-probe']").GetAttribute("data-edit").Should().Be("Editable");
    }

    [Fact]
    public void DefaultMask_NoExemption_StillInvokesChildWithMaskedState()
    {
        // Grid columns leave masked rendering to the consumer (per-row
        // CellTemplate); the wrapper just surfaces the IsMasked flag.
        var config = new SecurityConfiguration
        {
            UserTypeRoles = [new RoleDefinition { RoleName = "Customer" }],
            DefaultMasks =
            [
                new DefaultMaskRule { EntityKey = "Customer", ColumnKey = "SocialSecurityNumber", MaskFormat = "XXX-XX-{last4}" }
            ],
        };
        using var ctx = new TestContext();
        ctx.SetupForDabbler(
            roles: ["Customer"],
            configureServices: svc => Register(svc, config));

        var cut = ctx.RenderComponent<RestrictedGridColumn>(p => p
            .Add(c => c.EntityKey, "Customer")
            .Add(c => c.ColumnKey, "SocialSecurityNumber")
            .Add(c => c.ChildContent, StateProbe()));

        var probe = cut.Find("[data-testid='col-probe']");
        probe.GetAttribute("data-vis").Should().Be("Visible");
        probe.GetAttribute("data-masked").Should().Be("True");
        probe.GetAttribute("data-fmt").Should().Be("XXX-XX-{last4}");
    }

    [Fact]
    public async Task AuthorizationStateChange_RemovesColumnFromRender()
    {
        var manager = BuildManager(new SecurityConfiguration
        {
            UserTypeRoles = [new RoleDefinition { RoleName = "Switcher" }],
        });
        using var ctx = new TestContext();
        ctx.SetupForDabbler(
            roles: ["Switcher"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton<IBakedInRoleProvider, PlatformBakedInRoleProvider>();
                svc.AddScoped<IRestrictionBinder, RestrictionBinder>();
            });

        var cut = ctx.RenderComponent<RestrictedGridColumn>(p => p
            .Add(c => c.EntityKey, "Product")
            .Add(c => c.ColumnKey, "UnitPrice")
            .Add(c => c.ChildContent, StateProbe()));

        cut.Find("[data-testid='col-probe']").Should().NotBeNull();

        await cut.InvokeAsync(() => manager.SaveAsync(new SecurityConfiguration
        {
            UserTypeRoles =
            [
                new RoleDefinition
                {
                    RoleName = "Switcher",
                    ColumnRestrictions =
                    [
                        new ColumnRestriction { EntityKey = "Product", ColumnKey = "UnitPrice", RestrictionKind = ColumnRestrictionKind.Hidden }
                    ],
                }
            ],
        }));

        cut.WaitForAssertion(() => cut.FindAll("[data-testid='col-probe']").Should().BeEmpty());
    }
}
