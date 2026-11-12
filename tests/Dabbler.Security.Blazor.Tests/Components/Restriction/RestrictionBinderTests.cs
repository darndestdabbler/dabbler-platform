using Dabbler.Security.Authorization;
using Dabbler.Security.Blazor.Components.Restriction;
using Dabbler.Security.Configuration;

namespace Dabbler.Security.Blazor.Tests.Components.Restriction;

/// <summary>
/// Unit tests for <see cref="RestrictionBinder.Evaluate"/> — the pure
/// static evaluator that <see cref="RestrictionBinder.Resolve"/> and
/// <see cref="RestrictionBinder.ResolveAsync"/> both delegate to.
/// Covers visibility, editability, masking, and multi-role aggregation.
/// </summary>
public class RestrictionBinderTests
{
    private static readonly IBakedInRoleProvider Baked = new PlatformBakedInRoleProvider();
    private static readonly DateTimeOffset Now = new(2026, 4, 19, 12, 0, 0, TimeSpan.Zero);

    private static SecurityConfiguration ConfigWithRoles(params RoleDefinition[] roles) =>
        new() { UserTypeRoles = [.. roles] };

    private static RoleDefinition Role(string name, params ColumnRestriction[] restrictions) =>
        new() { RoleName = name, ColumnRestrictions = [.. restrictions] };

    private static ColumnRestriction Restrict(string entity, string column, ColumnRestrictionKind kind) =>
        new() { EntityKey = entity, ColumnKey = column, RestrictionKind = kind };

    [Fact]
    public void NoRoles_ReturnsUnrestricted()
    {
        var config = new SecurityConfiguration();

        var result = RestrictionBinder.Evaluate(
            "Product", "UnitPrice", config, [], Baked, Now);

        result.Visibility.Should().Be(ColumnVisibility.Visible);
        result.Editability.Should().Be(ColumnEditability.Editable);
        result.IsMasked.Should().BeFalse();
        result.MaskFormat.Should().BeEmpty();
    }

    [Fact]
    public void BakedInRole_AlwaysUnrestricted_EvenWithMatchingRestriction()
    {
        var config = ConfigWithRoles(
            Role("BackofficeUser", Restrict("Product", "UnitPrice", ColumnRestrictionKind.Hidden)));

        var result = RestrictionBinder.Evaluate(
            "Product", "UnitPrice", config, [BakedInRoles.SystemAdmin], Baked, Now);

        result.Visibility.Should().Be(ColumnVisibility.Visible);
        result.Editability.Should().Be(ColumnEditability.Editable);
        result.IsMasked.Should().BeFalse();
    }

    [Fact]
    public void UserTypeRole_NoMatchingRestriction_IsEditable()
    {
        var config = ConfigWithRoles(Role("BackofficeUser"));

        var result = RestrictionBinder.Evaluate(
            "Product", "UnitPrice", config, ["BackofficeUser"], Baked, Now);

        result.Visibility.Should().Be(ColumnVisibility.Visible);
        result.Editability.Should().Be(ColumnEditability.Editable);
    }

    [Fact]
    public void UserTypeRole_ViewOnlyRestriction_IsVisibleReadOnly()
    {
        var config = ConfigWithRoles(
            Role("BackofficeUser", Restrict("Product", "UnitPrice", ColumnRestrictionKind.ViewOnly)));

        var result = RestrictionBinder.Evaluate(
            "Product", "UnitPrice", config, ["BackofficeUser"], Baked, Now);

        result.Visibility.Should().Be(ColumnVisibility.Visible);
        result.Editability.Should().Be(ColumnEditability.ReadOnly);
    }

    [Fact]
    public void UserTypeRole_HiddenRestriction_IsHidden()
    {
        var config = ConfigWithRoles(
            Role("WarehouseUser", Restrict("Product", "UnitPrice", ColumnRestrictionKind.Hidden)));

        var result = RestrictionBinder.Evaluate(
            "Product", "UnitPrice", config, ["WarehouseUser"], Baked, Now);

        result.Visibility.Should().Be(ColumnVisibility.Hidden);
    }

    [Fact]
    public void RoleNotInConfig_IsTreatedAsDefaultPermissive()
    {
        var config = new SecurityConfiguration();

        var result = RestrictionBinder.Evaluate(
            "Product", "UnitPrice", config, ["GhostRole"], Baked, Now);

        result.Visibility.Should().Be(ColumnVisibility.Visible);
        result.Editability.Should().Be(ColumnEditability.Editable);
    }

    [Fact]
    public void MultiRole_MostPermissiveVisibility_OneEditableOneHidden_IsEditable()
    {
        var config = ConfigWithRoles(
            Role("Hidden", Restrict("Product", "UnitPrice", ColumnRestrictionKind.Hidden)),
            Role("Open"));

        var result = RestrictionBinder.Evaluate(
            "Product", "UnitPrice", config, ["Hidden", "Open"], Baked, Now);

        result.Visibility.Should().Be(ColumnVisibility.Visible);
        result.Editability.Should().Be(ColumnEditability.Editable);
    }

    [Fact]
    public void MultiRole_MostPermissiveEditability_OneViewOnlyOneEditable_IsEditable()
    {
        var config = ConfigWithRoles(
            Role("ReadOnly", Restrict("Product", "UnitPrice", ColumnRestrictionKind.ViewOnly)),
            Role("Open"));

        var result = RestrictionBinder.Evaluate(
            "Product", "UnitPrice", config, ["ReadOnly", "Open"], Baked, Now);

        result.Visibility.Should().Be(ColumnVisibility.Visible);
        result.Editability.Should().Be(ColumnEditability.Editable);
    }

    [Fact]
    public void MultiRole_BothViewOnly_StaysReadOnly()
    {
        var config = ConfigWithRoles(
            Role("R1", Restrict("Product", "UnitPrice", ColumnRestrictionKind.ViewOnly)),
            Role("R2", Restrict("Product", "UnitPrice", ColumnRestrictionKind.ViewOnly)));

        var result = RestrictionBinder.Evaluate(
            "Product", "UnitPrice", config, ["R1", "R2"], Baked, Now);

        result.Visibility.Should().Be(ColumnVisibility.Visible);
        result.Editability.Should().Be(ColumnEditability.ReadOnly);
    }

    [Fact]
    public void MultiRole_BothHidden_StaysHidden()
    {
        var config = ConfigWithRoles(
            Role("H1", Restrict("Product", "UnitPrice", ColumnRestrictionKind.Hidden)),
            Role("H2", Restrict("Product", "UnitPrice", ColumnRestrictionKind.Hidden)));

        var result = RestrictionBinder.Evaluate(
            "Product", "UnitPrice", config, ["H1", "H2"], Baked, Now);

        result.Visibility.Should().Be(ColumnVisibility.Hidden);
    }

    [Fact]
    public void DefaultMask_NoExemption_IsMasked()
    {
        var config = ConfigWithRoles(Role("Customer"));
        config.DefaultMasks.Add(new DefaultMaskRule
        {
            EntityKey = "Customer",
            ColumnKey = "SocialSecurityNumber",
            MaskFormat = "XXX-XX-{last4}"
        });

        var result = RestrictionBinder.Evaluate(
            "Customer", "SocialSecurityNumber", config, ["Customer"], Baked, Now);

        result.IsMasked.Should().BeTrue();
        result.MaskFormat.Should().Be("XXX-XX-{last4}");
    }

    [Fact]
    public void DefaultMask_ActiveExemption_NotMasked()
    {
        var config = ConfigWithRoles(Role("Customer"));
        config.DefaultMasks.Add(new DefaultMaskRule
        {
            EntityKey = "Customer",
            ColumnKey = "SocialSecurityNumber",
            MaskFormat = "XXX-XX-{last4}"
        });
        config.MaskExemptions.Add(new MaskExemption
        {
            EntityKey = "Customer",
            ColumnKey = "SocialSecurityNumber",
            TargetRoleName = "Customer",
            ExpiresAt = Now.AddDays(7)
        });

        var result = RestrictionBinder.Evaluate(
            "Customer", "SocialSecurityNumber", config, ["Customer"], Baked, Now);

        result.IsMasked.Should().BeFalse();
        result.MaskFormat.Should().BeEmpty();
    }

    [Fact]
    public void DefaultMask_ExpiredExemption_StaysMasked()
    {
        var config = ConfigWithRoles(Role("Customer"));
        config.DefaultMasks.Add(new DefaultMaskRule
        {
            EntityKey = "Customer",
            ColumnKey = "SocialSecurityNumber",
            MaskFormat = "XXX-XX-{last4}"
        });
        config.MaskExemptions.Add(new MaskExemption
        {
            EntityKey = "Customer",
            ColumnKey = "SocialSecurityNumber",
            TargetRoleName = "Customer",
            ExpiresAt = Now.AddDays(-1)
        });

        var result = RestrictionBinder.Evaluate(
            "Customer", "SocialSecurityNumber", config, ["Customer"], Baked, Now);

        result.IsMasked.Should().BeTrue();
    }

    [Fact]
    public void DefaultMask_ExemptionForOtherRole_StaysMasked()
    {
        var config = ConfigWithRoles(Role("Customer"), Role("Other"));
        config.DefaultMasks.Add(new DefaultMaskRule
        {
            EntityKey = "Customer",
            ColumnKey = "SocialSecurityNumber",
            MaskFormat = "XXX-XX-{last4}"
        });
        config.MaskExemptions.Add(new MaskExemption
        {
            EntityKey = "Customer",
            ColumnKey = "SocialSecurityNumber",
            TargetRoleName = "Other",
            ExpiresAt = null
        });

        var result = RestrictionBinder.Evaluate(
            "Customer", "SocialSecurityNumber", config, ["Customer"], Baked, Now);

        result.IsMasked.Should().BeTrue();
    }

    [Fact]
    public void DefaultMask_BakedInRoleInUserSet_AlwaysUnmasked()
    {
        var config = ConfigWithRoles(Role("Customer"));
        config.DefaultMasks.Add(new DefaultMaskRule
        {
            EntityKey = "Customer",
            ColumnKey = "SocialSecurityNumber",
            MaskFormat = "XXX-XX-{last4}"
        });

        var result = RestrictionBinder.Evaluate(
            "Customer",
            "SocialSecurityNumber",
            config,
            ["Customer", BakedInRoles.SystemAdmin],
            Baked,
            Now);

        result.IsMasked.Should().BeFalse();
    }

    [Fact]
    public void DefaultMask_PermanentExemption_NotMasked()
    {
        var config = ConfigWithRoles(Role("BackofficeUser"));
        config.DefaultMasks.Add(new DefaultMaskRule
        {
            EntityKey = "Customer",
            ColumnKey = "SocialSecurityNumber",
            MaskFormat = "XXX-XX-{last4}"
        });
        config.MaskExemptions.Add(new MaskExemption
        {
            EntityKey = "Customer",
            ColumnKey = "SocialSecurityNumber",
            TargetRoleName = "BackofficeUser",
            ExpiresAt = null
        });

        var result = RestrictionBinder.Evaluate(
            "Customer", "SocialSecurityNumber", config, ["BackofficeUser"], Baked, Now);

        result.IsMasked.Should().BeFalse();
    }

    [Fact]
    public void EntityKey_ColumnKey_AreEchoedInResult()
    {
        var config = ConfigWithRoles(Role("Customer"));

        var result = RestrictionBinder.Evaluate(
            "MyTable", "MyColumn", config, ["Customer"], Baked, Now);

        result.EntityKey.Should().Be("MyTable");
        result.ColumnKey.Should().Be("MyColumn");
    }
}
