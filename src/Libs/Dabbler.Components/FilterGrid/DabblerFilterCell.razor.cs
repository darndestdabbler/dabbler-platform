// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using Dabbler.Components.Data;
using Microsoft.AspNetCore.Components;

namespace Dabbler.Components;

/// <summary>
/// A single filter cell containing an optional operator button and a value editor.
/// </summary>
public partial class DabblerFilterCell : ComponentBase
{
    private string _currentOperator = FilterOperators.Contains;
    private object? _value;
    private DatePreset? _activeDatePreset;
    private bool _isDirty;

    /// <summary>
    /// The column definition for this cell.
    /// </summary>
    [Parameter, EditorRequired]
    public FilterColumnDefinition Column { get; set; } = null!;

    /// <summary>
    /// The index of the filter group this cell belongs to.
    /// </summary>
    [Parameter]
    public int GroupIndex { get; set; }

    /// <summary>
    /// The coordinator to notify on filter changes.
    /// </summary>
    [Parameter]
    public FilteringCoordinator? Coordinator { get; set; }

    /// <summary>
    /// When true, validation errors are shown even if the user has not yet interacted with
    /// this cell. Set to true by <c>DabblerFilterGrid.Validate()</c>.
    /// </summary>
    [Parameter]
    public bool ShowValidation { get; set; }

    /// <summary>
    /// CSS style for the cell container.
    /// Width is managed by the parent grid row's CSS grid-template-columns,
    /// so we only set min-width here as a fallback.
    /// </summary>
    private string? CellStyle => null;

    /// <summary>
    /// Accessible label for filter input fields, combining column header with "filter".
    /// </summary>
    private string FilterLabel => $"{Column.Header ?? Column.PropertyName} filter";

    /// <summary>
    /// Unique ID for the validation error element, used for aria-describedby linkage.
    /// </summary>
    private string FilterErrorId => $"filter-err-{GroupIndex}-{Column.PropertyName}";

    /// <summary>
    /// Additional ARIA attributes for filter input fields (required, invalid, describedby).
    /// </summary>
    private Dictionary<string, object> FilterInputAttributes
    {
        get
        {
            var attrs = new Dictionary<string, object>
            {
                ["aria-label"] = FilterLabel
            };
            if (Column.IsRequired)
                attrs["aria-required"] = "true";
            if (ValidationError is not null)
            {
                attrs["aria-invalid"] = "true";
                attrs["aria-describedby"] = FilterErrorId;
            }
            return attrs;
        }
    }

    /// <summary>
    /// Returns the first failing validation rule's error message, or null when valid.
    /// Errors are surfaced only after the user has interacted with the cell or
    /// <see cref="ShowValidation"/> is true.
    /// </summary>
    private string? ValidationError
    {
        get
        {
            // Show validation errors when the user has interacted (_isDirty), when
            // forced by the grid (ShowValidation), or when a value is present (e.g.,
            // restored from a preset). This keeps inline cell styling consistent with
            // the grid-level expression line which checks coordinator entries.
            if (!_isDirty && !ShowValidation && _value is null) return null;
            if (Column.ValidationRules is null) return null;

            foreach (var rule in Column.ValidationRules)
            {
                if (rule.ValidateFunc is not null && !rule.ValidateFunc(_value, Column.PropertyName))
                    return rule.ErrorMessage ?? "Invalid value.";
            }
            return null;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _currentOperator = Column.EffectiveDefaultOperator;

        // Restore value from coordinator if available
        if (Coordinator is not null)
        {
            var group = Coordinator.Groups.Groups.ElementAtOrDefault(GroupIndex);
            var entry = group?.GetFilter(Column.PropertyName);
            if (entry is not null)
            {
                _currentOperator = entry.Operator;
                _value = entry.Value;

                // Check if this has a date range (indicates preset was used)
                if (group!.HasDateRange(Column.PropertyName))
                {
                    // Preset is active — but we don't know which one from the coordinator alone
                    // The _activeDatePreset field preserves the selection locally
                }
            }
            else
            {
                // No filter entry — clear preset state if the coordinator cleared us (cascading)
                _value = null;
                _activeDatePreset = null;
                _isDirty = false;
            }
        }

        if (Column.DefaultValue is not null && _value is null)
        {
            _value = Column.DefaultValue;
        }
    }

    private async Task OnOperatorChanged(string newOp)
    {
        _currentOperator = newOp;
        _activeDatePreset = null; // Clear preset when switching to a standard operator
        ApplyFilter();
        await Task.CompletedTask;
    }

    private void OnTextChanged(string? value)
    {
        _value = value;
        _isDirty = true;
        ApplyFilter();
    }

    private void OnNumericChanged(decimal? value)
    {
        _value = value;
        _isDirty = true;
        ApplyFilter();
    }

    private void OnCheckBoxChanged(bool? value)
    {
        _value = value;
        _isDirty = true;
        ApplyFilter();
    }

    private void OnDateChanged(DateTime? value)
    {
        _value = value;
        _isDirty = true;
        ApplyFilter();
    }

    private void OnSelectChanged(object? value)
    {
        _value = value;
        _isDirty = true;
        ApplyFilter();
    }

    private void OnDatePresetSelected(DatePreset preset)
    {
        _activeDatePreset = preset;
        Coordinator?.ApplyDatePreset(GroupIndex, Column.PropertyName, preset);
    }

    private void OnDatePresetCleared()
    {
        _activeDatePreset = null;
        _value = null;
        Coordinator?.ClearFilter(Column.PropertyName);
    }

    private void ApplyFilter()
    {
        Coordinator?.SetFilter(GroupIndex, Column.PropertyName, _currentOperator, _value);
    }

    private FilterCellContext BuildCellContext() => new()
    {
        Value = _value,
        ValueChanged = EventCallback.Factory.Create<object?>(this, OnCustomValueChanged),
        Operator = _currentOperator,
        OperatorChanged = EventCallback.Factory.Create<string>(this, OnCustomOperatorChanged),
        Column = Column,
        GroupIndex = GroupIndex,
        SerializeValue = Column.SerializeValue,
        DeserializeValue = Column.DeserializeValue,
    };

    private void OnCustomValueChanged(object? value)
    {
        _value = value;
        _isDirty = true;
        ApplyFilter();
    }

    private void OnCustomOperatorChanged(string op)
    {
        _currentOperator = op;
        ApplyFilter();
    }

    private decimal? GetDecimalValue() => _value switch
    {
        decimal d => d,
        IConvertible c => Convert.ToDecimal(c),
        _ => null
    };

    private bool? GetBoolValue() => _value as bool?;

    private DateTime? GetDateValue() => _value as DateTime?;
}
