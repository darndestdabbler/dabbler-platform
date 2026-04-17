using AcmeCoffeeOnline.Shared.DTOs;
using Dabbler.Security.Configuration;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace AcmeCoffeeOnline.Web.Components.Shared;

public partial class UserContextBar : ComponentBase, IDisposable
{
    private static readonly IReadOnlyList<string> RolePrecedence =
    [
        "SystemAdmin",
        "BackofficeUserAdmin",
        "BackofficeUser",
        "Customer"
    ];

    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IRoleSwitcher RoleSwitcher { get; set; } = default!;
    [Inject] private SecurityConfigurationManager ConfigManager { get; set; } = default!;
    [Inject] private AuthorizationStateNotifier Notifier { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    [Parameter, EditorRequired] public string UserName { get; set; } = "User";

    [Parameter] public IReadOnlyList<string> UserRoles { get; set; } = [];

    [Parameter] public string? ActiveOrganizationName { get; set; }

    [Parameter] public IReadOnlyList<OrganizationOptionDto> OrganizationOptions { get; set; } = [];

    private MudMenu? _roleMenu;
    private MudMenu? _orgMenu;
    private bool _subscribed;

    private string _userName = "User";
    private string _activeOrganizationName = string.Empty;
    private List<string> _allowedSwitchRoles = [];
    private bool _canSwitchRole;
    private bool _canSwitchOrg;
    private string? _switchedRoleOverride;

    private string _activeRoleDisplay =>
        _switchedRoleOverride is not null
            ? FormatRoleName(_switchedRoleOverride)
            : FormatRoleName(ResolvePrimaryRole());

    private Dictionary<string, object?> _userLinkAttributes => new()
    {
        ["data-testid"] = "user-context-user",
        ["role"] = "button",
        ["aria-label"] = $"{_userName}, sign out"
    };

    private Dictionary<string, object?> _roleLinkAttributes => new()
    {
        ["data-testid"] = "user-context-role",
        ["role"] = _canSwitchRole ? "button" : null,
        ["aria-disabled"] = _canSwitchRole ? null : "true",
        ["aria-label"] = _canSwitchRole
            ? $"{_activeRoleDisplay}, switch role"
            : _activeRoleDisplay
    };

    private Dictionary<string, object?> _orgLinkAttributes => new()
    {
        ["data-testid"] = "active-organization-label",
        ["role"] = _canSwitchOrg ? "button" : null,
        ["aria-disabled"] = _canSwitchOrg ? null : "true",
        ["aria-label"] = _canSwitchOrg
            ? $"{_activeOrganizationName}, switch organization"
            : _activeOrganizationName
    };

    protected override async Task OnInitializedAsync()
    {
        await ConfigManager.EnsureLoadedAsync();
        RefreshFromParameters();
        Notifier.Subscribe(OnConfigChangedAsync);
        _subscribed = true;
    }

    protected override void OnParametersSet()
    {
        RefreshFromParameters();
    }

    private void RefreshFromParameters()
    {
        _userName = string.IsNullOrWhiteSpace(UserName) ? "User" : UserName;
        _activeOrganizationName = string.IsNullOrWhiteSpace(ActiveOrganizationName)
            ? string.Empty
            : ActiveOrganizationName!;

        _allowedSwitchRoles = ConfigManager.Current.RoleSwitching?.AllowedRoles ?? [];
        var rolesSet = new HashSet<string>(UserRoles, StringComparer.OrdinalIgnoreCase);
        _canSwitchRole = _allowedSwitchRoles.Any(rolesSet.Contains);
        _canSwitchOrg = OrganizationOptions.Count >= 2;
    }

    private string ResolvePrimaryRole()
    {
        if (UserRoles.Count == 0)
            return string.Empty;

        foreach (var role in RolePrecedence)
        {
            if (UserRoles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase)))
                return role;
        }

        return UserRoles[0];
    }

    private static string FormatRoleName(string role)
    {
        if (string.IsNullOrEmpty(role))
            return string.Empty;

        var builder = new System.Text.StringBuilder(role.Length + 4);
        builder.Append(role[0]);
        for (var i = 1; i < role.Length; i++)
        {
            var c = role[i];
            if (char.IsUpper(c) && !char.IsUpper(role[i - 1]))
                builder.Append(' ');
            builder.Append(c);
        }
        return builder.ToString();
    }

    private async Task OpenSignOutDialogAsync()
    {
        var reference = await DialogService.ShowAsync<SignOutConfirmationDialog>("Sign out");
        var result = await reference.Result;
        if (result is not null && !result.Canceled && result.Data is true)
        {
            NavigationManager.NavigateTo("/auth/logout", forceLoad: true);
        }
    }

    private async Task ApplyRoleSwitchAsync(string role)
    {
        RoleSwitcher.SwitchToRole(role);
        _switchedRoleOverride = role;
        Snackbar.Add($"Now viewing as '{FormatRoleName(role)}'.", Severity.Warning);
        if (_roleMenu is not null)
            await _roleMenu.CloseMenuAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task ClearRoleSwitchAsync()
    {
        RoleSwitcher.ClearSwitch();
        _switchedRoleOverride = null;
        Snackbar.Add("Role switch cleared. Viewing your natural roles.", Severity.Info);
        if (_roleMenu is not null)
            await _roleMenu.CloseMenuAsync();
        await InvokeAsync(StateHasChanged);
    }

    private void SwitchOrganization(int organizationId)
    {
        var relativePath = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        var returnUrl = string.IsNullOrWhiteSpace(relativePath)
            ? "/"
            : "/" + relativePath;

        NavigationManager.NavigateTo(
            $"/auth/switch-organization?organizationId={organizationId}&returnUrl={Uri.EscapeDataString(returnUrl)}",
            forceLoad: true);
    }

    private async Task OnConfigChangedAsync(AuthorizationConfigurationChanged _)
    {
        RefreshFromParameters();
        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        if (_subscribed)
            Notifier.Unsubscribe(OnConfigChangedAsync);
    }
}
