// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using Dabbler.Components.Data;
using Dabbler.Reports.Core.Data;
using Dabbler.Reports.Core.Layout;
using Dabbler.Reports.Core.Parameters;
using Dabbler.Reports.Core.Rdf;
using Dabbler.Reports.Blazor;
using Dabbler.Reports.Export;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Dabbler.Components;

/// <summary>
/// A page-level component that combines grid and report views with a shared filter grid.
/// The user can toggle between a <see cref="DabblerDataGrid{T}"/> view and a
/// <see cref="ReportViewer"/> view, with filters driving both.
/// </summary>
/// <typeparam name="T">The row data type for the grid view.</typeparam>
[CascadingTypeParameter(nameof(T))]
public partial class DabblerQueryReportPage<T> : DabblerComponentBase
{
    private DabblerFilterGrid<T>? _filterGrid;
    private DabblerDataGrid<T>? _dataGrid;
    private MudDataGrid<T>? _mudDataGrid;
    private ReportViewer? _viewer;
    private ReportDefinition? _reportDefinition;

    private PageViewMode _currentView;
    private bool _isLoading;
    private Exception? _error;
    private string? _statusText;

    // Grid state
    private IReadOnlyList<T>? _items;
    private int _currentTotal;
    private int? _unfilteredTotal;
    private CompositeFilterDescriptor? _currentFilter;
    private CancellationTokenSource? _debounceCts;

    // Report state
    private IReadOnlyList<PageLayout>? _reportPages;
    private bool _reportHasRun;
    private double _scale;
    private bool _filtersChangedInReportMode;
    private IJSObjectReference? _jsModule;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    // ── Required parameters ────────────────────────────────────────────

    /// <summary>
    /// Shared filter column definitions used in both Grid and Report views.
    /// </summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<FilterColumnDefinition> FilterColumns { get; set; } = default!;

    /// <summary>
    /// Column markup rendered inside the <c>MudDataGrid</c>'s <c>Columns</c> section.
    /// </summary>
    [Parameter, EditorRequired]
    public RenderFragment GridColumns { get; set; } = default!;

    /// <summary>
    /// Data source for the grid view.
    /// </summary>
    [Parameter, EditorRequired]
    public IGridDataProvider<T> DataProvider { get; set; } = default!;

    /// <summary>
    /// Data source for the report view.
    /// </summary>
    [Parameter, EditorRequired]
    public IReportDataProvider ReportDataProvider { get; set; } = default!;

    /// <summary>
    /// Path or resource name for the <c>.rdf.json</c> report definition.
    /// </summary>
    [Parameter, EditorRequired]
    public string RdfPath { get; set; } = default!;

    // ── Optional parameters ────────────────────────────────────────────

    /// <summary>
    /// Custom RDF loader function. Defaults to reading from the file system.
    /// </summary>
    [Parameter]
    public Func<string, Task<string>>? RdfLoader { get; set; }

    /// <summary>
    /// Column metadata for clipboard and CSV export.
    /// </summary>
    [Parameter]
    public IReadOnlyList<GridColumnInfo>? GridColumnInfo { get; set; }

    /// <summary>
    /// Page title displayed in the header area.
    /// </summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>
    /// Initial view mode.
    /// </summary>
    [Parameter]
    public PageViewMode DefaultView { get; set; } = PageViewMode.Grid;

    /// <summary>
    /// Show the Grid/Report toggle buttons.
    /// </summary>
    [Parameter]
    public bool ShowViewToggle { get; set; } = true;

    /// <summary>
    /// Automatically compose the report when switching to report view.
    /// </summary>
    [Parameter]
    public bool AutoRunReportOnSwitch { get; set; } = true;

    /// <summary>
    /// Automatically pass <c>GetFilterSummary()</c> to the report header.
    /// </summary>
    [Parameter]
    public bool InjectFilterSummary { get; set; } = true;

    /// <summary>
    /// Layout mode for the filter grid.
    /// </summary>
    [Parameter]
    public FilterGridLayout FilterLayout { get; set; } = FilterGridLayout.Horizontal;

    /// <summary>
    /// Whether to show the filter chip status bar.
    /// </summary>
    [Parameter]
    public bool ShowFilterStatusBar { get; set; } = true;

    /// <summary>
    /// Display total/filtered record count in the toolbar (Grid mode).
    /// </summary>
    [Parameter]
    public bool ShowRecordCount { get; set; } = true;

    /// <summary>
    /// Show CSV export button.
    /// </summary>
    [Parameter]
    public bool ShowCsvExport { get; set; } = true;

    /// <summary>
    /// Show copy-all button.
    /// </summary>
    [Parameter]
    public bool ShowCopyToClipboard { get; set; } = true;

    /// <summary>
    /// Show PDF download button (Report mode).
    /// </summary>
    [Parameter]
    public bool ShowPdfExport { get; set; } = true;

    /// <summary>
    /// Show print button (Report mode).
    /// </summary>
    [Parameter]
    public bool ShowPrint { get; set; } = true;

    /// <summary>
    /// Show zoom controls (Report mode).
    /// </summary>
    [Parameter]
    public bool ShowZoom { get; set; } = true;

    /// <summary>
    /// Default filename for CSV downloads.
    /// </summary>
    [Parameter]
    public string CsvFileName { get; set; } = "export.csv";

    /// <summary>
    /// Download filename for PDF export.
    /// </summary>
    [Parameter]
    public string? PdfFileName { get; set; }

    /// <summary>
    /// Initial zoom scale for the report viewer.
    /// </summary>
    [Parameter]
    public double DefaultScale { get; set; } = 1.0;

    /// <summary>
    /// When true, automatically re-query when filters change in Grid mode.
    /// </summary>
    [Parameter]
    public bool AutoLoadOnFilterChange { get; set; } = true;

    /// <summary>
    /// Enable inline editing on the grid.
    /// </summary>
    [Parameter]
    public bool Editable { get; set; }

    /// <summary>
    /// Rows per page.
    /// </summary>
    [Parameter]
    public int PageSize { get; set; } = 25;

    /// <summary>
    /// Dense grid layout.
    /// </summary>
    [Parameter]
    public bool Dense { get; set; } = true;

    /// <summary>
    /// Custom content above the filter grid.
    /// </summary>
    [Parameter]
    public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>
    /// Additional toolbar buttons.
    /// </summary>
    [Parameter]
    public RenderFragment? ToolbarTemplate { get; set; }

    /// <summary>
    /// Custom content below the view area.
    /// </summary>
    [Parameter]
    public RenderFragment? FooterTemplate { get; set; }

    /// <summary>
    /// Content shown in Report view before the report has been run.
    /// </summary>
    [Parameter]
    public RenderFragment? EmptyStateTemplate { get; set; }

    /// <summary>
    /// Raised after grid data is fetched.
    /// </summary>
    [Parameter]
    public EventCallback<IReadOnlyList<T>> OnDataLoaded { get; set; }

    /// <summary>
    /// Raised after successful report composition.
    /// </summary>
    [Parameter]
    public EventCallback<IReadOnlyList<PageLayout>> OnReportComposed { get; set; }

    /// <summary>
    /// Raised on data provider or report errors.
    /// </summary>
    [Parameter]
    public EventCallback<Exception> OnError { get; set; }

    // ── Public members ─────────────────────────────────────────────────

    /// <summary>
    /// Reference to the filter grid.
    /// </summary>
    public DabblerFilterGrid<T>? FilterGrid => _filterGrid;

    /// <summary>
    /// Reference to the data grid (available in Grid view).
    /// </summary>
    public DabblerDataGrid<T>? DataGrid => _dataGrid;

    /// <summary>
    /// Reference to the report viewer (available in Report view).
    /// </summary>
    public ReportViewer? Viewer => _viewer;

    /// <summary>
    /// Current filtered/sorted result set.
    /// </summary>
    public IReadOnlyList<T>? Items => _items;

    /// <summary>
    /// Composed report pages, available after a report run.
    /// </summary>
    public IReadOnlyList<PageLayout>? ReportPages => _reportPages;

    /// <summary>
    /// Current view mode.
    /// </summary>
    public PageViewMode CurrentView => _currentView;

    /// <summary>
    /// True while a data fetch or report composition is in progress.
    /// </summary>
    public bool IsLoading => _isLoading;

    /// <summary>
    /// True after at least one successful report composition.
    /// </summary>
    public bool ReportHasRun => _reportHasRun;

    /// <summary>
    /// Re-executes the current grid query.
    /// </summary>
    public async Task RefreshGridAsync()
    {
        if (_mudDataGrid is not null)
        {
            await _mudDataGrid.ReloadServerData();
        }
    }

    /// <summary>
    /// Programmatic CSV export.
    /// </summary>
    public async Task ExportToCsvAsync()
    {
        if (_dataGrid is not null)
        {
            await _dataGrid.ExportToCsvDownloadAsync(CsvFileName);
        }
    }

    /// <summary>
    /// Programmatic copy-all.
    /// </summary>
    public async Task CopyToClipboardAsync()
    {
        if (_dataGrid is not null)
        {
            await _dataGrid.CopyToClipboardAsync();
        }
    }

    /// <summary>
    /// Programmatic report execution using current filter values as parameters.
    /// </summary>
    public async Task RunReportAsync()
    {
        await RunReportCoreAsync();
    }

    /// <summary>
    /// Programmatic PDF export and download.
    /// </summary>
    public async Task ExportToPdfAsync()
    {
        if (_reportDefinition is null || _reportPages is null || _reportPages.Count == 0 || _jsModule is null)
            return;

        try
        {
            var exporter = new PdfExporter(_reportDefinition);
            using var ms = new MemoryStream();
            await exporter.ExportAsync(_reportPages, ms);

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

    /// <summary>
    /// Switches the active view mode.
    /// </summary>
    public async Task SwitchViewAsync(PageViewMode mode)
    {
        if (_currentView == mode) return;

        _currentView = mode;
        _filtersChangedInReportMode = false;

        if (mode == PageViewMode.Report && AutoRunReportOnSwitch)
        {
            await EnsureRdfLoadedAsync();
            await RunReportCoreAsync();
        }
        else if (mode == PageViewMode.Grid)
        {
            UpdateGridStatusText();
        }

        StateHasChanged();
    }

    // ── Lifecycle ──────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _currentView = DefaultView;
        _scale = DefaultScale;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Dabbler.Components/dabbler-clipboard.js");
        }

        base.OnAfterRender(firstRender);
    }

    // ── Grid data methods ──────────────────────────────────────────────

    private async Task<GridData<T>> ServerDataFuncAsync(GridState<T> state)
    {
        _isLoading = true;
        _error = null;
        StateHasChanged();

        try
        {
            var request = new GridDataRequest
            {
                PageIndex = state.Page,
                PageSize = state.PageSize,
                CompositeFilter = _currentFilter,
                Sorts = state.SortDefinitions
                    .Select(s => new SortDescriptor(s.SortBy, s.Descending))
                    .ToList(),
            };

            var response = await DataProvider.FetchAsync(request);

            _items = response.Items;
            _currentTotal = response.TotalCount;

            if (_currentFilter is null || !HasActiveFilters(_currentFilter))
            {
                _unfilteredTotal = response.TotalCount;
            }

            UpdateGridStatusText();

            if (OnDataLoaded.HasDelegate)
            {
                await OnDataLoaded.InvokeAsync(_items);
            }

            return new GridData<T>
            {
                Items = response.Items,
                TotalItems = response.TotalCount,
            };
        }
        catch (Exception ex)
        {
            _error = ex;
            _items = [];
            _currentTotal = 0;
            UpdateGridStatusText();

            if (OnError.HasDelegate)
            {
                await OnError.InvokeAsync(ex);
            }

            return new GridData<T> { Items = [], TotalItems = 0 };
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    // ── Filter handling ────────────────────────────────────────────────

    private async Task OnFilterChangedAsync(CompositeFilterDescriptor? filter)
    {
        _currentFilter = filter;

        if (_currentView == PageViewMode.Grid)
        {
            if (!AutoLoadOnFilterChange) return;

            _debounceCts?.Cancel();
            _debounceCts = new CancellationTokenSource();

            try
            {
                await Task.Delay(200, _debounceCts.Token);
                await RefreshGridAsync();
            }
            catch (TaskCanceledException)
            {
                // Debounce cancelled by a newer filter change.
            }
        }
        else
        {
            // In Report mode, show the "filters changed" banner
            if (_reportHasRun)
            {
                _filtersChangedInReportMode = true;
                StateHasChanged();
            }
        }
    }

    private async Task SearchClickedAsync()
    {
        await RefreshGridAsync();
    }

    // ── Report methods ─────────────────────────────────────────────────

    private async Task EnsureRdfLoadedAsync()
    {
        if (_reportDefinition is not null) return;

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
                _error = new InvalidOperationException(
                    $"Report definition errors: {string.Join("; ", result.Errors.Select(e => e.Message))}");
                if (OnError.HasDelegate)
                    await OnError.InvokeAsync(_error);
            }
        }
        catch (Exception ex)
        {
            _error = ex;
            if (OnError.HasDelegate)
                await OnError.InvokeAsync(ex);
        }
    }

    private async Task RunReportCoreAsync()
    {
        if (_reportDefinition is null)
        {
            await EnsureRdfLoadedAsync();
            if (_reportDefinition is null) return;
        }

        _isLoading = true;
        _error = null;
        _filtersChangedInReportMode = false;
        StateHasChanged();

        try
        {
            var parameterValues = BuildReportParameters();

            var engine = new ReportDataEngine(ReportDataProvider);

            var dataSource = _reportDefinition.DataSources.FirstOrDefault()
                ?? throw new InvalidOperationException(
                    "Report definition does not contain a data source.");

            var queryParams = parameterValues
                .Select(kv => new Dabbler.Reports.Core.Data.ParameterValue(kv.Key, kv.Value))
                .ToList();

            var data = await engine.ExecuteReportQueryAsync(dataSource, queryParams);

            _reportPages = await engine.ComposeReportAsync(
                _reportDefinition,
                data,
                parameterValues,
                fallbackResolver: null,
                textMeasurer: null);

            _reportHasRun = true;
            UpdateReportStatusText();

            if (OnReportComposed.HasDelegate)
                await OnReportComposed.InvokeAsync(_reportPages);
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

    private Dictionary<string, object?> BuildReportParameters()
    {
        var parameterValues = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        // Map filter values to report parameters by matching PropertyName to ParameterDefinition.Name
        if (_reportDefinition?.Parameters is not null && _filterGrid is not null)
        {
            var filterState = _filterGrid.ExportFilter();

            foreach (var param in _reportDefinition.Parameters)
            {
                // Find a filter column whose PropertyName matches this parameter name (case-insensitive)
                var matchingColumn = FilterColumns.FirstOrDefault(
                    fc => string.Equals(fc.PropertyName, param.Name, StringComparison.OrdinalIgnoreCase));

                if (matchingColumn is not null)
                {
                    // Find the filter value from the exported filter state
                    var filterValue = GetFilterValueForProperty(filterState, matchingColumn.PropertyName);
                    if (filterValue is not null)
                    {
                        parameterValues[param.Name] = filterValue;
                    }
                    else
                    {
                        parameterValues[param.Name] = DefaultValueResolver.Resolve(
                            param.DefaultValue, param.DataType);
                    }
                }
                else
                {
                    parameterValues[param.Name] = DefaultValueResolver.Resolve(
                        param.DefaultValue, param.DataType);
                }
            }
        }

        // Inject filter summary if configured
        if (InjectFilterSummary && _filterGrid is not null)
        {
            var summary = _filterGrid.GetFilterSummary();
            if (!string.IsNullOrEmpty(summary))
            {
                parameterValues["FilterSummary"] = summary;
            }
        }

        return parameterValues;
    }

    private static object? GetFilterValueForProperty(FilterPresetState filterState, string propertyName)
    {
        foreach (var group in filterState.Groups)
        {
            // Entries is Dictionary<string, FilterPresetEntry> keyed by property name
            if (group.Entries.TryGetValue(propertyName, out var entry) && entry.ValueJson is not null)
            {
                return System.Text.Json.JsonSerializer.Deserialize<object>(entry.ValueJson);
            }
        }

        return null;
    }

    private async Task RerunReportAsync()
    {
        await RunReportCoreAsync();
    }

    // ── Status text ────────────────────────────────────────────────────

    private void UpdateGridStatusText()
    {
        if (!ShowRecordCount || _currentView != PageViewMode.Grid)
        {
            _statusText = null;
            return;
        }

        var hasFilters = _currentFilter is not null && HasActiveFilters(_currentFilter);

        if (hasFilters && _unfilteredTotal.HasValue && _unfilteredTotal.Value != _currentTotal)
        {
            _statusText = $"Showing {_currentTotal:N0} of {_unfilteredTotal.Value:N0} records";
        }
        else
        {
            _statusText = $"{_currentTotal:N0} records";
        }
    }

    private void UpdateReportStatusText()
    {
        if (_currentView != PageViewMode.Report)
        {
            return;
        }

        if (_reportPages is not null && _reportPages.Count > 0)
        {
            _statusText = _reportPages.Count == 1
                ? "1 page"
                : $"{_reportPages.Count} pages";
        }
        else
        {
            _statusText = null;
        }
    }

    // ── Zoom / Print ───────────────────────────────────────────────────

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

    // ── Helpers ────────────────────────────────────────────────────────

    private Task DismissErrorAsync()
    {
        _error = null;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private static bool HasActiveFilters(CompositeFilterDescriptor filter)
    {
        return filter.Filters.Count > 0 || filter.Children.Count > 0;
    }

    // ── Dispose ────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();

        if (_jsModule is not null)
        {
            await _jsModule.DisposeAsync();
        }

        await base.DisposeAsyncCore();
    }
}
