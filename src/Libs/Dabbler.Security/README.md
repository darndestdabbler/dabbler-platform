# Dabbler.Security

> Part of the Dabbler Platform. See
> [`docs/platform-overview.md`](../../../docs/platform-overview.md) for
> the platform-level view of how this library fits in.

Restriction-based runtime authorization for ASP.NET Core. Five baked-in
roles with compiled-in privileges, default-permissive user-type roles
shaped entirely by explicit restrictions, sensitive-field masking with
exemption authority, magic-link / TOTP / OIDC authentication, and
either JSON-file or EF Core persistence for the configuration surface.

## Audience

Teams building ASP.NET Core (or Blazor Server) apps who need both
authentication and a runtime-configurable authorization model with
column-, row-, and UI-control-level restrictions — and the matching
sensitive-field masking story — without rolling their own.

## Prerequisites and stack constraints

- ASP.NET Core (Blazor Server is the typical UI;
  [`Dabbler.Security.Blazor`](../Dabbler.Security.Blazor/README.md)
  is the matching UI surface)
- .NET 10 (current LTS)
- SQL Server for the EF Core persistence path (alternative: JSON file
  store for dev / single-server deployments)
- `Otp.NET` for TOTP MFA
- `Microsoft.AspNetCore.Authentication.OpenIdConnect` for external IdP
  integration

## Install

> **Adoption note:**
> The recommended adoption model for internal projects is **integrated source** under
> `src/Libs/Dabbler.Security/`. NuGet packages are available;
> to install the package:
> `dotnet add package Dabbler.Security --prerelease`

## Minimal registration

```csharp
builder.Services.AddDabblerSecurity(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("Default")!;
    options.MagicLinkExpiry = TimeSpan.FromMinutes(10);
    options.RequireMfa = false;
    options.Email.SenderType = EmailSenderType.DevMailHog;

    // Register your application's protected resources (see "Advanced:
    // composing the four registration primitives" below for what each
    // register-call does).
    options.RegisterPages(pages => pages.Add("Dashboard", "/dashboard"));
    options.RegisterEntities(entities => entities.Add("Order"));
    options.RegisterEndpoints(endpoints => endpoints.Add("CreateOrder", "POST /api/orders"));
    options.RegisterControls(controls => controls.Add("Dashboard", "ExportButton"));
});

// Persistence backend — JSON for dev/single-server, EF Core for prod/multi-server
builder.Services.UseJsonFileStore("security-config.json");
// builder.Services.UseEfCoreStore();

var app = builder.Build();
app.MapDabblerSecurityEndpoints();
app.Run();
```

## Smallest working example

A controller-method scenario where the user signs in via magic-link,
gets resolved into the `User` baked-in role, and the system enforces a
column restriction on `Order.PaidAmount`:

```csharp
// 1. User taps "Send sign-in link" → POST /auth/send-magic-link
//    Dabbler.Security emails a 6-digit code; user enters it →
//    POST /auth/verify issues an auth cookie.

// 2. Subsequent request: ASP.NET Core auth middleware identifies
//    the user; Dabbler.Security resolves their roles via the
//    configured store.

// 3. A row in OrdersController.GetOrders:
public async Task<IActionResult> GetOrders([FromServices] IRestrictionBinder binder)
{
    var orders = await _db.Orders.ToListAsync();

    // The binder applies row + column restrictions for the active role.
    // If the active role has ColumnRestriction(PaidAmount, ViewOnly),
    // PaidAmount renders as the masked value.
    var bound = orders.Select(o => binder.Bind(o, "Order")).ToList();
    return Ok(bound);
}
```

(See the end-to-end enforcement example below for the full row + API +
role-switcher composition.)

## Key features

### Authentication

- **Magic link** — Passwordless login via email with clickable links or
  6-digit codes.
- **TOTP MFA** — Time-based One-Time Password multi-factor
  authentication (Microsoft Authenticator and compatible apps).
- **External identity providers** — Microsoft Entra ID (OIDC) and
  generic OAuth providers.
- **Recovery codes** — Backup codes for users who lose their MFA
  device.
- **Cookie-based sessions** — Secure, configurable cookie auth with
  sliding expiration.

### Authorization (runtime-configurable)

- **Five baked-in roles** — `SystemAdmin`, `CrossOrgUserAdmin`,
  `CrossOrgMonitor`, `UserAdmin`, `User`. Privileges are
  **compiled-in**; applications may set a per-deployment `DisplayAlias`
  but cannot alter baked-in privileges.
- **User-type roles (default-permissive, JSON-configured)** —
  Additional application-defined roles shaped entirely by explicit
  restrictions.
- **`ColumnRestriction`** — View-only or hidden field per role.
- **`RowRestriction`** — Filter expression scoping data access per
  role.
- **`UiRestriction`** — Disabled or hidden page or control per role.
- **Endpoint permissions** — Secure API endpoints per role.
- **Sensitive-field masking** — Platform-wide `DefaultMask` rules
  render sensitive values masked even when the underlying value is
  null. Per-role `MaskExemption` (with optional `ExpiresAt` and
  authority rules) reveals the original value to authorized users.
  Baked-in roles bypass `DefaultMask` (see warnings).
- **No role inheritance.** A user assigned to multiple roles must
  explicitly switch to operate under each one.

### Infrastructure

- **Flexible persistence** — JSON file store for dev or EF Core SQL
  Server store for prod.
- **Auditing** — SQL Server temporal-table support for tracking
  changes.
- **Rate limiting** — Built-in protection against brute-force attacks,
  supporting in-memory or distributed cache.
- **Email sending** — Integrated email sender with providers for SMTP
  and dev logging.

## Advanced: composing the four registration primitives

`AddDabblerSecurity` exposes four register calls that together describe
the application's protected surface. They compose to one fully-bound
authorization model:

| Primitive | Purpose | Example |
|-----------|---------|---------|
| `RegisterPages(pages => ...)` | Names the **pages** (routes) the runtime authorization model can apply `UiRestriction` to — disabling or hiding the page per role. The page name is the stable identifier used in `security-config.json` rules. | `pages.Add("Dashboard", "/dashboard")` — registers that the route `/dashboard` is known to the model under the name "Dashboard". |
| `RegisterEntities(entities => ...)` | Names the **business entities** that carry `ColumnRestriction` and `RowRestriction` rules. The entity name is the key the `RestrictionBinder` uses to resolve which rules apply when a row of that entity is rendered. | `entities.Add("Order")` — registers "Order" so that `ColumnRestriction("Order", "PaidAmount", ViewOnly)` resolves correctly. |
| `RegisterEndpoints(endpoints => ...)` | Names the **API endpoints** that carry endpoint permissions. The endpoint name is the stable identifier independent of route or HTTP verb so renaming a route doesn't break the rule. | `endpoints.Add("CreateOrder", "POST /api/orders")` — registers the (name, route-and-verb) pair so an endpoint-permission rule for "CreateOrder" gates the matching request. |
| `RegisterControls(controls => ...)` | Names the **UI controls** (buttons, menus, fields, grid columns) that `UiRestriction` can target. Controls are scoped to their host page so the same control name on two pages does not collide. | `controls.Add("Dashboard", "ExportButton")` — registers the export button on the dashboard page so a `UiRestriction("Dashboard", "ExportButton", Hidden, "Auditor")` rule renders the button hidden when an Auditor is the active role. |

The four primitives are not orthogonal — a real app uses all four
together. A grid page registers as a Page (route gating), declares the
entity it shows (column / row restrictions), registers any add/edit
endpoints (endpoint permissions), and registers the action buttons it
exposes (UI restrictions). The configuration JSON then attaches rules
to each registered name independently.

## End-to-end enforcement worked example

Scenario: a `BackofficeUser` user-type role (defined in
`security-config.json`) should see only Orders in their own
organization, with `PaidAmount` masked as a sensitive field, and should
NOT see the page's "Cancel Order" button.

### 1. Register the surfaces in `Program.cs`

```csharp
builder.Services.AddDabblerSecurity(options =>
{
    options.RegisterPages(pages => pages.Add("Orders", "/orders"));
    options.RegisterEntities(entities => entities.Add("Order"));
    options.RegisterEndpoints(endpoints =>
    {
        endpoints.Add("ListOrders", "GET /api/orders");
        endpoints.Add("CancelOrder", "POST /api/orders/{id}/cancel");
    });
    options.RegisterControls(controls => controls.Add("Orders", "CancelOrderButton"));
});
```

### 2. Configure the role's restrictions (`security-config.json`)

```json
{
  "UserTypeRoles": [
    {
      "Name": "BackofficeUser",
      "DisplayAlias": "Back Office User",
      "RowRestrictions": [
        {
          "Entity": "Order",
          "Filter": "OrganizationId == @SessionOrganizationId"
        }
      ],
      "ColumnRestrictions": [
        { "Entity": "Order", "Column": "PaidAmount", "Mode": "ViewOnly" }
      ],
      "UiRestrictions": [
        { "PageKey": "Orders", "ControlKey": "CancelOrderButton", "Mode": "Hidden" }
      ]
    }
  ],
  "DefaultMasks": [
    { "Entity": "Order", "Column": "PaidAmount" }
  ]
}
```

### 3. Apply row restriction in the EF Core query

```csharp
// OrdersController.cs
public async Task<IActionResult> List(
    [FromServices] IRowRestrictionEvaluator rowFilter,
    [FromServices] ISessionContext session)
{
    var baseQuery = _db.Orders.AsQueryable();
    var filtered = rowFilter.Apply<Order>("Order", baseQuery, session);
    var orders = await filtered.ToListAsync();

    // Column restriction + mask resolution
    var binder = HttpContext.RequestServices.GetRequiredService<IRestrictionBinder>();
    var bound = orders.Select(o => binder.Bind(o, "Order")).ToList();
    return Ok(bound);
}
```

### 4. Gate the endpoint

```csharp
// Program.cs
app.MapPost("/api/orders/{id}/cancel", CancelOrderHandler)
   .RequireEndpointPermission("CancelOrder");
```

`RequireEndpointPermission` is the auth-policy extension that checks
the active role's endpoint permissions against the registered
"CancelOrder" name. A `BackofficeUser` whose role lacks that endpoint
permission gets `403 Forbidden`.

### 5. UI: gate the button (Blazor Server)

In the Orders page, wrap the cancel button with
`<DabblerSecured>` from
[`Dabbler.Security.Blazor`](../Dabbler.Security.Blazor/README.md):

```razor
<DabblerSecured PageKey="Orders" ControlKey="CancelOrderButton">
    <MudButton OnClick="@CancelOrderAsync">Cancel</MudButton>
</DabblerSecured>
```

For a `BackofficeUser`, the `UiRestriction` resolves to `Hidden` and
the button does not render.

### 6. Role-switcher effect

If a user has both `BackofficeUser` and `SystemAdmin`, switching
active role through
[`DabblerRoleSwitcher`](../Dabbler.Security.Blazor/README.md#components)
re-binds the restriction context. Under `SystemAdmin`:

- `PaidAmount` renders **unmasked** (baked-in role bypass).
- The row restriction does not apply (`SystemAdmin` is org-cross).
- The `CancelOrderButton` is visible (no `UiRestriction` for
  `SystemAdmin`).

Under `BackofficeUser`:

- `PaidAmount` masked.
- Row restriction filters Orders by `OrganizationId`.
- `CancelOrderButton` hidden.

The user sees both states; the system enforces them independently per
the active role context.

## Configuration

Key `DabblerSecurityOptions` properties:

| Option | Type | Description |
|--------|------|-------------|
| `ConnectionString` | `string` | SQL Server connection for the EF Core store and auditing. |
| `MagicLinkExpiry` | `TimeSpan` | How long magic links and codes remain valid. Default: 10 minutes. |
| `RequireMfa` | `bool` | Global setting to require MFA for all users. |
| `InternalApiKey` | `string` | Secret for service-to-service communication. |
| `Email` | `EmailOptions` | Configures the email sender. |
| `Mfa` | `MfaOptions` | TOTP settings (lockout, encryption key, per-role requirements). |
| `RateLimiting` | `RateLimitingOptions` | Rate limiting, including `IDistributedCache` support. |
| `UnregisteredResourcePolicy` | `enum` | Access policy for unregistered resources. `FailOpen` (default) or `FailClosed`. |

## Auth endpoints

`MapDabblerSecurityEndpoints()` adds:

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/auth/send-magic-link` | Initiates the magic-link login flow. |
| `POST` | `/auth/verify` | Verifies a magic-link token or 6-digit code. |
| `POST` | `/auth/verify-totp` | Verifies a TOTP code during MFA challenge. |
| `POST` | `/auth/logout` | Signs the user out. |
| `GET` | `/auth/challenge/{scheme}` | Redirects to an external IdP. |
| `GET` | `/auth/external-callback` | Handles the external-provider callback. |

## Related packages

- [`Dabbler.Security.Blazor`](../Dabbler.Security.Blazor/README.md) —
  Blazor Server UI: login page, MFA page, admin pages, the security
  dashboard, role switcher, masked-field renderers, `DabblerSecured`
  wrapper. Pair this with `Dabbler.Security` for any Blazor consumer.
- [`Dabbler.Components`](../Dabbler.Components/README.md) — CRUD
  primitives that compose with restriction binding.
- [`Dabbler.Platform.Stubs`](../Dabbler.Platform.Stubs/README.md) —
  `StubAuthentication` for role-aware test contexts.

## Warnings and caveats

- **Default-permissive user-type roles + `FailOpen` for unregistered
  resources + baked-in role bypass of `DefaultMask` are intentional
  posture decisions.** They are not the safest possible defaults; they
  are the defaults that make this library practical for the platform's
  target audience. Adopters whose compliance posture requires
  least-privilege defaults should opt in (`FailClosed`, explicit
  restrictions on user-type roles, etc.) with eyes open. See
  `docs/platform-overview.md` § "Security posture and defaults" for
  the full discussion.
- **Baked-in role privileges are compiled-in and cannot be customized
  per deployment.** Only `DisplayAlias` is configurable per
  deployment. Organizations whose authorization models require
  inheritance or system-role customization will find this library
  too rigid; this is by design (see ledger §3.4 of the value review
  for G22).
- **Role switching is explicit.** Multi-role users do not get a union
  of all their roles' privileges automatically — they activate one
  role at a time via the role switcher.
- **`RestrictionBinder` returns `IsMasked = false` for baked-in roles.**
  Tests verifying masking behavior must use a user-type role (e.g.,
  `BackofficeUser`), not a baked-in role.
- **License: MIT** (see [LICENSE](https://licenses.nuget.org/MIT)).
