using Bunit;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Authorization;
using Dabbler.Security.Blazor.Components.Admin.Users;
using Dabbler.Security.Blazor.Tests.TestHelpers;
using Dabbler.Security.Configuration;
using Dabbler.Security.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Dabbler.Security.Blazor.Tests.Components.Admin.Users;

public class UserRoleAssignmentDetailsTests
{
    private static readonly AdminUserRow Target = new(
        "42", "Target User", "target@test.local", true,
        [new AdminUserOrganization(10, "internal", "Internal", true, true)],
        ["Customer"]);

    [Fact]
    public void RendersEmptyState_WhenNoUserSelected()
    {
        using var ctx = new TestContext();
        ctx.SetupForDabbler(configureServices: RegisterCommonServices);

        var cut = ctx.RenderComponent<UserRoleAssignmentDetails>();

        cut.Find("[data-testid='user-role-assignment-empty']").Should().NotBeNull();
    }

    [Fact]
    public void RendersCurrentRoles_AsChips()
    {
        using var ctx = new TestContext();
        ctx.SetupForDabbler(configureServices: RegisterCommonServices);

        var cut = ctx.RenderComponent<UserRoleAssignmentDetails>(parameters => parameters
            .Add(x => x.User, Target));

        cut.Find("[data-testid='user-role-assignment-current-Customer']").Should().NotBeNull();
    }

    [Fact]
    public void AssignableRoles_FilteredByAuthorityForCrossOrgUserAdmin()
    {
        using var ctx = new TestContext();
        var queryMock = new Mock<IAdminUserQueryService>();
        queryMock.Setup(q => q.GetActorActiveAdminRole()).Returns(BakedInRoles.CrossOrgUserAdmin);
        queryMock.Setup(q => q.GetActorUserId()).Returns("99");
        queryMock.Setup(q => q.GetActorAssignedOrganizationIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int> { 10 });

        ctx.SetupForDabbler(configureServices: services =>
        {
            RegisterCommonServices(services);
            services.RemoveAll<IAdminUserQueryService>();
            services.AddSingleton(queryMock.Object);
        });

        var cut = ctx.RenderComponent<UserRoleAssignmentDetails>(parameters => parameters
            .Add(x => x.User, Target));

        var assignable = GetAssignableRoleNames(cut.Instance);

        // CrossOrgUserAdmin cannot assign SystemAdmin to another user (authority rule).
        assignable.Should().NotContain(BakedInRoles.SystemAdmin);
        // Target already holds the user-type Customer role, so adding a
        // non-user-type role on top of it is denied by the combination guard.
        assignable.Should().NotContain(BakedInRoles.CrossOrgMonitor);
        // But adding another user-type role is allowed.
        assignable.Should().Contain("BackofficeUser");
    }

    [Fact]
    public void AssignableRoles_EmptyWhenActorHasNoAdminRole()
    {
        using var ctx = new TestContext();
        var queryMock = new Mock<IAdminUserQueryService>();
        queryMock.Setup(q => q.GetActorActiveAdminRole()).Returns((string?)null);
        queryMock.Setup(q => q.GetActorUserId()).Returns(string.Empty);
        queryMock.Setup(q => q.GetActorAssignedOrganizationIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());

        ctx.SetupForDabbler(configureServices: services =>
        {
            RegisterCommonServices(services);
            services.RemoveAll<IAdminUserQueryService>();
            services.AddSingleton(queryMock.Object);
        });

        var cut = ctx.RenderComponent<UserRoleAssignmentDetails>(parameters => parameters
            .Add(x => x.User, Target));

        cut.Find("[data-testid='user-role-assignment-no-actor-role']").Should().NotBeNull();
    }

    [Fact]
    public async Task AssignAsync_RejectsUnknownRoleName_WithoutCallingStore()
    {
        using var ctx = new TestContext();
        var roleStoreMock = new Mock<IUserRoleStore>();
        var raisedFor = string.Empty;

        ctx.SetupForDabbler(configureServices: services =>
        {
            RegisterCommonServices(services);
            services.RemoveAll<IUserRoleStore>();
            services.AddSingleton(roleStoreMock.Object);
        });

        var cut = ctx.RenderComponent<UserRoleAssignmentDetails>(parameters => parameters
            .Add(x => x.User, Target)
            .Add(x => x.RolesChanged, userId => raisedFor = userId));

        await cut.InvokeAsync(() =>
        {
            var type = cut.Instance.GetType();
            type.GetField("_pendingRoleName",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(cut.Instance, "TotallyMadeUpRole");

            var assign = type.GetMethod("AssignAsync",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            return (Task)assign.Invoke(cut.Instance, null)!;
        });

        roleStoreMock.Verify(
            s => s.AddRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        raisedFor.Should().BeEmpty();
    }

    [Fact]
    public async Task AssignAsync_RejectsBakedInRoleNotExposedByConfiguredProvider()
    {
        // Defense in depth: a deployment may register a custom
        // IBakedInRoleProvider that exposes only a subset of canonical
        // baked-in roles (the platform contract allows host apps to add
        // non-user-type roles or restrict the surface). A tampered
        // _pendingRoleName carrying a static canonical name like
        // "SystemAdmin" must not be accepted just because the static helper
        // BakedInRoles.IsBakedIn recognises it.
        using var ctx = new TestContext();
        var roleStoreMock = new Mock<IUserRoleStore>();

        // Provider that only exposes UserAdmin + User; SystemAdmin is not
        // returned. Tampering with _pendingRoleName to "SystemAdmin" must
        // therefore be rejected by IsRecognizedRole.
        var providerMock = new Mock<IBakedInRoleProvider>();
        var userAdminDef = new BakedInRoleDefinition(
            BakedInRoles.UserAdmin, "User Administrator", "scoped admin",
            UserAdminPrivileges.Instance);
        var userDef = new BakedInRoleDefinition(
            BakedInRoles.User, "User", "default user-type",
            UserPrivileges.Instance);
        providerMock.Setup(p => p.GetBakedInRoles())
            .Returns([userAdminDef, userDef]);
        providerMock.Setup(p => p.TryGetBakedInRole(It.IsAny<string>()))
            .Returns((string name) => string.Equals(name, BakedInRoles.UserAdmin, StringComparison.OrdinalIgnoreCase)
                ? userAdminDef
                : string.Equals(name, BakedInRoles.User, StringComparison.OrdinalIgnoreCase)
                    ? userDef
                    : null);

        ctx.SetupForDabbler(configureServices: services =>
        {
            RegisterCommonServices(services);
            services.RemoveAll<IUserRoleStore>();
            services.AddSingleton(roleStoreMock.Object);
            services.RemoveAll<IBakedInRoleProvider>();
            services.AddSingleton(providerMock.Object);
        });

        var cut = ctx.RenderComponent<UserRoleAssignmentDetails>(parameters => parameters
            .Add(x => x.User, Target));

        await cut.InvokeAsync(() =>
        {
            var type = cut.Instance.GetType();
            type.GetField("_pendingRoleName",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(cut.Instance, BakedInRoles.SystemAdmin);

            var assign = type.GetMethod("AssignAsync",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            return (Task)assign.Invoke(cut.Instance, null)!;
        });

        roleStoreMock.Verify(
            s => s.AddRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AssignAsync_RejectsRoleAuthorityForbidsAtSubmitTime_EvenIfPendingRoleIsTampered()
    {
        // Defense in depth: even if the dropdown filter excluded SystemAdmin
        // for a CrossOrgUserAdmin actor, a tampered _pendingRoleName must
        // still be rejected at submit time.
        using var ctx = new TestContext();
        var roleStoreMock = new Mock<IUserRoleStore>();

        ctx.SetupForDabbler(configureServices: services =>
        {
            RegisterCommonServices(services);
            services.RemoveAll<IUserRoleStore>();
            services.AddSingleton(roleStoreMock.Object);

            services.RemoveAll<IAdminUserQueryService>();
            var queryMock = new Mock<IAdminUserQueryService>();
            queryMock.Setup(q => q.GetActorActiveAdminRole()).Returns(BakedInRoles.CrossOrgUserAdmin);
            queryMock.Setup(q => q.GetActorUserId()).Returns("99");
            queryMock.Setup(q => q.GetActorAssignedOrganizationIdsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HashSet<int> { 10 });
            services.AddSingleton(queryMock.Object);
        });

        var cut = ctx.RenderComponent<UserRoleAssignmentDetails>(parameters => parameters
            .Add(x => x.User, Target));

        await cut.InvokeAsync(() =>
        {
            var type = cut.Instance.GetType();
            type.GetField("_pendingRoleName",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(cut.Instance, BakedInRoles.SystemAdmin);

            var assign = type.GetMethod("AssignAsync",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            return (Task)assign.Invoke(cut.Instance, null)!;
        });

        roleStoreMock.Verify(
            s => s.AddRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RemoveRoleAsync_RejectsForbiddenRemovalEvenIfChipIsClicked()
    {
        // Defense in depth: a UserAdmin actor whose scope does not intersect
        // the target's orgs must not be able to remove a role even if the
        // chip's OnClose handler is somehow triggered.
        using var ctx = new TestContext();
        var roleStoreMock = new Mock<IUserRoleStore>();

        ctx.SetupForDabbler(configureServices: services =>
        {
            RegisterCommonServices(services);
            services.RemoveAll<IUserRoleStore>();
            services.AddSingleton(roleStoreMock.Object);

            services.RemoveAll<IAdminUserQueryService>();
            var queryMock = new Mock<IAdminUserQueryService>();
            queryMock.Setup(q => q.GetActorActiveAdminRole()).Returns(BakedInRoles.UserAdmin);
            queryMock.Setup(q => q.GetActorUserId()).Returns("99");
            // UserAdmin in a different org than the target.
            queryMock.Setup(q => q.GetActorAssignedOrganizationIdsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HashSet<int> { 999 });
            services.AddSingleton(queryMock.Object);
        });

        var cut = ctx.RenderComponent<UserRoleAssignmentDetails>(parameters => parameters
            .Add(x => x.User, Target));

        await cut.InvokeAsync(() =>
        {
            var type = cut.Instance.GetType();
            var remove = type.GetMethod("RemoveRoleAsync",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            return (Task)remove.Invoke(cut.Instance, [Target.Roles[0]])!;
        });

        roleStoreMock.Verify(
            s => s.RemoveRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AssignAsync_CallsUserRoleStoreAndRaisesRolesChanged()
    {
        using var ctx = new TestContext();
        var roleStoreMock = new Mock<IUserRoleStore>();
        roleStoreMock.Setup(s => s.AddRoleAsync(Target.UserId, "BackofficeUser", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var raisedFor = string.Empty;
        ctx.SetupForDabbler(configureServices: services =>
        {
            RegisterCommonServices(services);
            services.RemoveAll<IUserRoleStore>();
            services.AddSingleton(roleStoreMock.Object);
        });

        var cut = ctx.RenderComponent<UserRoleAssignmentDetails>(parameters => parameters
            .Add(x => x.User, Target)
            .Add(x => x.RolesChanged, userId => raisedFor = userId));

        // Drive the component through its public surface: set the pending role
        // via reflection (the MudSelect binding target), then invoke AssignAsync
        // directly. Clicking the rendered button is brittle because MudSelect
        // renders its options in a popover that bUnit doesn't realise.
        await cut.InvokeAsync(() =>
        {
            var type = cut.Instance.GetType();
            type.GetField("_pendingRoleName",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(cut.Instance, "BackofficeUser");

            var assign = type.GetMethod("AssignAsync",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            return (Task)assign.Invoke(cut.Instance, null)!;
        });

        roleStoreMock.Verify(s => s.AddRoleAsync(Target.UserId, "BackofficeUser", It.IsAny<CancellationToken>()), Times.Once);
        raisedFor.Should().Be(Target.UserId);
    }

    [Fact]
    public void RemoveRole_CallsUserRoleStoreAndRaisesRolesChanged()
    {
        using var ctx = new TestContext();
        var roleStoreMock = new Mock<IUserRoleStore>();
        roleStoreMock.Setup(s => s.RemoveRoleAsync(Target.UserId, "Customer", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var raisedFor = string.Empty;
        ctx.SetupForDabbler(configureServices: services =>
        {
            RegisterCommonServices(services);
            services.RemoveAll<IUserRoleStore>();
            services.AddSingleton(roleStoreMock.Object);
        });

        var cut = ctx.RenderComponent<UserRoleAssignmentDetails>(parameters => parameters
            .Add(x => x.User, Target)
            .Add(x => x.RolesChanged, userId => raisedFor = userId));

        // Click the close button on the Customer chip. MudChip renders a close
        // icon when OnClose is wired; find it by its role description.
        var chip = cut.Find("[data-testid='user-role-assignment-current-Customer']");
        var closeButton = chip.ParentElement?.QuerySelector("button.mud-chip-close-button")
            ?? chip.QuerySelector("button.mud-chip-close-button");
        closeButton.Should().NotBeNull("the Customer chip should expose a close button when removal is allowed");
        closeButton!.Click();

        roleStoreMock.Verify(s => s.RemoveRoleAsync(Target.UserId, "Customer", It.IsAny<CancellationToken>()), Times.Once);
        raisedFor.Should().Be(Target.UserId);
    }

    // A target with no roles allows baked-in role assignment (no NonUserTypeCombination block).
    private static readonly AdminUserRow TargetNoRoles = new(
        "77", "Target No Roles", "nroles@test.local", true,
        [new AdminUserOrganization(10, "internal", "Internal", true, true)],
        []);

    [Fact]
    public void ExcludeUserAdminAssignment_True_FiltersUserAdminFromDropdown()
    {
        using var ctx = new TestContext();
        ctx.SetupForDabbler(configureServices: RegisterCommonServices);

        var cut = ctx.RenderComponent<UserRoleAssignmentDetails>(parameters => parameters
            .Add(x => x.User, TargetNoRoles)
            .Add(x => x.ExcludeUserAdminAssignment, true));

        var assignable = GetAssignableRoleNames(cut.Instance);
        assignable.Should().NotContain(BakedInRoles.UserAdmin);
        assignable.Should().Contain(BakedInRoles.SystemAdmin);
    }

    [Fact]
    public void ExcludeUserAdminAssignment_False_IncludesUserAdminInDropdown()
    {
        using var ctx = new TestContext();
        ctx.SetupForDabbler(configureServices: RegisterCommonServices);

        var cut = ctx.RenderComponent<UserRoleAssignmentDetails>(parameters => parameters
            .Add(x => x.User, TargetNoRoles)
            .Add(x => x.ExcludeUserAdminAssignment, false));

        var assignable = GetAssignableRoleNames(cut.Instance);
        assignable.Should().Contain(BakedInRoles.UserAdmin);
    }

    [Fact]
    public void ExcludeUserAdminAssignment_True_RendersUserAdminChipAsReadOnly_WhenAlreadyAssigned()
    {
        using var ctx = new TestContext();
        ctx.SetupForDabbler(configureServices: RegisterCommonServices);

        var targetWithUserAdmin = Target with { Roles = [BakedInRoles.UserAdmin, "Customer"] };

        var cut = ctx.RenderComponent<UserRoleAssignmentDetails>(parameters => parameters
            .Add(x => x.User, targetWithUserAdmin)
            .Add(x => x.ExcludeUserAdminAssignment, true));

        var chip = cut.Find("[data-testid='user-role-assignment-current-UserAdmin']");
        chip.Should().NotBeNull();
        var closeButton = chip.ParentElement?.QuerySelector("button.mud-chip-close-button")
            ?? chip.QuerySelector("button.mud-chip-close-button");
        closeButton.Should().BeNull("UserAdmin chip must be read-only when ExcludeUserAdminAssignment=true");
    }

    [Fact]
    public void ExcludeUserAdminAssignment_True_ShowsOrgAdminCrossLinkBanner_WhenUserHasOrgs()
    {
        using var ctx = new TestContext();
        ctx.SetupForDabbler(configureServices: RegisterCommonServices);

        var cut = ctx.RenderComponent<UserRoleAssignmentDetails>(parameters => parameters
            .Add(x => x.User, Target)
            .Add(x => x.ExcludeUserAdminAssignment, true));

        cut.Find("[data-testid='user-role-assignment-manage-org-admins-banner']")
            .Should().NotBeNull();
        cut.Find("[data-testid='user-role-assignment-org-admin-link-10']")
            .Should().NotBeNull();
    }

    [Fact]
    public void NoAssignableRoles_RendersExplanationAlert_InsteadOfEmptyDropdown()
    {
        // UAT-3 finding (admin-users-cross-links Session 1 closeout, 2026-05-11):
        // SystemAdmin selected own row in seeded data — every candidate role failed
        // the authority filter and the dropdown rendered empty without explanation.
        // This test confirms the explanation alert appears instead.
        //
        // We force the empty state with an empty IBakedInRoleProvider + empty
        // UserTypeRoles config so `candidates` is [] before the authority filter
        // runs. The exact mechanism by which candidates becomes empty is
        // immaterial to this test — what matters is the rendered behavior.
        using var ctx = new TestContext();
        var emptyProvider = new Mock<IBakedInRoleProvider>();
        emptyProvider.Setup(p => p.GetBakedInRoles()).Returns([]);
        emptyProvider.Setup(p => p.TryGetBakedInRole(It.IsAny<string>())).Returns((BakedInRoleDefinition?)null);

        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new SecurityConfiguration
        {
            UserTypeRoles = []
        });

        var queryMock = new Mock<IAdminUserQueryService>();
        queryMock.Setup(q => q.GetActorActiveAdminRole()).Returns(BakedInRoles.SystemAdmin);
        queryMock.Setup(q => q.GetActorUserId()).Returns("99");
        queryMock.Setup(q => q.GetActorAssignedOrganizationIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());

        var userRoleStore = new Mock<IUserRoleStore>();

        ctx.SetupForDabbler(configureServices: services =>
        {
            services.AddSingleton(emptyProvider.Object);
            services.AddSingleton(new SecurityConfigurationManager(store.Object));
            services.AddSingleton(userRoleStore.Object);
            services.AddSingleton(queryMock.Object);
        });

        var cut = ctx.RenderComponent<UserRoleAssignmentDetails>(parameters => parameters
            .Add(x => x.User, Target)
            .Add(x => x.ExcludeUserAdminAssignment, true));

        cut.Find("[data-testid='user-role-assignment-no-assignable-roles']")
            .Should().NotBeNull("the no-assignable-roles alert must replace an empty dropdown");
        cut.FindAll("[data-testid='user-role-assignment-role-select']")
            .Should().BeEmpty("the empty dropdown must not render when no roles are assignable");
    }

    [Fact]
    public async Task ExcludeUserAdminAssignment_True_BlocksAssignUserAdminAtActionLevel()
    {
        using var ctx = new TestContext();
        var roleStoreMock = new Mock<IUserRoleStore>();

        ctx.SetupForDabbler(configureServices: services =>
        {
            RegisterCommonServices(services);
            services.RemoveAll<IUserRoleStore>();
            services.AddSingleton(roleStoreMock.Object);
        });

        var cut = ctx.RenderComponent<UserRoleAssignmentDetails>(parameters => parameters
            .Add(x => x.User, Target)
            .Add(x => x.ExcludeUserAdminAssignment, true));

        await cut.InvokeAsync(() =>
        {
            var type = cut.Instance.GetType();
            type.GetField("_pendingRoleName",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(cut.Instance, BakedInRoles.UserAdmin);

            var assign = type.GetMethod("AssignAsync",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            return (Task)assign.Invoke(cut.Instance, null)!;
        });

        roleStoreMock.Verify(
            s => s.AddRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static IReadOnlyList<string> GetAssignableRoleNames(UserRoleAssignmentDetails component)
    {
        var field = typeof(UserRoleAssignmentDetails).GetField(
            "_assignableRoles",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var raw = field.GetValue(component);
        var enumerable = (System.Collections.IEnumerable)raw!;
        return enumerable.Cast<object>()
            .Select(x => (string)x.GetType().GetProperty("RoleName")!.GetValue(x)!)
            .ToList();
    }

    private static void RegisterCommonServices(IServiceCollection services)
    {
        services.AddSingleton<IBakedInRoleProvider, PlatformBakedInRoleProvider>();

        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new SecurityConfiguration
        {
            UserTypeRoles =
            [
                new RoleDefinition { RoleName = "BackofficeUser", DisplayAlias = "Backoffice User" },
                new RoleDefinition { RoleName = "Customer", DisplayAlias = "Customer" }
            ]
        });
        services.AddSingleton(new SecurityConfigurationManager(store.Object));

        var userRoleStore = new Mock<IUserRoleStore>();
        userRoleStore.Setup(s => s.AddRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        userRoleStore.Setup(s => s.RemoveRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        services.AddSingleton(userRoleStore.Object);

        var queryMock = new Mock<IAdminUserQueryService>();
        queryMock.Setup(q => q.GetActorActiveAdminRole()).Returns(BakedInRoles.SystemAdmin);
        queryMock.Setup(q => q.GetActorUserId()).Returns("99");
        queryMock.Setup(q => q.GetActorAssignedOrganizationIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());
        services.AddSingleton(queryMock.Object);
    }
}
