using Dabbler.Security.Authorization;
using Dabbler.Security.Configuration;

namespace Dabbler.Security.Tests.Authorization;

public class PermissionResolverTests
{
    private static readonly List<RoleDefinition> _threeLevelRoles =
    [
        new() { RoleName = "Admin" },
        new() { RoleName = "Manager", InheritsFrom = "Admin" },
        new() { RoleName = "Clerk", InheritsFrom = "Manager" },
        new() { RoleName = "Isolated" },
    ];

    [Fact]
    public void ResolveRoleChain_ExplicitOnRole_ReportsExplicit()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Manager" };

        var result = PermissionResolver.ResolveRoleChain(
            roleName: "Manager",
            hasValue: r => set.Contains(r),
            allRoles: _threeLevelRoles);

        result.Found.Should().BeTrue();
        result.InheritedFromRole.Should().BeNull();
    }

    [Fact]
    public void ResolveRoleChain_EntryOnDirectParent_ReportsInheritedFromParent()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Admin" };

        var result = PermissionResolver.ResolveRoleChain(
            roleName: "Manager",
            hasValue: r => set.Contains(r),
            allRoles: _threeLevelRoles);

        result.Found.Should().BeTrue();
        result.InheritedFromRole.Should().Be("Admin");
    }

    [Fact]
    public void ResolveRoleChain_EntryOnGrandparent_ReportsInheritedFromGrandparent()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Admin" };

        var result = PermissionResolver.ResolveRoleChain(
            roleName: "Clerk",
            hasValue: r => set.Contains(r),
            allRoles: _threeLevelRoles);

        result.Found.Should().BeTrue();
        result.InheritedFromRole.Should().Be("Admin");
    }

    [Fact]
    public void ResolveRoleChain_ChildHasOwnEntry_PrefersExplicitOverInherited()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Admin", "Clerk" };

        var result = PermissionResolver.ResolveRoleChain(
            roleName: "Clerk",
            hasValue: r => set.Contains(r),
            allRoles: _threeLevelRoles);

        result.Found.Should().BeTrue();
        result.InheritedFromRole.Should().BeNull("an explicit entry on the role itself wins over any ancestor");
    }

    [Fact]
    public void ResolveRoleChain_NoEntryAnywhere_ReturnsNotFound()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var result = PermissionResolver.ResolveRoleChain(
            roleName: "Manager",
            hasValue: r => set.Contains(r),
            allRoles: _threeLevelRoles);

        result.Found.Should().BeFalse();
        result.InheritedFromRole.Should().BeNull();
    }

    [Fact]
    public void ResolveRoleChain_NoInheritance_OnlyChecksTheRoleItself()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Admin" };

        var result = PermissionResolver.ResolveRoleChain(
            roleName: "Isolated",
            hasValue: r => set.Contains(r),
            allRoles: _threeLevelRoles);

        result.Found.Should().BeFalse("Isolated has no InheritsFrom so the chain ends at itself");
    }

    [Fact]
    public void ResolveRoleChain_CyclicInheritance_DoesNotLoopForever()
    {
        var cyclicRoles = new List<RoleDefinition>
        {
            new() { RoleName = "A", InheritsFrom = "B" },
            new() { RoleName = "B", InheritsFrom = "A" },
        };
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var result = PermissionResolver.ResolveRoleChain(
            roleName: "A",
            hasValue: r => set.Contains(r),
            allRoles: cyclicRoles);

        result.Found.Should().BeFalse();
    }
}
