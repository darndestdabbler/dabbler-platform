// These tests cover the obsolete legacy Dabbler.Security members on purpose.
#pragma warning disable CS0618

using AcmeCoffeeOnline.Shared.DTOs;
using AcmeCoffeeOnline.Web.Components.Shared;
using Bunit;
using Bunit.Rendering;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Configuration;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using MudBlazor.Services;

namespace AcmeCoffeeOnline.Web.Tests.Components.Shared;

public sealed class UserContextBarTests : BunitContext, IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    public UserContextBarTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddScoped<IRoleSwitcher, SessionRoleSwitcher>();
    }

    [Fact]
    public void SystemAdmin_WithMultipleOrgs_EnablesAllThreeLinks()
    {
        RegisterSecurityConfig(new RoleSwitchingPermission
        {
            AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin"]
        });

        var cut = RenderBar(
            userName: "Casey Hart",
            userRoles: ["SystemAdmin"],
            activeOrgName: "Internal Operations",
            organizationOptions: TwoOrgs(activeName: "Internal Operations"));

        AssertLinkEnabled(cut, "user-context-user", expectedText: "Casey Hart");
        AssertLinkEnabled(cut, "user-context-role", expectedText: "System Admin");
        AssertLinkEnabled(cut, "active-organization-label", expectedText: "Internal Operations");
    }

    [Fact]
    public void CustomerWithSingleOrg_DisablesRoleAndOrgLinks()
    {
        RegisterSecurityConfig(new RoleSwitchingPermission
        {
            AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin"]
        });

        var cut = RenderBar(
            userName: "Jamie Retail",
            userRoles: ["Customer"],
            activeOrgName: "Jamie's Coffee Shop",
            organizationOptions: SingleOrg("jamies", "Jamie's Coffee Shop"));

        AssertLinkEnabled(cut, "user-context-user", expectedText: "Jamie Retail");
        AssertLinkDisabled(cut, "user-context-role", expectedText: "Customer");
        AssertLinkDisabled(cut, "active-organization-label", expectedText: "Jamie's Coffee Shop");
    }

    [Fact]
    public void BackofficeUserAdmin_EnablesRoleLinkEvenWithSingleOrg()
    {
        RegisterSecurityConfig(new RoleSwitchingPermission
        {
            AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin"]
        });

        var cut = RenderBar(
            userName: "Robin Manager",
            userRoles: ["BackofficeUserAdmin"],
            activeOrgName: "Internal Operations",
            organizationOptions: SingleOrg("internal", "Internal Operations"));

        AssertLinkEnabled(cut, "user-context-role", expectedText: "Backoffice User Admin");
        AssertLinkDisabled(cut, "active-organization-label", expectedText: "Internal Operations");
    }

    [Fact]
    public void BackofficeUser_WithMultipleOrgs_EnablesOrgButNotRole()
    {
        RegisterSecurityConfig(new RoleSwitchingPermission
        {
            AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin"]
        });

        var cut = RenderBar(
            userName: "Sam Agent",
            userRoles: ["BackofficeUser"],
            activeOrgName: "Retail Alpha",
            organizationOptions: TwoOrgs(activeName: "Retail Alpha"));

        AssertLinkDisabled(cut, "user-context-role", expectedText: "Backoffice User");
        AssertLinkEnabled(cut, "active-organization-label", expectedText: "Retail Alpha");
    }

    [Fact]
    public void RendersSentenceStructureWithAsAndForConnectors()
    {
        RegisterSecurityConfig(new RoleSwitchingPermission { AllowedRoles = [] });

        var cut = RenderBar(
            userName: "Pat",
            userRoles: ["Customer"],
            activeOrgName: "Local Cafe",
            organizationOptions: SingleOrg("local", "Local Cafe"));

        var text = cut.Find("[data-testid='user-context-bar']").TextContent;
        Assert.Contains("Pat", text);
        Assert.Contains("as", text);
        Assert.Contains("Customer", text);
        Assert.Contains("for", text);
        Assert.Contains("Local Cafe", text);
    }

    [Fact]
    public void ExposesAllThreeDataTestIds()
    {
        RegisterSecurityConfig(new RoleSwitchingPermission { AllowedRoles = [] });

        var cut = RenderBar(
            userName: "Alex",
            userRoles: ["Customer"],
            activeOrgName: "Org X",
            organizationOptions: SingleOrg("x", "Org X"));

        Assert.NotNull(cut.Find("[data-testid='user-context-bar']"));
        Assert.NotNull(cut.Find("[data-testid='user-context-user']"));
        Assert.NotNull(cut.Find("[data-testid='user-context-role']"));
        Assert.NotNull(cut.Find("[data-testid='active-organization-label']"));
    }

    [Fact]
    public void ClickingUser_OpensSignOutConfirmationDialog()
    {
        RegisterSecurityConfig(new RoleSwitchingPermission { AllowedRoles = [] });

        var cut = RenderBar(
            userName: "Casey Hart",
            userRoles: ["SystemAdmin"],
            activeOrgName: "Internal Operations",
            organizationOptions: SingleOrg("internal", "Internal Operations"));

        cut.Find("[data-testid='user-context-user']").Click();

        cut.WaitForAssertion(() =>
            Assert.NotNull(cut.Find("[data-testid='signout-confirmation-dialog']")));
        Assert.NotNull(cut.Find("[data-testid='signout-cancel']"));
        Assert.NotNull(cut.Find("[data-testid='signout-confirm']"));
    }

    [Fact]
    public void ConfirmingSignOut_NavigatesToLogoutWithForceLoad()
    {
        RegisterSecurityConfig(new RoleSwitchingPermission { AllowedRoles = [] });

        var cut = RenderBar(
            userName: "Casey Hart",
            userRoles: ["SystemAdmin"],
            activeOrgName: "Internal Operations",
            organizationOptions: SingleOrg("internal", "Internal Operations"));

        var nav = Services.GetRequiredService<NavigationManager>();

        cut.Find("[data-testid='user-context-user']").Click();
        cut.WaitForAssertion(() =>
            Assert.NotNull(cut.Find("[data-testid='signout-confirmation-dialog']")));

        cut.Find("[data-testid='signout-confirm']").Click();

        cut.WaitForAssertion(() =>
            Assert.EndsWith("/auth/logout", nav.Uri, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Bar_HasGroupRoleAndAriaLabel_ForScreenReaders()
    {
        RegisterSecurityConfig(new RoleSwitchingPermission { AllowedRoles = [] });

        var cut = RenderBar(
            userName: "Alex",
            userRoles: ["Customer"],
            activeOrgName: "Org X",
            organizationOptions: SingleOrg("x", "Org X"));

        var bar = cut.Find("[data-testid='user-context-bar']");
        Assert.Equal("group", bar.GetAttribute("role"));
        Assert.Equal("User context", bar.GetAttribute("aria-label"));
    }

    [Fact]
    public void ConnectorWords_AreAriaHidden()
    {
        RegisterSecurityConfig(new RoleSwitchingPermission { AllowedRoles = [] });

        var cut = RenderBar(
            userName: "Alex",
            userRoles: ["Customer"],
            activeOrgName: "Org X",
            organizationOptions: SingleOrg("x", "Org X"));

        var connectors = cut.FindAll("[data-testid='user-context-bar'] > span[aria-hidden='true']");
        var texts = connectors.Select(s => s.TextContent.Trim()).ToList();
        Assert.Contains("as", texts);
        Assert.Contains("for", texts);
    }

    [Fact]
    public void Links_ExposeSpecSentenceAriaLabels()
    {
        RegisterSecurityConfig(new RoleSwitchingPermission
        {
            AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin"]
        });

        var cut = RenderBar(
            userName: "Casey Hart",
            userRoles: ["SystemAdmin"],
            activeOrgName: "Internal Operations",
            organizationOptions: TwoOrgs(activeName: "Internal Operations"));

        Assert.Equal(
            "Casey Hart, sign out",
            cut.Find("[data-testid='user-context-user']").GetAttribute("aria-label"));
        Assert.Equal(
            "System Admin, switch role",
            cut.Find("[data-testid='user-context-role']").GetAttribute("aria-label"));
        Assert.Equal(
            "Internal Operations, switch organization",
            cut.Find("[data-testid='active-organization-label']").GetAttribute("aria-label"));
    }

    [Fact]
    public void DisabledLinks_HaveTabindexMinusOne_AndSkipMenuAttributes()
    {
        RegisterSecurityConfig(new RoleSwitchingPermission { AllowedRoles = [] });

        var cut = RenderBar(
            userName: "Jamie Retail",
            userRoles: ["Customer"],
            activeOrgName: "Jamie's Coffee Shop",
            organizationOptions: SingleOrg("jamies", "Jamie's Coffee Shop"));

        var roleLink = cut.Find("[data-testid='user-context-role']");
        Assert.Equal("-1", roleLink.GetAttribute("tabindex"));
        Assert.False(roleLink.HasAttribute("aria-haspopup"),
            "Disabled role link should not advertise a popup.");

        var orgLink = cut.Find("[data-testid='active-organization-label']");
        Assert.Equal("-1", orgLink.GetAttribute("tabindex"));
        Assert.False(orgLink.HasAttribute("aria-haspopup"),
            "Disabled org link should not advertise a popup.");
    }

    [Fact]
    public void EnabledMenuLinks_AdvertisePopup()
    {
        RegisterSecurityConfig(new RoleSwitchingPermission
        {
            AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin"]
        });

        var cut = RenderBar(
            userName: "Casey Hart",
            userRoles: ["SystemAdmin"],
            activeOrgName: "Internal Operations",
            organizationOptions: TwoOrgs(activeName: "Internal Operations"));

        Assert.Equal("menu",
            cut.Find("[data-testid='user-context-role']").GetAttribute("aria-haspopup"));
        Assert.Equal("menu",
            cut.Find("[data-testid='active-organization-label']").GetAttribute("aria-haspopup"));
    }

    [Fact]
    public void Links_CarryCssHookClasses_ForStyling()
    {
        RegisterSecurityConfig(new RoleSwitchingPermission
        {
            AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin"]
        });

        var cut = RenderBar(
            userName: "Casey Hart",
            userRoles: ["SystemAdmin"],
            activeOrgName: "Internal Operations",
            organizationOptions: TwoOrgs(activeName: "Internal Operations"));

        var userLink = cut.Find("[data-testid='user-context-user']");
        Assert.Contains("user-context-name", userLink.GetAttribute("class") ?? string.Empty);

        var roleLink = cut.Find("[data-testid='user-context-role']");
        Assert.Contains("user-context-role", roleLink.GetAttribute("class") ?? string.Empty);

        var orgLink = cut.Find("[data-testid='active-organization-label']");
        Assert.Contains("user-context-org", orgLink.GetAttribute("class") ?? string.Empty);
    }

    [Fact]
    public void EnabledLinks_UseAlwaysUnderlineVariant()
    {
        RegisterSecurityConfig(new RoleSwitchingPermission
        {
            AllowedRoles = ["SystemAdmin", "BackofficeUserAdmin"]
        });

        var cut = RenderBar(
            userName: "Casey Hart",
            userRoles: ["SystemAdmin"],
            activeOrgName: "Internal Operations",
            organizationOptions: TwoOrgs(activeName: "Internal Operations"));

        // MudLink with Underline.Always renders class "mud-link-underline-always"
        foreach (var testId in new[] { "user-context-user", "user-context-role", "active-organization-label" })
        {
            var link = cut.Find($"[data-testid='{testId}']");
            var classes = link.GetAttribute("class") ?? string.Empty;
            Assert.Contains("mud-link-underline-always", classes);
        }
    }

    [Fact]
    public void DisabledLinks_UseNoneUnderlineVariant()
    {
        RegisterSecurityConfig(new RoleSwitchingPermission { AllowedRoles = [] });

        var cut = RenderBar(
            userName: "Jamie Retail",
            userRoles: ["Customer"],
            activeOrgName: "Jamie's Coffee Shop",
            organizationOptions: SingleOrg("jamies", "Jamie's Coffee Shop"));

        foreach (var testId in new[] { "user-context-role", "active-organization-label" })
        {
            var link = cut.Find($"[data-testid='{testId}']");
            var classes = link.GetAttribute("class") ?? string.Empty;
            Assert.Contains("mud-link-underline-none", classes);
        }
    }

    [Fact]
    public void CancelingSignOut_DoesNotNavigate()
    {
        RegisterSecurityConfig(new RoleSwitchingPermission { AllowedRoles = [] });

        var cut = RenderBar(
            userName: "Casey Hart",
            userRoles: ["SystemAdmin"],
            activeOrgName: "Internal Operations",
            organizationOptions: SingleOrg("internal", "Internal Operations"));

        var nav = Services.GetRequiredService<NavigationManager>();
        var initialUri = nav.Uri;

        cut.Find("[data-testid='user-context-user']").Click();
        cut.WaitForAssertion(() =>
            Assert.NotNull(cut.Find("[data-testid='signout-confirmation-dialog']")));

        cut.Find("[data-testid='signout-cancel']").Click();

        Assert.Equal(initialUri, nav.Uri);
    }

    private IRenderedComponent<ContainerFragment> RenderBar(
        string userName,
        IReadOnlyList<string> userRoles,
        string activeOrgName,
        IReadOnlyList<OrganizationOptionDto> organizationOptions)
    {
        return Render(builder =>
        {
            builder.OpenComponent<MudThemeProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<MudPopoverProvider>(1);
            builder.CloseComponent();
            builder.OpenComponent<MudDialogProvider>(2);
            builder.CloseComponent();
            builder.OpenComponent<MudSnackbarProvider>(3);
            builder.CloseComponent();

            builder.OpenComponent<UserContextBar>(4);
            builder.AddComponentParameter(5, nameof(UserContextBar.UserName), userName);
            builder.AddComponentParameter(6, nameof(UserContextBar.UserRoles), userRoles);
            builder.AddComponentParameter(7, nameof(UserContextBar.ActiveOrganizationName), activeOrgName);
            builder.AddComponentParameter(8, nameof(UserContextBar.OrganizationOptions), organizationOptions);
            builder.CloseComponent();
        });
    }

    private void RegisterSecurityConfig(RoleSwitchingPermission roleSwitching)
    {
        var store = new StubSecurityConfigStore(new SecurityConfiguration
        {
            RoleSwitching = roleSwitching
        });
        var manager = new SecurityConfigurationManager(store);
        manager.EnsureLoadedAsync().GetAwaiter().GetResult();
        Services.AddSingleton(manager);
        Services.AddSingleton<AuthorizationStateNotifier>(sp =>
            new AuthorizationStateNotifier(manager, NullLogger<AuthorizationStateNotifier>.Instance));
    }

    private static IReadOnlyList<OrganizationOptionDto> SingleOrg(string key, string name)
        => [new OrganizationOptionDto
        {
            OrganizationId = 1,
            OrganizationKey = key,
            OrganizationName = name,
            IsActive = true
        }];

    private static IReadOnlyList<OrganizationOptionDto> TwoOrgs(string activeName)
        =>
        [
            new OrganizationOptionDto
            {
                OrganizationId = 1,
                OrganizationKey = "internal",
                OrganizationName = activeName,
                IsActive = true
            },
            new OrganizationOptionDto
            {
                OrganizationId = 2,
                OrganizationKey = "retail-alpha",
                OrganizationName = "Retail Alpha",
                IsActive = false
            }
        ];

    private static void AssertLinkEnabled(IRenderedComponent<ContainerFragment> cut, string testId, string expectedText)
    {
        var link = cut.Find($"[data-testid='{testId}']");
        Assert.Contains(expectedText, link.TextContent);
        Assert.False(
            link.HasAttribute("aria-disabled") && link.GetAttribute("aria-disabled") == "true",
            $"Expected link '{testId}' to be enabled, but aria-disabled=true");
    }

    private static void AssertLinkDisabled(IRenderedComponent<ContainerFragment> cut, string testId, string expectedText)
    {
        var link = cut.Find($"[data-testid='{testId}']");
        Assert.Contains(expectedText, link.TextContent);
        Assert.True(
            link.HasAttribute("aria-disabled") && link.GetAttribute("aria-disabled") == "true",
            $"Expected link '{testId}' to be disabled (aria-disabled=true)");
    }

    private sealed class StubSecurityConfigStore : ISecurityConfigurationStore
    {
        private SecurityConfiguration _configuration;

        public StubSecurityConfigStore(SecurityConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task<SecurityConfiguration> LoadAsync(CancellationToken ct = default)
            => Task.FromResult(_configuration);

        public Task SaveAsync(SecurityConfiguration configuration, CancellationToken ct = default)
        {
            _configuration = configuration;
            return Task.CompletedTask;
        }
    }
}
