# Dabbler.Security

Dabbler.Security is a comprehensive authentication and authorization library for ASP.NET Core and Blazor Server applications. It provides passwordless magic link authentication, TOTP-based MFA, external identity provider integration, and a powerful runtime-configurable authorization system for fine-grained access control.

## Quick Start

1. Install the NuGet package:

    ```sh
    dotnet add package Dabbler.Security
    ```

2. Configure services and map endpoints in your `Program.cs`:

    ```csharp
    // 1. Add Dabbler.Security services
    builder.Services.AddDabblerSecurity(options =>
    {
        options.ConnectionString = builder.Configuration.GetConnectionString("Default")!;
        options.MagicLinkExpiry = TimeSpan.FromMinutes(10);
        options.RequireMfa = false;
        options.Email.SenderType = EmailSenderType.DevMailHog;

        // Register your application's protected resources
        options.RegisterPages(pages => pages.Add("Dashboard", "/dashboard"));
        options.RegisterEntities(entities => entities.Add("Order"));
        options.RegisterEndpoints(endpoints => endpoints.Add("CreateOrder", "POST /api/orders"));
        options.RegisterControls(controls => controls.Add("Dashboard", "ExportButton"));
    });

    // 2. Choose a persistence backend
    // For development or single-server deployments:
    builder.Services.UseJsonFileStore("security-config.json");

    // For production or multi-server deployments:
    // builder.Services.UseEfCoreStore();

    var app = builder.Build();

    // 3. Map the authentication endpoints
    app.MapDabblerSecurityEndpoints();

    app.Run();
    ```

## Key Features

### Authentication

- **Magic Link** -- Passwordless login via email with clickable links or 6-digit codes.
- **TOTP MFA** -- Time-based One-Time Password multi-factor authentication (e.g., Microsoft Authenticator).
- **External Identity Providers** -- Supports Microsoft Entra ID (OIDC) and generic OAuth providers.
- **Recovery Codes** -- Backup codes for users who lose their MFA device.
- **Cookie-Based Sessions** -- Secure, configurable cookie authentication with sliding expiration.

### Authorization (Runtime-Configurable)

- **Page Permissions** -- Control which roles can access specific pages/routes.
- **Entity Permissions** -- Grant CRUD-level (Create, Read, Update, Delete) access per entity, per role.
- **Field Permissions** -- Set per-role field visibility (Editable, ReadOnly, Hidden).
- **Endpoint Permissions** -- Secure API endpoints based on role.
- **Control Permissions** -- Manage UI control states (Enabled, Disabled, Hidden) by role.
- **Row Filters** -- Scope data access with per-role data filtering expressions.
- **Role Inheritance** -- Create role hierarchies where child roles inherit permissions.

### Infrastructure

- **Flexible Persistence** -- Use a JSON file store for development or an EF Core SQL Server store for production.
- **Auditing** -- SQL Server temporal table support for tracking changes.
- **Rate Limiting** -- Built-in protection against brute-force attacks, supporting in-memory or distributed cache.
- **Email Sending** -- Integrated email sender with providers for SMTP and development logging.

## Configuration

Key `DabblerSecurityOptions` properties:

| Option | Type | Description |
|---|---|---|
| `ConnectionString` | `string` | SQL Server connection string for the EF Core store and auditing. |
| `MagicLinkExpiry` | `TimeSpan` | How long magic links and codes remain valid. Default: 10 minutes. |
| `RequireMfa` | `bool` | A global setting to require MFA for all users. |
| `InternalApiKey` | `string` | A secret key for securing service-to-service communication. |
| `Email` | `EmailOptions` | Configures the email sender (type, host, port, credentials, from address). |
| `Mfa` | `MfaOptions` | Configures TOTP settings (lockout policies, encryption key, per-role requirements). |
| `RateLimiting` | `RateLimitingOptions` | Configures rate limiting, including an option to use `IDistributedCache`. |
| `UnregisteredResourcePolicy` | `UnregisteredResourcePolicy` | Determines access to unregistered resources. `FailOpen` (default) or `FailClosed`. |

## Auth Endpoints

`MapDabblerSecurityEndpoints()` adds the following routes:

| Method | Path | Description |
|---|---|---|
| `POST` | `/auth/send-magic-link` | Initiates the magic link login flow. |
| `POST` | `/auth/verify` | Verifies a magic link token or 6-digit code. |
| `POST` | `/auth/verify-totp` | Verifies a TOTP code during MFA challenge. |
| `POST` | `/auth/logout` | Signs the user out. |
| `GET` | `/auth/challenge/{scheme}` | Redirects to an external identity provider. |
| `GET` | `/auth/external-callback` | Handles the callback from an external provider. |

## Dependencies

- Microsoft.EntityFrameworkCore (>= 9.0.0)
- Microsoft.EntityFrameworkCore.SqlServer (>= 9.0.0)
- Otp.NET (>= 1.4.0)
- Microsoft.AspNetCore.Authentication.OpenIdConnect (>= 10.0.5)

## License

This package is licensed under the [MIT License](https://opensource.org/licenses/MIT).
