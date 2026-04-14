// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using System.ComponentModel;
using Dabbler.Components.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Dabbler.Components;

/// <summary>
/// Wrapper component that adds keyboard navigation to <see cref="MudBlazor.MudDataGrid{T}"/>.
/// </summary>
/// <remarks>
/// <para>
/// Wrap a <c>MudDataGrid</c> inside this component to enable arrow keys, Tab, Page Up/Down,
/// Home/End, F2 to edit, and Escape to cancel — matching the WPF DabblerDataGrid behavior.
/// </para>
/// <para>
/// When <see cref="DataGrid"/> is set, the wrapper manages the full edit lifecycle
/// (begin, commit, cancel) via the MudDataGrid public API. In Cell edit mode, Enter commits
/// and moves down; Tab commits and moves to the next editable cell.
/// </para>
/// <para>
/// Navigation state is managed by <see cref="GridNavigationModel"/> (pure C#, testable).
/// Cell focus highlighting and scroll-into-view are handled via JS interop.
/// </para>
/// <para>
/// When <see cref="GroupDescriptors"/> and <see cref="DataRowTemplate"/> are set, the grid
/// renders its own grouped table with collapsible group headers and aggregate values.
/// </para>
/// </remarks>
/// <typeparam name="T">The row data type.</typeparam>
/// <example>
/// <code>
/// &lt;DabblerDataGrid T="Customer" DataGrid="@_grid"&gt;
///     &lt;MudDataGrid @ref="_grid" T="Customer" Items="@_customers"&gt;
///         &lt;Columns&gt;
///             &lt;PropertyColumn Property="x =&gt; x.Name" /&gt;
///             &lt;PropertyColumn Property="x =&gt; x.City" /&gt;
///         &lt;/Columns&gt;
///     &lt;/MudDataGrid&gt;
/// &lt;/DabblerDataGrid&gt;
/// </code>
/// </example>
public partial class DabblerDataGrid<T> : DabblerComponentBase
{
    private ElementReference _wrapperRef;
    private IJSObjectReference? _jsModule;
    private IJSObjectReference? _clipboardModule;
    private bool _hasFocus;
    private bool _isEditingInternal;
    private CancellationTokenSource? _focusOutCts;
    private bool _pendingEditorFocus;
    private int _pendingEditorRow;
    private int _pendingEditorCol;
    private string? _activeDescendantId;
    private string? _liveAnnouncement;

    // ── Grouping state ─────────────────────────────────────────────────
    private GroupExpandState? _internalGroupExpandState;
    private GroupExpandState? _subscribedExpandState;
    private IReadOnlyList<GridRowDescriptor<T>> _visibleRows = [];
    private GridDataResponse<T>? _lastGroupResponse;

    // ── Detail state ────────────────────────────────────────────────────
    private DetailExpandState<object>? _internalDetailState;
    private DetailExpandState<object>? _subscribedDetailState;
    private string? _pendingDetailFocusId;

    // ── Popup form state ──────────────────────────────────────────────
    private bool _popupFormVisible;
    private T? _popupFormItem;
    private T? _popupFormOriginalItem;
    private bool _popupFormIsNewItem;
    private MudForm? _popupForm;
    private bool _popupFormIsValid = true;
    private bool _popupFormShowDiscardConfirm;

    /// <summary>
    /// The content to render inside the wrapper (typically a <c>MudDataGrid</c>).
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Reference to the <see cref="MudBlazor.MudDataGrid{T}"/> being wrapped.
    /// When set, the wrapper manages the edit lifecycle (begin/commit/cancel) directly
    /// via the MudDataGrid public API, including keyboard-driven commit on Enter and Tab.
    /// </summary>
    [Parameter]
    public MudDataGrid<T>? DataGrid { get; set; }

    /// <summary>
    /// The navigation model that tracks the current cell position.
    /// </summary>
    /// <remarks>
    /// Consumers can supply their own instance (e.g., to set <see cref="GridNavigationModel.ShouldSkipRow"/>)
    /// or leave it null to use the default.
    /// </remarks>
    [Parameter]
    public GridNavigationModel? NavigationModel { get; set; }

    /// <summary>
    /// Gets or sets the total number of data rows. Update this when the data source changes.
    /// </summary>
    [Parameter]
    public int RowCount { get; set; }

    /// <summary>
    /// Gets or sets the total number of visible columns.
    /// </summary>
    [Parameter]
    public int ColumnCount { get; set; }

    /// <summary>
    /// Callback invoked when the user presses F2 or Enter to begin editing.
    /// Receives the (rowIndex, columnIndex) of the cell.
    /// When <see cref="DataGrid"/> is set, this fires after the edit has been started.
    /// </summary>
    [Parameter]
    public EventCallback<(int Row, int Column)> OnBeginEdit { get; set; }

    /// <summary>
    /// Callback invoked when the user presses Escape to cancel editing.
    /// When <see cref="DataGrid"/> is set, this fires after the edit has been canceled.
    /// </summary>
    [Parameter]
    public EventCallback OnCancelEdit { get; set; }

    /// <summary>
    /// Callback invoked when the user presses Enter or Tab while editing to commit.
    /// When <see cref="DataGrid"/> is set, this fires after the edit has been committed.
    /// </summary>
    [Parameter]
    public EventCallback OnCommitEdit { get; set; }

    /// <summary>
    /// Whether the grid is currently in edit mode. When true, arrow keys pass through
    /// to the editor and only Enter/Tab/Escape are intercepted.
    /// When <see cref="DataGrid"/> is set, this parameter is ignored; the wrapper tracks
    /// editing state internally.
    /// </summary>
    [Parameter]
    public bool IsEditing { get; set; }

    /// <summary>
    /// Predicate that returns true if the column at the given index is editable.
    /// Used for Tab navigation during editing to skip read-only columns.
    /// </summary>
    [Parameter]
    public Func<int, bool>? IsColumnEditable { get; set; }

    /// <summary>
    /// Callback invoked when the focused cell changes.
    /// </summary>
    [Parameter]
    public EventCallback<(int Row, int Column)> CurrentCellChanged { get; set; }

    /// <summary>
    /// Accessible label for the grid. Used as the <c>aria-label</c> attribute.
    /// </summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>
    /// Column definitions used for clipboard copy/paste and CSV export.
    /// When set, Ctrl+C copies selected rows, Ctrl+X cuts the current cell,
    /// and Ctrl+V pastes into the current cell.
    /// </summary>
    [Parameter]
    public IReadOnlyList<GridColumnInfo>? Columns { get; set; }

    /// <summary>
    /// The items currently displayed in the grid. Used for clipboard copy and CSV export.
    /// When null, falls back to <c>DataGrid.FilteredItems</c>.
    /// </summary>
    [Parameter]
    public IReadOnlyList<T>? Items { get; set; }

    /// <summary>
    /// Optional callback invoked when a cell value is modified by cut or paste.
    /// Receives the affected item. Use this to mark items dirty in your editing service.
    /// </summary>
    [Parameter]
    public EventCallback<T> OnCellModified { get; set; }

    /// <summary>
    /// Whether the grid is read-only. When true, paste and cut are disabled.
    /// </summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Callback invoked after a clipboard operation (copy, cut, paste, export) completes.
    /// The string parameter describes the operation performed.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Parameter]
    public EventCallback<string> OnClipboardOperation { get; set; }

    // ── Grouping parameters ────────────────────────────────────────────

    /// <summary>
    /// Active grouping columns. When set together with <see cref="DataRowTemplate"/>, the grid
    /// fetches with grouping and renders collapsible group header rows.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Parameter]
    public IReadOnlyList<GroupDescriptor>? GroupDescriptors { get; set; }

    /// <summary>
    /// External expand/collapse state. If null, the grid creates and owns an internal instance.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Parameter]
    public GroupExpandState? GroupExpandState { get; set; }

    /// <summary>
    /// Which aggregates to show in group header rows.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Parameter]
    public IReadOnlyList<GridAggregateColumn>? AggregateColumns { get; set; }

    /// <summary>
    /// When true, groups start expanded instead of collapsed.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Parameter]
    public bool DefaultGroupsExpanded { get; set; }

    /// <summary>
    /// Raised when a group is expanded or collapsed.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Parameter]
    public EventCallback<(int Level, object? GroupKey, bool IsExpanded)> OnGroupToggled { get; set; }

    // ── Detail parameters ──────────────────────────────────────────────

    /// <summary>
    /// Template rendered below an expanded row. Receives the parent row's data item.
    /// When set, the grid inserts an expand/collapse indicator column and a detail content row.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Parameter]
    public RenderFragment<T>? DetailTemplate { get; set; }

    /// <summary>
    /// Extracts a unique key from a row for detail expand-state tracking.
    /// Required when <see cref="DetailTemplate"/> is set.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Parameter]
    public Func<T, object>? DetailKeySelector { get; set; }

    /// <summary>
    /// Whether single or multiple rows can have their detail expanded simultaneously.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Parameter]
    public DetailExpandMode DetailExpandMode { get; set; } = DetailExpandMode.Multiple;

    /// <summary>
    /// Whether to render the expand/collapse indicator column. Defaults to <c>true</c>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Parameter]
    public bool ShowDetailColumn { get; set; } = true;

    /// <summary>
    /// Raised when a detail row is expanded or collapsed.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Parameter]
    public EventCallback<(T Item, bool IsExpanded)> OnDetailToggled { get; set; }

    /// <summary>
    /// Template for rendering data cells in each data row. When set, the grid renders its own
    /// grouped table instead of delegating to <see cref="ChildContent"/>.
    /// Each invocation receives a data item of type <typeparamref name="T"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Parameter]
    public RenderFragment<T>? DataRowTemplate { get; set; }

    /// <summary>
    /// Template for rendering the table header row (the <c>&lt;tr&gt;</c> with <c>&lt;th&gt;</c>
    /// elements). Used only when <see cref="DataRowTemplate"/> is set.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Parameter]
    public RenderFragment? ColumnHeaderTemplate { get; set; }

    /// <summary>
    /// Raised when the user presses Escape (while not editing) or Left on the first column.
    /// Intended for child grids embedded in a master-detail layout: the parent handles this
    /// callback by collapsing the detail row and returning focus to the parent grid.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Parameter]
    public EventCallback OnEscapeOrCollapseRequest { get; set; }

    // ── Column pinning parameters ──────────────────────────────────────

    /// <summary>
    /// Enables column pinning behavior. When true, columns with a non-<c>None</c>
    /// <see cref="GridColumnInfo.PinPosition"/> are made sticky after each render.
    /// Clipboard copy and CSV export follow visual order (pinned-left → unpinned → pinned-right).
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Parameter]
    public bool AllowColumnPinning { get; set; }

    /// <summary>
    /// Raised when a column's pin position changes via <see cref="PinColumn"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Parameter]
    public EventCallback<(string PropertyName, ColumnPinPosition Position)> OnColumnPinChanged { get; set; }

    // ── Popup form parameters ─────────────────────────────────────────

    /// <summary>
    /// When set, uses a <c>MudDialog</c> popup instead of MudDataGrid's built-in form editor.
    /// The value determines the form layout: <see cref="PopupFormMode.OneColumn"/> for a simple
    /// vertical form, or <see cref="PopupFormMode.TwoColumn"/> for a wider two-column layout.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Parameter]
    public PopupFormMode? PopupFormMode { get; set; }

    /// <summary>
    /// Title displayed in the popup form dialog header. Defaults to <c>"Edit Row"</c>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Parameter]
    public string? PopupFormTitle { get; set; } = "Edit Row";

    /// <summary>
    /// Field grouping for <see cref="Data.PopupFormMode.TwoColumn"/> mode.
    /// When set, fields are arranged under section headers. When null, all fields appear in one group.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Parameter]
    public IReadOnlyList<PopupFormFieldGroup>? PopupFormFieldGroups { get; set; }

    /// <summary>
    /// CSS <c>max-width</c> for the popup form dialog (e.g., <c>"600px"</c> for one-column,
    /// <c>"900px"</c> for two-column). When null, uses a sensible default based on the mode.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Parameter]
    public string? PopupFormWidth { get; set; }

    /// <summary>
    /// Raised when the user saves the popup form. The consumer handles persistence.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Parameter]
    public EventCallback<T> OnPopupFormSave { get; set; }

    /// <summary>
    /// Raised when the user cancels the popup form.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Parameter]
    public EventCallback<T> OnPopupFormCancel { get; set; }

    /// <summary>
    /// Gets the navigation model instance (creates a default if none was supplied).
    /// </summary>
    public GridNavigationModel Navigation => NavigationModel ?? (_defaultNav ??= new GridNavigationModel());
    private GridNavigationModel? _defaultNav;

    /// <summary>
    /// Gets whether the grid is currently in edit mode.
    /// When <see cref="DataGrid"/> is set, this reflects internal state;
    /// otherwise it reflects the <see cref="IsEditing"/> parameter.
    /// </summary>
    public bool EffectiveIsEditing => DataGrid is not null ? _isEditingInternal : IsEditing;

    /// <summary>
    /// Gets the effective group expand state (external if supplied, otherwise the internal instance).
    /// </summary>
    public GroupExpandState EffectiveGroupExpandState =>
        GroupExpandState ?? (_internalGroupExpandState ??= CreateDefaultExpandState());

    /// <summary>
    /// Gets the internal detail expand state, re-creating it if <see cref="DetailExpandMode"/>
    /// has changed since the instance was last created.
    /// </summary>
    private DetailExpandState<object> EffectiveDetailState
    {
        get
        {
            if (_internalDetailState is null || _internalDetailState.Mode != DetailExpandMode)
                _internalDetailState = new DetailExpandState<object>(DetailExpandMode);
            return _internalDetailState;
        }
    }

    /// <summary>
    /// Gets the effective column span for full-width rows (group headers, detail rows).
    /// Accounts for the optional detail indicator column.
    /// </summary>
    private int EffectiveColumnSpan =>
        ColumnCount + (DetailTemplate is not null && ShowDetailColumn ? 1 : 0);

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        Navigation.ColumnCount = ColumnCount;

        // In grouped mode the row count is managed by RebuildVisibleRows(); otherwise use the parameter.
        if (DataRowTemplate is null)
            Navigation.RowCount = RowCount;

        // Subscribe to whichever GroupExpandState is now active.
        var newState = EffectiveGroupExpandState;
        if (!ReferenceEquals(_subscribedExpandState, newState))
        {
            if (_subscribedExpandState is not null)
                _subscribedExpandState.StateChanged -= OnGroupExpandStateChanged;
            _subscribedExpandState = newState;
            _subscribedExpandState.StateChanged += OnGroupExpandStateChanged;
        }

        // Subscribe to detail expand state when a DetailTemplate is provided.
        // EffectiveDetailState re-creates the instance if DetailExpandMode has changed.
        if (DetailTemplate is not null)
        {
            var detailState = EffectiveDetailState;
            if (!ReferenceEquals(_subscribedDetailState, detailState))
            {
                if (_subscribedDetailState is not null)
                    _subscribedDetailState.StateChanged -= OnDetailExpandStateChanged;
                _subscribedDetailState = detailState;
                _subscribedDetailState.StateChanged += OnDetailExpandStateChanged;
            }
        }
        else if (_subscribedDetailState is not null)
        {
            _subscribedDetailState.StateChanged -= OnDetailExpandStateChanged;
            _subscribedDetailState = null;
            _internalDetailState = null;
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Dabbler.Components/dabbler-datagrid.js");
            _clipboardModule = await JSRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Dabbler.Components/dabbler-clipboard.js");

            await _jsModule.InvokeVoidAsync("registerKeyInterceptor", _wrapperRef);
            Navigation.CurrentCellChanged += OnNavigationCellChanged;
        }

        // Focus the editor input in a cell after BeginEditAsync triggers a re-render.
        if (_pendingEditorFocus && _jsModule is not null)
        {
            _pendingEditorFocus = false;
            await _jsModule.InvokeVoidAsync("focusEditorInCell",
                _wrapperRef, _pendingEditorRow, _pendingEditorCol);
        }

        // Focus the detail content after expansion if a pending focus was requested.
        if (_pendingDetailFocusId is not null && _jsModule is not null)
        {
            var focusId = _pendingDetailFocusId;
            _pendingDetailFocusId = null;
            await _jsModule.InvokeVoidAsync("focusDetailContent", focusId);
        }

        // Apply sticky column pin offsets after every render when pinning is active.
        if (AllowColumnPinning && _jsModule is not null && Columns is not null)
        {
            var pinConfig = BuildPinConfig();
            if (pinConfig.Count > 0)
                await _jsModule.InvokeVoidAsync("applyPinOffsets", _wrapperRef, pinConfig);
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async void OnNavigationCellChanged(object? sender, EventArgs e)
    {
        await UpdateFocusVisualAsync();
        if (CurrentCellChanged.HasDelegate)
        {
            await InvokeAsync(() => CurrentCellChanged.InvokeAsync(
                (Navigation.CurrentRow, Navigation.CurrentColumn)));
        }
    }

    private async Task UpdateFocusVisualAsync()
    {
        if (_jsModule is not null && _hasFocus)
        {
            _activeDescendantId = await _jsModule.InvokeAsync<string?>("focusCell",
                _wrapperRef, Navigation.CurrentRow, Navigation.CurrentColumn);
        }
    }

    private async Task HandleFocusIn()
    {
        _focusOutCts?.Cancel();
        _hasFocus = true;
        await UpdateFocusVisualAsync();
        StateHasChanged();
    }

    private async Task HandleFocusOut()
    {
        _focusOutCts?.Cancel();
        _focusOutCts = new CancellationTokenSource();
        var token = _focusOutCts.Token;
        try
        {
            // Short delay to let focus settle on the new element before checking.
            await Task.Delay(100, token);
            if (_jsModule is not null)
            {
                bool within = await _jsModule.InvokeAsync<bool>("isFocusWithin", _wrapperRef);
                if (!within)
                {
                    _hasFocus = false;
                    _activeDescendantId = null;
                    await _jsModule.InvokeVoidAsync("clearFocus", _wrapperRef);
                    StateHasChanged();
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    // --- Edit lifecycle helpers (when DataGrid is set) ---

    private T? GetItemAtRow(int rowIndex)
    {
        if (Items is not null)
            return rowIndex >= 0 && rowIndex < Items.Count ? Items[rowIndex] : default;

        if (DataGrid is null) return default;
        return DataGrid.FilteredItems.ElementAtOrDefault(rowIndex);
    }

    private async Task BeginEditAsync(int row, int col)
    {
        if (DataGrid is not null)
        {
            var item = GetItemAtRow(row);
            if (item is not null)
            {
                await DataGrid.SetEditingItemAsync(item);
                _isEditingInternal = true;

                // Queue focus on the editor input for after the render cycle
                _pendingEditorFocus = true;
                _pendingEditorRow = row;
                _pendingEditorCol = col;
            }
        }

        Announce($"Editing row {row + 1}, column {col + 1}");

        if (OnBeginEdit.HasDelegate)
            await OnBeginEdit.InvokeAsync((row, col));
    }

    private async Task CommitEditAsync()
    {
        if (DataGrid is not null)
        {
            // In Cell mode, values are already committed on each change.
            // In Form mode, validate and commit via the parameterless overload.
            if (DataGrid.EditMode == DataGridEditMode.Form)
                await DataGrid.CommitItemChangesAsync();

            // Close the editor UI.
            await DataGrid.CancelEditingItemAsync();
            _isEditingInternal = false;
        }

        Announce("Edit committed");

        if (OnCommitEdit.HasDelegate)
            await OnCommitEdit.InvokeAsync();
    }

    private async Task CancelEditAsync()
    {
        if (DataGrid is not null)
        {
            await DataGrid.CancelEditingItemAsync();
            _isEditingInternal = false;
        }

        Announce("Edit canceled");

        if (OnCancelEdit.HasDelegate)
            await OnCancelEdit.InvokeAsync();
    }

    private void Announce(string message)
    {
        _liveAnnouncement = message;
    }

    // ── Column pinning public API ──────────────────────────────────────

    /// <summary>
    /// Programmatically pins or unpins a column identified by its property name.
    /// Triggers re-application of sticky offsets and raises <see cref="OnColumnPinChanged"/>.
    /// </summary>
    /// <param name="propertyName">The property name of the column to pin/unpin.</param>
    /// <param name="position">The new pin position.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public async Task PinColumn(string propertyName, ColumnPinPosition position)
    {
        if (Columns is null) return;
        var col = Columns.FirstOrDefault(c => c.PropertyName == propertyName);
        if (col is null || col.PinPosition == position) return;

        col.PinPosition = position;

        if (OnColumnPinChanged.HasDelegate)
            await OnColumnPinChanged.InvokeAsync((propertyName, position));

        await InvokeAsync(StateHasChanged);
    }

    // ── Grouping public API ────────────────────────────────────────────

    /// <summary>Expands all groups at all levels.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void ExpandAllGroups() => EffectiveGroupExpandState.ExpandAll();

    /// <summary>Collapses all groups at all levels.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void CollapseAllGroups() => EffectiveGroupExpandState.CollapseAll();

    /// <summary>Programmatically expand or collapse a specific group.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void SetGroupExpanded(int level, object? groupKey, bool expanded) =>
        EffectiveGroupExpandState.SetExpanded(level, groupKey, expanded);

    /// <summary>
    /// Provides the grouped response from a <see cref="IGridDataProvider{T}"/> fetch so the grid
    /// can build the visible row list and re-render with group headers.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public async Task SetGroupDataAsync(GridDataResponse<T> response)
    {
        ArgumentNullException.ThrowIfNull(response);
        _lastGroupResponse = response;
        RebuildVisibleRows();
        await InvokeAsync(StateHasChanged);
    }

    // ── Detail public API ──────────────────────────────────────────────

    /// <summary>Programmatically expands the detail row for the given item.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void ExpandDetail(T item)
    {
        if (DetailKeySelector is null) return;
        var key = DetailKeySelector(item);
        EffectiveDetailState.SetExpanded(key, true);
    }

    /// <summary>Programmatically collapses the detail row for the given item.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void CollapseDetail(T item)
    {
        if (DetailKeySelector is null) return;
        var key = DetailKeySelector(item);
        EffectiveDetailState.SetExpanded(key, false);
    }

    /// <summary>Collapses all expanded detail rows.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void CollapseAllDetails() => EffectiveDetailState.CollapseAll();

    // ── Popup form public API ─────────────────────────────────────────

    /// <summary>
    /// Opens the popup form for editing an existing row item.
    /// Only works when <see cref="PopupFormMode"/> is set.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public async Task OpenPopupFormAsync(T item)
    {
        if (PopupFormMode is null) return;

        _popupFormOriginalItem = item;
        _popupFormItem = CloneItem(item);
        _popupFormIsNewItem = false;
        _popupFormShowDiscardConfirm = false;
        _popupFormVisible = true;
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Opens the popup form for adding a new item.
    /// Only works when <see cref="PopupFormMode"/> is set.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public async Task OpenPopupFormForNewItemAsync(T item)
    {
        if (PopupFormMode is null) return;

        _popupFormOriginalItem = default;
        _popupFormItem = item;
        _popupFormIsNewItem = true;
        _popupFormShowDiscardConfirm = false;
        _popupFormVisible = true;
        await InvokeAsync(StateHasChanged);
    }

    private async Task PopupFormSaveAsync()
    {
        if (_popupFormItem is null) return;

        // Copy edited values back to the original item if editing (not adding new)
        if (!_popupFormIsNewItem && _popupFormOriginalItem is not null)
        {
            CopyProperties(_popupFormItem, _popupFormOriginalItem);
        }

        _popupFormVisible = false;

        if (OnPopupFormSave.HasDelegate)
            await OnPopupFormSave.InvokeAsync(_popupFormIsNewItem ? _popupFormItem : _popupFormOriginalItem!);

        _popupFormItem = default;
        _popupFormOriginalItem = default;
    }

    private async Task PopupFormCancelAsync()
    {
        // If form has unsaved changes, show a confirmation prompt
        if (IsPopupFormDirty() && !_popupFormShowDiscardConfirm)
        {
            _popupFormShowDiscardConfirm = true;
            return;
        }

        _popupFormShowDiscardConfirm = false;
        var item = _popupFormIsNewItem ? _popupFormItem : _popupFormOriginalItem;
        _popupFormVisible = false;
        _popupFormItem = default;
        _popupFormOriginalItem = default;

        if (OnPopupFormCancel.HasDelegate && item is not null)
            await OnPopupFormCancel.InvokeAsync(item);
    }

    private void PopupFormDismissDiscard()
    {
        _popupFormShowDiscardConfirm = false;
    }

    /// <summary>
    /// Returns true if the popup form item has been modified from the original.
    /// For new items, returns true if any non-default value has been entered.
    /// </summary>
    private bool IsPopupFormDirty()
    {
        if (_popupFormItem is null) return false;

        if (_popupFormIsNewItem)
        {
            // For new items, consider dirty if any writable property differs from default
            var blank = Activator.CreateInstance<T>();
            foreach (var prop in typeof(T).GetProperties())
            {
                if (!prop.CanRead || !prop.CanWrite) continue;
                var currentVal = prop.GetValue(_popupFormItem);
                var defaultVal = prop.GetValue(blank);
                if (!Equals(currentVal, defaultVal)) return true;
            }
            return false;
        }

        if (_popupFormOriginalItem is null) return false;

        foreach (var prop in typeof(T).GetProperties())
        {
            if (!prop.CanRead) continue;
            var editedVal = prop.GetValue(_popupFormItem);
            var originalVal = prop.GetValue(_popupFormOriginalItem);
            if (!Equals(editedVal, originalVal)) return true;
        }
        return false;
    }

    private static T CloneItem(T source)
    {
        // Shallow clone via reflection — copies all public property values
        var clone = Activator.CreateInstance<T>();
        foreach (var prop in typeof(T).GetProperties())
        {
            if (prop.CanRead && prop.CanWrite)
                prop.SetValue(clone, prop.GetValue(source));
        }
        return clone;
    }

    private static void CopyProperties(T source, T target)
    {
        foreach (var prop in typeof(T).GetProperties())
        {
            if (prop.CanRead && prop.CanWrite)
                prop.SetValue(target, prop.GetValue(source));
        }
    }

    /// <summary>
    /// Focuses the grid's wrapper element, returning keyboard focus to this grid.
    /// Useful when collapsing a detail row from the child grid and returning to the parent.
    /// </summary>
    public async Task FocusAsync()
    {
        if (_jsModule is not null)
            await _jsModule.InvokeVoidAsync("focusElement", _wrapperRef);
    }

    // ── Detail private helpers ─────────────────────────────────────────

    private bool IsDetailExpanded(T item)
    {
        if (DetailKeySelector is null) return false;
        var key = DetailKeySelector(item);
        return EffectiveDetailState.IsExpanded(key);
    }

    private string GetDetailKeyString(T item)
    {
        if (DetailKeySelector is null) return "";
        return DetailKeySelector(item)?.ToString() ?? "";
    }

    private async Task ToggleDetailAsync(T item)
    {
        if (DetailKeySelector is null) return;
        var key = DetailKeySelector(item);
        bool newExpanded = !EffectiveDetailState.IsExpanded(key);
        EffectiveDetailState.SetExpanded(key, newExpanded);

        Announce(newExpanded
            ? $"Detail expanded for {key}"
            : $"Detail collapsed for {key}");

        if (OnDetailToggled.HasDelegate)
            await OnDetailToggled.InvokeAsync((item, newExpanded));
    }

    private void OnDetailExpandStateChanged()
    {
        InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Expands a detail row (if not already expanded) and queues focus on its content.
    /// Focus is applied in <see cref="OnAfterRenderAsync"/> after the detail row is in the DOM.
    /// </summary>
    private async Task ExpandAndFocusDetailAsync(T item)
    {
        if (DetailKeySelector is null) return;
        var key = DetailKeySelector(item);

        if (!EffectiveDetailState.IsExpanded(key))
        {
            EffectiveDetailState.SetExpanded(key, true);
            Announce($"Detail expanded for {key}");
            if (OnDetailToggled.HasDelegate)
                await OnDetailToggled.InvokeAsync((item, true));
        }

        // Queue focus to be applied after the next render (when the detail row is in the DOM)
        _pendingDetailFocusId = $"dabbler-detail-{key}";
    }

    // ── Popup form private helpers ────────────────────────────────────

    private MaxWidth PopupFormDialogMaxWidth =>
        PopupFormMode == Data.PopupFormMode.TwoColumn ? MaxWidth.Large : MaxWidth.Medium;

    private string PopupFormContainerStyle
    {
        get
        {
            if (PopupFormWidth is not null)
                return $"max-width:{PopupFormWidth}";
            return "";
        }
    }

    private static string GetPropertyDisplayValue(T item, string propertyName)
    {
        var prop = typeof(T).GetProperty(propertyName);
        if (prop is null) return "";
        var value = prop.GetValue(item);
        return value?.ToString() ?? "";
    }

    private RenderFragment RenderPopupFormField(GridColumnInfo col) => builder =>
    {
        if (_popupFormItem is null || col.PropertyName is null) return;

        var prop = typeof(T).GetProperty(col.PropertyName);
        if (prop is null) return;

        // Read-only columns render as plain text
        if (col.IsReadOnly)
        {
            builder.OpenComponent<MudTextField<string>>(0);
            builder.AddAttribute(1, "Label", col.Header);
            builder.AddAttribute(2, "Value", GetPropertyDisplayValue(_popupFormItem, col.PropertyName));
            builder.AddAttribute(3, "ReadOnly", true);
            builder.AddAttribute(4, "Variant", Variant.Outlined);
            builder.AddAttribute(5, "Margin", Margin.Dense);
            builder.AddAttribute(6, "UserAttributes", new Dictionary<string, object> { ["aria-readonly"] = "true" });
            builder.CloseComponent();
            return;
        }

        // Custom EditTemplate takes precedence
        if (col.EditTemplate is not null)
        {
            col.EditTemplate((object)_popupFormItem!)(builder);
            return;
        }

        // Auto-generate editor based on property type
        var propType = prop.PropertyType;
        var underlyingType = Nullable.GetUnderlyingType(propType) ?? propType;
        bool isNullable = Nullable.GetUnderlyingType(propType) is not null || !propType.IsValueType;

        if (underlyingType == typeof(string))
        {
            RenderStringEditor(builder, prop, col);
        }
        else if (underlyingType == typeof(int))
        {
            if (isNullable)
                RenderNullableNumericEditor<int?>(builder, prop, col);
            else
                RenderNumericEditor<int>(builder, prop, col);
        }
        else if (underlyingType == typeof(decimal))
        {
            if (isNullable)
                RenderNullableNumericEditor<decimal?>(builder, prop, col);
            else
                RenderNumericEditor<decimal>(builder, prop, col);
        }
        else if (underlyingType == typeof(double))
        {
            if (isNullable)
                RenderNullableNumericEditor<double?>(builder, prop, col);
            else
                RenderNumericEditor<double>(builder, prop, col);
        }
        else if (underlyingType == typeof(DateTime))
        {
            RenderDateEditor(builder, prop, col, isNullable);
        }
        else if (underlyingType == typeof(bool))
        {
            RenderBoolEditor(builder, prop, col);
        }
        else
        {
            // Fallback: render as string text field
            RenderStringFallbackEditor(builder, prop, col);
        }
    };

    private void RenderStringEditor(
        Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder,
        System.Reflection.PropertyInfo prop,
        GridColumnInfo col)
    {
        var value = (string?)prop.GetValue(_popupFormItem) ?? "";
        builder.OpenComponent<MudTextField<string>>(0);
        builder.AddAttribute(1, "Label", col.Header);
        builder.AddAttribute(2, "Value", value);
        builder.AddAttribute(3, "ValueChanged",
            EventCallback.Factory.Create<string>(this, v =>
            {
                prop.SetValue(_popupFormItem, v);
            }));
        builder.AddAttribute(4, "Variant", Variant.Outlined);
        builder.AddAttribute(5, "Margin", Margin.Dense);
        if (col.IsRequired)
        {
            builder.AddAttribute(6, "Required", true);
            builder.AddAttribute(7, "RequiredError", $"{col.Header} is required");
            builder.AddAttribute(8, "UserAttributes", new Dictionary<string, object>
            {
                ["aria-required"] = "true",
                ["aria-invalid"] = "false"
            });
        }
        builder.CloseComponent();
    }

    private void RenderNumericEditor<TNum>(
        Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder,
        System.Reflection.PropertyInfo prop,
        GridColumnInfo col)
    {
        var value = (TNum?)prop.GetValue(_popupFormItem);
        builder.OpenComponent<MudNumericField<TNum>>(0);
        builder.AddAttribute(1, "Label", col.Header);
        builder.AddAttribute(2, "Value", value);
        builder.AddAttribute(3, "ValueChanged",
            EventCallback.Factory.Create<TNum>(this, v =>
            {
                prop.SetValue(_popupFormItem, v);
            }));
        builder.AddAttribute(4, "Variant", Variant.Outlined);
        builder.AddAttribute(5, "Margin", Margin.Dense);
        if (col.Format is not null)
            builder.AddAttribute(6, "Format", col.Format);
        if (col.IsRequired)
        {
            builder.AddAttribute(7, "Required", true);
            builder.AddAttribute(8, "RequiredError", $"{col.Header} is required");
            builder.AddAttribute(9, "UserAttributes", new Dictionary<string, object> { ["aria-required"] = "true" });
        }
        builder.CloseComponent();
    }

    private void RenderNullableNumericEditor<TNum>(
        Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder,
        System.Reflection.PropertyInfo prop,
        GridColumnInfo col)
    {
        var value = (TNum?)prop.GetValue(_popupFormItem);
        builder.OpenComponent<MudNumericField<TNum>>(0);
        builder.AddAttribute(1, "Label", col.Header);
        builder.AddAttribute(2, "Value", value);
        builder.AddAttribute(3, "ValueChanged",
            EventCallback.Factory.Create<TNum>(this, v =>
            {
                prop.SetValue(_popupFormItem, v);
            }));
        builder.AddAttribute(4, "Variant", Variant.Outlined);
        builder.AddAttribute(5, "Margin", Margin.Dense);
        builder.AddAttribute(6, "Clearable", true);
        if (col.Format is not null)
            builder.AddAttribute(7, "Format", col.Format);
        if (col.IsRequired)
        {
            builder.AddAttribute(8, "Required", true);
            builder.AddAttribute(9, "RequiredError", $"{col.Header} is required");
            builder.AddAttribute(10, "UserAttributes", new Dictionary<string, object> { ["aria-required"] = "true" });
        }
        builder.CloseComponent();
    }

    private void RenderDateEditor(
        Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder,
        System.Reflection.PropertyInfo prop,
        GridColumnInfo col,
        bool isNullable)
    {
        var rawValue = prop.GetValue(_popupFormItem);
        DateTime? dateValue = rawValue is DateTime dt ? dt : null;

        builder.OpenComponent<MudDatePicker>(0);
        builder.AddAttribute(1, "Label", col.Header);
        builder.AddAttribute(2, "Date", dateValue);
        builder.AddAttribute(3, "DateChanged",
            EventCallback.Factory.Create<DateTime?>(this, v =>
            {
                if (isNullable)
                    prop.SetValue(_popupFormItem, v);
                else
                    prop.SetValue(_popupFormItem, v ?? DateTime.MinValue);
            }));
        builder.AddAttribute(4, "Variant", Variant.Outlined);
        builder.AddAttribute(5, "Margin", Margin.Dense);
        if (col.Format is not null)
            builder.AddAttribute(6, "DateFormat", col.Format);
        if (col.IsRequired)
        {
            builder.AddAttribute(7, "Required", true);
            builder.AddAttribute(8, "RequiredError", $"{col.Header} is required");
            builder.AddAttribute(9, "UserAttributes", new Dictionary<string, object> { ["aria-required"] = "true" });
        }
        builder.CloseComponent();
    }

    private void RenderBoolEditor(
        Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder,
        System.Reflection.PropertyInfo prop,
        GridColumnInfo col)
    {
        var value = (bool?)prop.GetValue(_popupFormItem) ?? false;
        builder.OpenComponent<MudSwitch<bool>>(0);
        builder.AddAttribute(1, "Label", col.Header);
        builder.AddAttribute(2, "Value", value);
        builder.AddAttribute(3, "ValueChanged",
            EventCallback.Factory.Create<bool>(this, v =>
            {
                prop.SetValue(_popupFormItem, v);
            }));
        builder.AddAttribute(4, "Color", Color.Primary);
        builder.CloseComponent();
    }

    private void RenderStringFallbackEditor(
        Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder,
        System.Reflection.PropertyInfo prop,
        GridColumnInfo col)
    {
        var value = prop.GetValue(_popupFormItem)?.ToString() ?? "";
        builder.OpenComponent<MudTextField<string>>(0);
        builder.AddAttribute(1, "Label", col.Header);
        builder.AddAttribute(2, "Value", value);
        builder.AddAttribute(3, "ValueChanged",
            EventCallback.Factory.Create<string>(this, v =>
            {
                // Attempt to convert back for non-string types
                try
                {
                    var converted = Convert.ChangeType(v, prop.PropertyType);
                    prop.SetValue(_popupFormItem, converted);
                }
                catch { /* ignore conversion errors */ }
            }));
        builder.AddAttribute(4, "Variant", Variant.Outlined);
        builder.AddAttribute(5, "Margin", Margin.Dense);
        builder.CloseComponent();
    }

    // ── Grouping private helpers ───────────────────────────────────────

    private GroupExpandState CreateDefaultExpandState()
    {
        var state = new GroupExpandState();
        if (DefaultGroupsExpanded)
            state.ExpandAll();
        return state;
    }

    private void OnGroupExpandStateChanged()
    {
        RebuildVisibleRows();
        InvokeAsync(StateHasChanged);
    }

    private void RebuildVisibleRows()
    {
        if (_lastGroupResponse is null)
        {
            _visibleRows = [];
            Navigation.RowCount = 0;
            Navigation.ShouldSkipRow = null;
            Navigation.IsGroupHeaderRow = null;
            return;
        }

        _visibleRows = GroupedRowBuilder.Build(_lastGroupResponse, EffectiveGroupExpandState);

        // Navigation row count = all visible rows (headers + data).
        // ShouldSkipRow skips group headers during Tab navigation.
        // IsGroupHeaderRow exempts group headers from being skipped by Up/Down.
        Navigation.RowCount = _visibleRows.Count;
        Navigation.ShouldSkipRow = i =>
            i >= 0 && i < _visibleRows.Count && _visibleRows[i].RowType == GridRowType.GroupHeader;
        Navigation.IsGroupHeaderRow = i =>
            i >= 0 && i < _visibleRows.Count && _visibleRows[i].RowType == GridRowType.GroupHeader;
    }

    private async Task ToggleGroupHeaderAsync(GridRowDescriptor<T> row)
    {
        bool newExpanded = !row.IsExpanded;
        EffectiveGroupExpandState.SetExpanded(row.GroupLevel, row.GroupKey, newExpanded);

        Announce(newExpanded
            ? $"Group {row.GroupKey} expanded, {row.GroupItemCount} items"
            : $"Group {row.GroupKey} collapsed");

        if (OnGroupToggled.HasDelegate)
            await OnGroupToggled.InvokeAsync((row.GroupLevel, row.GroupKey, newExpanded));
    }

    private static string FormatAggregateValue(object val, string? format)
    {
        if (format is not null && val is IFormattable formattable)
            return formattable.ToString(format, null);
        return val.ToString() ?? "";
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        // Use Navigation.RowCount because in grouped mode it reflects the actual visible row count
        // (set by RebuildVisibleRows), not the RowCount parameter which counts only data rows.
        int effectiveRowCount = Navigation.RowCount > 0 ? Navigation.RowCount : RowCount;
        if (effectiveRowCount == 0) return;

        bool ctrl = e.CtrlKey;
        bool shift = e.ShiftKey;

        // Detect editing state from the DOM: if an input/textarea/select inside the
        // grid has focus, the user is editing (regardless of _isEditingInternal,
        // which only tracks edits started via our Enter/F2 handler).
        bool isInputActive = _jsModule is not null
            && await _jsModule.InvokeAsync<bool>("isInputFocused", _wrapperRef);

        // In DataGrid mode, auto-detect when the editor closed (user clicked away
        // or MudDataGrid committed). Sync _isEditingInternal so EffectiveIsEditing
        // reflects reality.
        if (DataGrid is not null && _isEditingInternal && !isInputActive)
        {
            _isEditingInternal = false;
            if (OnCommitEdit.HasDelegate)
                await OnCommitEdit.InvokeAsync();
        }

        // Editing mode: either an input has focus (DOM truth) or the IsEditing
        // parameter / _isEditingInternal flag says so (callback mode or just-started edit).
        bool isEditing = isInputActive || EffectiveIsEditing;

        // When editing, only intercept edit-control keys
        if (isEditing)
        {
            switch (e.Key)
            {
                case "Enter":
                    await CommitEditAsync();
                    Navigation.MoveDown();
                    // Return focus to wrapper for continued navigation
                    if (_jsModule is not null)
                        await _jsModule.InvokeVoidAsync("focusElement", _wrapperRef);
                    break;

                case "Tab":
                    await CommitEditAsync();
                    if (IsColumnEditable is not null)
                    {
                        if (shift)
                            Navigation.MoveToPreviousEditableCell(IsColumnEditable);
                        else
                            Navigation.MoveToNextEditableCell(IsColumnEditable);
                    }
                    else
                    {
                        if (shift) Navigation.MoveToPreviousCell();
                        else Navigation.MoveToNextCell();
                    }
                    // Begin editing the new cell
                    await BeginEditAsync(Navigation.CurrentRow, Navigation.CurrentColumn);
                    break;

                case "Escape":
                    await CancelEditAsync();
                    // Return focus to wrapper for continued navigation
                    if (_jsModule is not null)
                        await _jsModule.InvokeVoidAsync("focusElement", _wrapperRef);
                    break;
            }

            // Let other keys pass through to the editor
            return;
        }

        // Check if the current row is a group header
        bool onGroupHeader = Navigation.IsGroupHeaderRow?.Invoke(Navigation.CurrentRow) == true;
        GridRowDescriptor<T>? currentGroupHeader = onGroupHeader && Navigation.CurrentRow < _visibleRows.Count
            ? _visibleRows[Navigation.CurrentRow]
            : null;

        // Check if the cursor is on the detail indicator column (column 0 of a data row)
        bool onDetailIndicator = !onGroupHeader
            && DetailTemplate is not null
            && ShowDetailColumn
            && Navigation.CurrentColumn == 0;
        T? currentDetailItem = onDetailIndicator && Navigation.CurrentRow < _visibleRows.Count
            ? _visibleRows[Navigation.CurrentRow].DataItem
            : default;

        // Non-editing keyboard handling
        // (preventDefault is handled by the JS key interceptor registered in OnAfterRenderAsync)
        switch (e.Key)
        {
            case "ArrowUp":
                if (ctrl) Navigation.MoveToFirstRowInColumn();
                else Navigation.MoveUp();
                break;

            case "ArrowDown":
                if (ctrl) Navigation.MoveToLastRowInColumn();
                else Navigation.MoveDown();
                break;

            case "ArrowLeft":
                if (currentGroupHeader is not null)
                {
                    // On a group header: Left collapses (if expanded), no-op if already collapsed
                    if (currentGroupHeader.IsExpanded)
                        await ToggleGroupHeaderAsync(currentGroupHeader);
                }
                else if (Navigation.CurrentColumn == 0 && OnEscapeOrCollapseRequest.HasDelegate)
                {
                    // At the leftmost column in a child grid: signal parent to collapse and return focus
                    await OnEscapeOrCollapseRequest.InvokeAsync();
                }
                else
                {
                    if (ctrl) Navigation.MoveHome();
                    else Navigation.MoveLeft();
                }
                break;

            case "ArrowRight":
                if (currentGroupHeader is not null)
                {
                    // On a group header: Right expands (if collapsed), no-op if already expanded
                    if (!currentGroupHeader.IsExpanded)
                        await ToggleGroupHeaderAsync(currentGroupHeader);
                }
                else if (onDetailIndicator && currentDetailItem is not null)
                {
                    // On detail indicator column: expand detail and focus the child grid
                    await ExpandAndFocusDetailAsync(currentDetailItem);
                }
                else
                {
                    if (ctrl) Navigation.MoveEnd();
                    else Navigation.MoveRight();
                }
                break;

            case "Tab":
                if (shift) Navigation.MoveToPreviousCell();
                else Navigation.MoveToNextCell();
                break;

            case "Home":
                if (ctrl) Navigation.MoveToStart();
                else Navigation.MoveHome();
                break;

            case "End":
                if (ctrl) Navigation.MoveToEnd();
                else Navigation.MoveEnd();
                break;

            case "PageUp":
                int vpUp = await GetViewportRowCountAsync();
                Navigation.MovePageUp(vpUp);
                break;

            case "PageDown":
                int vpDown = await GetViewportRowCountAsync();
                Navigation.MovePageDown(vpDown);
                break;

            case " " when currentGroupHeader is not null:
                // Space on a group header toggles expand/collapse
                await ToggleGroupHeaderAsync(currentGroupHeader);
                break;

            case "F2":
            case "Enter":
                if (currentGroupHeader is not null)
                    await ToggleGroupHeaderAsync(currentGroupHeader);
                else if (onDetailIndicator && currentDetailItem is not null)
                    await ExpandAndFocusDetailAsync(currentDetailItem);
                else
                    await BeginEditAsync(Navigation.CurrentRow, Navigation.CurrentColumn);
                break;

            case "Escape":
                if (OnEscapeOrCollapseRequest.HasDelegate)
                    // In a child detail grid: signal parent to collapse and return focus
                    await OnEscapeOrCollapseRequest.InvokeAsync();
                break;

            case "c" when ctrl:
                await CopyToClipboardAsync();
                break;

            case "x" when ctrl:
                await CutCurrentCellAsync();
                break;

            case "v" when ctrl:
                await PasteToCurrentCellAsync();
                break;
        }
    }

    private async Task<int> GetViewportRowCountAsync()
    {
        if (_jsModule is null) return 10;
        return await _jsModule.InvokeAsync<int>("getViewportRowCount", _wrapperRef);
    }

    // ── Clipboard & Export ─────────────────────────────────────────

    /// <summary>
    /// Copies all rows (or the current row) to the clipboard as tab-delimited text.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public async Task CopyToClipboardAsync()
    {
        if (Columns is null || _clipboardModule is null) return;

        var items = GetExportItems();
        if (items.Count == 0) return;

        var exportColumns = AllowColumnPinning ? ColumnPinSorter.GetVisualOrder(Columns) : Columns;
        string text = CsvExporter.FormatAsTabDelimited(items, exportColumns, includeHeaders: true);
        if (!string.IsNullOrEmpty(text))
        {
            await _clipboardModule.InvokeAsync<bool>("writeText", text);
            Announce($"Copied {items.Count} row(s) to clipboard");
            if (OnClipboardOperation.HasDelegate)
                await OnClipboardOperation.InvokeAsync($"Copied {items.Count} row(s)");
        }
    }

    /// <summary>
    /// Cuts the current cell value to the clipboard.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public async Task CutCurrentCellAsync()
    {
        if (Columns is null || _clipboardModule is null || ReadOnly) return;

        int row = Navigation.CurrentRow;
        int col = Navigation.CurrentColumn;
        if (col < 0 || col >= Columns.Count) return;

        var column = Columns[col];
        if (column.IsReadOnly || column.PropertyName is null) return;

        var item = GetItemAtRow(row);
        if (item is null) return;

        string valueText = ClipboardCoordinator.GetCellValueAsString(item, column.PropertyName);
        await _clipboardModule.InvokeAsync<bool>("writeText", valueText);

        if (ClipboardCoordinator.ClearCellValue(item, column.PropertyName))
        {
            if (OnCellModified.HasDelegate)
                await OnCellModified.InvokeAsync(item);
            Announce("Cell cut to clipboard");
            if (OnClipboardOperation.HasDelegate)
                await OnClipboardOperation.InvokeAsync("Cut cell");
            StateHasChanged();
        }
    }

    /// <summary>
    /// Pastes clipboard text into the current cell.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public async Task PasteToCurrentCellAsync()
    {
        if (Columns is null || _clipboardModule is null || ReadOnly) return;

        int row = Navigation.CurrentRow;
        int col = Navigation.CurrentColumn;
        if (col < 0 || col >= Columns.Count) return;

        var column = Columns[col];
        if (column.IsReadOnly || column.PropertyName is null) return;

        var item = GetItemAtRow(row);
        if (item is null) return;

        string? text = await _clipboardModule.InvokeAsync<string?>("readText");
        if (text is null) return;

        // Trim trailing newlines (common when pasting single values)
        text = text.TrimEnd('\r', '\n');

        if (ClipboardCoordinator.PasteCellValue(item, column.PropertyName, text))
        {
            if (OnCellModified.HasDelegate)
                await OnCellModified.InvokeAsync(item);
            Announce("Pasted from clipboard");
            if (OnClipboardOperation.HasDelegate)
                await OnClipboardOperation.InvokeAsync("Pasted cell");
            StateHasChanged();
        }
    }

    /// <summary>
    /// Exports the current data to CSV format and writes to the given stream.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public async Task ExportToCsvAsync(Stream output)
    {
        ArgumentNullException.ThrowIfNull(output);
        if (Columns is null) return;

        var items = GetExportItems();
        var exportColumns = AllowColumnPinning ? ColumnPinSorter.GetVisualOrder(Columns) : Columns;
        await CsvExporter.ExportAsync(items, exportColumns, output);
    }

    /// <summary>
    /// Exports the current data to CSV and triggers a browser file download.
    /// </summary>
    /// <param name="fileName">The suggested file name (default: "export.csv").</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public async Task ExportToCsvDownloadAsync(string fileName = "export.csv")
    {
        if (Columns is null || _clipboardModule is null) return;

        using var ms = new MemoryStream();
        await ExportToCsvAsync(ms);

        var base64 = Convert.ToBase64String(ms.ToArray());
        await _clipboardModule.InvokeVoidAsync("downloadFile", fileName, base64, "text/csv");

        var items = GetExportItems();
        Announce($"Exported {items.Count} row(s) to {fileName}");
        if (OnClipboardOperation.HasDelegate)
            await OnClipboardOperation.InvokeAsync($"Exported {items.Count} row(s)");
    }

    /// <summary>
    /// Builds the pin configuration array passed to the JS <c>applyPinOffsets</c> function.
    /// Each entry is an anonymous object with <c>columnIndex</c> (DOM cell index, accounting for
    /// the optional detail indicator column) and <c>position</c> ("left" or "right").
    /// </summary>
    private IReadOnlyList<object> BuildPinConfig()
    {
        if (Columns is null) return [];

        // Detail indicator column is prepended, so data column DOM index = Columns index + offset
        int offset = (DetailTemplate is not null && ShowDetailColumn) ? 1 : 0;

        var config = new List<object>();
        for (int i = 0; i < Columns.Count; i++)
        {
            var pos = Columns[i].PinPosition;
            if (pos == ColumnPinPosition.None) continue;
            config.Add(new { columnIndex = i + offset, position = pos == ColumnPinPosition.Left ? "left" : "right" });
        }

        return config;
    }

    private IReadOnlyList<object> GetExportItems()
    {
        if (Items is not null)
            return Items.Cast<object>().ToList();

        if (DataGrid?.FilteredItems is not null)
            return DataGrid.FilteredItems.Cast<object>().ToList();

        return Array.Empty<object>();
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        Navigation.CurrentCellChanged -= OnNavigationCellChanged;
        _focusOutCts?.Cancel();
        _focusOutCts?.Dispose();

        if (_subscribedExpandState is not null)
        {
            _subscribedExpandState.StateChanged -= OnGroupExpandStateChanged;
            _subscribedExpandState = null;
        }

        if (_subscribedDetailState is not null)
        {
            _subscribedDetailState.StateChanged -= OnDetailExpandStateChanged;
            _subscribedDetailState = null;
        }

        if (_jsModule is not null)
        {
            try { await _jsModule.InvokeVoidAsync("unregisterKeyInterceptor", _wrapperRef); }
            catch (JSDisconnectedException) { }
            await _jsModule.DisposeAsync();
            _jsModule = null;
        }

        if (_clipboardModule is not null)
        {
            await _clipboardModule.DisposeAsync();
            _clipboardModule = null;
        }

        await base.DisposeAsyncCore();
    }
}
