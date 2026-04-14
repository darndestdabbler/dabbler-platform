// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using Dabbler.Components.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Dabbler.Components;

/// <summary>
/// A combo box component that wraps <see cref="MudAutocomplete{T}"/> and adds
/// <see cref="OnValueNotInList"/> support for detecting user-typed text that
/// does not match any item in the list.
/// </summary>
/// <typeparam name="T">The type of item in the combo box.</typeparam>
/// <example>
/// <code>
/// &lt;DabblerComboBox T="string" @bind-Value="_selectedCity"
///                   Items="@_cities"
///                   Label="City"
///                   OnValueNotInList="HandleNewCity" /&gt;
/// </code>
/// </example>
public partial class DabblerComboBox<T> : DabblerComponentBase
{
    private MudAutocomplete<T>? _autocomplete;
    private ElementReference _wrapperRef;
    private IJSObjectReference? _a11yModule;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    /// <summary>
    /// The currently selected value.
    /// </summary>
    [Parameter]
    public T? Value { get; set; }

    /// <summary>
    /// Occurs when the selected value changes.
    /// </summary>
    [Parameter]
    public EventCallback<T?> ValueChanged { get; set; }

    /// <summary>
    /// The function that provides search results.
    /// </summary>
    [Parameter]
    public Func<string?, CancellationToken, Task<IEnumerable<T>>>? SearchFunc { get; set; }

    /// <summary>
    /// The function that converts an item to its display string.
    /// </summary>
    [Parameter]
    public Func<T?, string?>? ToStringFunc { get; set; }

    /// <summary>
    /// Called when the user enters text that does not match any selected item and leaves the control.
    /// Set <see cref="ValueNotInListEventArgs.Handled"/> to <c>true</c> to keep the entered text;
    /// otherwise the text reverts to the selected value's display text.
    /// </summary>
    [Parameter]
    public Func<ValueNotInListEventArgs, Task>? OnValueNotInList { get; set; }

    /// <summary>
    /// Occurs when the control loses focus.
    /// </summary>
    [Parameter]
    public EventCallback<FocusEventArgs> OnBlur { get; set; }

    /// <summary>
    /// The label text displayed above the input.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    /// The placeholder text displayed when the input is empty.
    /// </summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>
    /// The visual variant of the input.
    /// </summary>
    [Parameter]
    public Variant Variant { get; set; }

    /// <summary>
    /// The maximum number of items shown in the dropdown.
    /// </summary>
    [Parameter]
    public int MaxItems { get; set; } = 10;

    /// <summary>
    /// The debounce interval in milliseconds for search input.
    /// </summary>
    [Parameter]
    public int DebounceInterval { get; set; } = 300;

    /// <summary>
    /// Whether the clear button is shown.
    /// </summary>
    [Parameter]
    public bool Clearable { get; set; }

    /// <summary>
    /// Whether the component uses dense spacing.
    /// </summary>
    [Parameter]
    public bool Dense { get; set; }

    /// <summary>
    /// Whether the component is disabled.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Whether the component is read-only.
    /// </summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Whether a value is required.
    /// </summary>
    [Parameter]
    public bool Required { get; set; }

    /// <summary>
    /// The error message shown when the required value is missing.
    /// </summary>
    [Parameter]
    public string? RequiredError { get; set; }

    /// <summary>
    /// Whether the value resets when the text is cleared.
    /// </summary>
    [Parameter]
    public bool ResetValueOnEmptyText { get; set; }

    /// <summary>
    /// Occurs when the clear button is clicked.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClearButtonClick { get; set; }

    /// <summary>
    /// The custom template for rendering each item in the dropdown.
    /// </summary>
    [Parameter]
    public RenderFragment<T>? ItemTemplate { get; set; }

    /// <summary>
    /// When <c>true</c>, an "Add '{text}'…" option appears at the bottom of the dropdown
    /// when the typed text does not match the selected value. Use with <see cref="OnAddNewItem"/>
    /// (simple mode) or <see cref="AddNewTemplate"/> (dialog mode).
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
    /// and callbacks to complete or cancel the add-new flow. Rendered inline below the combo box.
    /// </summary>
    [Parameter]
    public RenderFragment<AddNewItemContext<T>>? AddNewTemplate { get; set; }

    /// <summary>
    /// Custom prompt text for the add-new dropdown option. Use <c>{0}</c> as a placeholder for
    /// the typed text. Defaults to <c>"Add '{0}'…"</c>.
    /// </summary>
    [Parameter]
    public string? AddNewPromptText { get; set; }

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

    // Shared search helper — tracks current search text and wraps SearchFunc.
    private readonly ComboBoxSearchHelper<T> _searchHelper = new();

    // ARIA attributes for the MudAutocomplete input (listbox popup role).
    private readonly Dictionary<string, object> _comboBoxAttributes = new()
    {
        ["aria-haspopup"] = "listbox"
    };

    // Maps the first item in each group to its group label, for group header rendering.
    // Stored as a list of pairs to avoid the Dictionary<T,V> notnull constraint on T.
    private readonly List<(T Item, string Header)> _itemGroupHeaders = [];

    // Controls whether the AddNewTemplate dialog panel is visible.
    private bool _showAddNewDialog;

    // Context passed to AddNewTemplate when dialog mode is active.
    private AddNewItemContext<T>? _addNewContext;

    /// <inheritdoc/>
    protected override void OnParametersSet() => _searchHelper.UpdateSearchFunc(SearchFunc);

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

    private string GetAddNewPromptText() =>
        string.Format(AddNewPromptText ?? "Add '{0}'…", _searchHelper.CurrentSearchText);

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

    /// <summary>
    /// Builds the effective item template, incorporating group headers and column layout.
    /// The consumer's <see cref="ItemTemplate"/> takes priority if set.
    /// </summary>
    private RenderFragment<T> BuildItemTemplate()
    {
        if (ItemTemplate is not null) return ItemTemplate;

        return item => builder =>
        {
            int seq = 0;

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

            if (Columns is { Count: > 0 })
            {
                // Multi-column row.
                builder.OpenElement(seq++, "div");
                builder.AddAttribute(seq++, "style", "display: flex; gap: 8px; width: 100%;");
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
                builder.AddContent(seq, GetDisplayText(item) ?? "");
            }
        };
    }

    /// <summary>
    /// Returns <c>true</c> when any column definition has a header text set.
    /// </summary>
    private bool HasColumnHeaders =>
        Columns is not null && Columns.Any(c => c.Header is not null);

    private async Task HandleAddNewClickAsync()
    {
        var enteredText = _searchHelper.CurrentSearchText ?? "";

        if (OnAddNewItem is not null)
        {
            var newItem = await OnAddNewItem(enteredText);
            if (newItem is not null)
            {
                await HandleValueChangedAsync(newItem);
                // Re-invoke search with the new item's text so any caches are warmed.
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
                    await HandleValueChangedAsync(newItem);
                    if (SearchFunc is not null)
                        await SearchFunc(GetDisplayText(newItem), CancellationToken.None);
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

    private string GetDisplayText(T? value) =>
        value is null ? string.Empty : (ToStringFunc?.Invoke(value) ?? value?.ToString() ?? string.Empty);

    private async Task HandleValueChangedAsync(T? value)
    {
        Value = value;
        await ValueChanged.InvokeAsync(value);
    }

    private async Task HandleBlurAsync(FocusEventArgs args)
    {
        var currentText = _autocomplete?.Text;
        var selectedText = GetDisplayText(Value);

        // Detect text that doesn't match the selected item
        if (ComboBoxSearchHelper<T>.IsNotInList(currentText, selectedText))
        {
            if (OnValueNotInList is not null)
            {
                var eventArgs = new ValueNotInListEventArgs(currentText!);
                await OnValueNotInList(eventArgs);

                if (!eventArgs.Handled)
                {
                    await RevertTextAsync();
                }
            }
            else
            {
                // No handler registered — revert to selected value's text
                await RevertTextAsync();
            }
        }

        if (OnBlur.HasDelegate)
        {
            await OnBlur.InvokeAsync(args);
        }
    }

    private async Task RevertTextAsync()
    {
        if (_autocomplete is not null && Value is not null)
        {
            // Re-select the current value to reset both text and internal state
            await _autocomplete.SelectOptionAsync(Value);
        }
        else if (_autocomplete is not null)
        {
            // No value selected — clear the input
            await _autocomplete.ClearAsync();
        }
    }
}
