// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using Dabbler.Components.Data;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Dabbler.Components;

/// <summary>
/// A page-level component that composes <see cref="DabblerFilterGrid{T}"/> and
/// <see cref="DabblerDataGrid{T}"/> into a ready-made query page layout:
/// filter grid, toolbar with record count and export buttons, and a data grid.
/// </summary>
/// <typeparam name="T">The row data type.</typeparam>
[CascadingTypeParameter(nameof(T))]
public partial class DabblerQueryPage<T> : DabblerComponentBase
{
    private DabblerFilterGrid<T>? _filterGrid;
    private DabblerDataGrid<T>? _dataGrid;
    private MudDataGrid<T>? _mudDataGrid;

    private bool _isLoading;
    private Exception? _error;
    private IReadOnlyList<T>? _items;
    private int _currentTotal;
    private int? _unfilteredTotal;
    private CompositeFilterDescriptor? _currentFilter;
    private CancellationTokenSource? _debounceCts;
    private string? _statusText;

    // ── Required parameters ────────────────────────────────────────────

    /// <summary>
    /// Column definitions for the filter grid.
    /// </summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<FilterColumnDefinition> FilterColumns { get; set; } = default!;

    /// <summary>
    /// Column markup rendered inside the <c>MudDataGrid</c>'s <c>Columns</c> section.
    /// Typically contains <c>PropertyColumn</c> and <c>TemplateColumn</c> elements.
    /// </summary>
    [Parameter, EditorRequired]
    public RenderFragment GridColumns { get; set; } = default!;

    /// <summary>
    /// Data source for the grid.
    /// </summary>
    [Parameter, EditorRequired]
    public IGridDataProvider<T> DataProvider { get; set; } = default!;

    // ── Optional parameters ────────────────────────────────────────────

    /// <summary>
    /// Column metadata for clipboard and CSV export.
    /// When null, falls back to <c>DabblerDataGrid</c> defaults.
    /// </summary>
    [Parameter]
    public IReadOnlyList<GridColumnInfo>? GridColumnInfo { get; set; }

    /// <summary>
    /// Page title displayed in the header area.
    /// </summary>
    [Parameter]
    public string? Title { get; set; }

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
    /// Display total/filtered record count in the toolbar.
    /// </summary>
    [Parameter]
    public bool ShowRecordCount { get; set; } = true;

    /// <summary>
    /// Show CSV export button in the toolbar.
    /// </summary>
    [Parameter]
    public bool ShowCsvExport { get; set; } = true;

    /// <summary>
    /// Show copy-all button in the toolbar.
    /// </summary>
    [Parameter]
    public bool ShowCopyToClipboard { get; set; } = true;

    /// <summary>
    /// Default filename for CSV downloads.
    /// </summary>
    [Parameter]
    public string CsvFileName { get; set; } = "export.csv";

    /// <summary>
    /// When true, automatically re-query when filters change.
    /// When false, the user must click a "Search" button.
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
    /// Additional toolbar buttons inserted before built-in buttons.
    /// </summary>
    [Parameter]
    public RenderFragment? ToolbarTemplate { get; set; }

    /// <summary>
    /// Custom content below the grid.
    /// </summary>
    [Parameter]
    public RenderFragment? FooterTemplate { get; set; }

    /// <summary>
    /// Raised after data is fetched, before grid binding.
    /// </summary>
    [Parameter]
    public EventCallback<IReadOnlyList<T>> OnDataLoaded { get; set; }

    /// <summary>
    /// Raised on data provider errors.
    /// </summary>
    [Parameter]
    public EventCallback<Exception> OnError { get; set; }

    // ── Public members ─────────────────────────────────────────────────

    /// <summary>
    /// Reference to the filter grid for programmatic access.
    /// </summary>
    public DabblerFilterGrid<T>? FilterGrid => _filterGrid;

    /// <summary>
    /// Reference to the data grid.
    /// </summary>
    public DabblerDataGrid<T>? DataGrid => _dataGrid;

    /// <summary>
    /// Current filtered/sorted result set.
    /// </summary>
    public IReadOnlyList<T>? Items => _items;

    /// <summary>
    /// True while a data fetch is in progress.
    /// </summary>
    public bool IsLoading => _isLoading;

    /// <summary>
    /// Re-executes the current query.
    /// </summary>
    public async Task RefreshAsync()
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

    // ── Private methods ────────────────────────────────────────────────

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

            UpdateStatusText();

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
            UpdateStatusText();

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

    private async Task OnFilterChangedAsync(CompositeFilterDescriptor? filter)
    {
        _currentFilter = filter;

        if (!AutoLoadOnFilterChange)
        {
            return;
        }

        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(200, _debounceCts.Token);
            await RefreshAsync();
        }
        catch (TaskCanceledException)
        {
            // Debounce cancelled by a newer filter change.
        }
    }

    private async Task SearchClickedAsync()
    {
        await RefreshAsync();
    }

    private Task DismissErrorAsync()
    {
        _error = null;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void UpdateStatusText()
    {
        if (!ShowRecordCount)
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

    private static bool HasActiveFilters(CompositeFilterDescriptor filter)
    {
        return filter.Filters.Count > 0 || filter.Children.Count > 0;
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        await base.DisposeAsyncCore();
    }
}
