using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dabbler.Api.Querying;
using Dabbler.Security.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AcmeCoffeeOnline.Api.Tests;

public sealed class SuppliersControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SuppliersControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task QueryAsync_FilterOnUnknownProperty_Returns400ProblemDetailsAsync()
    {
        using var client = await CreateAdminClientAsync();
        var request = new GridQueryRequest
        {
            Filter = new GridCompositeFilter
            {
                Filters = [new GridQueryFilter("NoSuchProperty", "Contains", "x")],
            },
        };

        var response = await client.PostAsJsonAsync("/api/suppliers/query", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(400, body.RootElement.GetProperty("status").GetInt32());
        Assert.Contains("'NoSuchProperty'", body.RootElement.GetProperty("detail").GetString());
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        int adminUserId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var securityDb = scope.ServiceProvider.GetRequiredService<DabblerSecurityDbContext>();
            adminUserId = (await securityDb.AppUsers
                .AsNoTracking()
                .FirstAsync(u => u.Email == "admin@acmecoffee.com")).Id;
        }

        var apiKey = _factory.Services.GetRequiredService<IConfiguration>()["DabblerSecurity:InternalApiKey"]
            ?? "dev-internal-key-acme";
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Internal-Api-Key", apiKey);
        client.DefaultRequestHeaders.Add("X-App-User-Id", adminUserId.ToString());
        return client;
    }
}
