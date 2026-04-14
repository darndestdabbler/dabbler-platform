// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using Dabbler.Components.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using MudBlazor;

namespace Dabbler.Components;

/// <summary>
/// A multi-select combo box component. Selected items display as dismissible chips.
/// Uses <see cref="ComboBoxSearchHelper{T}"/> for shared search/debounce logic.
/// </summary>
/// <typeparam name="T">The type of item in the combo box.</typeparam>
public partial class DabblerMultiComboBox<T> : DabblerComponentBase
{
    private MudAutocomplete<T>? _autocomplete;
    private readonly ComboBoxSearchHelper<T> _searchHelper = new();
    private ElementReference _wrapperRef;
    private IJSObjectReference? _a11yModule;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    // Internal mutable list; exposed as IReadOnlyList<T> via Values.
    private List<T> _values = [];

    // HashSet mirror of _values for O(1) IsSelected lookups.
    private HashSet<T> _selectedSet = new(EqualityComparer<T>.Default);

    // Sentinel used to reset MudAutocomplete after each selection.
    private T? _pendingValue;

    // Controls whether the AddNewTemplate dialog panel is visible.
    private bool _showAddNewDialog;

    // Context passed to AddNewTemplate when dialog mode is active.
    private AddNewItemContext<T>? _addNewContext;

    // ─── Parameters ───────────────────────────────────────────────────────────

    /// <summary>Currently selected values.</summary>
    [Parameter]
    public IReadOnlyList<T> Values { get; set; } = [];

    /// <summary>Raised when the selection changes.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<T>> ValuesChanged { get; set; }

    /// <summary>The function that provides search results.</summary>
    [Parameter]
    public Func<string?, CancellationToken, Task<IEnumerable<T>>>? SearchFunc { get; set; }

    /// <summary>Converts an item to its display string.</summary>
    [Parameter]
    public Func<T?, string?>? ToStringFunc { get; set; }

    /// <summary>Optional cap on the number of selected items.</summary>
    [Parameter]
    public int? MaxSelections { get; set; }

    /// <summary>
    /// Called when the user enters text that does not match any item and leaves the control.
    /// Set <see cref="ValueNotInListEventArgs.Handled"/> to <c>true</c> to suppress revert.
    /// </summary>
    [Parameter]
    public Func<ValueNotInListEventArgs, Task>? OnValueNotInList { get; set; }

    /// <summary>
    /// When <c>true</c>, an "Add '{text}'…" option appears at the bottom of the dropdown
    /// when the typed text does not match the selected value.
    /// </summary>
    [Parameter]
    public bool AllowAddNew { get; set; }

    /// <summary>
    /// Simple mode callback. Receives the typed text and returns the newly created item,
    /// or <c>null</c> to cancel. Takes priority over <see cref="AddNewTemplate"/> if both are set.
    /// </summary>
    [Parameter]
    public Func<string, Task<T?>>? OnAddNewItem { get; set; }

    /// <summary>
    /// Dialog mode template. Receives an <see cref="AddNewItemContext{T}"/> with the typed text
    /// and callbacks to complete or cancel the add-new flow.
    /// </summary>
    [Parameter]
    public RenderFragment<AddNewItemContext<T>>? AddNewTemplate { get; set; }

    /// <summary>
    /// Custom prompt text for the add-new dropdown option. Use <c>{0}</c> for typed text.
    /// Defaults to <c>"Add '{0}'…"</c>.
    /// </summary>
    [Parameter]
    public string? AddNewPromptText { get; set; }

    /// <summary>Visual style of the selected chips.</summary>
    [Parameter]
    public Variant ChipVariant { get; set; } = Variant.Filled;

    /// <summary>Size of the selected chips.</summary>
    [Parameter]
    public Size ChipSize { get; set; } = Size.Small;

    /// <summary>The label text displayed above the input.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>The placeholder text displayed when the input is empty.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>The visual variant of the input.</summary>
    [Parameter]
    public Variant Variant { get; set; }

    /// <summary>The maximum number of items shown in the dropdown.</summary>
    [Parameter]
    public int MaxItems { get; set; } = 10;

    /// <summary>The debounce interval in milliseconds for search input.</summary>
    [Parameter]
    public int DebounceInterval { get; set; } = 300;

    /// <summary>Whether the clear button is shown on the input.</summary>
    [Parameter]
    public bool Clearable { get; set; }

    /// <summary>Whether the component uses dense spacing.</summary>
    [Parameter]
    public bool Dense { get; set; }

    /// <summary>Whether the component is disabled.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Whether the component is read-only.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Whether a value is required.</summary>
    [Parameter]
    public bool Required { get; set; }

    /// <summary>The error message shown when the required value is missing.</summary>
    [Parameter]
    public string? RequiredError { get; set; }

    /// <summary>The custom template for rendering each item in the dropdown.</summary>
    [Parameter]
    public RenderFragment<T>? ItemTemplate { get; set; }

    /// <summary>
    /// Extracts a group label from each item. When set, search results are grouped by this label.
    /// Group headers appear as non-selectable rows above each group.
    /// </summary>
    [Parameter]
    public Func<T, string>? GroupFunc { get; set; }

    /// <summary>
    /// Custom group sort comparator. Receives two group label strings and returns a negative,
    /// zero, or positive integer. Defaults to alphabetical when <c>null</c>.
    /// </summary>
    [Parameter]
    public Func<string, string, int>? GroupSortFunc { get; set; }

    /// <summary>
    /// Column definitions for tabular dropdown display. When set (and <see cref="ItemTemplate"/> is null),
    /// each dropdown item is rendered as a row with cells for each column.
    /// </summary>
    [Parameter]
    public IReadOnlyList<ComboBoxColumn<T>>? Columns { get; set; }

    // Maps the first item in each group to its group label, for group header rendering.
    // Stored as a list of pairs to avoid the Dictionary<T,V> notnull constraint on T.
    private readonly List<(T Item, string Header)> _itemGroupHeaders = [];

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && Clearable)
        {
            _a11yModule = await JSRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Dabbler.Components/dabbler-combobox-a11y.js");
            await _a11yModule.InvokeVoidAsync("labelClearButtons", _wrapperRef);
        }
    }

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        _searchHelper.UpdateSearchFunc(SearchFunc);
        // Only sync from the parameter when the parent has genuinely changed the list.
        // Without this guard, a parent re-render before ValuesChanged is processed
        // would wipe out any locally-pending selections.
        if (!_values.SequenceEqual(Values))
        {
            _values = Values.ToList();
            _selectedSet = new HashSet<T>(_values, EqualityComparer<T>.Default);
        }
    }

    // ─── Computed helpers ─────────────────────────────────────────────────────

    private bool _maxSelectionsReached =>
        MaxSelections.HasValue && _values.Count >= MaxSelections.Value;

    private string GetDisplayText(T? value) =>
        value is null ? "" : (ToStringFunc?.Invoke(value) ?? value?.ToString() ?? "");

    private string GetAddNewPromptText() =>
        string.Format(AddNewPromptText ?? "Add '{0}'…", _searchHelper.CurrentSearchText);

    private bool IsSelected(T item) => _selectedSet.Contains(item);

    // ─── Event handlers ───────────────────────────────────────────────────────

    private async Task HandleItemSelectedAsync(T? item)
    {
        // MudAutocomplete fires with the selected item; reset pending value immediately.
        _pendingValue = default;

        if (item is null || _maxSelectionsReached) return;

        // Don't add duplicates.
        if (IsSelected(item)) return;

        _values.Add(item);
        _selectedSet.Add(item);
        await ValuesChanged.InvokeAsync(_values.AsReadOnly());

        // Re-open dropdown so user can continue selecting.
        StateHasChanged();
        if (_autocomplete is not null)
        {
            await _autocomplete.ClearAsync();
            await _autocomplete.FocusAsync();
        }
    }

    private async Task RemoveItemAsync(T item)
    {
        _values.RemoveAll(v => EqualityComparer<T>.Default.Equals(v, item));
        _selectedSet.Remove(item);
        await ValuesChanged.InvokeAsync(_values.AsReadOnly());
        StateHasChanged();
    }

    private async Task HandleAddNewClickAsync()
    {
        var enteredText = _searchHelper.CurrentSearchText ?? "";

        if (OnAddNewItem is not null)
        {
            var newItem = await OnAddNewItem(enteredText);
            if (newItem is not null && !IsSelected(newItem) && !_maxSelectionsReached)
            {
                await HandleItemSelectedAsync(newItem);
                if (SearchFunc is not null)
                    await SearchFunc(GetDisplayText(newItem), CancellationToken.None);
            }
        }
        else if (AddNewTemplate is not null)
        {
            _addNewContext = new AddNewItemContext<T>
            {
                EnteredText = enteredText,
                CompleteAsync = async newItem =>
                {
                    _showAddNewDialog = false;
                    _addNewContext = null;
                    if (!IsSelected(newItem) && !_maxSelectionsReached)
                    {
                        _values.Add(newItem);
                        _selectedSet.Add(newItem);
                        await ValuesChanged.InvokeAsync(_values.AsReadOnly());
                        if (SearchFunc is not null)
                            await SearchFunc(GetDisplayText(newItem), CancellationToken.None);
                    }
                    StateHasChanged();
                },
                CancelAsync = () =>
                {
                    _showAddNewDialog = false;
                    _addNewContext = null;
                    StateHasChanged();
                    return Task.CompletedTask;
                }
            };
            _showAddNewDialog = true;
        }
    }

    // ─── Grouping & Column helpers ────────────────────────────────────────────

    /// <summary>
    /// Wraps the search helper to apply optional grouping after results are fetched.
    /// Populates <see cref="_itemGroupHeaders"/> so the item template can render headers.
    /// </summary>
    private async Task<IEnumerable<T>> SearchWithGroupingAsync(string? text, CancellationToken ct)
    {
        var results = (await _searchHelper.SearchAsync(text, ct)).ToList();
        _itemGroupHeaders.Clear();

        if (GroupFunc is null) return results;

        IComparer<string> comparer = GroupSortFunc is not null
            ? Comparer<string>.Create((a, b) => GroupSortFunc(a, b))
            : StringComparer.Ordinal;

        var flattened = new List<T>();
        foreach (var group in results.GroupBy(GroupFunc).OrderBy(g => g.Key, comparer))
        {
            bool isFirst = true;
            foreach (var item in group.OrderBy(i => GetDisplayText(i)))
            {
                if (isFirst) { _itemGroupHeaders.Add((item, group.Key)); isFirst = false; }
                flattened.Add(item);
            }
        }
        return flattened;
    }

    /// <summary>Returns <c>true</c> when any column has a header text set.</summary>
    private bool HasColumnHeaders =>
        Columns is not null && Columns.Any(c => c.Header is not null);

    /// <summary>
    /// Builds the effective item template, incorporating group headers, column layout,
    /// and selected-item checkmarks. The consumer's <see cref="ItemTemplate"/> takes priority.
    /// </summary>
    internal RenderFragment<T> BuildItemTemplate()
    {
        if (ItemTemplate is not null) return ItemTemplate;

        return item => builder =>
        {
            int seq = 0;
            bool selected = IsSelected(item);

            // Render group header above the first item in each group.
            var headerEntry = _itemGroupHeaders.FirstOrDefault(e => EqualityComparer<T>.Default.Equals(e.Item, item));
            if (headerEntry.Header is not null)
            {
                builder.OpenElement(seq++, "div");
                builder.AddAttribute(seq++, "style",
                    "padding: 2px 0 4px; font-weight: 600; font-size: 0.75rem; text-transform: uppercase; " +
                    "letter-spacing: 0.05em; color: rgba(0,0,0,0.54); pointer-events: none; line-height: 1.2;");
                builder.AddAttribute(seq++, "role", "presentation");
                builder.AddAttribute(seq++, "aria-label", headerEntry.Header);
                builder.AddContent(seq++, headerEntry.Header);
                builder.CloseElement();
            }

            // Row: item content + optional checkmark for selected items.
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "style",
                "display: flex; align-items: center; justify-content: space-between; width: 100%;");

            if (Columns is { Count: > 0 })
            {
                // Multi-column row.
                builder.OpenElement(seq++, "div");
                builder.AddAttribute(seq++, "style", "display: flex; gap: 8px; flex: 1; min-width: 0;");
                foreach (var col in Columns)
                {
                    var width = col.Width ?? $"{100.0 / Columns.Count:0.##}%";
                    builder.OpenElement(seq++, "div");
                    builder.AddAttribute(seq++, "style",
                        $"flex: 0 0 {width}; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;");
                    builder.AddContent(seq++, col.ValueFunc?.Invoke(item) ?? "");
                    builder.CloseElement();
                }
                builder.CloseElement();
            }
            else
            {
                // Plain text.
                builder.OpenComponent<MudText>(seq++);
                builder.AddAttribute(seq++, "Typo", Typo.body1);
                builder.AddAttribute(seq++, "ChildContent",
                    (RenderFragment)(__b => __b.AddContent(0, GetDisplayText(item))));
                builder.CloseComponent();
            }

            // Checkmark for already-selected items.
            if (selected)
            {
                builder.OpenComponent<MudIcon>(seq++);
                builder.AddAttribute(seq++, "Icon", Icons.Material.Filled.Check);
                builder.AddAttribute(seq++, "Size", Size.Small);
                builder.AddAttribute(seq++, "Color", Color.Primary);
                builder.CloseComponent();
            }

            builder.CloseElement(); // row div
        };
    }
}
