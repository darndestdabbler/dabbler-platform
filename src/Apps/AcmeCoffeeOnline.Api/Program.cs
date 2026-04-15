using AcmeCoffeeOnline.Api.Data;
using AcmeCoffeeOnline.Api.Middleware;
using AcmeCoffeeOnline.Api.Services;
using AcmeCoffeeOnline.Shared.Diagnostics;
using Dabbler.Security.Extensions;
using Dabbler.Security.Middleware;
using Dabbler.Security.Options;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;

builder.Services.AddDbContext<AcmeCoffeeDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddHttpContextAccessor();

builder.Services.AddDabblerSecurity(options =>
{
    options.ConnectionString = connectionString;
    options.InternalApiKey = builder.Configuration["DabblerSecurity:InternalApiKey"] ?? "dev-internal-key-acme";
    options.DefaultOrganizationKey = builder.Configuration["DabblerSecurity:DefaultOrganizationKey"] ?? "internal";
    options.DefaultOrganizationName = builder.Configuration["DabblerSecurity:DefaultOrganizationName"] ?? "Internal Operations";
    options.MagicLinkExpiry = TimeSpan.FromMinutes(15);
    options.RequireMfa = false;
    options.UnregisteredResourcePolicy = UnregisteredResourcePolicy.FailOpen;

    options.Email.SenderType = EmailSenderType.DevMailHog;

    options.Mfa.EncryptionKey = builder.Configuration["DabblerSecurity:MfaEncryptionKey"]
        ?? Convert.ToBase64String(new byte[32]);

    // -- Protectable pages --
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

    // -- Protectable entities with fields --
    options.RegisterEntities(entities =>
    {
        entities.Add("Product", "Products", fields =>
        {
            fields.Add("Name", "Name");
            fields.Add("RoastLevel", "Roast Level");
            fields.Add("CaffeineContent", "Caffeine Content");
            fields.Add("GrindType", "Grind Type");
            fields.Add("UnitPrice", "Unit Price");
            fields.Add("Description", "Description");
            fields.Add("IsAvailable", "Is Available");
        });
        entities.Add("Customer", "Customers", fields =>
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
        });
        entities.Add("Order", "Orders", fields =>
        {
            fields.Add("OrderDate", "Order Date");
            fields.Add("ShipDate", "Ship Date");
            fields.Add("Status", "Status");
            fields.Add("Total", "Total");
        });
        entities.Add("OrderItem", "Order Items", fields =>
        {
            fields.Add("ProductId", "Product");
            fields.Add("Quantity", "Quantity");
            fields.Add("UnitPrice", "Unit Price");
            fields.Add("Discount", "Discount");
        });
        entities.Add("OrderNote", "Order Notes", fields =>
        {
            fields.Add("NoteText", "Note Text");
            fields.Add("NoteDate", "Note Date");
            fields.Add("Author", "Author");
        });
    });

    // -- Protectable API endpoints --
    options.RegisterEndpoints(endpoints =>
    {
        // Products
        endpoints.Add("GetProducts", "GET Products", "GET", "/api/products");
        endpoints.Add("GetProductById", "GET Product by ID", "GET", "/api/products/{id}");
        endpoints.Add("CreateProduct", "Create Product", "POST", "/api/products");
        endpoints.Add("UpdateProduct", "Update Product", "PUT", "/api/products/{id}");
        endpoints.Add("ToggleProductAvailability", "Toggle Availability", "PATCH", "/api/products/{id}/availability");
        // Customers
        endpoints.Add("GetCustomers", "GET Customers", "GET", "/api/customers");
        endpoints.Add("GetCustomerById", "GET Customer by ID", "GET", "/api/customers/{id}");
        endpoints.Add("CreateCustomer", "Create Customer", "POST", "/api/customers");
        endpoints.Add("UpdateCustomer", "Update Customer", "PUT", "/api/customers/{id}");
        endpoints.Add("SearchCustomers", "Search Customers", "GET", "/api/customers/search");
        // Orders
        endpoints.Add("GetOrders", "GET Orders", "GET", "/api/orders");
        endpoints.Add("GetOrderById", "GET Order by ID", "GET", "/api/orders/{id}");
        endpoints.Add("GetOrdersByCustomer", "GET Orders by Customer", "GET", "/api/orders/by-customer/{customerId}");
        endpoints.Add("CreateOrder", "Create Order", "POST", "/api/orders");
        endpoints.Add("UpdateOrderStatus", "Update Order Status", "PATCH", "/api/orders/{id}/status");
        endpoints.Add("AddOrderItem", "Add Order Item", "POST", "/api/orders/{orderId}/items");
        endpoints.Add("RemoveOrderItem", "Remove Order Item", "DELETE", "/api/orders/{orderId}/items/{itemId}");
        // Order Notes
        endpoints.Add("GetOrderNotes", "GET Order Notes", "GET", "/api/orders/{orderId}/notes");
        endpoints.Add("CreateOrderNote", "Create Order Note", "POST", "/api/orders/{orderId}/notes");
        endpoints.Add("UpdateOrderNote", "Update Order Note", "PUT", "/api/orders/{orderId}/notes/{noteId}");
        endpoints.Add("DeleteOrderNote", "Delete Order Note", "DELETE", "/api/orders/{orderId}/notes/{noteId}");
    });

    // -- Protectable UI controls --
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

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAdminUserScopeService, AdminUserScopeService>();
builder.Services.AddScoped<IOrganizationOptionsService, OrganizationOptionsService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    await DatabaseInitializer.InitializeAsync(app.Services);
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "AcmeCoffeeOnline API";
        options.Theme = ScalarTheme.BluePlanet;
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();

app.MapGet("/health", () => Results.Ok(
    HealthResponseFactory.Create(
        "api",
        app.Environment.EnvironmentName,
        typeof(Program).Assembly)));

var internalApiKey = builder.Configuration["DabblerSecurity:InternalApiKey"] ?? "dev-internal-key-acme";
app.UseMiddleware<InternalUserMiddleware>(internalApiKey);

app.UseAuthorization();
app.MapControllers();
app.MapDabblerSecurityEndpoints();

app.Run();
