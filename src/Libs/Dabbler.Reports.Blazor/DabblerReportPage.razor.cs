// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using Dabbler.Reports.Core.Data;
using Dabbler.Reports.Core.Layout;
using Dabbler.Reports.Core.Parameters;
using Dabbler.Reports.Core.Rdf;
using Dabbler.Reports.Blazor;
using Dabbler.Reports.Export;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Dabbler.Components;

/// <summary>
/// A page-level component that orchestrates the full report lifecycle:
/// RDF loading, parameter prompting, report execution, page composition,
/// viewing, and PDF export.
/// </summary>
public partial class DabblerReportPage : DabblerComponentBase
{
    private ParameterPromptForm? _parameterForm;
    private ReportViewer? _viewer;
    private ReportDefinition? _reportDefinition;
    private IReadOnlyList<RdfValidationError>? _rdfErrors;
    private IReadOnlyList<PageLayout>? _pages;
    private bool _isLoading;
    private bool _hasRun;
    private double _scale;
    private bool _parametersCollapsed;
    private string? _parameterSummary;
    private Exception? _error;
    private string? _statusText;
    private IJSObjectReference? _jsModule;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    // ── Required parameters ────────────────────────────────────────────

    /// <summary>
    /// Path or resource name for the <c>.rdf.json</c> report definition.
    /// </summary>
    [Parameter, EditorRequired]
    public string RdfPath { get; set; } = default!;

    /// <summary>
    /// Data source for the report query.
    /// </summary>
    [Parameter, EditorRequired]
    public IReportDataProvider DataProvider { get; set; } = default!;

    // ── Optional parameters ────────────────────────────────────────────

    /// <summary>
    /// Custom RDF loader function. Defaults to reading from the file system.
    /// </summary>
    [Parameter]
    public Func<string, Task<string>>? RdfLoader { get; set; }

    /// <summary>
    /// Loader for subreport RDF definitions (synchronous, as required by <see cref="ReportDataEngine"/>).
    /// </summary>
    [Parameter]
    public Func<string, string>? SubreportRdfLoader { get; set; }

    /// <summary>
    /// Factory that returns an <see cref="IReportDataProvider"/> for a given connection name.
    /// Used when the report contains subreport elements.
    /// </summary>
    [Parameter]
    public Func<string, IReportDataProvider>? SubreportProviderFactory { get; set; }

    /// <summary>
    /// Page title. Defaults to the RDF metadata name.
    /// </summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>
    /// Pre-filled parameter values.
    /// </summary>
    [Parameter]
    public Dictionary<string, object?>? InitialParameterValues { get; set; }

    /// <summary>
    /// If true and all required parameters have defaults, run immediately on load.
    /// </summary>
    [Parameter]
    public bool AutoRunOnLoad { get; set; }

    /// <summary>
    /// Collapse the parameter form after the first successful run.
    /// </summary>
    [Parameter]
    public bool CollapseParametersAfterRun { get; set; } = true;

    /// <summary>
    /// Show PDF download button.
    /// </summary>
    [Parameter]
    public bool ShowPdfExport { get; set; } = true;

    /// <summary>
    /// Show print button.
    /// </summary>
    [Parameter]
    public bool ShowPrint { get; set; } = true;

    /// <summary>
    /// Show zoom controls.
    /// </summary>
    [Parameter]
    public bool ShowZoom { get; set; } = true;

    /// <summary>
    /// Initial zoom scale for the viewer.
    /// </summary>
    [Parameter]
    public double DefaultScale { get; set; } = 1.0;

    /// <summary>
    /// Download filename for PDF export. Defaults to <c>"{ReportName}.pdf"</c>.
    /// </summary>
    [Parameter]
    public string? PdfFileName { get; set; }

    /// <summary>
    /// Optional filter summary text to inject into the report header.
    /// </summary>
    [Parameter]
    public string? FilterSummary { get; set; }

    /// <summary>
    /// Custom content above the parameter form.
    /// </summary>
    [Parameter]
    public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>
    /// Additional toolbar buttons.
    /// </summary>
    [Parameter]
    public RenderFragment? ToolbarTemplate { get; set; }

    /// <summary>
    /// Custom content below the viewer.
    /// </summary>
    [Parameter]
    public RenderFragment? FooterTemplate { get; set; }

    /// <summary>
    /// Content shown before the report has been run. Defaults to an instructional message.
    /// </summary>
    [Parameter]
    public RenderFragment? EmptyStateTemplate { get; set; }

    /// <summary>
    /// Raised after successful page composition.
    /// </summary>
    [Parameter]
    public EventCallback<IReadOnlyList<PageLayout>> OnReportComposed { get; set; }

    /// <summary>
    /// Raised on RDF parse, query, or composition errors.
    /// </summary>
    [Parameter]
    public EventCallback<Exception> OnError { get; set; }

    // ── Public members ─────────────────────────────────────────────────

    /// <summary>
    /// Reference to the parameter form.
    /// </summary>
    public ParameterPromptForm? ParameterForm => _parameterForm;

    /// <summary>
    /// Reference to the report viewer.
    /// </summary>
    public ReportViewer? Viewer => _viewer;

    /// <summary>
    /// The parsed RDF, available after first load.
    /// </summary>
    public ReportDefinition? ReportDefinition => _reportDefinition;

    /// <summary>
    /// Composed pages, available after run.
    /// </summary>
    public IReadOnlyList<PageLayout>? Pages => _pages;

    /// <summary>
    /// True during RDF load, query execution, or page composition.
    /// </summary>
    public bool IsLoading => _isLoading;

    /// <summary>
    /// True after at least one successful composition.
    /// </summary>
    public bool HasRun => _hasRun;

    /// <summary>
    /// Programmatic report execution using current parameter values.
    /// </summary>
    public async Task RunReportAsync()
    {
        var paramValues = GetCurrentParameterValues();
        await RunReportCoreAsync(paramValues);
    }

    /// <summary>
    /// Execute with explicit parameters.
    /// </summary>
    /// <param name="parameters">The parameter values to use.</param>
    public async Task RunReportAsync(Dictionary<string, object?> parameters)
    {
        await RunReportCoreAsync(parameters);
    }

    /// <summary>
    /// Programmatic PDF export and download.
    /// </summary>
    public async Task ExportToPdfAsync()
    {
        if (_reportDefinition is null || _pages is null || _pages.Count == 0 || _jsModule is null)
            return;

        try
        {
            var exporter = new PdfExporter(_reportDefinition);
            using var ms = new MemoryStream();
            await exporter.ExportAsync(_pages, ms);

            var base64 = Convert.ToBase64String(ms.ToArray());
            var fileName = PdfFileName
                ?? $"{(_reportDefinition.Metadata.Name is { Length: > 0 } name ? name : "report")}.pdf";

            await _jsModule.InvokeVoidAsync("downloadFile", fileName, base64, "application/pdf");
        }
        catch (Exception ex)
        {
            _error = ex;
            if (OnError.HasDelegate)
                await OnError.InvokeAsync(ex);
            StateHasChanged();
        }
    }

    // ── Lifecycle ──────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        _scale = DefaultScale;
        await LoadRdfAsync();

        if (_reportDefinition is not null && AutoRunOnLoad)
        {
            bool allRequiredHaveValues = _reportDefinition.Parameters
                .Where(p => p.IsRequired)
                .All(p =>
                    p.DefaultValue is not null
                    || (InitialParameterValues?.ContainsKey(p.Name) == true));

            if (allRequiredHaveValues || _reportDefinition.Parameters.Count == 0)
            {
                var paramValues = BuildInitialParameterValues();
                await RunReportCoreAsync(paramValues);
            }
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Dabbler.Components/dabbler-download.js");
        }

        base.OnAfterRender(firstRender);
    }

    // ── Private methods ────────────────────────────────────────────────

    private async Task LoadRdfAsync()
    {
        try
        {
            string json;
            if (RdfLoader is not null)
            {
                json = await RdfLoader(RdfPath);
            }
            else
            {
                json = await File.ReadAllTextAsync(RdfPath);
            }

            var parser = new RdfParser();
            var result = parser.TryParse(json);

            if (result.IsValid)
            {
                _reportDefinition = result.Definition;
            }
            else
            {
                _rdfErrors = result.Errors;
            }
        }
        catch (Exception ex)
        {
            _error = ex;
            if (OnError.HasDelegate)
                await OnError.InvokeAsync(ex);
        }
    }

    private async Task OnRunReportAsync(Dictionary<string, object?> parameterValues)
    {
        await RunReportCoreAsync(parameterValues);
    }

    private async Task RunReportCoreAsync(Dictionary<string, object?> parameterValues)
    {
        if (_reportDefinition is null) return;

        _isLoading = true;
        _error = null;
        StateHasChanged();

        try
        {
            // Inject filter summary if provided
            if (FilterSummary is not null && !parameterValues.ContainsKey("FilterSummary"))
            {
                parameterValues["FilterSummary"] = FilterSummary;
            }

            var engine = new ReportDataEngine(DataProvider);

            // Get the primary data source
            var dataSource = _reportDefinition.DataSources.FirstOrDefault()
                ?? throw new InvalidOperationException(
                    "Report definition does not contain a data source.");

            // Build parameter values for query execution
            var queryParams = parameterValues
                .Select(kv => new Dabbler.Reports.Core.Data.ParameterValue(kv.Key, kv.Value))
                .ToList();

            // Execute the report query
            var data = await engine.ExecuteReportQueryAsync(dataSource, queryParams);

            // Compose report pages
            _pages = await engine.ComposeReportAsync(
                _reportDefinition,
                data,
                parameterValues,
                SubreportRdfLoader,
                SubreportProviderFactory,
                fallbackResolver: null,
                textMeasurer: null);

            _hasRun = true;
            UpdateStatusText();

            // Collapse parameters if configured
            if (CollapseParametersAfterRun && _reportDefinition.Parameters.Count > 0)
            {
                _parametersCollapsed = true;
                _parameterSummary = BuildParameterSummary(parameterValues);
            }

            if (OnReportComposed.HasDelegate)
                await OnReportComposed.InvokeAsync(_pages);
        }
        catch (Exception ex)
        {
            _error = ex;
            if (OnError.HasDelegate)
                await OnError.InvokeAsync(ex);
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private Dictionary<string, object?> GetCurrentParameterValues()
    {
        if (_parameterForm is not null)
        {
            var current = _parameterForm.GetCurrentValues();
            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in current)
                result[kv.Key] = kv.Value;
            return result;
        }

        return BuildInitialParameterValues();
    }

    private Dictionary<string, object?> BuildInitialParameterValues()
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (_reportDefinition?.Parameters is not null)
        {
            foreach (var param in _reportDefinition.Parameters)
            {
                if (InitialParameterValues is not null
                    && InitialParameterValues.TryGetValue(param.Name, out var initial))
                {
                    values[param.Name] = initial;
                }
                else
                {
                    values[param.Name] = DefaultValueResolver.Resolve(
                        param.DefaultValue, param.DataType);
                }
            }
        }

        return values;
    }

    private string BuildParameterSummary(Dictionary<string, object?> parameterValues)
    {
        if (_reportDefinition?.Parameters is null)
            return string.Empty;

        var parts = new List<string>();
        foreach (var param in _reportDefinition.Parameters.OrderBy(p => p.SortOrder))
        {
            if (parameterValues.TryGetValue(param.Name, out var value) && value is not null)
            {
                var displayValue = value switch
                {
                    DateTime dt => dt.ToShortDateString(),
                    _ => value.ToString() ?? string.Empty,
                };

                if (displayValue.Length > 0)
                    parts.Add($"{param.PromptText}: {displayValue}");
            }
        }

        return parts.Count > 0
            ? string.Join(", ", parts)
            : "No parameters specified";
    }

    private void UpdateStatusText()
    {
        if (_pages is not null && _pages.Count > 0)
        {
            _statusText = _pages.Count == 1
                ? "1 page"
                : $"{_pages.Count} pages";
        }
        else
        {
            _statusText = null;
        }
    }

    private Task ExpandParametersAsync()
    {
        _parametersCollapsed = false;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task DismissErrorAsync()
    {
        _error = null;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task ZoomInAsync()
    {
        _scale = Math.Min(_scale + 0.25, 3.0);
        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task ZoomOutAsync()
    {
        _scale = Math.Max(_scale - 0.25, 0.25);
        StateHasChanged();
        return Task.CompletedTask;
    }

    private async Task PrintAsync()
    {
        if (IsJSRuntimeAvailable)
        {
            await JSRuntime.InvokeVoidAsync("window.print");
        }
    }

    private string ZoomPercentText => $"{(int)(_scale * 100)}%";

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        if (_jsModule is not null)
        {
            await _jsModule.DisposeAsync();
        }

        await base.DisposeAsyncCore();
    }
}
