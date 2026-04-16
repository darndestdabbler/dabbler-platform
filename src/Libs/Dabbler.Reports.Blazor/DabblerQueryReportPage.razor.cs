// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using System.ComponentModel;
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
/// Compatibility stub — pending Session 5 rebuild on MudBlazor-native patterns.
/// </summary>
/// <typeparam name="T">The row data type for the grid view.</typeparam>
[CascadingTypeParameter(nameof(T))]
[EditorBrowsable(EditorBrowsableState.Never)]
[Obsolete("DabblerQueryReportPage<T> is compatibility-only. Will be rebuilt in Session 5 on MudBlazor-native patterns.")]
public partial class DabblerQueryReportPage<T> : DabblerComponentBase
{
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

    // Report state
    private IReadOnlyList<PageLayout>? _reportPages;
    private bool _reportHasRun;
    private double _scale;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    // ── Required parameters ────────────────────────────────────────────

    [Parameter, EditorRequired]
    public RenderFragment GridColumns { get; set; } = default!;

    [Parameter, EditorRequired]
    public IGridDataProvider<T> DataProvider { get; set; } = default!;

    [Parameter, EditorRequired]
    public IReportDataProvider ReportDataProvider { get; set; } = default!;

    [Parameter, EditorRequired]
    public string RdfPath { get; set; } = default!;

    // ── Optional parameters ────────────────────────────────────────────

    [Parameter]
    public Func<string, Task<string>>? RdfLoader { get; set; }

    [Parameter]
    public string? Title { get; set; }

    [Parameter]
    public PageViewMode DefaultView { get; set; } = PageViewMode.Grid;

    [Parameter]
    public bool ShowViewToggle { get; set; } = true;

    [Parameter]
    public bool AutoRunReportOnSwitch { get; set; } = true;

    [Parameter]
    public bool InjectFilterSummary { get; set; } = true;

    [Parameter]
    public IReadOnlyList<FilterColumnDefinition>? FilterColumns { get; set; }

    [Parameter]
    public bool ShowFilterStatusBar { get; set; } = true;

    [Parameter]
    public bool ShowRecordCount { get; set; } = true;

    [Parameter]
    public bool ShowCsvExport { get; set; } = true;

    [Parameter]
    public bool ShowCopyToClipboard { get; set; } = true;

    [Parameter]
    public bool ShowPdfExport { get; set; } = true;

    [Parameter]
    public bool ShowPrint { get; set; } = true;

    [Parameter]
    public bool ShowZoom { get; set; } = true;

    [Parameter]
    public string CsvFileName { get; set; } = "export.csv";

    [Parameter]
    public string? PdfFileName { get; set; }

    [Parameter]
    public double DefaultScale { get; set; } = 1.0;

    [Parameter]
    public bool AutoLoadOnFilterChange { get; set; } = true;

    [Parameter]
    public bool Editable { get; set; }

    [Parameter]
    public int PageSize { get; set; } = 25;

    [Parameter]
    public bool Dense { get; set; } = true;

    [Parameter]
    public RenderFragment? HeaderTemplate { get; set; }

    [Parameter]
    public RenderFragment? ToolbarTemplate { get; set; }

    [Parameter]
    public RenderFragment? FooterTemplate { get; set; }

    [Parameter]
    public RenderFragment? EmptyStateTemplate { get; set; }

    [Parameter]
    public EventCallback<IReadOnlyList<T>> OnDataLoaded { get; set; }

    [Parameter]
    public EventCallback<IReadOnlyList<PageLayout>> OnReportComposed { get; set; }

    [Parameter]
    public EventCallback<Exception> OnError { get; set; }

    // ── Public members ─────────────────────────────────────────────────

    public IReadOnlyList<T>? Items => _items;

    public IReadOnlyList<PageLayout>? ReportPages => _reportPages;

    public PageViewMode CurrentView => _currentView;

    public bool IsLoading => _isLoading;

    public bool ReportHasRun => _reportHasRun;

    public async Task RefreshGridAsync()
    {
        if (_mudDataGrid is not null)
            await _mudDataGrid.ReloadServerData();
    }

    public async Task SwitchViewAsync(PageViewMode mode)
    {
        if (_currentView == mode) return;

        _currentView = mode;

        if (mode == PageViewMode.Report && AutoRunReportOnSwitch)
        {
            await EnsureRdfLoadedAsync();
            await RunReportCoreAsync();
        }

        StateHasChanged();
    }

    // ── Lifecycle ──────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        _currentView = DefaultView;
        _scale = DefaultScale;
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
                _unfilteredTotal = response.TotalCount;

            if (OnDataLoaded.HasDelegate)
                await OnDataLoaded.InvokeAsync(_items);

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

            if (OnError.HasDelegate)
                await OnError.InvokeAsync(ex);

            return new GridData<T> { Items = [], TotalItems = 0 };
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    // ── Report methods ─────────────────────────────────────────────────

    private async Task EnsureRdfLoadedAsync()
    {
        if (_reportDefinition is not null) return;

        try
        {
            string json;
            if (RdfLoader is not null)
                json = await RdfLoader(RdfPath);
            else
                json = await File.ReadAllTextAsync(RdfPath);

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
        StateHasChanged();

        try
        {
            var parameterValues = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            if (_reportDefinition.Parameters is not null)
            {
                foreach (var param in _reportDefinition.Parameters)
                {
                    parameterValues[param.Name] = DefaultValueResolver.Resolve(
                        param.DefaultValue, param.DataType);
                }
            }

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

    private static bool HasActiveFilters(CompositeFilterDescriptor filter)
        => filter.Filters.Count > 0 || filter.Children.Count > 0;
}
