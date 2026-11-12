using System.Data;
using AcmeCoffeeOnline.Shared.DTOs;
using AcmeCoffeeOnline.Shared.Enums;
using AcmeCoffeeOnline.Web.Services;
using Dabbler.Reports.Core.Data;

namespace AcmeCoffeeOnline.Web.Data;

/// <summary>
/// Report data provider for the Sales by Product report.
/// Joins order items with product information via API clients and returns
/// flattened rows as a <see cref="DataTable"/> for the Dabbler Reports engine.
/// </summary>
public class SalesReportDataProvider : IReportDataProvider
{
    private readonly OrderApiClient _orderClient;
    private readonly ProductApiClient _productClient;

    public SalesReportDataProvider(OrderApiClient orderClient, ProductApiClient productClient)
    {
        _orderClient = orderClient;
        _productClient = productClient;
    }

    public async Task<DataTable> ExecuteQueryAsync(
        string query,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        var startDate = GetDateParameter(parameters, "StartDate");
        var endDate = GetDateParameter(parameters, "EndDate");
        var roastFilter = GetStringParameter(parameters, "RoastLevel");

        var ordersTask = FetchAllOrdersAsync(cancellationToken);
        var productsTask = FetchAllProductsAsync(cancellationToken);

        await Task.WhenAll(ordersTask, productsTask);

        var allOrders = ordersTask.Result;
        var products = productsTask.Result.ToDictionary(p => p.Id);

        var filteredOrders = allOrders
            .Where(o => o.OrderDate.Date >= startDate.Date && o.OrderDate.Date <= endDate.Date);

        var table = new DataTable("SalesByProduct");
        table.Columns.Add("ProductName", typeof(string));
        table.Columns.Add("RoastLevel", typeof(string));
        table.Columns.Add("OrderId", typeof(int));
        table.Columns.Add("OrderDate", typeof(DateTime));
        table.Columns.Add("CustomerName", typeof(string));
        table.Columns.Add("Quantity", typeof(int));
        table.Columns.Add("UnitPrice", typeof(decimal));
        table.Columns.Add("LineTotal", typeof(decimal));

        foreach (var order in filteredOrders)
        {
            foreach (var item in order.OrderItems)
            {
                var product = products.GetValueOrDefault(item.ProductId);
                var roastLevel = product?.RoastLevel.ToString() ?? "Unknown";

                if (!string.IsNullOrEmpty(roastFilter)
                    && !roastLevel.Equals(roastFilter, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var lineTotal = item.Quantity * item.UnitPrice * (1 - item.Discount);

                table.Rows.Add(
                    item.ProductName,
                    roastLevel,
                    order.Id,
                    order.OrderDate,
                    order.CustomerName,
                    item.Quantity,
                    item.UnitPrice,
                    lineTotal);
            }
        }

        table.DefaultView.Sort = "ProductName ASC, OrderDate ASC";
        var sorted = table.DefaultView.ToTable();
        sorted.TableName = "SalesByProduct";
        return sorted;
    }

    public Task<DataTable> ExecuteLookupAsync(
        string query,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var table = new DataTable("Lookup");

        if (query.Contains("RoastLevel", StringComparison.OrdinalIgnoreCase))
        {
            table.Columns.Add("RoastLevel", typeof(string));
            foreach (var level in Enum.GetNames<RoastLevel>())
            {
                table.Rows.Add(level);
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

    private async Task<List<ProductDto>> FetchAllProductsAsync(CancellationToken cancellationToken)
    {
        var allProducts = new List<ProductDto>();
        var page = 1;
        const int pageSize = 200;

        while (true)
        {
            var request = new PagedRequest { Page = page, PageSize = pageSize };
            var response = await _productClient.GetAllAsync(request, cancellationToken);

            allProducts.AddRange(response.Items);

            if (allProducts.Count >= response.TotalCount || response.Items.Count < pageSize)
                break;

            page++;
        }

        return allProducts;
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
