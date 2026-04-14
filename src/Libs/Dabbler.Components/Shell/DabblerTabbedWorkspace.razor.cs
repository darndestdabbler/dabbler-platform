// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Dabbler.Components;

/// <summary>
/// A tabbed workspace that manages opening, closing, activating, and rendering
/// <see cref="WorkspaceTab"/> instances. Enforces no-duplicate-record constraints,
/// dirty-close confirmation, max-tab limits, and provides tab overflow handling.
/// </summary>
public partial class DabblerTabbedWorkspace : DabblerComponentBase
{
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
    private bool _showOverflowMenu;
    private bool _showDirtyCloseDialog;
    private WorkspaceTab? _pendingCloseTab;
    private bool _showAllDirtyWarning;
    private string? _dragTabId;
    private string? _dragOverTabId;
    private bool _preventTabBarDefault;

    // ── Parameters ────────────────────────────────────────────────────

    /// <summary>
    /// Current open tabs.
    /// </summary>
    [Parameter]
    public IReadOnlyList<WorkspaceTab> Tabs { get; set; } = [];

    /// <summary>
    /// Currently active tab ID.
    /// </summary>
    [Parameter]
    public string? ActiveTabId { get; set; }

    /// <summary>
    /// Two-way binding callback for <see cref="ActiveTabId"/>.
    /// </summary>
    [Parameter]
    public EventCallback<string?> ActiveTabIdChanged { get; set; }

    /// <summary>
    /// Maximum number of open tabs. When exceeded, the oldest non-dirty tab is auto-closed.
    /// </summary>
    [Parameter]
    public int MaxTabs { get; set; } = 15;

    /// <summary>
    /// Raised when a tab close is requested (after dirty-check, if applicable).
    /// </summary>
    [Parameter]
    public EventCallback<WorkspaceTab> OnTabClose { get; set; }

    /// <summary>
    /// Raised on drag-and-drop tab reorder.
    /// </summary>
    [Parameter]
    public EventCallback<(int OldIndex, int NewIndex)> OnTabReorder { get; set; }

    /// <summary>
    /// Raised after the user confirms closing a dirty tab.
    /// </summary>
    [Parameter]
    public EventCallback<WorkspaceTab> OnDirtyTabCloseConfirmed { get; set; }

    /// <summary>
    /// Raised when a new tab should be opened. The parent is responsible for
    /// adding the tab to the <see cref="Tabs"/> collection and setting <see cref="ActiveTabId"/>.
    /// </summary>
    [Parameter]
    public EventCallback<WorkspaceTab> OnTabOpen { get; set; }

    /// <summary>
    /// Content shown when no tabs are open.
    /// </summary>
    [Parameter]
    public RenderFragment? EmptyStateTemplate { get; set; }

    // ── Public members ────────────────────────────────────────────────

    /// <summary>
    /// Opens a new tab, or activates an existing tab if a Detail tab for
    /// the same entity/record already exists.
    /// </summary>
    public async Task OpenTabAsync(WorkspaceTab tab)
    {
        // No duplicate records: check for existing Detail tab with same entity + key
        if (tab.TabType == WorkspaceTabType.Detail
            && tab.EntityName is not null
            && tab.RecordKey is not null)
        {
            var existing = FindTabByRecord(tab.EntityName, tab.RecordKey);
            if (existing is not null)
            {
                ActivateTab(existing.Id);
                return;
            }
        }

        // Max-tab enforcement
        if (Tabs.Count >= MaxTabs)
        {
            var oldest = Tabs
                .Where(t => !t.IsDirty)
                .OrderBy(t => t.CreatedAt)
                .FirstOrDefault();

            if (oldest is not null)
            {
                await OnTabClose.InvokeAsync(oldest);
            }
            else
            {
                // All tabs are dirty — show warning
                _showAllDirtyWarning = true;
                StateHasChanged();
                return;
            }
        }

        // Notify the parent to add the tab and activate it.
        await OnTabOpen.InvokeAsync(tab);
    }

    /// <summary>
    /// Closes a tab. If the tab is dirty, shows a confirmation dialog.
    /// </summary>
    public async Task CloseTabAsync(string tabId)
    {
        var tab = Tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab is null) return;

        if (tab.IsDirty)
        {
            _pendingCloseTab = tab;
            _showDirtyCloseDialog = true;
            StateHasChanged();
            return;
        }

        await ExecuteCloseAsync(tab);
    }

    /// <summary>
    /// Switches to the specified tab.
    /// </summary>
    public void ActivateTab(string tabId)
    {
        if (Tabs.Any(t => t.Id == tabId))
        {
            _ = SetActiveTabAsync(tabId);
        }
    }

    /// <summary>
    /// Finds an open Detail tab for a specific entity/record.
    /// </summary>
    public WorkspaceTab? FindTabByRecord(string entityName, object recordKey)
    {
        return Tabs.FirstOrDefault(t =>
            t.TabType == WorkspaceTabType.Detail
            && string.Equals(t.EntityName, entityName, StringComparison.OrdinalIgnoreCase)
            && Equals(t.RecordKey, recordKey));
    }

    // ── Internal methods ──────────────────────────────────────────────

    private async Task SetActiveTabAsync(string? tabId)
    {
        if (ActiveTabId != tabId)
        {
            ActiveTabId = tabId;
            await ActiveTabIdChanged.InvokeAsync(tabId);
            StateHasChanged();
        }
    }

    private async Task ExecuteCloseAsync(WorkspaceTab tab)
    {
        // If closing the active tab, activate adjacent
        if (ActiveTabId == tab.Id)
        {
            var index = Tabs.ToList().IndexOf(tab);
            string? nextTabId = null;

            if (Tabs.Count > 1)
            {
                nextTabId = index > 0
                    ? Tabs[index - 1].Id
                    : Tabs[index + 1].Id;
            }

            await SetActiveTabAsync(nextTabId);
        }

        await OnTabClose.InvokeAsync(tab);
    }

    private async Task HandleTabClickAsync(WorkspaceTab tab)
    {
        await SetActiveTabAsync(tab.Id);
    }

    private async Task HandleTabCloseClickAsync(WorkspaceTab tab)
    {
        await CloseTabAsync(tab.Id);
    }

    private async Task HandleTabAuxClickAsync(MouseEventArgs e, WorkspaceTab tab)
    {
        // Middle-click closes the tab
        if (e.Button == 1)
        {
            await CloseTabAsync(tab.Id);
        }
    }

    private async Task HandleDirtyCloseConfirmAsync()
    {
        _showDirtyCloseDialog = false;

        if (_pendingCloseTab is not null)
        {
            await OnDirtyTabCloseConfirmed.InvokeAsync(_pendingCloseTab);
            await ExecuteCloseAsync(_pendingCloseTab);
            _pendingCloseTab = null;
        }
    }

    private void HandleDirtyCloseCancelAsync()
    {
        _showDirtyCloseDialog = false;
        _pendingCloseTab = null;
    }

    private void DismissAllDirtyWarning()
    {
        _showAllDirtyWarning = false;
    }

    private void ToggleOverflowMenu()
    {
        _showOverflowMenu = !_showOverflowMenu;
    }

    private async Task HandleOverflowTabClickAsync(WorkspaceTab tab)
    {
        _showOverflowMenu = false;
        await SetActiveTabAsync(tab.Id);
    }

    // ── Drag-and-drop ─────────────────────────────────────────────────

    private void HandleDragStart(WorkspaceTab tab)
    {
        _dragTabId = tab.Id;
    }

    private async Task HandleDropAsync(WorkspaceTab targetTab)
    {
        if (_dragTabId is null) return;

        var tabList = Tabs.ToList();
        var oldIndex = tabList.FindIndex(t => t.Id == _dragTabId);
        var newIndex = tabList.FindIndex(t => t.Id == targetTab.Id);

        _dragTabId = null;

        if (oldIndex >= 0 && newIndex >= 0 && oldIndex != newIndex)
        {
            await OnTabReorder.InvokeAsync((oldIndex, newIndex));
        }
    }

    private void HandleDragEnter(WorkspaceTab tab)
    {
        if (_dragTabId is not null && _dragTabId != tab.Id)
        {
            _dragOverTabId = tab.Id;
        }
    }

    private void HandleDragLeave()
    {
        _dragOverTabId = null;
    }

    private void HandleDragEnd()
    {
        _dragTabId = null;
        _dragOverTabId = null;
    }

    // ── Tab bar keyboard navigation (ARIA Tabs pattern) ────────────────

    private async Task HandleTabBarKeyDownAsync(KeyboardEventArgs e)
    {
        if (Tabs.Count <= 1)
        {
            _preventTabBarDefault = false;
            return;
        }

        var currentIndex = Tabs.ToList().FindIndex(t => t.Id == ActiveTabId);
        if (currentIndex < 0)
        {
            _preventTabBarDefault = false;
            return;
        }

        int nextIndex = -1;

        switch (e.Key)
        {
            case "ArrowRight":
                nextIndex = (currentIndex + 1) % Tabs.Count;
                _preventTabBarDefault = true;
                break;
            case "ArrowLeft":
                nextIndex = (currentIndex - 1 + Tabs.Count) % Tabs.Count;
                _preventTabBarDefault = true;
                break;
            case "Home":
                nextIndex = 0;
                _preventTabBarDefault = true;
                break;
            case "End":
                nextIndex = Tabs.Count - 1;
                _preventTabBarDefault = true;
                break;
            default:
                _preventTabBarDefault = false;
                return;
        }

        if (nextIndex >= 0 && nextIndex != currentIndex)
        {
            await SetActiveTabAsync(Tabs[nextIndex].Id);
            // Move focus to the newly activated tab button
            await JSRuntime.InvokeVoidAsync("eval",
                $"document.getElementById('tab-{Tabs[nextIndex].Id}')?.focus()");
        }
    }

    // ── Keyboard ──────────────────────────────────────────────────────

    private async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        // Ctrl+W closes active tab
        if (e.CtrlKey && e.Key == "w")
        {
            if (ActiveTabId is not null)
            {
                await CloseTabAsync(ActiveTabId);
            }
        }
        // Ctrl+Tab cycles forward
        else if (e.CtrlKey && e.Key == "Tab" && !e.ShiftKey)
        {
            CycleTab(forward: true);
        }
        // Ctrl+Shift+Tab cycles backward
        else if (e.CtrlKey && e.Key == "Tab" && e.ShiftKey)
        {
            CycleTab(forward: false);
        }
    }

    private void CycleTab(bool forward)
    {
        if (Tabs.Count <= 1) return;

        var currentIndex = Tabs.ToList().FindIndex(t => t.Id == ActiveTabId);
        if (currentIndex < 0) return;

        int nextIndex;
        if (forward)
        {
            nextIndex = (currentIndex + 1) % Tabs.Count;
        }
        else
        {
            nextIndex = (currentIndex - 1 + Tabs.Count) % Tabs.Count;
        }

        _ = SetActiveTabAsync(Tabs[nextIndex].Id);
    }

    private string GetTabAriaLabel(WorkspaceTab tab)
    {
        return tab.IsDirty
            ? $"{tab.Title} (unsaved changes)"
            : tab.Title;
    }

    private string GetActiveTabTitle()
    {
        var active = Tabs.FirstOrDefault(t => t.Id == ActiveTabId);
        return active?.Title ?? "Dabbler";
    }

    private string GetTabCssClass(WorkspaceTab tab)
    {
        var css = "dabbler-workspace-tab";
        if (tab.Id == ActiveTabId)
            css += " dabbler-workspace-tab-active";
        if (tab.IsDirty)
            css += " dabbler-workspace-tab-dirty";
        if (tab.Id == _dragTabId)
            css += " dabbler-workspace-tab-dragging";
        if (tab.Id == _dragOverTabId)
            css += " dabbler-workspace-tab-drop-target";
        return css;
    }
}
