using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.Playwright;
using Xunit;

namespace AcmeCoffeeOnline.Playwright.Infrastructure;

public sealed class PlaywrightFixture : IAsyncLifetime
{
    public const string TestAuthSecret = "acme-local-seeded-session";
    private const string InternalApiKey = "dev-internal-key-acme";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private Process? _apiProcess;
    private Process? _webProcess;
    private readonly ConcurrentQueue<string> _apiProcessLog = new();
    private readonly ConcurrentQueue<string> _webProcessLog = new();
    private readonly ConcurrentDictionary<IPage, ConcurrentQueue<string>> _pageIssues = new();
    private readonly ConcurrentDictionary<IPage, IBrowserContext> _pageContexts = new();
    private readonly ConcurrentDictionary<string, string> _seededUserIdCache = new(StringComparer.OrdinalIgnoreCase);

    public string BaseUrl { get; } = Environment.GetEnvironmentVariable("PLAYWRIGHT_BASE_URL")
        ?? "http://localhost:5010";

    public string ApiBaseUrl { get; } = Environment.GetEnvironmentVariable("PLAYWRIGHT_API_BASE_URL")
        ?? "http://localhost:5011";

    public async Task InitializeAsync()
    {
        if (!await IsReachableAsync($"{ApiBaseUrl}/health"))
        {
            _apiProcess = StartProject("src/Apps/AcmeCoffeeOnline.Api", _apiProcessLog);
        }

        if (!await IsReachableAsync($"{BaseUrl}/health"))
        {
            _webProcess = StartProject("src/Apps/AcmeCoffeeOnline.Web", _webProcessLog);
        }

        await WaitForServerAsync($"{ApiBaseUrl}/health", "API", _apiProcess, _apiProcessLog);
        await WaitForServerAsync($"{BaseUrl}/health", "Web", _webProcess, _webProcessLog);

        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    public async Task<IPage> CreatePageAsync()
    {
        if (_browser is null)
        {
            throw new InvalidOperationException("Playwright browser is not initialized.");
        }

        var context = await _browser.NewContextAsync();
        var page = await context.NewPageAsync();
        var issues = new ConcurrentQueue<string>();
        _pageIssues[page] = issues;
        _pageContexts[page] = context;

        page.Console += (_, message) =>
        {
            if (message.Type == "error")
            {
                issues.Enqueue($"Console error: {message.Text}");
            }
        };

        page.PageError += (_, exception) =>
        {
            issues.Enqueue($"Page error: {exception}");
        };

        page.RequestFailed += (_, request) =>
        {
            if (ShouldIgnoreRequestFailure(request))
            {
                return;
            }

            issues.Enqueue($"Request failed: {request.Method} {request.Url} ({request.Failure})");
        };

        page.Response += (_, response) =>
        {
            if (response.Status < 400 || ShouldIgnoreResponseIssue(response))
            {
                return;
            }

            issues.Enqueue($"HTTP {response.Status}: {response.Request.Method} {response.Url}");
        };

        page.Close += (_, _) =>
        {
            _pageIssues.TryRemove(page, out _);
            _pageContexts.TryRemove(page, out _);
        };

        return page;
    }

    public async Task ClosePageAsync(IPage page)
    {
        await AssertNoClientErrorsAsync(page);
        if (_pageContexts.TryGetValue(page, out var context))
        {
            await context.CloseAsync();
        }
        else
        {
            await page.CloseAsync();
        }
    }

    public async Task SeededSignInAsync(
        IPage page,
        string email,
        string? roles = null,
        string? displayName = null)
    {
        await page.SetExtraHTTPHeadersAsync(BuildTestHeaders());

        var url = $"{BaseUrl}/test-auth/seeded-session?email={Uri.EscapeDataString(email)}";
        if (!string.IsNullOrWhiteSpace(roles))
        {
            url += $"&roles={Uri.EscapeDataString(roles)}";
        }

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            url += $"&displayName={Uri.EscapeDataString(displayName)}";
        }

        await page.GotoAsync(url, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>());
    }

    public async Task SignOutAsync(IPage page)
    {
        await page.SetExtraHTTPHeadersAsync(BuildTestHeaders());
        await page.GotoAsync($"{BaseUrl}/test-auth/sign-out", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>());
    }

    public async Task ClearMagicLinksAsync(string? email = null)
    {
        var url = $"{BaseUrl}/test-auth/magic-links?clear=true";
        if (!string.IsNullOrWhiteSpace(email))
        {
            url += $"&email={Uri.EscapeDataString(email)}";
        }

        using var client = CreateWebHelperClient();
        var payload = await client.GetStringAsync(url);
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new InvalidOperationException("Failed to clear magic links.");
        }
    }

    public async Task<MagicLinkMessage> WaitForMagicLinkAsync(
        string email,
        int timeoutMs = 15000)
    {
        using var client = CreateWebHelperClient();
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        var url = $"{BaseUrl}/test-auth/magic-links?email={Uri.EscapeDataString(email)}";

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var payload = await client.GetStringAsync(url);
                var messages = JsonSerializer.Deserialize<List<MagicLinkMessage>>(payload, JsonOptions)
                    ?? [];

                var message = messages.FirstOrDefault(link => !string.IsNullOrWhiteSpace(link.LinkUrl));
                if (message is not null)
                {
                    return message;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException)
            {
            }
            catch (JsonException)
            {
            }

            await Task.Delay(250);
        }

        throw new TimeoutException($"Timed out waiting for a magic link for {email}.");
    }

    public async Task<SessionInfo> GetSessionInfoAsync(IPage page)
    {
        var payload = await ReadJsonFromHelperPageAsync(page.Context, $"{BaseUrl}/test-auth/session-info");
        return JsonSerializer.Deserialize<SessionInfo>(payload, JsonOptions)
            ?? throw new InvalidOperationException("Session info payload was empty.");
    }

    public async Task<string> GetSeededUserIdAsync(string email)
    {
        if (_seededUserIdCache.TryGetValue(email, out var cachedUserId))
        {
            return cachedUserId;
        }

        var page = await CreatePageAsync();
        try
        {
            await SeededSignInAsync(page, email);
            var session = await GetSessionInfoAsync(page);
            var userId = session.UserId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new InvalidOperationException($"Seeded session for {email} did not return a user id.");
            }

            _seededUserIdCache[email] = userId;
            return userId;
        }
        finally
        {
            await ClosePageAsync(page);
        }
    }

    public async Task<string> SwitchOrganizationFromShellAsync(IPage page, string organizationName)
    {
        var activeOrganizationLabel = page.GetByTestId("active-organization-label");
        var currentLabel = (await activeOrganizationLabel.TextContentAsync())?.Trim() ?? string.Empty;
        if (string.Equals(currentLabel, organizationName, StringComparison.Ordinal))
        {
            return currentLabel;
        }

        await page.GetByRole(AriaRole.Button, new() { Name = "Switch org" }).ClickAsync();

        var targetOrganization = page
            .Locator(".mud-popover-open")
            .GetByText(organizationName, new() { Exact = true })
            .First;
        await Assertions.Expect(targetOrganization).ToBeVisibleAsync();
        await targetOrganization.ClickAsync();

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var switchedSession = await GetSessionInfoAsync(page);
        Assert.Equal(organizationName, switchedSession.ActiveOrganizationName);

        if (page.Url.Contains("/access-denied", StringComparison.OrdinalIgnoreCase))
        {
            await page.GotoAsync($"{BaseUrl}/", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });
        }

        await Assertions.Expect(activeOrganizationLabel).ToHaveTextAsync(organizationName);

        return organizationName;
    }

    public async Task NavigateViaDrawerAsync(IPage page, string linkText, string expectedUrlPattern)
    {
        var drawer = page.GetByTestId("nav-drawer");
        var navLink = drawer.GetByText(linkText, new() { Exact = true }).First;

        await Assertions.Expect(navLink).ToBeVisibleAsync();
        await navLink.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Assertions.Expect(page).ToHaveURLAsync(new Regex(expectedUrlPattern));
        await Assertions.Expect(page.GetByTestId("access-denied-page")).ToHaveCountAsync(0);
    }

    public async Task NavigateViaDrawerGroupAsync(
        IPage page,
        string groupTitle,
        string linkText,
        string expectedUrlPattern)
    {
        var drawer = page.GetByTestId("nav-drawer");

        var group = drawer.GetByText(groupTitle, new() { Exact = true }).First;
        await group.ClickAsync();
        await Task.Delay(200);

        var navLink = drawer.GetByText(linkText, new() { Exact = true }).First;
        await Assertions.Expect(navLink).ToBeVisibleAsync();
        await navLink.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Assertions.Expect(page).ToHaveURLAsync(new Regex(expectedUrlPattern));
        await Assertions.Expect(page.GetByTestId("access-denied-page")).ToHaveCountAsync(0);
    }

    public async Task ClickDashboardNavCardAsync(IPage page, string testId, string expectedUrlPattern)
    {
        var card = page.GetByTestId(testId);
        await Assertions.Expect(card).ToBeVisibleAsync();
        await card.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Assertions.Expect(page).ToHaveURLAsync(new Regex(expectedUrlPattern));
    }

    public async Task NavigateToAdminProductsAsync(IPage page)
    {
        await NavigateViaDrawerGroupAsync(page, "Admin", "Product Management", ".*/admin/products$");

        var routedContent = page.GetByTestId("routed-page-content");
        await Assertions.Expect(routedContent.GetByRole(AriaRole.Heading, new() { Name = "Product Administration" }))
            .ToBeVisibleAsync();
    }

    public static async Task AssertAccessibilityBasicsAsync(IPage page)
    {
        await Assertions.Expect(page.Locator("html[lang]")).ToHaveCountAsync(1);

        var mainContent = page.GetByTestId("routed-page-content");
        await Assertions.Expect(mainContent).ToBeVisibleAsync();

        var headings = page.Locator("h1, h2, h3, h4, h5, h6, .mud-typography-h4, .mud-typography-h5, .mud-typography-h6");
        var headingCount = await headings.CountAsync();
        Assert.True(headingCount > 0, "Page should have at least one heading for accessibility.");

        var menuToggle = page.Locator("button[aria-label='Toggle navigation menu']");
        await Assertions.Expect(menuToggle).ToBeVisibleAsync();
    }

    public static async Task AssertLayoutSpacingAsync(IPage page)
    {
        var mainContent = page.GetByTestId("routed-page-content");
        var boundingBox = await mainContent.BoundingBoxAsync();

        Assert.NotNull(boundingBox);
        Assert.True(boundingBox.Width > 200, "Main content area should have meaningful width.");
        Assert.True(boundingBox.Height > 50, "Main content area should have meaningful height.");

        var appBar = page.Locator("header.mud-appbar");
        await Assertions.Expect(appBar).ToBeVisibleAsync();
        var appBarBox = await appBar.BoundingBoxAsync();
        Assert.NotNull(appBarBox);
        Assert.True(appBarBox.Y < boundingBox.Y, "App bar should be above main content.");
    }

    public static async Task AssertNavDrawerVisibleAsync(IPage page)
    {
        var drawer = page.GetByTestId("nav-drawer");
        await Assertions.Expect(drawer).ToBeVisibleAsync();

        var navLinks = drawer.Locator(".mud-nav-link");
        var linkCount = await navLinks.CountAsync();
        Assert.True(linkCount >= 4, $"Nav drawer should have at least 4 links, found {linkCount}.");
    }

    public async Task<ProductSnapshot> GetProductByExactNameAsync(string userId, string productName)
    {
        using var payload = await GetApiJsonAsync(
            $"api/products?page=1&pageSize=100&searchTerm={Uri.EscapeDataString(productName)}",
            userId);

        var product = payload.RootElement
            .GetProperty("items")
            .EnumerateArray()
            .FirstOrDefault(item => string.Equals(
                item.GetProperty("name").GetString(),
                productName,
                StringComparison.Ordinal));

        if (product.ValueKind == JsonValueKind.Undefined)
        {
            throw new Xunit.Sdk.XunitException($"Could not find product '{productName}' through the API.");
        }

        return MapProduct(product);
    }

    public async Task<ProductSnapshot?> TryGetProductByExactNameAsync(string userId, string productName)
    {
        using var payload = await GetApiJsonAsync(
            $"api/products?page=1&pageSize=100&searchTerm={Uri.EscapeDataString(productName)}",
            userId);

        var product = payload.RootElement
            .GetProperty("items")
            .EnumerateArray()
            .FirstOrDefault(item => string.Equals(
                item.GetProperty("name").GetString(),
                productName,
                StringComparison.Ordinal));

        return product.ValueKind == JsonValueKind.Undefined ? null : MapProduct(product);
    }

    public async Task<ProductSnapshot> GetProductByIdAsync(string userId, int productId)
    {
        using var payload = await GetApiJsonAsync($"api/products/{productId}", userId);
        return MapProduct(payload.RootElement);
    }

    public async Task<ProductSnapshot> UpdateProductAsync(string userId, ProductSnapshot product)
    {
        using var payload = await PutApiJsonAsync(
            $"api/products/{product.Id}",
            userId,
            new
            {
                id = product.Id,
                name = product.Name,
                roastLevel = product.RoastLevel,
                caffeineContent = product.CaffeineContent,
                grindType = product.GrindType,
                unitPrice = product.UnitPrice,
                isAvailable = product.IsAvailable,
                description = product.Description,
                imageUrl = product.ImageUrl
            });

        return MapProduct(payload.RootElement);
    }

    public async Task RestoreProductAvailabilityAsync(string userId, int productId, bool expectedAvailability)
    {
        var current = await GetProductByIdAsync(userId, productId);
        if (current.IsAvailable != expectedAvailability)
        {
            await PatchApiAsync($"api/products/{productId}/availability", userId);
        }
    }

    public async Task<ProductSnapshot?> ArchiveProductByIdAsync(
        string userId,
        int productId,
        string archivedNamePrefix)
    {
        ProductSnapshot current;
        try
        {
            current = await GetProductByIdAsync(userId, productId);
        }
        catch (HttpRequestException)
        {
            return null;
        }

        var archived = current with
        {
            Name = $"{archivedNamePrefix}-{current.Id}",
            IsAvailable = false
        };

        return await UpdateProductAsync(userId, archived);
    }

    public async Task<AdminUserScopeSnapshot> GetAdminUserScopeAsync(string userId, int activeOrganizationId)
    {
        using var payload = await GetApiJsonAsync("api/admin/users/scope", userId, activeOrganizationId);
        return DeserializeJson<AdminUserScopeSnapshot>(payload.RootElement);
    }

    public async Task<IReadOnlyList<OrganizationOptionSnapshot>> GetOrganizationOptionsAsync(
        string userId,
        int? activeOrganizationId = null)
    {
        using var payload = await GetApiJsonAsync("api/session/organization-options", userId, activeOrganizationId);
        return DeserializeJson<OrganizationOptionSnapshot[]>(payload.RootElement);
    }

    public async Task<AdminUserScopeSnapshot> GrantScopedUserAdminAsync(
        string userId,
        int activeOrganizationId,
        int organizationMembershipId)
    {
        using var payload = await PostApiJsonAsync(
            $"api/admin/users/memberships/{organizationMembershipId}/grant-user-admin",
            userId,
            payload: null,
            activeOrganizationId);

        return DeserializeJson<AdminUserScopeSnapshot>(payload.RootElement);
    }

    public async Task<AdminUserScopeSnapshot> RevokeScopedUserAdminAsync(
        string userId,
        int activeOrganizationId,
        int organizationMembershipId)
    {
        using var payload = await SendApiJsonAsync(
            HttpMethod.Delete,
            $"api/admin/users/memberships/{organizationMembershipId}/grant-user-admin",
            userId,
            payload: null,
            activeOrganizationId);

        return DeserializeJson<AdminUserScopeSnapshot>(payload.RootElement);
    }

    public async Task<IReadOnlyList<OrderSnapshot>> GetOrdersAsync(
        string userId,
        int? activeOrganizationId = null,
        int page = 1,
        int pageSize = 25)
    {
        using var payload = await GetApiJsonAsync(
            $"api/orders?page={page}&pageSize={pageSize}",
            userId,
            activeOrganizationId);

        return DeserializePagedItems<OrderSnapshot>(payload.RootElement);
    }

    public async Task<IReadOnlyList<OrderSnapshot>> GetOrdersByCustomerIdAsync(
        string userId,
        int customerId,
        int? activeOrganizationId = null,
        int page = 1,
        int pageSize = 25)
    {
        using var payload = await GetApiJsonAsync(
            $"api/orders/by-customer/{customerId}?page={page}&pageSize={pageSize}",
            userId,
            activeOrganizationId);

        return DeserializePagedItems<OrderSnapshot>(payload.RootElement);
    }

    public async Task<OrderSnapshot> GetOrderByIdAsync(string userId, int orderId, int? activeOrganizationId = null)
    {
        using var payload = await GetApiJsonAsync($"api/orders/{orderId}", userId, activeOrganizationId);
        return DeserializeJson<OrderSnapshot>(payload.RootElement);
    }

    public async Task<OrderSnapshot?> TryGetOrderByIdAsync(string userId, int orderId, int? activeOrganizationId = null)
    {
        try
        {
            return await GetOrderByIdAsync(userId, orderId, activeOrganizationId);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<CustomerOrderVerificationContext> GetCustomerOrderVerificationContextAsync(
        string customerUserId,
        int customerId,
        int? activeOrganizationId = null)
    {
        var visibleOrders = await GetOrdersByCustomerIdAsync(
            customerUserId,
            customerId,
            activeOrganizationId,
            pageSize: 10);

        var visibleOrder = visibleOrders.FirstOrDefault()
            ?? throw new Xunit.Sdk.XunitException($"No visible orders were returned for customer id {customerId}.");

        var systemAdminUserId = await GetSeededUserIdAsync("admin@acmecoffee.com");
        var allOrders = await GetOrdersAsync(systemAdminUserId, pageSize: 100);
        var foreignOrder = allOrders.FirstOrDefault(order => order.CustomerId != customerId)
            ?? throw new Xunit.Sdk.XunitException($"Could not find a foreign order id for customer id {customerId}.");

        return new CustomerOrderVerificationContext(visibleOrder, foreignOrder);
    }

    // ── Customer API helpers ──

    public async Task<CustomerSnapshot> GetCustomerByIdAsync(string userId, int customerId)
    {
        using var payload = await GetApiJsonAsync($"api/customers/{customerId}", userId);
        return DeserializeJson<CustomerSnapshot>(payload.RootElement);
    }

    public async Task<IReadOnlyList<CustomerSnapshot>> GetCustomersAsync(
        string userId,
        int page = 1,
        int pageSize = 25)
    {
        using var payload = await GetApiJsonAsync($"api/customers?page={page}&pageSize={pageSize}", userId);
        return DeserializePagedItems<CustomerSnapshot>(payload.RootElement);
    }

    public async Task<CustomerSnapshot> CreateCustomerAsync(string userId, CustomerSnapshot customer)
    {
        using var payload = await PostApiJsonAsync(
            "api/customers",
            userId,
            new
            {
                email = customer.Email,
                firstName = customer.FirstName,
                lastName = customer.LastName,
                phone = customer.Phone,
                address = customer.Address,
                city = customer.City,
                state = customer.State,
                zipCode = customer.ZipCode,
                isActive = customer.IsActive
            });

        return DeserializeJson<CustomerSnapshot>(payload.RootElement);
    }

    public async Task<CustomerSnapshot> UpdateCustomerAsync(string userId, CustomerSnapshot customer)
    {
        using var payload = await PutApiJsonAsync(
            $"api/customers/{customer.Id}",
            userId,
            new
            {
                id = customer.Id,
                email = customer.Email,
                firstName = customer.FirstName,
                lastName = customer.LastName,
                phone = customer.Phone,
                address = customer.Address,
                city = customer.City,
                state = customer.State,
                zipCode = customer.ZipCode,
                isActive = customer.IsActive
            });

        return DeserializeJson<CustomerSnapshot>(payload.RootElement);
    }

    // ── Order mutation API helpers ──

    public async Task UpdateOrderStatusAsync(string userId, int orderId, int newStatus)
    {
        using var _ = await SendApiJsonAsync(
            HttpMethod.Patch,
            $"api/orders/{orderId}/status",
            userId,
            new { newStatus });
    }

    public async Task<OrderItemSnapshot> AddOrderItemAsync(
        string userId,
        int orderId,
        int productId,
        int quantity,
        decimal unitPrice,
        decimal discount = 0m)
    {
        using var payload = await PostApiJsonAsync(
            $"api/orders/{orderId}/items",
            userId,
            new
            {
                orderId,
                productId,
                quantity,
                unitPrice,
                discount
            });

        return DeserializeJson<OrderItemSnapshot>(payload.RootElement);
    }

    public async Task RemoveOrderItemAsync(string userId, int orderId, int itemId)
    {
        using var client = new HttpClient
        {
            BaseAddress = new Uri(ApiBaseUrl),
            Timeout = TimeSpan.FromSeconds(10)
        };

        client.DefaultRequestHeaders.Add("X-Internal-Api-Key", InternalApiKey);
        client.DefaultRequestHeaders.Add("X-App-User-Id", userId);

        using var response = await client.DeleteAsync($"api/orders/{orderId}/items/{itemId}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<OrderNoteSnapshot>> GetOrderNotesAsync(string userId, int orderId)
    {
        using var payload = await GetApiJsonAsync($"api/orders/{orderId}/notes", userId);
        return DeserializeJson<OrderNoteSnapshot[]>(payload.RootElement);
    }

    public async Task<OrderNoteSnapshot> AddOrderNoteAsync(
        string userId,
        int orderId,
        string noteText,
        string author)
    {
        using var payload = await PostApiJsonAsync(
            $"api/orders/{orderId}/notes",
            userId,
            new { orderId, noteText, author });

        return DeserializeJson<OrderNoteSnapshot>(payload.RootElement);
    }

    public async Task<OrderNoteSnapshot> UpdateOrderNoteAsync(
        string userId,
        int orderId,
        int noteId,
        string noteText,
        string author)
    {
        using var payload = await PutApiJsonAsync(
            $"api/orders/{orderId}/notes/{noteId}",
            userId,
            new { id = noteId, orderId, noteText, author });

        return DeserializeJson<OrderNoteSnapshot>(payload.RootElement);
    }

    public async Task DeleteOrderNoteAsync(string userId, int orderId, int noteId)
    {
        using var client = new HttpClient
        {
            BaseAddress = new Uri(ApiBaseUrl),
            Timeout = TimeSpan.FromSeconds(10)
        };

        client.DefaultRequestHeaders.Add("X-Internal-Api-Key", InternalApiKey);
        client.DefaultRequestHeaders.Add("X-App-User-Id", userId);

        using var response = await client.DeleteAsync($"api/orders/{orderId}/notes/{noteId}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<JsonDocument> GetApiJsonAsync(string relativeUrl, string userId, int? activeOrganizationId = null)
    {
        return await SendApiJsonAsync(HttpMethod.Get, relativeUrl, userId, payload: null, activeOrganizationId);
    }

    public async Task<JsonDocument> PostApiJsonAsync(string relativeUrl, string userId, object? payload, int? activeOrganizationId = null)
    {
        return await SendApiJsonAsync(HttpMethod.Post, relativeUrl, userId, payload, activeOrganizationId);
    }

    public async Task<JsonDocument> PutApiJsonAsync(string relativeUrl, string userId, object payload, int? activeOrganizationId = null)
    {
        return await SendApiJsonAsync(HttpMethod.Put, relativeUrl, userId, payload, activeOrganizationId);
    }

    public async Task PatchApiAsync(string relativeUrl, string userId, int? activeOrganizationId = null)
    {
        using var client = new HttpClient
        {
            BaseAddress = new Uri(ApiBaseUrl),
            Timeout = TimeSpan.FromSeconds(10)
        };

        client.DefaultRequestHeaders.Add("X-Internal-Api-Key", InternalApiKey);
        client.DefaultRequestHeaders.Add("X-App-User-Id", userId);
        if (activeOrganizationId.HasValue)
        {
            client.DefaultRequestHeaders.Add("X-Active-Organization-Id", activeOrganizationId.Value.ToString());
        }

        using var request = new HttpRequestMessage(HttpMethod.Patch, relativeUrl);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public Task AssertNoClientErrorsAsync(IPage page)
    {
        if (_pageIssues.TryGetValue(page, out var issues) && !issues.IsEmpty)
        {
            var details = string.Join(Environment.NewLine, issues);
            throw new Xunit.Sdk.XunitException($"Browser-side failures were detected:{Environment.NewLine}{details}");
        }

        return Task.CompletedTask;
    }

    private async Task<JsonDocument> SendApiJsonAsync(
        HttpMethod method,
        string relativeUrl,
        string userId,
        object? payload = null,
        int? activeOrganizationId = null)
    {
        using var client = new HttpClient
        {
            BaseAddress = new Uri(ApiBaseUrl),
            Timeout = TimeSpan.FromSeconds(10)
        };

        client.DefaultRequestHeaders.Add("X-Internal-Api-Key", InternalApiKey);
        client.DefaultRequestHeaders.Add("X-App-User-Id", userId);
        if (activeOrganizationId.HasValue)
        {
            client.DefaultRequestHeaders.Add("X-Active-Organization-Id", activeOrganizationId.Value.ToString());
        }

        using var request = new HttpRequestMessage(method, relativeUrl);
        if (payload is not null)
        {
            request.Content = JsonContent.Create(payload);
        }

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        StopProcess(_webProcess);
        StopProcess(_apiProcess);
    }

    private static Process StartProject(string relativeProjectPath, ConcurrentQueue<string> output)
    {
        var repoRoot = GetRepoRoot();
        var projectDir = Path.Combine(repoRoot, relativeProjectPath);
        var projectName = Path.GetFileName(projectDir);
        var binDir = Path.Combine(projectDir, "bin");
        if (!Directory.Exists(binDir)
            || !Directory.EnumerateFiles(binDir, $"{projectName}.dll", SearchOption.AllDirectories).Any())
        {
            throw new InvalidOperationException(
                $"No build output was found for {relativeProjectPath}. Run 'dotnet build Dabbler.Platform.sln' before starting Playwright infrastructure.");
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "run --no-build --launch-profile http",
                WorkingDirectory = projectDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        process.StartInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        process.StartInfo.Environment["DOTNET_ENVIRONMENT"] = "Development";

        process.OutputDataReceived += (_, args) => CaptureProcessOutput(output, args.Data);
        process.ErrorDataReceived += (_, args) => CaptureProcessOutput(output, args.Data);
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (process.WaitForExit(2000))
        {
            var details = BuildProcessDiagnostics(output);
            throw new InvalidOperationException(
                $"The {relativeProjectPath} process exited immediately with code {process.ExitCode}.{details}");
        }

        return process;
    }

    private static async Task<bool> IsReachableAsync(string url)
    {
        try
        {
            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(2)
            };

            using var response = await client.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static async Task WaitForServerAsync(
        string url,
        string name,
        Process? process,
        ConcurrentQueue<string> output)
    {
        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(2)
        };

        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < deadline)
        {
            if (process is not null && process.HasExited)
            {
                var details = BuildProcessDiagnostics(output);
                throw new TimeoutException(
                    $"{name} exited before becoming healthy at {url} (exit code {process.ExitCode}).{details}");
            }

            try
            {
                using var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"{name} did not become healthy at {url}.{BuildProcessDiagnostics(output)}");
    }

    private static void StopProcess(Process? process)
    {
        if (process is null || process.HasExited)
        {
            return;
        }

        process.Kill(entireProcessTree: true);
        process.WaitForExit(5000);
        process.Dispose();
    }

    private static string GetRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (directory.EnumerateFiles("Dabbler.Platform.sln").Any())
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root from the current test output path.");
    }

    private static Dictionary<string, string> BuildTestHeaders() => new()
    {
        ["X-Test-Auth-Secret"] = TestAuthSecret
    };

    private static async Task<string> ReadJsonFromHelperPageAsync(IBrowserContext context, string url)
    {
        var helperPage = await context.NewPageAsync();
        try
        {
            await helperPage.SetExtraHTTPHeadersAsync(BuildTestHeaders());
            await helperPage.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 5000
            });

            return await helperPage.TextContentAsync("body") ?? string.Empty;
        }
        finally
        {
            await helperPage.CloseAsync();
        }
    }

    private static bool ShouldIgnoreResponseIssue(IResponse response)
    {
        var url = response.Url;

        if (url.Contains("/test-auth/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (url.Contains("/access-denied", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (url.Contains("/_blazor/disconnect", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static bool ShouldIgnoreRequestFailure(IRequest request)
    {
        return request.Url.Contains("/_blazor/disconnect", StringComparison.OrdinalIgnoreCase);
    }

    private static HttpClient CreateWebHelperClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
        client.DefaultRequestHeaders.Add("X-Test-Auth-Secret", TestAuthSecret);
        return client;
    }

    private static void CaptureProcessOutput(ConcurrentQueue<string> output, string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        output.Enqueue(line);
        while (output.Count > 40 && output.TryDequeue(out _))
        {
        }
    }

    private static string BuildProcessDiagnostics(ConcurrentQueue<string> output)
    {
        if (output.IsEmpty)
        {
            return string.Empty;
        }

        var lines = output.ToArray();
        return $"{Environment.NewLine}Recent process output:{Environment.NewLine}{string.Join(Environment.NewLine, lines)}";
    }

    private static ProductSnapshot MapProduct(JsonElement element) => new(
        element.GetProperty("id").GetInt32(),
        element.GetProperty("name").GetString() ?? string.Empty,
        element.GetProperty("roastLevel").GetInt32(),
        element.GetProperty("caffeineContent").GetInt32(),
        element.GetProperty("grindType").GetInt32(),
        element.GetProperty("unitPrice").GetDecimal(),
        element.GetProperty("isAvailable").GetBoolean(),
        element.TryGetProperty("description", out var description) && description.ValueKind != JsonValueKind.Null
            ? description.GetString()
            : null,
        element.TryGetProperty("imageUrl", out var imageUrl) && imageUrl.ValueKind != JsonValueKind.Null
            ? imageUrl.GetString()
            : null);

    private static T DeserializeJson<T>(JsonElement element)
    {
        return JsonSerializer.Deserialize<T>(element.GetRawText(), JsonOptions)
            ?? throw new InvalidOperationException($"Could not deserialize {typeof(T).Name}.");
    }

    private static IReadOnlyList<T> DeserializePagedItems<T>(JsonElement element)
    {
        if (!element.TryGetProperty("items", out var itemsElement) || itemsElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return itemsElement.EnumerateArray()
            .Select(item => DeserializeJson<T>(item))
            .ToArray();
    }
}

public sealed record MagicLinkMessage(
    string EmailType,
    string Email,
    string? Code,
    string? LinkUrl,
    string? QrCodeDataUri,
    DateTimeOffset CreatedAtUtc);

public sealed record SessionInfo(
    bool IsAuthenticated,
    string? UserId,
    string? Name,
    string? Email,
    string? CustomerId,
    string? ActiveOrganizationId,
    string? ActiveOrganizationKey,
    string? ActiveOrganizationName,
    SessionOrganization[]? AvailableOrganizations,
    string[]? Roles);

public sealed record SessionOrganization(
    int OrganizationId,
    string OrganizationKey,
    string OrganizationName,
    int? OrganizationMembershipId,
    bool IsDefault,
    bool IsInternal,
    string[]? Roles);

public sealed record ProductSnapshot(
    int Id,
    string Name,
    int RoastLevel,
    int CaffeineContent,
    int GrindType,
    decimal UnitPrice,
    bool IsAvailable,
    string? Description,
    string? ImageUrl);

public sealed record CustomerOrderVerificationContext(OrderSnapshot VisibleOrder, OrderSnapshot ForeignOrder);

public sealed class AdminUserScopeSnapshot
{
    public bool IsSystemAdmin { get; set; }

    public bool CanManageUsers { get; set; }

    public int? ActiveOrganizationId { get; set; }

    public string? ActiveOrganizationKey { get; set; }

    public string? ActiveOrganizationName { get; set; }

    public IReadOnlyList<AdminUserScopeUserSnapshot> Users { get; set; } = [];
}

public sealed class AdminUserScopeUserSnapshot
{
    public int UserId { get; set; }

    public int OrganizationMembershipId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public IReadOnlyList<string> ScopedRoles { get; set; } = [];

    public IReadOnlyList<string> OtherOrganizations { get; set; } = [];

    public bool HasScopedUserAdmin { get; set; }

    public bool CanGrantUserAdmin { get; set; }

    public bool CanRevokeUserAdmin { get; set; }
}

public sealed class OrderSnapshot
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;
}

public sealed class OrganizationOptionSnapshot
{
    public int OrganizationId { get; set; }

    public string OrganizationKey { get; set; } = string.Empty;

    public string OrganizationName { get; set; } = string.Empty;

    public IReadOnlyList<string> RoleNames { get; set; } = [];

    public bool IsActive { get; set; }
}

public sealed class CustomerSnapshot
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? ZipCode { get; set; }

    public bool IsActive { get; set; }
}

public sealed class OrderItemSnapshot
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Discount { get; set; }
}

public sealed class OrderNoteSnapshot
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public string NoteText { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;
}
