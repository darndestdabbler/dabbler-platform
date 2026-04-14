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

    /// <summary>
    /// Builds the effective item template, incorporating column layout when configured.
    /// The consumer's <see cref="ItemTemplate"/> takes priority if set.
    /// </summary>
    private RenderFragment<T> BuildItemTemplate()
    {
        if (ItemTemplate is not null) return ItemTemplate;

        return item => builder =>
        {
            int seq = 0;

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
