using Bunit;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Authorization;
using Dabbler.Security.Blazor.Components.Restriction;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Dabbler.Security.Configuration;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Dabbler.Security.Blazor.Tests.Components.Restriction;

/// <summary>
/// bUnit interaction tests for <see cref="RestrictedFormField"/>. Covers
/// the four render branches (Hidden, Masked, ReadOnly state passed through,
/// Editable state passed through) and the role-switch reactivity wired
/// through <see cref="AuthorizationStateNotifier"/>.
/// </summary>
public class RestrictedFormFieldTests
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
                $"<div data-testid=\"probe\" " +
                $"data-vis=\"{state.Visibility}\" " +
                $"data-edit=\"{state.Editability}\" " +
                $"data-masked=\"{state.IsMasked}\">child</div>");
        };

    [Fact]
    public async Task HiddenRestriction_RendersNothing()
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
        await using var ctx = new BunitContext();
        ctx.SetupForDabbler(
            roles: ["Warehouse"],
            configureServices: svc => Register(svc, config));

        var cut = ctx.Render<RestrictedFormField>(p => p
            .Add(c => c.EntityKey, "Product")
            .Add(c => c.ColumnKey, "UnitPrice")
            .Add(c => c.ChildContent, StateProbe()));

        cut.FindAll("[data-testid='probe']").Should().BeEmpty();
        cut.Markup.Should().NotContain("child");
    }

    [Fact]
    public async Task ViewOnlyRestriction_PassesReadOnlyStateToChild()
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
        await using var ctx = new BunitContext();
        ctx.SetupForDabbler(
            roles: ["Backoffice"],
            configureServices: svc => Register(svc, config));

        var cut = ctx.Render<RestrictedFormField>(p => p
            .Add(c => c.EntityKey, "Product")
            .Add(c => c.ColumnKey, "UnitPrice")
            .Add(c => c.ChildContent, StateProbe()));

        var probe = cut.Find("[data-testid='probe']");
        probe.GetAttribute("data-vis").Should().Be("Visible");
        probe.GetAttribute("data-edit").Should().Be("ReadOnly");
        probe.GetAttribute("data-masked").Should().Be("False");
    }

    [Fact]
    public async Task NoRestriction_PassesEditableStateToChild()
    {
        var config = new SecurityConfiguration
        {
            UserTypeRoles = [new RoleDefinition { RoleName = "Open" }],
        };
        await using var ctx = new BunitContext();
        ctx.SetupForDabbler(
            roles: ["Open"],
            configureServices: svc => Register(svc, config));

        var cut = ctx.Render<RestrictedFormField>(p => p
            .Add(c => c.EntityKey, "Product")
            .Add(c => c.ColumnKey, "UnitPrice")
            .Add(c => c.ChildContent, StateProbe()));

        var probe = cut.Find("[data-testid='probe']");
        probe.GetAttribute("data-vis").Should().Be("Visible");
        probe.GetAttribute("data-edit").Should().Be("Editable");
    }

    [Fact]
    public async Task BakedInRole_BypassesRestriction()
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
        await using var ctx = new BunitContext();
        ctx.SetupForDabbler(
            roles: [BakedInRoles.SystemAdmin],
            configureServices: svc => Register(svc, config));

        var cut = ctx.Render<RestrictedFormField>(p => p
            .Add(c => c.EntityKey, "Product")
            .Add(c => c.ColumnKey, "UnitPrice")
            .Add(c => c.ChildContent, StateProbe()));

        var probe = cut.Find("[data-testid='probe']");
        probe.GetAttribute("data-vis").Should().Be("Visible");
        probe.GetAttribute("data-edit").Should().Be("Editable");
    }

    [Fact]
    public async Task DefaultMask_NoExemption_RendersPlaceholderInsteadOfChild()
    {
        var config = new SecurityConfiguration
        {
            UserTypeRoles = [new RoleDefinition { RoleName = "Customer" }],
            DefaultMasks =
            [
                new DefaultMaskRule { EntityKey = "Customer", ColumnKey = "SocialSecurityNumber", MaskFormat = "XXX-XX-{last4}" }
            ],
        };
        await using var ctx = new BunitContext();
        ctx.SetupForDabbler(
            roles: ["Customer"],
            configureServices: svc => Register(svc, config));

        var cut = ctx.Render<RestrictedFormField>(p => p
            .Add(c => c.EntityKey, "Customer")
            .Add(c => c.ColumnKey, "SocialSecurityNumber")
            .Add(c => c.ChildContent, StateProbe()));

        var maskedNode = cut.Find($"[data-testid='restricted-field-masked-Customer-SocialSecurityNumber']");
        maskedNode.GetAttribute("data-masked").Should().Be("true");
        maskedNode.GetAttribute("data-mask-format").Should().Be("XXX-XX-{last4}");
        // No Value supplied → null → FieldMaskService renders {last4} as ****
        maskedNode.TextContent.Trim().Should().Be("XXX-XX-****");

        cut.FindAll("[data-testid='probe']").Should().BeEmpty();
    }

    [Fact]
    public async Task DefaultMask_CustomMaskedTemplate_OverridesPlaceholder()
    {
        var config = new SecurityConfiguration
        {
            UserTypeRoles = [new RoleDefinition { RoleName = "Customer" }],
            DefaultMasks =
            [
                new DefaultMaskRule { EntityKey = "Customer", ColumnKey = "SocialSecurityNumber", MaskFormat = "XXX-XX-{last4}" }
            ],
        };
        await using var ctx = new BunitContext();
        ctx.SetupForDabbler(
            roles: ["Customer"],
            configureServices: svc => Register(svc, config));

        RenderFragment<RestrictionResolution> custom = state => b =>
        {
            b.AddMarkupContent(0, $"<span data-testid='custom-mask' data-fmt='{state.MaskFormat}'>fancy</span>");
        };

        var cut = ctx.Render<RestrictedFormField>(p => p
            .Add(c => c.EntityKey, "Customer")
            .Add(c => c.ColumnKey, "SocialSecurityNumber")
            .Add(c => c.MaskedTemplate, custom)
            .Add(c => c.ChildContent, StateProbe()));

        var custom1 = cut.Find("[data-testid='custom-mask']");
        custom1.GetAttribute("data-fmt").Should().Be("XXX-XX-{last4}");
        cut.FindAll("[data-testid='probe']").Should().BeEmpty();
        cut.FindAll("[data-testid='restricted-field-masked-Customer-SocialSecurityNumber']").Should().BeEmpty();
    }

    [Fact]
    public async Task DefaultMask_WithStoredValue_RendersTokenSubstitutedMask()
    {
        // Spec §Session 1 step 6: Customer.SocialSecurityNumber = "123-45-6789" +
        // default mask "XXX-XX-{last4}" → renders as "XXX-XX-6789".
        var config = new SecurityConfiguration
        {
            UserTypeRoles = [new RoleDefinition { RoleName = "Customer" }],
            DefaultMasks =
            [
                new DefaultMaskRule { EntityKey = "Customer", ColumnKey = "SocialSecurityNumber", MaskFormat = "XXX-XX-{last4}" }
            ],
        };
        await using var ctx = new BunitContext();
        ctx.SetupForDabbler(
            roles: ["Customer"],
            configureServices: svc => Register(svc, config));

        var cut = ctx.Render<RestrictedFormField>(p => p
            .Add(c => c.EntityKey, "Customer")
            .Add(c => c.ColumnKey, "SocialSecurityNumber")
            .Add(c => c.Value, "123-45-6789")
            .Add(c => c.ChildContent, StateProbe()));

        var maskedNode = cut.Find($"[data-testid='restricted-field-masked-Customer-SocialSecurityNumber']");
        maskedNode.TextContent.Trim().Should().Be("XXX-XX-6789");
        cut.FindAll("[data-testid='probe']").Should().BeEmpty();
    }

    [Fact]
    public async Task AuthorizationStateChange_TriggersReResolutionWithinCircuit()
    {
        var initialConfig = new SecurityConfiguration
        {
            UserTypeRoles = [new RoleDefinition { RoleName = "Open" }],
        };
        var manager = BuildManager(initialConfig);
        await using var ctx = new BunitContext();
        ctx.SetupForDabbler(
            roles: ["Open"],
            configureServices: svc =>
            {
                svc.AddSingleton(manager);
                svc.AddSingleton<IBakedInRoleProvider, PlatformBakedInRoleProvider>();
                svc.AddScoped<IRestrictionBinder, RestrictionBinder>();
            });

        var cut = ctx.Render<RestrictedFormField>(p => p
            .Add(c => c.EntityKey, "Product")
            .Add(c => c.ColumnKey, "UnitPrice")
            .Add(c => c.ChildContent, StateProbe()));

        cut.Find("[data-testid='probe']").GetAttribute("data-edit").Should().Be("Editable");

        // Re-bind: the new configuration adds a Hidden restriction on the
        // active role's binding for (Product, UnitPrice). Saving routes
        // through SecurityConfigurationManager → AuthorizationStateNotifier
        // → the wrapper's OnConfigChangedAsync handler.
        await cut.InvokeAsync(() => manager.SaveAsync(new SecurityConfiguration
        {
            UserTypeRoles =
            [
                new RoleDefinition
                {
                    RoleName = "Open",
                    ColumnRestrictions =
                    [
                        new ColumnRestriction { EntityKey = "Product", ColumnKey = "UnitPrice", RestrictionKind = ColumnRestrictionKind.Hidden }
                    ],
                }
            ],
        }));

        cut.WaitForAssertion(() => cut.FindAll("[data-testid='probe']").Should().BeEmpty());
    }
}
