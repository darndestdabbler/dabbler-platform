using System.Security.Claims;
using Bunit;
using Bunit.TestDoubles;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Authorization;
using Dabbler.Security.Blazor.Components.Admin.Users;
using Dabbler.Security.Configuration;
using Dabbler.Security.Models;
using Dabbler.Security.Options;
using Dabbler.Security.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;

namespace Dabbler.Security.Blazor.Tests.Components.Admin.Users;

/// <summary>
/// bUnit tests for <see cref="CreateBackofficeUserDialog"/>.
///
/// MudBlazor dialog content is rendered inside MudDialogProvider, not inside
/// the MudDialog component itself, so these tests capture the provider ref and
/// search within it after calling IDialogService.ShowAsync.
/// </summary>
public class CreateBackofficeUserDialogTests
{
    private static readonly AdminUserOrganization InternalOrg =
        new(10, "internal", "Internal Org", false, true);

    private static readonly AdminUserOrganization ExternalOrg =
        new(20, "acme-corp", "Acme Corp", false, false);

    private static readonly AdminUserRow CreatedUser = new(
        "99", "New User", "newuser@test.local", true,
        [InternalOrg],
        [BakedInRoles.User]);

    [Fact]
    public async Task EmptyRender_ShowsAllFormFields()
    {
        using var ctx = new TestContext();
        var provider = SetupTestContext(ctx, BakedInRoles.SystemAdmin, "1",
            [InternalOrg, ExternalOrg]);

        await OpenDialogAsync(ctx, provider);

        provider.Find("[data-testid='create-user-display-name']").Should().NotBeNull();
        provider.Find("[data-testid='create-user-email']").Should().NotBeNull();
        provider.Find("[data-testid='create-user-role-select']").Should().NotBeNull();
        provider.Find("[data-testid='create-user-org-select']").Should().NotBeNull();
        provider.Find("[data-testid='create-user-submit-button']").Should().NotBeNull();
        provider.Find("[data-testid='create-user-cancel-button']").Should().NotBeNull();
    }

    [Fact]
    public async Task SubmitButton_DisabledWhileFormInvalid()
    {
        using var ctx = new TestContext();
        var provider = SetupTestContext(ctx, BakedInRoles.SystemAdmin, "1", [InternalOrg]);

        await OpenDialogAsync(ctx, provider);

        // Immediately after render, form not yet validated — submit is disabled.
        var button = provider.Find("[data-testid='create-user-submit-button']");
        button.HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public async Task AuthFilter_ExcludesRolesGranterCannotAssign()
    {
        using var ctx = new TestContext();
        // UserAdmin (org 10 only) — cannot assign SystemAdmin or CrossOrgUserAdmin.
        var provider = SetupTestContext(ctx, BakedInRoles.UserAdmin, "5",
            [InternalOrg], actorOrgIds: new HashSet<int> { 10 });

        await OpenDialogAsync(ctx, provider);

        var dialogInstance = provider.FindComponent<CreateBackofficeUserDialog>();
        var assignableRoles = GetAssignableRoleNames(dialogInstance.Instance);

        assignableRoles.Should().NotContain(BakedInRoles.SystemAdmin);
        assignableRoles.Should().NotContain(BakedInRoles.CrossOrgUserAdmin);
    }

    [Fact]
    public async Task SuccessfulSubmit_CallsServiceOnce_AndRaisesOnCreated()
    {
        using var ctx = new TestContext();
        var creationMock = new Mock<IBackofficeUserCreationService>();
        creationMock
            .Setup(s => s.CreateAsync(
                It.IsAny<CreateBackofficeUserRequest>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BackofficeUserCreationResult(CreatedUser, MagicLinkSent: true));

        AdminUserRow? receivedRow = null;
        var provider = SetupTestContext(ctx, BakedInRoles.SystemAdmin, "1",
            [InternalOrg], overrideCreationService: creationMock.Object);

        await OpenDialogAsync(ctx, provider, onCreated: row => receivedRow = row);

        var dialogInstance = provider.FindComponent<CreateBackofficeUserDialog>();

        // Set private fields directly (same pattern as AssignAsync tests).
        await provider.InvokeAsync(() =>
        {
            var type = dialogInstance.Instance.GetType();
            SetField(type, dialogInstance.Instance, "_displayName", "New User");
            SetField(type, dialogInstance.Instance, "_email", "newuser@test.local");
            SetField(type, dialogInstance.Instance, "_initialRoleName", BakedInRoles.User);
            SetField(type, dialogInstance.Instance, "_initialOrganizationId", 10);
            SetField(type, dialogInstance.Instance, "_isFormValid", true);
        });

        await provider.InvokeAsync(async () =>
        {
            var type = dialogInstance.Instance.GetType();
            var submit = type.GetMethod("SubmitAsync",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            await (Task)submit.Invoke(dialogInstance.Instance, null)!;
        });

        creationMock.Verify(
            s => s.CreateAsync(
                It.IsAny<CreateBackofficeUserRequest>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        receivedRow.Should().NotBeNull();
        receivedRow!.UserId.Should().Be(CreatedUser.UserId);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Sets up a bUnit TestContext with MudBlazor services and returns the
    /// MudDialogProvider rendered component so tests can search dialog content.
    /// The provider is the correct search root because MudBlazor renders dialog
    /// content inside the provider, not inside the MudDialog component itself.
    /// </summary>
    private static IRenderedComponent<MudDialogProvider> SetupTestContext(
        TestContext ctx,
        string actorRole,
        string actorUserId,
        IReadOnlyList<AdminUserOrganization> grantableOrgs,
        IReadOnlySet<int>? actorOrgIds = null,
        IBackofficeUserCreationService? overrideCreationService = null)
    {
        var resolvedOrgIds = actorOrgIds
            ?? grantableOrgs.Select(o => o.OrganizationId).ToHashSet();

        var authCtx = ctx.AddTestAuthorization();
        authCtx.SetAuthorized("testuser@test.local");
        authCtx.SetRoles([actorRole]);

        ctx.Services.AddMudServices(cfg =>
        {
            cfg.SnackbarConfiguration.ShowTransitionDuration = 0;
            cfg.SnackbarConfiguration.HideTransitionDuration = 0;
        });

        ctx.Services.AddScoped<IRoleSwitcher, SessionRoleSwitcher>();
        ctx.Services.AddSingleton<AuthorizationStateNotifier>();
        ctx.Services.AddSingleton<IFieldMaskService, FieldMaskService>();
        ctx.Services.AddSingleton<ISensitiveAccessAuditor>(
            new TestHelpers.NullSensitiveAccessAuditor());
        ctx.Services.Configure<DabblerSecurityOptions>(_ => { });

        // Required by CreateBackofficeUserDialog (injects IBakedInRoleProvider and SecurityConfigurationManager).
        ctx.Services.AddSingleton<IBakedInRoleProvider, PlatformBakedInRoleProvider>();
        var configStore = new Mock<ISecurityConfigurationStore>();
        configStore.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SecurityConfiguration
            {
                UserTypeRoles =
                [
                    new RoleDefinition { RoleName = "BackofficeUser", DisplayAlias = "Backoffice User" },
                    new RoleDefinition { RoleName = "Customer", DisplayAlias = "Customer" }
                ]
            });
        ctx.Services.AddSingleton(new SecurityConfigurationManager(configStore.Object));
        var userRoleStore = new Mock<IUserRoleStore>();
        ctx.Services.AddSingleton(userRoleStore.Object);

        var queryMock = BuildQueryMock(actorRole, actorUserId, resolvedOrgIds, grantableOrgs);
        ctx.Services.AddSingleton(queryMock.Object);

        var creationSvc = overrideCreationService ?? BuildDefaultCreationService();
        ctx.Services.AddSingleton(creationSvc);

        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        ctx.RenderComponent<MudPopoverProvider>();
        var dialogProvider = ctx.RenderComponent<MudDialogProvider>();
        ctx.RenderComponent<MudSnackbarProvider>();

        return dialogProvider;
    }

    private static async Task OpenDialogAsync(
        TestContext ctx,
        IRenderedComponent<MudDialogProvider> provider,
        Action<AdminUserRow>? onCreated = null)
    {
        var dialogService = ctx.Services.GetRequiredService<IDialogService>();

        if (onCreated is not null)
        {
            var parameters = new DialogParameters<CreateBackofficeUserDialog>();
            parameters.Add(d => d.OnCreated,
                Microsoft.AspNetCore.Components.EventCallback.Factory.Create<AdminUserRow>(
                    new object(), onCreated));
            await provider.InvokeAsync(() =>
                dialogService.ShowAsync<CreateBackofficeUserDialog>("Add User", parameters));
        }
        else
        {
            await provider.InvokeAsync(() =>
                dialogService.ShowAsync<CreateBackofficeUserDialog>("Add User"));
        }

        provider.WaitForElement("[data-testid='create-user-submit-button']",
            TimeSpan.FromSeconds(3));
    }

    private static Mock<IAdminUserQueryService> BuildQueryMock(
        string actorRole,
        string actorUserId,
        IReadOnlySet<int> actorOrgIds,
        IReadOnlyList<AdminUserOrganization> grantableOrgs)
    {
        var mock = new Mock<IAdminUserQueryService>();
        mock.Setup(q => q.GetActorActiveAdminRole()).Returns(actorRole);
        mock.Setup(q => q.GetActorUserId()).Returns(actorUserId);
        mock.Setup(q => q.GetActorAssignedOrganizationIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(actorOrgIds);
        mock.Setup(q => q.GetActorActiveAdminRole(It.IsAny<ClaimsPrincipal>()))
            .Returns(actorRole);
        mock.Setup(q => q.GetActorUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(actorUserId);
        mock.Setup(q => q.GetActorAssignedOrganizationIdsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(actorOrgIds);
        mock.Setup(q => q.GetGrantableOrganizationsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(grantableOrgs);
        return mock;
    }

    private static IBackofficeUserCreationService BuildDefaultCreationService()
    {
        var mock = new Mock<IBackofficeUserCreationService>();
        mock.Setup(s => s.CreateAsync(
                It.IsAny<CreateBackofficeUserRequest>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BackofficeUserCreationResult(CreatedUser, MagicLinkSent: true));
        return mock.Object;
    }

    private static IReadOnlyList<string> GetAssignableRoleNames(
        CreateBackofficeUserDialog instance)
    {
        var field = instance.GetType()
            .GetField("_assignableRoles",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var list = field?.GetValue(instance) as System.Collections.IEnumerable;
        if (list is null) return [];

        return list
            .Cast<object>()
            .Select(item =>
            {
                var prop = item.GetType().GetProperty("RoleName");
                return prop?.GetValue(item)?.ToString() ?? string.Empty;
            })
            .ToList();
    }

    private static void SetField(Type type, object instance, string fieldName, object? value)
    {
        var field = type.GetField(fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Field '{fieldName}' not found on {type.Name}");
        field.SetValue(instance, value);
    }
}
