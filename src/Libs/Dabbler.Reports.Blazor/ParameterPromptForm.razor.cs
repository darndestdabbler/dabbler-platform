using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using Dabbler.Reports.Core.Data;
using Dabbler.Reports.Core.Parameters;
using Dabbler.Reports.Core.Rdf;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using MudBlazor;

namespace Dabbler.Reports.Blazor;

/// <summary>
/// Renders a dynamic form for report parameter entry, supporting text, date, combo box,
/// multi-select, checkbox, and numeric controls with cascading dependency resolution.
/// </summary>
public partial class ParameterPromptForm : ComponentBase
{
    private MudForm? _form;
    private bool _isFormValid;
    private readonly Dictionary<string, object?> _values = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<LookupItem>> _lookupItems = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> _dependents = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The parameter definitions to render.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<ParameterDefinition>? Parameters { get; set; }

    /// <summary>Data provider for executing lookup queries.</summary>
    [Parameter]
    public IReportDataProvider? DataProvider { get; set; }

    /// <summary>Form title.</summary>
    [Parameter]
    public string Title { get; set; } = "Report Parameters";

    /// <summary>Optional pre-filled values.</summary>
    [Parameter]
    public Dictionary<string, object?>? InitialValues { get; set; }

    /// <summary>Fires when the user clicks Run Report with valid values.</summary>
    [Parameter]
    public EventCallback<Dictionary<string, object?>> OnRunReport { get; set; }

    /// <summary>Shows a loading indicator and disables the Run button.</summary>
    [Parameter]
    public bool IsLoading { get; set; }

    private bool _initialValidationScheduled;

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && _form is not null && !_initialValidationScheduled)
        {
            _initialValidationScheduled = true;
            // MudForm treats untouched required fields as valid until something
            // triggers validation. Force an initial pass so the Run Report button
            // starts disabled whenever required parameters are empty.
            await _form.Validate();
        }
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (Parameters is null) return;

        // Build dependency graph
        _dependents.Clear();
        foreach (var param in Parameters)
        {
            if (param.DependsOn is not null)
            {
                if (!_dependents.TryGetValue(param.DependsOn, out var list))
                {
                    list = [];
                    _dependents[param.DependsOn] = list;
                }
                if (!list.Contains(param.Name))
                    list.Add(param.Name);
            }
        }

        // Initialize values from InitialValues or DefaultValueResolver
        foreach (var param in Parameters)
        {
            if (!_values.ContainsKey(param.Name))
            {
                if (InitialValues is not null && InitialValues.TryGetValue(param.Name, out var initial))
                    _values[param.Name] = initial;
                else
                    _values[param.Name] = DefaultValueResolver.Resolve(param.DefaultValue, param.DataType);
            }
        }

        // Load lookups for ComboBox/MultiSelectList parameters
        foreach (var param in Parameters.Where(p =>
            p.ControlType is ParameterControlType.ComboBox or ParameterControlType.MultiSelectList
            && p.LookupQuery is not null
            && p.DependsOn is null))
        {
            await LoadLookupAsync(param);
        }

        // Load dependent lookups that already have parent values
        foreach (var param in Parameters.Where(p =>
            p.ControlType is ParameterControlType.ComboBox or ParameterControlType.MultiSelectList
            && p.LookupQuery is not null
            && p.DependsOn is not null))
        {
            if (_values.TryGetValue(param.DependsOn!, out var parentVal) && parentVal is not null)
            {
                await LoadLookupAsync(param);
            }
        }
    }

    private RenderFragment RenderParameterControl(ParameterDefinition param) => builder =>
    {
        switch (param.ControlType)
        {
            case ParameterControlType.TextBox:
                RenderTextBox(builder, param);
                break;
            case ParameterControlType.DatePicker:
                RenderDatePicker(builder, param);
                break;
            case ParameterControlType.ComboBox:
                RenderComboBox(builder, param);
                break;
            case ParameterControlType.MultiSelectList:
                RenderMultiSelect(builder, param);
                break;
            case ParameterControlType.CheckBox:
                RenderCheckBox(builder, param);
                break;
            case ParameterControlType.NumericUpDown:
                RenderNumericField(builder, param);
                break;
        }
    };

    private void RenderTextBox(RenderTreeBuilder builder, ParameterDefinition param)
    {
        builder.OpenComponent<MudTextField<string>>(0);
        builder.AddAttribute(1, "Label", param.PromptText);
        builder.AddAttribute(2, "Value", _values.GetValueOrDefault(param.Name)?.ToString() ?? "");
        builder.AddAttribute(3, "ValueChanged",
            EventCallback.Factory.Create<string>(this, val => OnValueChanged(param, val)));
        builder.AddAttribute(4, "Required", param.IsRequired);
        builder.AddAttribute(5, "RequiredError", $"{param.PromptText} is required");
        if (param.Validation?.Pattern is not null)
        {
            builder.AddAttribute(6, "Validation",
                new Func<string, string?>(val => ValidatePattern(val, param.Validation.Pattern)));
        }
        builder.AddAttribute(7, "Immediate", true);
        builder.AddAttribute(8, "UserAttributes", BuildAriaAttributes(param));
        builder.CloseComponent();
    }

    private void RenderDatePicker(RenderTreeBuilder builder, ParameterDefinition param)
    {
        var currentValue = _values.GetValueOrDefault(param.Name);
        DateTime? dateValue = currentValue is DateTime dt ? dt : null;

        builder.OpenComponent<MudDatePicker>(0);
        builder.AddAttribute(1, "Label", param.PromptText);
        builder.AddAttribute(2, "Date", dateValue);
        builder.AddAttribute(3, "DateChanged",
            EventCallback.Factory.Create<DateTime?>(this, val => OnValueChanged(param, val)));
        builder.AddAttribute(4, "Required", param.IsRequired);
        builder.AddAttribute(5, "RequiredError", $"{param.PromptText} is required");
        builder.AddAttribute(6, "Editable", true);
        builder.AddAttribute(7, "DateFormat", "MM/dd/yyyy");
        builder.AddAttribute(8, "Placeholder", "MM/DD/YYYY");
        builder.AddAttribute(9, "UserAttributes", BuildAriaAttributes(param));
        builder.CloseComponent();
    }

    private void RenderComboBox(RenderTreeBuilder builder, ParameterDefinition param)
    {
        var items = _lookupItems.GetValueOrDefault(param.Name) ?? [];
        var currentValue = _values.GetValueOrDefault(param.Name)?.ToString() ?? "";

        builder.OpenComponent<MudSelect<string>>(0);
        builder.AddAttribute(1, "Label", param.PromptText);
        builder.AddAttribute(2, "Value", currentValue);
        builder.AddAttribute(3, "ValueChanged",
            EventCallback.Factory.Create<string>(this, val => OnComboValueChanged(param, val)));
        builder.AddAttribute(4, "Required", param.IsRequired);
        builder.AddAttribute(5, "RequiredError", $"{param.PromptText} is required");
        builder.AddAttribute(6, "ChildContent", (RenderFragment)(itemBuilder =>
        {
            foreach (var item in items)
            {
                itemBuilder.OpenComponent<MudSelectItem<string>>(0);
                itemBuilder.AddAttribute(1, "Value", item.Value);
                itemBuilder.AddContent(2, item.Display);
                itemBuilder.CloseComponent();
            }
        }));
        builder.AddAttribute(7, "UserAttributes", BuildAriaAttributes(param));
        builder.CloseComponent();
    }

    private void RenderMultiSelect(RenderTreeBuilder builder, ParameterDefinition param)
    {
        var items = _lookupItems.GetValueOrDefault(param.Name) ?? [];
        var currentValues = _values.GetValueOrDefault(param.Name) as IEnumerable<string>
                            ?? Array.Empty<string>();

        builder.OpenComponent<MudSelect<string>>(0);
        builder.AddAttribute(1, "Label", param.PromptText);
        builder.AddAttribute(2, "MultiSelection", true);
        builder.AddAttribute(3, "SelectedValues", currentValues);
        builder.AddAttribute(4, "SelectedValuesChanged",
            EventCallback.Factory.Create<IEnumerable<string>>(this, val => OnValueChanged(param, val?.ToList())));
        builder.AddAttribute(5, "Required", param.IsRequired);
        builder.AddAttribute(6, "RequiredError", $"{param.PromptText} is required");
        builder.AddAttribute(7, "ChildContent", (RenderFragment)(itemBuilder =>
        {
            foreach (var item in items)
            {
                itemBuilder.OpenComponent<MudSelectItem<string>>(0);
                itemBuilder.AddAttribute(1, "Value", item.Value);
                itemBuilder.AddContent(2, item.Display);
                itemBuilder.CloseComponent();
            }
        }));
        builder.AddAttribute(8, "UserAttributes", BuildAriaAttributes(param));
        builder.CloseComponent();
    }

    private void RenderCheckBox(RenderTreeBuilder builder, ParameterDefinition param)
    {
        var currentValue = _values.GetValueOrDefault(param.Name);
        bool isChecked = currentValue is bool b && b;

        builder.OpenComponent<MudCheckBox<bool>>(0);
        builder.AddAttribute(1, "Label", param.PromptText);
        builder.AddAttribute(2, "Value", isChecked);
        builder.AddAttribute(3, "ValueChanged",
            EventCallback.Factory.Create<bool>(this, val => OnValueChanged(param, val)));
        builder.CloseComponent();
    }

    private void RenderNumericField(RenderTreeBuilder builder, ParameterDefinition param)
    {
        var currentValue = _values.GetValueOrDefault(param.Name);
        decimal? numValue = currentValue switch
        {
            decimal d => d,
            int i => i,
            _ => null,
        };

        builder.OpenComponent<MudNumericField<decimal?>>(0);
        builder.AddAttribute(1, "Label", param.PromptText);
        builder.AddAttribute(2, "Value", numValue);
        builder.AddAttribute(3, "ValueChanged",
            EventCallback.Factory.Create<decimal?>(this, val => OnValueChanged(param, val)));
        builder.AddAttribute(4, "Required", param.IsRequired);
        builder.AddAttribute(5, "RequiredError", $"{param.PromptText} is required");
        if (param.Validation is not null)
        {
            if (decimal.TryParse(param.Validation.MinValue, CultureInfo.InvariantCulture, out var min))
                builder.AddAttribute(6, "Min", min);
            if (decimal.TryParse(param.Validation.MaxValue, CultureInfo.InvariantCulture, out var max))
                builder.AddAttribute(7, "Max", max);
        }
        builder.AddAttribute(8, "UserAttributes", BuildAriaAttributes(param));
        builder.CloseComponent();
    }

    private async Task OnValueChanged(ParameterDefinition param, object? newValue)
    {
        _values[param.Name] = newValue;

        // Handle cascading dependencies
        if (_dependents.TryGetValue(param.Name, out var dependentNames))
        {
            foreach (var depName in dependentNames)
            {
                _values[depName] = null;
                _lookupItems.Remove(depName);

                var depParam = Parameters?.FirstOrDefault(p =>
                    string.Equals(p.Name, depName, StringComparison.OrdinalIgnoreCase));

                if (depParam?.LookupQuery is not null && newValue is not null)
                {
                    await LoadLookupAsync(depParam);
                }
            }
        }

        StateHasChanged();
    }

    private async Task OnComboValueChanged(ParameterDefinition param, string? newValue)
    {
        var typedValue = ParameterTypeConverter.ConvertToType(newValue, param.DataType);
        await OnValueChanged(param, typedValue ?? newValue);
    }

    private async Task LoadLookupAsync(ParameterDefinition param)
    {
        if (DataProvider is null || param.LookupQuery is null) return;

        try
        {
            // Build parameters for cascading lookups
            Dictionary<string, object?>? queryParams = null;
            if (param.DependsOn is not null && _values.TryGetValue(param.DependsOn, out var parentValue))
            {
                queryParams = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    [param.DependsOn] = parentValue
                };
            }

            var data = await DataProvider.ExecuteLookupAsync(param.LookupQuery, queryParams);
            var items = new List<LookupItem>();

            string displayField = param.LookupDisplayField ?? data.Columns[0].ColumnName;
            string valueField = param.LookupValueField ?? displayField;

            foreach (DataRow row in data.Rows)
            {
                items.Add(new LookupItem(
                    row[displayField]?.ToString() ?? "",
                    row[valueField]?.ToString() ?? ""));
            }

            _lookupItems[param.Name] = items;
        }
        catch
        {
            _lookupItems[param.Name] = [];
        }
    }

    private async Task OnRunReportClicked()
    {
        if (_form is not null)
            await _form.Validate();

        if (!_isFormValid) return;

        // Convert all values to their declared types
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (Parameters is not null)
        {
            foreach (var param in Parameters)
            {
                var raw = _values.GetValueOrDefault(param.Name);
                result[param.Name] = ParameterTypeConverter.ConvertToType(raw, param.DataType);
            }
        }

        await OnRunReport.InvokeAsync(result);
    }

    private static Dictionary<string, object> BuildAriaAttributes(ParameterDefinition param)
    {
        var attrs = new Dictionary<string, object>();
        if (param.IsRequired)
            attrs["aria-required"] = "true";
        return attrs;
    }

    private static string? ValidatePattern(string? value, string pattern)
    {
        if (string.IsNullOrEmpty(value))
            return null; // Required validation handles empty
        return Regex.IsMatch(value, pattern) ? null : $"Value does not match required pattern";
    }

    /// <summary>
    /// Gets the current parameter values (for testing/programmatic access).
    /// </summary>
    public IReadOnlyDictionary<string, object?> GetCurrentValues() => _values;

    internal sealed record LookupItem(string Display, string Value);
}
