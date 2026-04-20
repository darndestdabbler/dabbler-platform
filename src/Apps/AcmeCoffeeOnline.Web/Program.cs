using AcmeCoffeeOnline.Web.Configuration;
using System.Net;
using System.Security.Claims;
using AcmeCoffeeOnline.Shared.DTOs;
using AcmeCoffeeOnline.Shared.Diagnostics;
using AcmeCoffeeOnline.Web.Data;
using AcmeCoffeeOnline.Web.Diagnostics;
using AcmeCoffeeOnline.Web.Services;
using AcmeCoffeeOnline.Web.StubMode;
using Dabbler.Components.Data;
using Dabbler.Components.Data.Metadata;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Blazor.Extensions;
using Dabbler.Security.Data;
using Dabbler.Security.Extensions;
using Dabbler.Security.Options;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<DabblerAssetOptions>()
    .Bind(builder.Configuration.GetSection(DabblerAssetOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<DabblerAssetOptions>, DabblerAssetOptionsValidator>();

builder.Services
    .AddOptions<AppAuthenticationOptions>()
    .Bind(builder.Configuration.GetSection(AppAuthenticationOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<AppAuthenticationOptions>, AppAuthenticationOptionsValidator>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();

// ── Dabbler.Security (cookie auth + OIDC scheme + user lookup) ──────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\mssqllocaldb;Database=AcmeCoffeeOnline_Dev;Trusted_Connection=True;MultipleActiveResultSets=true";

builder.Services.AddDabblerSecurity(options =>
{
    options.ConnectionString = connectionString;
    options.InternalApiKey = builder.Configuration["DabblerSecurity:InternalApiKey"] ?? "dev-internal-key-acme";
    options.MagicLinkExpiry = TimeSpan.FromMinutes(15);
    options.RequireMfa = false;
    options.UnregisteredResourcePolicy = UnregisteredResourcePolicy.FailOpen;

    options.Email.SenderType = EmailSenderType.DevMailHog;

    options.Mfa.EncryptionKey = builder.Configuration["DabblerSecurity:MfaEncryptionKey"]
        ?? Convert.ToBase64String(new byte[32]);

    // ── OIDC: register Entra ID (points to DevIdp in development) ────────
    var oidcSection = builder.Configuration.GetSection("DabblerSecurity:Oidc");
    if (oidcSection.Exists())
    {
        options.AddEntraId(entra =>
        {
            entra.Authority = oidcSection["Authority"] ?? string.Empty;
            entra.ClientId = oidcSection["ClientId"] ?? string.Empty;
            entra.ClientSecret = oidcSection["ClientSecret"] ?? string.Empty;
            entra.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            entra.AutoProvisionUsers = true;
            entra.DefaultRoles = ["Customer"];
        });
    }

    // ── Protectable pages (mirrors API registrations) ────────────────────
    options.RegisterPages(pages =>
    {
        pages.Add("/", "Home", "Landing page");
        pages.Add("/products", "Products", "Product catalog");
        pages.Add("/customers", "Customers", "Customer management");
        pages.Add("/orders", "Orders", "Order management");
        pages.Add("/admin/users", "User Administration", "Manage backoffice users and roles");
        pages.Add("/admin/products", "Product Administration", "Manage product lists and pricing");
        pages.Add("/admin/security", "Security Dashboard", "Runtime security configuration");
    });

    // ── Protectable entities with fields ─────────────────────────────────
    options.RegisterEntities(entities =>
    {
        entities.Add("Product", "Product", fields =>
        {
            fields.Add("Name", "Name");
            fields.Add("RoastLevel", "Roast Level");
            fields.Add("CaffeineContent", "Caffeine Content");
            fields.Add("GrindType", "Grind Type");
            fields.Add("UnitPrice", "Unit Price");
            fields.Add("Description", "Description");
            fields.Add("IsAvailable", "Is Available");
        });
        entities.Add("Customer", "Customer", fields =>
        {
            fields.Add("Email", "Email");
            fields.Add("FirstName", "First Name");
            fields.Add("LastName", "Last Name");
            fields.Add("Phone", "Phone");
            fields.Add("Address", "Address");
            fields.Add("City", "City");
            fields.Add("State", "State");
            fields.Add("ZipCode", "Zip Code");
            fields.Add("IsActive", "Is Active");
            fields.Add("SocialSecurityNumber", "SSN");
        });
        entities.Add("Order", "Order", fields =>
        {
            fields.Add("OrderDate", "Order Date");
            fields.Add("ShipDate", "Ship Date");
            fields.Add("Status", "Status");
            fields.Add("Total", "Total");
        });
        entities.Add("OrderItem", "Order Item", fields =>
        {
            fields.Add("ProductId", "Product");
            fields.Add("Quantity", "Quantity");
            fields.Add("UnitPrice", "Unit Price");
            fields.Add("Discount", "Discount");
        });
        entities.Add("OrderNote", "Order Note", fields =>
        {
            fields.Add("NoteText", "Note Text");
            fields.Add("NoteDate", "Note Date");
            fields.Add("Author", "Author");
        });
    });

    // ── Protectable API endpoints ────────────────────────────────────────
    options.RegisterEndpoints(endpoints =>
    {
        endpoints.Add("GetProducts", "GET Products", "GET", "/api/products");
        endpoints.Add("GetProductById", "GET Product by ID", "GET", "/api/products/{id}");
        endpoints.Add("CreateProduct", "Create Product", "POST", "/api/products");
        endpoints.Add("UpdateProduct", "Update Product", "PUT", "/api/products/{id}");
        endpoints.Add("ToggleProductAvailability", "Toggle Availability", "PATCH", "/api/products/{id}/availability");
        endpoints.Add("GetCustomers", "GET Customers", "GET", "/api/customers");
        endpoints.Add("GetCustomerById", "GET Customer by ID", "GET", "/api/customers/{id}");
        endpoints.Add("CreateCustomer", "Create Customer", "POST", "/api/customers");
        endpoints.Add("UpdateCustomer", "Update Customer", "PUT", "/api/customers/{id}");
        endpoints.Add("SearchCustomers", "Search Customers", "GET", "/api/customers/search");
        endpoints.Add("GetOrders", "GET Orders", "GET", "/api/orders");
        endpoints.Add("GetOrderById", "GET Order by ID", "GET", "/api/orders/{id}");
        endpoints.Add("GetOrdersByCustomer", "GET Orders by Customer", "GET", "/api/orders/by-customer/{customerId}");
        endpoints.Add("CreateOrder", "Create Order", "POST", "/api/orders");
        endpoints.Add("UpdateOrderStatus", "Update Order Status", "PATCH", "/api/orders/{id}/status");
        endpoints.Add("AddOrderItem", "Add Order Item", "POST", "/api/orders/{orderId}/items");
        endpoints.Add("RemoveOrderItem", "Remove Order Item", "DELETE", "/api/orders/{orderId}/items/{itemId}");
        endpoints.Add("GetOrderNotes", "GET Order Notes", "GET", "/api/orders/{orderId}/notes");
        endpoints.Add("CreateOrderNote", "Create Order Note", "POST", "/api/orders/{orderId}/notes");
        endpoints.Add("UpdateOrderNote", "Update Order Note", "PUT", "/api/orders/{orderId}/notes/{noteId}");
        endpoints.Add("DeleteOrderNote", "Delete Order Note", "DELETE", "/api/orders/{orderId}/notes/{noteId}");
    });

    // ── Protectable UI controls ──────────────────────────────────────────
    options.RegisterControls(controls =>
    {
        controls.Add("CreateProductButton", "/products", "Create Product button");
        controls.Add("EditPriceButton", "/products", "Edit Price button");
        controls.Add("DeleteCustomerButton", "/customers", "Delete Customer button");
        controls.Add("DeleteOrderButton", "/orders", "Delete Order button");
        controls.Add("UpdateStatusButton", "/orders", "Update Order Status button");
    });
});

builder.Services.UseJsonFileStore(
    Path.Combine(builder.Environment.ContentRootPath, "security-config.json"));

// Cookie overrides for development
builder.Services.PostConfigure<CookieAuthenticationOptions>(
    CookieAuthenticationDefaults.AuthenticationScheme,
    options =>
    {
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Events ??= new CookieAuthenticationEvents();
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/auth-check"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/auth-check"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

builder.Services.PostConfigure<CookieAuthenticationOptions>(
    "DabblerExternalAuth",
    options =>
    {
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
    });

// Exchange-token store for Blazor Server auth hand-off
builder.Services.AddMemoryCache();

builder.Services.AddDabblerSecurityBlazor();

// Override the default NullUserListProvider so the User Assignments tab shows real users.
builder.Services.AddScoped<IUserListProvider, EfUserListProvider>();

if ((builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Test"))
    && builder.Configuration.GetValue<bool>("PlatformStubMode:Enabled"))
{
    builder.Services.AddPlatformStubMode();
}

// Typed HTTP clients backed by IHttpClientFactory with user identity forwarding
var apiBaseUrl = builder.Configuration["Services:ApiBaseUrl"]
    ?? builder.Configuration["ApiBaseUrl"]
    ?? "http://localhost:5011";
var internalApiKey = builder.Configuration["DabblerSecurity:InternalApiKey"] ?? "dev-internal-key-acme";

builder.Services.AddHttpClient<ProductApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Add("X-Internal-Api-Key", internalApiKey);
}).AddHttpMessageHandler<UserIdentityHandler>();

builder.Services.AddHttpClient<CustomerApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Add("X-Internal-Api-Key", internalApiKey);
}).AddHttpMessageHandler<UserIdentityHandler>();

builder.Services.AddHttpClient<OrderApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Add("X-Internal-Api-Key", internalApiKey);
}).AddHttpMessageHandler<UserIdentityHandler>();

builder.Services.AddHttpClient<AdminUserApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Add("X-Internal-Api-Key", internalApiKey);
}).AddHttpMessageHandler<UserIdentityHandler>();

builder.Services.AddHttpClient<OrganizationOptionsApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Add("X-Internal-Api-Key", internalApiKey);
}).AddHttpMessageHandler<UserIdentityHandler>();

builder.Services.AddScoped<ShellSessionStateLoader>();
builder.Services.AddTransient<OrderDetailState>();

// Grid data providers
builder.Services.AddTransient<IGridDataProvider<ProductDto>, ProductDataProvider>();
builder.Services.AddTransient<IGridDataProvider<CustomerDto>, CustomerDataProvider>();
builder.Services.AddTransient<IGridDataProvider<OrderDto>, OrderDataProvider>();
builder.Services.AddTransient<OrderItemDataProvider>();
builder.Services.AddTransient<OrderNoteDataProvider>();

// Report data providers (concrete types for report page injection)
builder.Services.AddTransient<OrderReportDataProvider>();
builder.Services.AddTransient<SalesReportDataProvider>();

// Dabbler Components: metadata and navigation providers
builder.Services.AddSingleton<IEntityMetadataProvider, AcmeCoffeeMetadataProvider>();
// INavigationDataProvider registration removed — was consumed only by DabblerShell,
// which has been replaced by MudBlazor-native layout with route-based navigation.

// Dev Tools: TestAuthClient is registered unconditionally. It reports IsEnabled from
// the same AppAuthenticationOptions gate the DevToolsMenu and /test-auth/* endpoints
// read, so the menu still hides in prod. Unconditional registration avoids DI failures
// if a dialog is resolved before the gate is consulted.
builder.Services.AddScoped<AcmeCoffeeOnline.Web.Services.DevTools.TestAuthClient>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Re-execute /not-found ONLY for genuine 404 responses. Do not intercept
// 401/403 — those must surface through the Blazor <NotAuthorized> fragment
// so the user sees the centered access-denied card instead of a 404 page.
app.UseStatusCodePages(async statusContext =>
{
    if (statusContext.HttpContext.Response.StatusCode != StatusCodes.Status404NotFound)
    {
        return;
    }

    var context = statusContext.HttpContext;
    var originalPath = context.Request.Path;
    var originalQueryString = context.Request.QueryString;

    context.Request.Path = "/not-found";
    context.Request.QueryString = QueryString.Empty;

    try
    {
        await statusContext.Next(context);
    }
    finally
    {
        context.Request.Path = originalPath;
        context.Request.QueryString = originalQueryString;
    }
});

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

var authOptions = app.Services.GetRequiredService<IOptions<AppAuthenticationOptions>>().Value;
var authObservabilityLogger = app.Services
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("AcmeCoffeeOnline.AuthObservability");

app.MapGet("/health", () => Results.Ok(
    HealthResponseFactory.CreateWeb(
        app.Environment.EnvironmentName,
        typeof(Program).Assembly,
        authOptions.DeliveryMode)));

app.MapGet("/auth-check", (ClaimsPrincipal user) => Results.Ok(new
{
    status = "authorized",
    userId = user.FindFirstValue(ClaimTypes.NameIdentifier),
    user = user.Identity?.Name ?? user.FindFirstValue(ClaimTypes.NameIdentifier),
    email = user.FindFirstValue(ClaimTypes.Email),
    customerId = user.FindFirstValue("CustomerId"),
    activeOrganizationId = user.FindFirstValue(UserSessionContextConstants.ActiveOrganizationIdClaimType),
    activeOrganizationKey = user.FindFirstValue(UserSessionContextConstants.ActiveOrganizationKeyClaimType),
    activeOrganizationName = user.FindFirstValue(UserSessionContextConstants.ActiveOrganizationNameClaimType),
    roles = user.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray()
})).RequireAuthorization();

if ((app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Test"))
    && authOptions.SeededSession.Enabled)
{
    app.MapGet("/test-auth/session-info", async (
        ClaimsPrincipal user,
        HttpContext context,
        IUserSessionContextService sessionContextService) =>
    {
        if (!authOptions.SeededSession.AllowNonLoopback && !IsLoopbackOrInMemoryRequest(context))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        if (!context.Request.Headers.TryGetValue("X-Test-Auth-Secret", out var providedSecret)
            || !string.Equals(providedSecret, authOptions.SeededSession.SharedSecret, StringComparison.Ordinal))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        UserSessionContext? sessionContext = null;
        if (int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var signedInUserId))
        {
            var activeOrganizationId = ParseActiveOrganizationId(user);
            sessionContext = await sessionContextService.BuildAsync(signedInUserId, activeOrganizationId);
        }

        return Results.Ok(new
        {
            isAuthenticated = user.Identity?.IsAuthenticated ?? false,
            userId = user.FindFirstValue(ClaimTypes.NameIdentifier),
            name = user.FindFirstValue(ClaimTypes.Name),
            email = user.FindFirstValue(ClaimTypes.Email),
            customerId = user.FindFirstValue("CustomerId"),
            activeOrganizationId = user.FindFirstValue(UserSessionContextConstants.ActiveOrganizationIdClaimType),
            activeOrganizationKey = user.FindFirstValue(UserSessionContextConstants.ActiveOrganizationKeyClaimType),
            activeOrganizationName = user.FindFirstValue(UserSessionContextConstants.ActiveOrganizationNameClaimType),
            availableOrganizations = sessionContext?.OrganizationMemberships.Select(organization => new
            {
                organizationId = organization.OrganizationId,
                organizationKey = organization.OrganizationKey,
                organizationName = organization.OrganizationName,
                organizationMembershipId = organization.OrganizationMembershipId,
                isDefault = organization.IsDefault,
                isInternal = organization.IsInternal,
                roles = organization.RoleNames
            }).ToArray() ?? [],
            roles = user.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray()
        });
    });

    app.MapGet("/test-auth/magic-links", (
        HttpContext context,
        DevEmailInbox inbox,
        string? email,
        bool clear = false) =>
    {
        if (!authOptions.SeededSession.AllowNonLoopback && !IsLoopbackOrInMemoryRequest(context))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        if (!context.Request.Headers.TryGetValue("X-Test-Auth-Secret", out var providedSecret)
            || !string.Equals(providedSecret, authOptions.SeededSession.SharedSecret, StringComparison.Ordinal))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var messages = inbox.GetAll()
            .Where(message => message.EmailType == "MagicLink")
            .Where(message => string.IsNullOrWhiteSpace(email)
                || string.Equals(message.Email, email, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(message => message.CreatedAtUtc)
            .ToArray();

        if (clear)
        {
            inbox.Clear();
        }

        AuthObservabilityLog.MagicLinkDiagnosticsRequested(
            authObservabilityLogger,
            string.IsNullOrWhiteSpace(email) ? "<all>" : MaskEmail(email),
            clear,
            messages.Length);

        return Results.Ok(messages);
    });

    app.MapGet("/test-auth/seeded-session", async (
        HttpContext context,
        DabblerSecurityDbContext db,
        IUserSessionContextService sessionContextService,
        string? roles,
        string? displayName,
        string? email,
        string? userId,
        int? organizationId) =>
    {
        if (!authOptions.SeededSession.AllowNonLoopback && !IsLoopbackOrInMemoryRequest(context))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        if (!context.Request.Headers.TryGetValue("X-Test-Auth-Secret", out var providedSecret)
            || !string.Equals(providedSecret, authOptions.SeededSession.SharedSecret, StringComparison.Ordinal))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var seededUser = await ResolveSeededUserAsync(db, email, userId);

        string[] requestedRoles;
        var parsedRoles = ParseRequestedRoles(roles);
        if (parsedRoles is { Length: > 0 })
        {
            requestedRoles = parsedRoles;
        }
        else if (seededUser is not null
                 && int.TryParse(seededUser.UserId, out var seededUserId)
                 && await sessionContextService.BuildAsync(seededUserId, organizationId) is { } seededSessionContext)
        {
            requestedRoles = seededSessionContext.EffectiveRoles.ToArray();
        }
        else
        {
            requestedRoles = seededUser?.Roles ?? ["BackofficeUser"];
        }

        var allowedRoles = authOptions.SeededSession.AllowedRoles;
        if (requestedRoles.Any(role => !allowedRoles.Contains(role, StringComparer.Ordinal)))
        {
            return Results.BadRequest(new { error = "One or more requested roles are not allowed for seeded sessions." });
        }

        var resolvedUserId = seededUser?.UserId ?? userId ?? "seeded-user";
        var resolvedDisplayName = string.IsNullOrWhiteSpace(displayName)
            ? seededUser?.DisplayName ?? "Seeded Session User"
            : displayName;
        var resolvedEmail = string.IsNullOrWhiteSpace(email)
            ? seededUser?.Email ?? "seeded.user@acme.test"
            : email;
        var principal = await BuildSeededSessionPrincipalAsync(
            sessionContextService,
            resolvedUserId,
            resolvedEmail,
            resolvedDisplayName,
            requestedRoles,
            organizationId,
            default);

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = true
            });

        AuthObservabilityLog.SeededSessionIssued(
            authObservabilityLogger,
            MaskEmail(resolvedEmail),
            resolvedUserId,
            string.Join(',', requestedRoles),
            seededUser is not null);

        return Results.Ok(new
        {
            status = "signed-in",
            userId = resolvedUserId,
            displayName = resolvedDisplayName,
            email = resolvedEmail,
            roles = requestedRoles,
            activeOrganizationId = principal.FindFirstValue(UserSessionContextConstants.ActiveOrganizationIdClaimType),
            activeOrganizationKey = principal.FindFirstValue(UserSessionContextConstants.ActiveOrganizationKeyClaimType),
            activeOrganizationName = principal.FindFirstValue(UserSessionContextConstants.ActiveOrganizationNameClaimType)
        });
    });

    app.MapGet("/test-auth/switch-organization", async (
        HttpContext context,
        ClaimsPrincipal user,
        IUserSessionContextService sessionContextService,
        int organizationId) =>
    {
        if (!authOptions.SeededSession.AllowNonLoopback && !IsLoopbackOrInMemoryRequest(context))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        if (!context.Request.Headers.TryGetValue("X-Test-Auth-Secret", out var providedSecret)
            || !string.Equals(providedSecret, authOptions.SeededSession.SharedSecret, StringComparison.Ordinal))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var signedInUserId))
        {
            return Results.Unauthorized();
        }

        var switchedPrincipal = await sessionContextService.CreatePrincipalAsync(
            signedInUserId,
            CookieAuthenticationDefaults.AuthenticationScheme,
            organizationId);

        if (switchedPrincipal is null)
        {
            return Results.BadRequest(new { error = "Unable to switch organizations for the current session." });
        }

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            switchedPrincipal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = true
            });

        return Results.Ok(new
        {
            status = "switched",
            activeOrganizationId = switchedPrincipal.FindFirstValue(UserSessionContextConstants.ActiveOrganizationIdClaimType),
            activeOrganizationKey = switchedPrincipal.FindFirstValue(UserSessionContextConstants.ActiveOrganizationKeyClaimType),
            activeOrganizationName = switchedPrincipal.FindFirstValue(UserSessionContextConstants.ActiveOrganizationNameClaimType),
            roles = switchedPrincipal.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray()
        });
    });

    app.MapGet("/test-auth/sign-out", async (HttpContext context) =>
    {
        if (!authOptions.SeededSession.AllowNonLoopback && !IsLoopbackOrInMemoryRequest(context))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        if (!context.Request.Headers.TryGetValue("X-Test-Auth-Secret", out var providedSecret)
            || !string.Equals(providedSecret, authOptions.SeededSession.SharedSecret, StringComparison.Ordinal))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Ok(new { status = "signed-out" });
    });
}

// ── Auth endpoints ──────────────────────────────────────────────────────────

// OIDC challenge — redirects browser to DevIdp/Entra ID login
app.MapGet("/auth/challenge/{scheme}", async (
    string scheme,
    string? returnUrl,
    HttpContext context,
    ILoggerFactory loggerFactory) =>
{
    var safeReturnUrl = IsLocalUrl(returnUrl) ? returnUrl! : "/";

    var properties = new AuthenticationProperties
    {
        RedirectUri = $"/auth/external-callback?returnUrl={Uri.EscapeDataString(safeReturnUrl)}",
        Items = { ["scheme"] = scheme, ["provider"] = scheme }
    };

    try
    {
        await context.ChallengeAsync(scheme, properties);
    }
    catch (Exception ex)
    {
        var logger = loggerFactory.CreateLogger("AcmeCoffeeOnline.ExternalAuth");
        logger.LogWarning(ex, "SSO challenge for scheme {Scheme} failed; redirecting to /login?error=sso-unavailable.", scheme);
        return Results.Redirect("/login?error=sso-unavailable");
    }

    return Results.Empty;
});

// OIDC callback — exchanges external cookie for application cookie
app.MapGet("/auth/external-callback", async (
    string? returnUrl,
    IExternalLoginService externalLoginService,
    IUserSessionContextService sessionContextService,
    ILoggerFactory loggerFactory,
    HttpContext context) =>
{
    var logger = loggerFactory.CreateLogger("AcmeCoffeeOnline.ExternalAuth");

    var authResult = await context.AuthenticateAsync("DabblerExternalAuth");
    if (!authResult.Succeeded || authResult.Principal == null)
    {
        logger.LogWarning("External authentication failed: {Failure}",
            authResult.Failure?.Message ?? "No principal");
        return Results.Redirect("/login?error=external-auth-failed");
    }

    var providerName = authResult.Properties?.Items["provider"] ?? "EntraId";

    ExternalClaimMapper.ExternalUserInfo userInfo;
    try
    {
        userInfo = ExternalClaimMapper.ExtractUserInfo(providerName, authResult.Principal);
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "Failed to extract user info from external claims");
        return Results.Redirect("/login?error=external-auth-failed");
    }

    var user = await externalLoginService.FindByExternalLoginAsync(providerName, userInfo.SubjectId);
    if (user == null)
    {
        try
        {
            user = await externalLoginService.ProvisionAndLinkAsync(
                providerName, userInfo.SubjectId, userInfo.Email, userInfo.DisplayName, ["Customer"]);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Failed to provision user from {Provider}", providerName);
            return Results.Redirect("/login?error=provisioning-failed");
        }
    }

    var principal = await sessionContextService.CreatePrincipalAsync(
        user.Id,
        CookieAuthenticationDefaults.AuthenticationScheme);
    if (principal is null)
    {
        logger.LogWarning("Failed to resolve session context for external login user {UserId}", user.Id);
        return Results.Redirect("/login?error=external-auth-failed");
    }

    await context.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principal);

    await context.SignOutAsync("DabblerExternalAuth");

    logger.LogInformation("External login successful: {Provider} user {UserId}", providerName, user.Id);

    var safeReturnUrl = IsLocalUrl(returnUrl) ? returnUrl! : "/";
    return Results.Redirect(safeReturnUrl);
});

// Magic link verify — exchanges token for exchange token (Blazor Server hand-off)
app.MapPost("/auth/verify", async (
    VerifyMagicLinkRequest req,
    IMagicLinkService svc,
    DabblerSecurityDbContext db,
    IMemoryCache cache,
    CancellationToken ct) =>
{
    var result = !string.IsNullOrWhiteSpace(req.Token)
        ? await svc.VerifyByTokenAsync(req.Token, ct)
        : await svc.VerifyByCodeAsync(req.Email ?? string.Empty, req.Code ?? string.Empty, ct);

    if (!result.Success || result.UserId is null || result.Email is null)
        return Results.BadRequest(result);

    if (result.RequiresMfa)
        return Results.Ok(new { result.Success, result.RequiresMfa, UserId = result.UserId.Value });

    var user = await db.AppUsers
        .Include(x => x.Roles)
        .ThenInclude(x => x.Role)
        .FirstAsync(x => x.Id == result.UserId.Value, ct);

    var exchangeToken = Guid.NewGuid().ToString("N");
    var userInfo = new ExchangeTokenInfo(
        user.Id, user.Email, user.DisplayName,
        user.Roles.Select(r => r.Role!.Name).ToArray());

    cache.Set(exchangeToken, userInfo, TimeSpan.FromSeconds(60));

    return Results.Ok(new { result.Success, result.RequiresMfa, user.Id, user.Email, ExchangeToken = exchangeToken });
});

if (app.Environment.IsDevelopment())
{
    app.MapPost("/auth/dev-test-sign-in", async (
        MagicLinkRequest req,
        HttpContext http,
        DabblerSecurityDbContext db,
        IMemoryCache cache,
        CancellationToken ct) =>
    {
        if (http.Connection.RemoteIpAddress is { } remoteIp && !IPAddress.IsLoopback(remoteIp))
        {
            return Results.NotFound();
        }

        var user = await db.AppUsers
            .Include(x => x.Roles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Email == req.Email, ct);

        if (user is null)
        {
            return Results.NotFound();
        }

        var exchangeToken = Guid.NewGuid().ToString("N");
        var userInfo = new ExchangeTokenInfo(
            user.Id, user.Email, user.DisplayName,
            user.Roles.Select(r => r.Role!.Name).ToArray());

        cache.Set(exchangeToken, userInfo, TimeSpan.FromSeconds(60));

        return Results.Ok(new { ExchangeToken = exchangeToken });
    });
}

app.MapPost("/auth/send-magic-link", async (
    MagicLinkRequest req,
    HttpContext ctx,
    IMagicLinkService svc,
    CancellationToken ct) =>
{
    var baseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}/login";
    var result = await svc.IssueAsync(req.Email, baseUrl, ctx.Connection.RemoteIpAddress?.ToString(), ct);
    return Results.Ok(result);
});

// Real browser request — sets cookie on the browser correctly
app.MapGet("/auth/complete", async (
    string token,
    string? returnUrl,
    HttpContext http,
    IMemoryCache cache,
    IUserSessionContextService sessionContextService) =>
{
    if (!cache.TryGetValue<ExchangeTokenInfo>(token, out var userInfo) || userInfo is null)
        return Results.Redirect("/login?error=expired");

    cache.Remove(token);
    var principal = await sessionContextService.CreatePrincipalAsync(
        userInfo.UserId,
        CookieAuthenticationDefaults.AuthenticationScheme);
    if (principal is null)
    {
        return Results.Redirect("/login?error=expired");
    }

    await http.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principal);

    var destination = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
    return Results.Redirect(destination);
});

// MFA verification — validates TOTP code and completes login
app.MapPost("/auth/verify-mfa", async (
    VerifyMfaRequest req,
    IMfaService mfaService,
    DabblerSecurityDbContext db,
    IMemoryCache cache,
    CancellationToken ct) =>
{
    var valid = await mfaService.ValidateTotpAsync(req.UserId, req.Code);
    if (!valid)
    {
        // Try recovery code as fallback
        valid = await mfaService.ValidateRecoveryCodeAsync(req.UserId, req.Code);
    }

    if (!valid)
        return Results.BadRequest(new { message = "Invalid code." });

    var user = await db.AppUsers
        .Include(x => x.Roles)
        .ThenInclude(x => x.Role)
        .FirstAsync(x => x.Id == req.UserId, ct);

    var exchangeToken = Guid.NewGuid().ToString("N");
    var userInfo = new ExchangeTokenInfo(
        user.Id, user.Email, user.DisplayName,
        user.Roles.Select(r => r.Role!.Name).ToArray());

    cache.Set(exchangeToken, userInfo, TimeSpan.FromSeconds(60));

    return Results.Ok(new { ExchangeToken = exchangeToken });
});

app.MapGet("/auth/logout", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.MapGet("/auth/switch-organization", async (
    int organizationId,
    string? returnUrl,
    HttpContext context,
    ClaimsPrincipal user,
    IUserSessionContextService sessionContextService) =>
{
    if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var signedInUserId))
    {
        return Results.Unauthorized();
    }

    var principal = await sessionContextService.CreatePrincipalAsync(
        signedInUserId,
        CookieAuthenticationDefaults.AuthenticationScheme,
        organizationId);

    if (principal is null)
    {
        return Results.BadRequest(new { error = "Unable to switch organizations for the current session." });
    }

    await context.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principal,
        new AuthenticationProperties
        {
            IsPersistent = true,
            AllowRefresh = true
        });

    var safeReturnUrl = IsLocalUrl(returnUrl) ? returnUrl! : "/";
    return Results.Redirect(safeReturnUrl);
}).RequireAuthorization();

app.MapStaticAssets();

app.MapRazorComponents<AcmeCoffeeOnline.Web.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();

static bool IsLocalUrl(string? url)
{
    if (string.IsNullOrEmpty(url)) return false;
    return url.StartsWith('/') && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'));
}

static bool IsLoopbackOrInMemoryRequest(HttpContext context)
{
    var remoteIp = context.Connection.RemoteIpAddress;
    return remoteIp is null || IPAddress.IsLoopback(remoteIp);
}

static string[] ParseRequestedRoles(string? roles)
{
    if (string.IsNullOrWhiteSpace(roles))
    {
        return [];
    }

    return roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

static string MaskEmail(string? email)
{
    if (string.IsNullOrWhiteSpace(email))
    {
        return "<none>";
    }

    var at = email.IndexOf('@');
    if (at <= 1)
    {
        return "***";
    }

    return $"{email[0]}***{email[(at - 1)..]}";
}

static int? ParseActiveOrganizationId(ClaimsPrincipal user)
{
    var activeOrganizationClaim = user.FindFirstValue(UserSessionContextConstants.ActiveOrganizationIdClaimType);
    return int.TryParse(activeOrganizationClaim, out var activeOrganizationId)
        ? activeOrganizationId
        : null;
}

static async Task<ClaimsPrincipal> BuildSeededSessionPrincipalAsync(
    IUserSessionContextService sessionContextService,
    string resolvedUserId,
    string resolvedEmail,
    string resolvedDisplayName,
    IReadOnlyList<string> requestedRoles,
    int? requestedOrganizationId,
    CancellationToken cancellationToken)
{
    if (int.TryParse(resolvedUserId, out var numericUserId))
    {
        var principal = await sessionContextService.CreatePrincipalAsync(
            numericUserId,
            CookieAuthenticationDefaults.AuthenticationScheme,
            requestedOrganizationId,
            requestedRoles,
            cancellationToken);

        if (principal is not null)
        {
            return principal;
        }
    }

    var fallbackClaims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, resolvedUserId),
        new(ClaimTypes.Name, resolvedDisplayName),
        new(ClaimTypes.Email, resolvedEmail)
    };

    fallbackClaims.AddRange(
        requestedRoles
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(role => new Claim(ClaimTypes.Role, role)));

    return new ClaimsPrincipal(
        new ClaimsIdentity(fallbackClaims, CookieAuthenticationDefaults.AuthenticationScheme));
}

static async Task<SeededUserInfo?> ResolveSeededUserAsync(
    DabblerSecurityDbContext db,
    string? email,
    string? userId)
{
    var query = db.AppUsers
        .AsNoTracking()
        .Include(user => user.Roles)
        .ThenInclude(userRole => userRole.Role)
        .AsQueryable();

    if (!string.IsNullOrWhiteSpace(email))
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await query.FirstOrDefaultAsync(appUser => appUser.Email == normalizedEmail);
        return user is null ? null : new SeededUserInfo(
            user.Id.ToString(),
            user.Email,
            user.DisplayName,
            user.Roles
                .Select(role => role.Role?.Name)
                .Where(roleName => !string.IsNullOrWhiteSpace(roleName))
                .Cast<string>()
                .ToArray());
    }

    if (int.TryParse(userId, out var parsedUserId))
    {
        var user = await query.FirstOrDefaultAsync(appUser => appUser.Id == parsedUserId);
        return user is null ? null : new SeededUserInfo(
            user.Id.ToString(),
            user.Email,
            user.DisplayName,
            user.Roles
                .Select(role => role.Role?.Name)
                .Where(roleName => !string.IsNullOrWhiteSpace(roleName))
                .Cast<string>()
                .ToArray());
    }

    return null;
}

public record MagicLinkRequest(string Email);
public record VerifyMagicLinkRequest(string? Token, string? Email, string? Code);
public record VerifyMfaRequest(int UserId, string Code);
public record ExchangeTokenInfo(int UserId, string Email, string DisplayName, string[] Roles);

public sealed record SeededUserInfo(
    string UserId,
    string Email,
    string DisplayName,
    string[] Roles);
