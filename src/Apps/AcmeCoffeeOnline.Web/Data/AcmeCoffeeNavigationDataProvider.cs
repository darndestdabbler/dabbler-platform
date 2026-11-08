using System.Security.Claims;
using AcmeCoffeeOnline.Shared.DTOs;
using AcmeCoffeeOnline.Web.Services;
using Dabbler.Components.Data.Metadata;
using Microsoft.AspNetCore.Components.Authorization;

namespace AcmeCoffeeOnline.Web.Data;

/// <summary>
/// Queries the REST API via typed HTTP clients to return smart filter
/// top-N records and counts for the DabblerShell navigation panel.
/// </summary>
public class AcmeCoffeeNavigationDataProvider : INavigationDataProvider
{
    private readonly ProductApiClient _productClient;
    private readonly CustomerApiClient _customerClient;
    private readonly OrderApiClient _orderClient;
    private readonly AuthenticationStateProvider _authStateProvider;

    public AcmeCoffeeNavigationDataProvider(
        ProductApiClient productClient,
        CustomerApiClient customerClient,
        OrderApiClient orderClient,
        AuthenticationStateProvider authStateProvider)
    {
        _productClient = productClient;
        _customerClient = customerClient;
        _orderClient = orderClient;
        _authStateProvider = authStateProvider;
    }

    public async Task<IReadOnlyList<NavigationRecord>> GetSmartFilterRecordsAsync(
        string entityName,
        SmartFilterDefinition filter,
        int topN,
        object? context = null)
    {
        return entityName switch
        {
            "Product" => await GetFilteredProductsAsync(filter, topN),
            "Customer" => await GetFilteredCustomersAsync(filter, topN),
            "Order" => await GetFilteredOrdersAsync(filter, topN, context),
            _ => Array.Empty<NavigationRecord>()
        };
    }

    public async Task<int> GetSmartFilterCountAsync(
        string entityName,
        SmartFilterDefinition filter,
        object? context = null)
    {
        return entityName switch
        {
            "Product" => await CountFilteredProductsAsync(filter),
            "Customer" => await CountFilteredCustomersAsync(filter),
            "Order" => await CountFilteredOrdersAsync(filter, context),
            _ => 0
        };
    }

    // ── Products ────────────────────────────────────────────────────────

    private async Task<IReadOnlyList<NavigationRecord>> GetFilteredProductsAsync(
        SmartFilterDefinition filter, int topN)
    {
        var request = new PagedRequest { Page = 1, PageSize = topN * 2, SortBy = "Name" };
        var response = await _productClient.GetAllAsync(request);

        var filtered = ApplyProductFilter(response.Items, filter);

        return filtered
            .Take(topN)
            .Select(p => new NavigationRecord(p.Id, p.Name))
            .ToList();
    }

    private async Task<int> CountFilteredProductsAsync(SmartFilterDefinition filter)
    {
        var request = new PagedRequest { Page = 1, PageSize = 100 };
        var response = await _productClient.GetAllAsync(request);
        return ApplyProductFilter(response.Items, filter).Count();
    }

    private static IEnumerable<ProductDto> ApplyProductFilter(
        IReadOnlyList<ProductDto> items, SmartFilterDefinition filter)
    {
        var query = items.AsEnumerable();

        if (filter.Category == SmartFilterCategory.Custom
            && filter.FilterExpression == "IsAvailable == true")
        {
            query = query.Where(p => p.IsAvailable);
        }

        return query;
    }

    // ── Customers ───────────────────────────────────────────────────────

    private async Task<IReadOnlyList<NavigationRecord>> GetFilteredCustomersAsync(
        SmartFilterDefinition filter, int topN)
    {
        var sortDescending = filter.Category is SmartFilterCategory.Temporal or SmartFilterCategory.Audit;
        var sortBy = filter.Category switch
        {
            SmartFilterCategory.Temporal => "CreatedDate",
            SmartFilterCategory.Audit => "ModifiedDate",
            _ => "LastName"
        };
        var request = new PagedRequest
        {
            Page = 1,
            PageSize = topN * 3,
            SortBy = sortBy,
            SortDescending = sortDescending
        };
        var response = await _customerClient.GetAllAsync(request);

        var filtered = await ApplyCustomerFilterAsync(response.Items, filter);

        return filtered
            .Take(topN)
            .Select(c => new NavigationRecord(c.Id, $"{c.FirstName} {c.LastName}"))
            .ToList();
    }

    private async Task<int> CountFilteredCustomersAsync(SmartFilterDefinition filter)
    {
        var request = new PagedRequest { Page = 1, PageSize = 200 };
        var response = await _customerClient.GetAllAsync(request);
        var filtered = await ApplyCustomerFilterAsync(response.Items, filter);
        return filtered.Count();
    }

    private async Task<IEnumerable<CustomerDto>> ApplyCustomerFilterAsync(
        IReadOnlyList<CustomerDto> items, SmartFilterDefinition filter)
    {
        var query = items.AsEnumerable();

        if (filter.Category == SmartFilterCategory.Custom
            && filter.FilterExpression == "IsActive == true")
        {
            query = query.Where(c => c.IsActive);
        }
        else if (filter.Category == SmartFilterCategory.Temporal)
        {
            var cutoff = GetTemporalCutoff(filter.RelativeDateRange);
            if (cutoff.HasValue)
                query = query.Where(c => c.CreatedDate >= cutoff.Value);
        }
        else if (filter.Category == SmartFilterCategory.Audit)
        {
            var currentUser = await GetCurrentUserNameAsync();
            if (currentUser is not null)
                query = query.Where(c => c.ModifiedBy == currentUser)
                    .OrderByDescending(c => c.ModifiedDate);
        }

        return query;
    }

    // ── Orders ──────────────────────────────────────────────────────────

    private async Task<IReadOnlyList<NavigationRecord>> GetFilteredOrdersAsync(
        SmartFilterDefinition filter, int topN, object? context)
    {
        // Relational filters require a parent context; return empty if none provided
        if (filter.Category == SmartFilterCategory.Relational && context is null)
            return Array.Empty<NavigationRecord>();

        PagedResponse<OrderDto> response;

        if (filter.Category == SmartFilterCategory.Relational
            && filter.RelatedEntityName == "Customer"
            && context is int customerId)
        {
            var request = new PagedRequest
            {
                Page = 1, PageSize = topN, SortBy = "OrderDate", SortDescending = true
            };
            response = await _orderClient.GetByCustomerIdAsync(customerId, request);
        }
        else
        {
            var sortBy = filter.Category == SmartFilterCategory.Audit ? "ModifiedDate" : "OrderDate";
            var request = new PagedRequest
            {
                Page = 1,
                PageSize = topN * 3,
                SortBy = sortBy,
                SortDescending = true
            };
            response = await _orderClient.GetAllAsync(request);
        }

        var filtered = await ApplyOrderFilterAsync(response.Items, filter);

        return filtered
            .Take(topN)
            .Select(o => new NavigationRecord(o.Id, $"Order #{o.Id}"))
            .ToList();
    }

    private async Task<int> CountFilteredOrdersAsync(SmartFilterDefinition filter, object? context)
    {
        // Relational filters require a parent context; return 0 if none provided
        if (filter.Category == SmartFilterCategory.Relational && context is null)
            return 0;

        if (filter.Category == SmartFilterCategory.Relational
            && filter.RelatedEntityName == "Customer"
            && context is int customerId)
        {
            var request = new PagedRequest { Page = 1, PageSize = 1 };
            var response = await _orderClient.GetByCustomerIdAsync(customerId, request);
            return response.TotalCount;
        }

        var allRequest = new PagedRequest { Page = 1, PageSize = 200 };
        var allResponse = await _orderClient.GetAllAsync(allRequest);
        var filtered = await ApplyOrderFilterAsync(allResponse.Items, filter);
        return filtered.Count();
    }

    private async Task<IEnumerable<OrderDto>> ApplyOrderFilterAsync(
        IReadOnlyList<OrderDto> items, SmartFilterDefinition filter)
    {
        var query = items.AsEnumerable();

        if (filter.Category == SmartFilterCategory.Temporal)
        {
            var cutoff = GetTemporalCutoff(filter.RelativeDateRange);
            if (cutoff.HasValue)
                query = query.Where(o => o.OrderDate >= cutoff.Value);
        }
        else if (filter.Category == SmartFilterCategory.Audit)
        {
            var currentUser = await GetCurrentUserNameAsync();
            if (currentUser is not null)
                query = query.Where(o => o.ModifiedBy == currentUser)
                    .OrderByDescending(o => o.ModifiedDate);
        }

        return query;
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private static DateTime? GetTemporalCutoff(string? relativeDateRange)
    {
        if (string.IsNullOrEmpty(relativeDateRange))
            return null;

        var now = DateTime.Now;
        return relativeDateRange switch
        {
            "1d" => now.AddDays(-1),
            "7d" => now.AddDays(-7),
            "30d" => now.AddDays(-30),
            "thisWeek" => now.AddDays(-(int)now.DayOfWeek),
            "thisMonth" => new DateTime(now.Year, now.Month, 1),
            _ => null
        };
    }

    private async Task<string?> GetCurrentUserNameAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        return authState.User.FindFirst(ClaimTypes.Name)?.Value;
    }
}
