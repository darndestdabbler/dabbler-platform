using System.Data;
using AcmeCoffeeOnline.Shared.DTOs;
using AcmeCoffeeOnline.Shared.Enums;
using AcmeCoffeeOnline.Web.Services;
using Dabbler.Reports.Core.Data;

namespace AcmeCoffeeOnline.Web.Data;

/// <summary>
/// Report data provider for the Orders by Customer report.
/// Fetches order data via <see cref="OrderApiClient"/> and returns it as a
/// <see cref="DataTable"/> for the Dabbler Reports engine.
/// </summary>
public class OrderReportDataProvider : IReportDataProvider
{
    private readonly OrderApiClient _orderClient;

    public OrderReportDataProvider(OrderApiClient orderClient)
    {
        _orderClient = orderClient;
    }

    public async Task<DataTable> ExecuteQueryAsync(
        string query,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        var startDate = GetDateParameter(parameters, "StartDate");
        var endDate = GetDateParameter(parameters, "EndDate");
        var statusFilter = GetStringParameter(parameters, "Status");

        var allOrders = await FetchAllOrdersAsync(cancellationToken);

        var filtered = allOrders
            .Where(o => o.OrderDate.Date >= startDate.Date && o.OrderDate.Date <= endDate.Date);

        if (!string.IsNullOrEmpty(statusFilter)
            && Enum.TryParse<OrderStatus>(statusFilter, out var status))
        {
            filtered = filtered.Where(o => o.Status == status);
        }

        var table = new DataTable("OrdersByCustomer");
        table.Columns.Add("CustomerName", typeof(string));
        table.Columns.Add("OrderId", typeof(int));
        table.Columns.Add("OrderDate", typeof(DateTime));
        table.Columns.Add("ShipDate", typeof(DateTime));
        table.Columns.Add("Status", typeof(string));
        table.Columns.Add("ItemCount", typeof(int));
        table.Columns.Add("Total", typeof(decimal));

        foreach (var order in filtered.OrderBy(o => o.CustomerName).ThenBy(o => o.OrderDate))
        {
            table.Rows.Add(
                order.CustomerName,
                order.Id,
                order.OrderDate,
                order.ShipDate ?? (object)DBNull.Value,
                order.Status.ToString(),
                order.OrderItems.Count,
                order.Total);
        }

        return table;
    }

    public Task<DataTable> ExecuteLookupAsync(
        string query,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var table = new DataTable("Lookup");

        if (query.Contains("OrderStatuses", StringComparison.OrdinalIgnoreCase))
        {
            table.Columns.Add("Status", typeof(string));
            foreach (var status in Enum.GetNames<OrderStatus>())
            {
                table.Rows.Add(status);
            }
        }

        return Task.FromResult(table);
    }

    private async Task<List<OrderDto>> FetchAllOrdersAsync(CancellationToken cancellationToken)
    {
        var allOrders = new List<OrderDto>();
        var page = 1;
        const int pageSize = 200;

        while (true)
        {
            var request = new PagedRequest { Page = page, PageSize = pageSize };
            var response = await _orderClient.GetAllAsync(request, cancellationToken);

            allOrders.AddRange(response.Items);

            if (allOrders.Count >= response.TotalCount || response.Items.Count < pageSize)
                break;

            page++;
        }

        return allOrders;
    }

    private static DateTime GetDateParameter(IReadOnlyDictionary<string, object?> parameters, string name)
    {
        if (parameters.TryGetValue(name, out var value) && value is DateTime dt)
            return dt;

        if (value is string s && DateTime.TryParse(s, out var parsed))
            return parsed;

        return name == "StartDate"
            ? DateTime.Today.AddMonths(-1)
            : DateTime.Today;
    }

    private static string? GetStringParameter(IReadOnlyDictionary<string, object?> parameters, string name)
    {
        if (parameters.TryGetValue(name, out var value) && value is string s && !string.IsNullOrEmpty(s))
            return s;

        return null;
    }
}
