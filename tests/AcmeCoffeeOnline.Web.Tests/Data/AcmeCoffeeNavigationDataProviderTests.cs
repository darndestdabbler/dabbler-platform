using System.Net;
using System.Security.Claims;
using AcmeCoffeeOnline.Shared.DTOs;
using AcmeCoffeeOnline.Shared.Enums;
using AcmeCoffeeOnline.Web.Data;
using AcmeCoffeeOnline.Web.Services;
using AcmeCoffeeOnline.Web.Tests.Infrastructure;
using Dabbler.Components.Data.Metadata;

namespace AcmeCoffeeOnline.Web.Tests.Data;

public sealed class AcmeCoffeeNavigationDataProviderTests
{
    [Fact]
    public async Task GetSmartFilterRecordsAsync_ForAvailableProducts_ReturnsOnlyAvailableTopN()
    {
        var provider = CreateProvider(
            productHandler: new StubHttpMessageHandler((request, _) =>
            {
                return Task.FromResult(request.RequestUri?.AbsolutePath == "/api/products"
                    ? StubHttpMessageHandler.Json(HttpStatusCode.OK, new PagedResponse<ProductDto>
                    {
                        Items =
                        [
                            new ProductDto { Id = 1, Name = "Available A", IsAvailable = true },
                            new ProductDto { Id = 2, Name = "Unavailable", IsAvailable = false },
                            new ProductDto { Id = 3, Name = "Available B", IsAvailable = true }
                        ]
                    })
                    : StubHttpMessageHandler.Empty(HttpStatusCode.NotFound));
            }));

        var records = await provider.GetSmartFilterRecordsAsync(
            "Product",
            new SmartFilterDefinition
            {
                Category = SmartFilterCategory.Custom,
                FilterExpression = "IsAvailable == true"
            },
            topN: 1);

        var record = Assert.Single(records);
        Assert.Equal(1, record.PrimaryKeyValue);
        Assert.Equal("Available A", record.RecordLabel);
    }

    [Fact]
    public async Task GetSmartFilterRecordsAsync_ForCustomerAuditFilter_UsesCurrentUserName()
    {
        var provider = CreateProvider(
            customerHandler: new StubHttpMessageHandler((request, _) =>
            {
                return Task.FromResult(request.RequestUri?.AbsolutePath == "/api/customers"
                    ? StubHttpMessageHandler.Json(HttpStatusCode.OK, new PagedResponse<CustomerDto>
                    {
                        Items =
                        [
                            new CustomerDto
                            {
                                Id = 2,
                                FirstName = "Alex",
                                LastName = "Avery",
                                ModifiedBy = "Jordan Reviewer",
                                ModifiedDate = DateTime.UtcNow.AddMinutes(-20)
                            },
                            new CustomerDto
                            {
                                Id = 3,
                                FirstName = "Casey",
                                LastName = "Cole",
                                ModifiedBy = "Someone Else",
                                ModifiedDate = DateTime.UtcNow
                            },
                            new CustomerDto
                            {
                                Id = 4,
                                FirstName = "Dana",
                                LastName = "Dunn",
                                ModifiedBy = "Jordan Reviewer",
                                ModifiedDate = DateTime.UtcNow.AddMinutes(-5)
                            }
                        ]
                    })
                    : StubHttpMessageHandler.Empty(HttpStatusCode.NotFound));
            }),
            userName: "Jordan Reviewer");

        var records = await provider.GetSmartFilterRecordsAsync(
            "Customer",
            new SmartFilterDefinition
            {
                Category = SmartFilterCategory.Audit,
                AuditFieldName = "ModifiedDate",
                AuditUserFieldName = "ModifiedBy"
            },
            topN: 5);

        Assert.Equal(2, records.Count);
        Assert.Equal("Dana Dunn", records[0].RecordLabel);
        Assert.Equal("Alex Avery", records[1].RecordLabel);
    }

    [Fact]
    public async Task GetSmartFilterCountAsync_ForRecentCustomers_ReturnsOnlyMatchingRows()
    {
        var provider = CreateProvider(
            customerHandler: new StubHttpMessageHandler((request, _) =>
            {
                return Task.FromResult(request.RequestUri?.AbsolutePath == "/api/customers"
                    ? StubHttpMessageHandler.Json(HttpStatusCode.OK, new PagedResponse<CustomerDto>
                    {
                        Items =
                        [
                            new CustomerDto
                            {
                                Id = 1,
                                FirstName = "Recent",
                                LastName = "Customer",
                                CreatedDate = DateTime.Now.AddDays(-10)
                            },
                            new CustomerDto
                            {
                                Id = 2,
                                FirstName = "Older",
                                LastName = "Customer",
                                CreatedDate = DateTime.Now.AddDays(-45)
                            }
                        ]
                    })
                    : StubHttpMessageHandler.Empty(HttpStatusCode.NotFound));
            }));

        var count = await provider.GetSmartFilterCountAsync(
            "Customer",
            new SmartFilterDefinition
            {
                Category = SmartFilterCategory.Temporal,
                RelativeDateRange = "30d"
            });

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetSmartFilterRecordsAsync_ForRelationalOrderFilterWithoutContext_ReturnsEmpty()
    {
        var orderHandler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(StubHttpMessageHandler.Empty(HttpStatusCode.OK)));
        var provider = CreateProvider(orderHandler: orderHandler);

        var records = await provider.GetSmartFilterRecordsAsync(
            "Order",
            new SmartFilterDefinition
            {
                Category = SmartFilterCategory.Relational,
                RelatedEntityName = "Customer"
            },
            topN: 5,
            context: null);

        Assert.Empty(records);
        Assert.Empty(orderHandler.Requests);
    }

    [Fact]
    public async Task GetSmartFilterCountAsync_ForCustomerOrders_UsesCustomerScopedEndpoint()
    {
        var orderHandler = new StubHttpMessageHandler((request, _) =>
        {
            return Task.FromResult(request.RequestUri?.PathAndQuery == "/api/orders/by-customer/12?page=1&pageSize=1"
                ? StubHttpMessageHandler.Json(HttpStatusCode.OK, new PagedResponse<OrderDto>
                {
                    TotalCount = 4,
                    Items = [new OrderDto { Id = 120, Status = OrderStatus.Pending }]
                })
                : StubHttpMessageHandler.Empty(HttpStatusCode.NotFound));
        });
        var provider = CreateProvider(orderHandler: orderHandler);

        var count = await provider.GetSmartFilterCountAsync(
            "Order",
            new SmartFilterDefinition
            {
                Category = SmartFilterCategory.Relational,
                RelatedEntityName = "Customer"
            },
            context: 12);

        Assert.Equal(4, count);
        Assert.Contains(orderHandler.Requests, request => request.PathAndQuery == "/api/orders/by-customer/12?page=1&pageSize=1");
    }

    private static AcmeCoffeeNavigationDataProvider CreateProvider(
        StubHttpMessageHandler? productHandler = null,
        StubHttpMessageHandler? customerHandler = null,
        StubHttpMessageHandler? orderHandler = null,
        string? userName = "Jordan Reviewer")
    {
        var productClient = new ProductApiClient(CreateHttpClient(productHandler));
        var customerClient = new CustomerApiClient(CreateHttpClient(customerHandler));
        var orderClient = new OrderApiClient(CreateHttpClient(orderHandler));
        var authProvider = new TestAuthenticationStateProvider(CreateUser(userName));

        return new AcmeCoffeeNavigationDataProvider(productClient, customerClient, orderClient, authProvider);
    }

    private static HttpClient CreateHttpClient(StubHttpMessageHandler? handler)
        => new(handler ?? new StubHttpMessageHandler((_, _) =>
            Task.FromResult(StubHttpMessageHandler.Empty(HttpStatusCode.OK))))
        {
            BaseAddress = new Uri("https://acme.test/")
        };

    private static ClaimsPrincipal CreateUser(string? userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        return new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, userName)
        ], "TestAuth"));
    }
}
