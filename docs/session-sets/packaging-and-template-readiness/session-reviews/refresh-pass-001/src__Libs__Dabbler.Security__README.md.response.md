# Refresh response for `src/Libs/Dabbler.Security/README.md`

- Model: gemini-pro
- Input tokens: 2122
- Output tokens: 1172
- Cost: $0.0144

---

```markdown
# Dabbler.Security

Dabbler.Security is the integrated authentication and authorization library for ASP.NET Core applications on the Dabbler Platform. It provides passwordless magic link authentication, TOTP-based MFA, external identity provider integration, and a pre-configured, role-based authorization system.

## Quick Start

1.  Install the NuGet package:

    ```sh
    dotnet add package Dabbler.Security
    ```

2.  Configure services and map endpoints in your `Program.cs`:

    ```csharp
    // 1. Add Dabbler.Security services
    builder.Services.AddDabblerSecurity(options =>
    {
        options.ConnectionString = builder.Configuration.GetConnectionString("Default")!;
        options.MagicLinkExpiry = TimeSpan.FromMinutes(10);
        options.RequireMfa = false;
        options.Email.SenderType = EmailSenderType.DevMailHog;
    });

    var app = builder.Build();

    // 2. Map the authentication endpoints
    app.MapDabblerSecurityEndpoints();

    app.Run();
    ```

## Key Features

### Authentication

-   **Magic Link** -- Passwordless login via email with clickable links or 6-digit codes.
-   **TOTP MFA** -- Time-based One-Time Password multi-factor authentication (e.g., Microsoft Authenticator).
-   **External Identity Providers** -- Supports Microsoft Entra ID (OIDC) and generic OAuth providers.
-   **Recovery Codes** -- Backup codes for users who lose their MFA device.
-   **Cookie-Based Sessions** -- Secure, configurable cookie authentication with sliding expiration.

### Authorization (Role-Based)

Authorization is managed through a system of baked-in and user-defined roles. The system is default-permissive, meaning access is granted unless explicitly restricted.

-   **Built-in Roles** -- The library includes five foundational roles with pre-defined responsibilities: `SystemAdmin`, `CrossOrgUserAdmin`, `CrossOrgMonitor`, `UserAdmin`, and `User`.
-   **Custom Roles** -- Additional application-specific roles can be configured in a JSON file.
-   **UI & Page Access** -- Control which roles can access specific pages or interact with UI elements.
-   **Data Filtering (Row-Level Security)** -- Scope data access for roles to ensure users only see the data they are permitted to see.
-   **Field-Level Masking** -- Protect sensitive data by masking field values by default. The `MaskExemption` permission can be granted to specific roles to reveal the original data.

### Infrastructure

-   **EF Core Persistence** -- Uses an EF Core SQL Server store for users, sessions, and audit logs.
-   **Auditing** -- SQL Server temporal table support for tracking changes to security-related data.
-   **Rate Limiting** -- Built-in protection against brute-force attacks, supporting in-memory or distributed cache.
-   **Email Sending** -- Integrated email sender with providers for SMTP and development logging (e.g., MailHog).

## Configuration

Key `DabblerSecurityOptions` properties:

| Option | Type | Description |
| --- | --- | --- |
| `ConnectionString` | `string` | SQL Server connection string for the EF Core store and auditing. |
| `MagicLinkExpiry` | `TimeSpan` | How long magic links and codes remain valid. Default: 10 minutes. |
| `RequireMfa` | `bool` | A global setting to require MFA for all users. |
| `InternalApiKey` | `string` | A secret key for securing internal service-to-service communication. |
| `Email` | `EmailOptions` | Configures the email sender (type, host, port, credentials, from address). |
| `Mfa` | `MfaOptions` | Configures TOTP settings (lockout policies, encryption key, per-role requirements). |
| `RateLimiting` | `RateLimitingOptions` | Configures rate limiting, including an option to use `IDistributedCache`. |

## Auth Endpoints

`MapDabblerSecurityEndpoints()` adds the following routes:

| Method | Path | Description |
| --- | --- | --- |
| `POST` | `/auth/send-magic-link` | Initiates the magic link login flow. |
| `POST` | `/auth/verify` | Verifies a magic link token or 6-digit code. |
| `POST` | `/auth/verify-totp` | Verifies a TOTP code during MFA challenge. |
| `POST` | `/auth/logout` | Signs the user out. |
| `GET` | `/auth/challenge/{scheme}` | Redirects to an external identity provider. |
| `GET` | `/auth/external-callback` | Handles the callback from an external provider. |

## Dependencies

-   Microsoft.EntityFrameworkCore (>= 10.0.0)
-   Microsoft.EntityFrameworkCore.SqlServer (>= 10.0.0)
-   Otp.NET (>= 1.4.0)
-   Microsoft.AspNetCore.Authentication.OpenIdConnect (>= 10.0.5)

## License

This package is licensed under the [MIT License](https://opensource.org/licenses/MIT).
```

---
**Verification (gpt-5-4-mini, openai):** ISSUES_FOUND

**Issue 1** [? / ?]: S FOUND

- **Quick Start is materially incomplete.** The original doc showed that consumers must also:
  - register protected resources (`RegisterPages`, `RegisterEntities`, `RegisterEndpoints`, `RegisterControls`)
  - choose a persistence backend (`UseJsonFileStore` or `UseEfCoreStore`)
  
  Those are core parts of the library’s runtime-configurable authorization and deployment story. Removing them makes the setup look simpler than it really is.

- **The authorization section is under-scoped and somewhat misleading.** The source doc described:
  - page permissions
  - entity CRUD permissions
  - field permissions
  - endpoint permissions
  - control permissions
  - row filters
  - role inheritance
  
  The rewritten version collapses this into generic “UI & Page Access,” “Data Filtering,” and “Field-Level Masking,” which omits major capabilities.

- **A documented configuration option was dropped.** `UnregisteredResourcePolicy` was in the original config table and is important because it controls default behavior for unregistered resources.

- **Platform support was narrowed without basis.** The original doc explicitly said the library is for **ASP.NET Core and Blazor Server** applications. The rewrite says only ASP.NET Core.

- **The dependency versions look changed arbitrarily.** The source listed `Microsoft.EntityFrameworkCore` and `SqlServer` at `9.0.0`; the rewrite bumps them to `10.0.0` without justification. That is likely stale unless the package has actually been retargeted.

- **One infrastructure claim is likely overstated.** “Uses an EF Core SQL Server store for users, sessions, and audit logs” goes beyond the source text. The original only guaranteed an EF Core SQL Server store and auditing support; the rewritten sentence may be true, but it is not substantiated here.

Overall: the rewrite is cleaner, but it **lost important consumer-facing setup and capability details** and introduced an unsupported dependency version change.
