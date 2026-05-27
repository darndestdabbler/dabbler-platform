# Getting Started with the Dabbler Platform

This tutorial walks you from zero to a running Dabbler starter solution in under 30 minutes. You will scaffold a multi-project app with magic-link authentication, restriction-based authorization, a CRUD data grid, a report with PDF export, and stub-backed bUnit tests — all from a single `dotnet new` command.

---

## What you'll build

The `dabbler-app` template scaffolds:

| Feature | Platform library |
|---------|-----------------|
| Magic-link sign-in + cookie auth | `Dabbler.Security` |
| Restriction-based authorization (roles, column/row/UI restrictions) | `Dabbler.Security` |
| Security admin dashboard (role management, masking, compliance) | `Dabbler.Security.Blazor` |
| CRUD list page with QBE filter and server paging | `Dabbler.Components` |
| Parameterized report with PDF export | `Dabbler.Reports.*` |
| Playwright watermark for async UI synchronization | `Dabbler.Testing.Blazor` |
| bUnit tests with stub DI providers | `Dabbler.Platform.Stubs` |

The `dabbler-min` template is a lighter alternative — security only, no reports, no test project. Use it when you want to adopt Dabbler security incrementally.

The full `dabbler-app` template scaffolds a solution with separate Web, API, Shared, and test projects.

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (LTS)
- A SQL Server instance for the security database (SQL Server LocalDB, SQL Server Express, or Docker)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) to run MailHog (the local magic-link email catcher)
- A terminal (PowerShell, bash, or zsh)

> **Stack note:** Dabbler targets Blazor Server + MudBlazor 8.x + .NET 10 + SQL Server. If any of these don't match your project's stack, evaluate whether to adopt Dabbler before proceeding.

---

## Step 1 — Install the Dabbler templates

Install the template package from the Dabbler NuGet feed:

```bash
dotnet new install Dabbler.Templates
```

Verify the templates are registered:

```bash
dotnet new list dabbler
```

Expected output:

```
Template Name               Short Name    Language  Tags
--------------------------  ------------  --------  ----------------------------------
Dabbler App                 dabbler-app   [C#]      Web/Blazor/Server/Security/Reports
Dabbler App (minimal)       dabbler-min   [C#]      Web/Blazor/Server/Security
```

---

## Step 2 — Scaffold your app

Use `dotnet new dabbler-app` to generate a multi-project starter solution. Replace `MyApp` with your project name:

```bash
dotnet new dabbler-app -o MyApp
cd MyApp
```

The generated tree:

```
MyApp/
  MyApp.slnx
  README.md
  src/
    MyApp.Web/              # Blazor Server web project
      Components/
        Layout/             # MudBlazor shell layout
        Pages/
          Home.razor
          SampleItems.razor # CRUD list page
          SampleReport.razor# Report page with PDF export
          Login.razor       # Magic-link sign-in
      Data/
        AppDbContext.cs     # EF Core in-memory DB for sample data
        SampleItem.cs
        SampleItemSeed.cs   # 5 seeded rows (A13)
        AppDbInitializer.cs # Seeds admin@local.test on first run (A12)
      Reports/
        SampleReportDataProvider.cs
        sample-items.rdf.json
      Services/
        SampleItemProvider.cs
      Program.cs
      appsettings.json
      appsettings.Development.json
      security-config.json  # JSON file store (roles, restrictions, masks)
    MyApp.Api/              # Starter Web API host for app-specific endpoints
    MyApp.Shared/           # Shared contracts / models library
  tests/
    MyApp.Web.Tests/        # bUnit test project
      SampleItemListTests.cs
```

---

## Step 3 — Configure the SQL Server connection

The security database (users, roles, organizations) stores in SQL Server. Update the connection string in `src/MyApp.Web/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MyApp_Dev;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

For Docker SQL Server:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=MyApp_Dev;User Id=sa;Password=YourPassword;TrustServerCertificate=True"
  }
}
```

> **Schema on first run:** The app calls `DabblerSecurityDbContext.Database.MigrateAsync()` at startup and seeds `admin@local.test` automatically — no manual `dotnet ef database update` step is needed.

---

## Step 4 — Start MailHog (magic-link email catcher)

The app sends magic-link sign-in emails to **MailHog** on `localhost:1025`. Start it before running the app:

```bash
docker run -d --name mailhog -p 1025:1025 -p 8025:8025 mailhog/mailhog
```

Or with [Mailpit](https://mailpit.axllent.org/) (modern alternative, same ports):

```bash
docker run -d --name mailpit -p 1025:1025 -p 8025:8025 axllent/mailpit
```

Browse the email inbox at **<http://localhost:8025>** after starting the container.

---

## Step 5 — Run the app

```bash
dotnet run --project src/MyApp.Web/
```

The console shows the URL (typically `https://localhost:7001` or `http://localhost:5000`). Open it in your browser.

You will be redirected to `/login` — that's expected. The app is secured by default.

---

## Step 6 — Sign in as admin

1. On `/login`, enter `admin@local.test`
2. Click **Send sign-in link**
3. Open MailHog at <http://localhost:8025>
4. Find the email from the app. Click the sign-in link inside it.

You are now signed in as **admin@local.test** with the `SystemAdmin` baked-in role, which grants full access to all pages and all security admin features.

> **What just happened:** The magic-link flow issued a time-limited URL containing a signed token. The `/auth/complete` endpoint exchanged the token for a session cookie. The token expires after 15 minutes.

---

## Step 7 — Explore the sample query-list page

Navigate to **`/items`**.

The page shows a server-paged `MudDataGrid<SampleItem>` backed by `IGridDataProvider<SampleItem>` with 5 pre-seeded rows:

| Name | Value | Active |
|------|-------|--------|
| Alpha Widget | $12.50 | ✓ |
| Beta Gadget | $99.00 | ✓ |
| Gamma Thingamajig | $3.75 | |
| Delta Doohickey | $255.00 | ✓ |
| Epsilon Gizmo | $18.99 | ✓ |

The grid uses server-side paging via `IGridDataProvider<SampleItem>`. The backing store is an EF Core InMemory database seeded at startup.

---

## Step 8 — Render the sample report and export to PDF

Navigate to **`/reports/sample`**.

The **Sample Items Report** auto-runs on load (it has no parameters). You will see:

- A banded report with Name, Value, and Description columns
- A grand-total aggregate in the footer showing the sum of all active-item values

Click **Export to PDF** to download `sample-items-report.pdf`. The PDF is generated server-side by the `DabblerPdf` rendering engine (LGPL-2.1-only OR MPL-2.0 — see [THIRD-PARTY-NOTICES](../src/Libs/Dabbler.Reports.Export/THIRD-PARTY-NOTICES.txt)).

---

## Step 9 — Explore the Security Dashboard

Navigate to **`/admin/security`**.

The Dabbler Security Dashboard lets you:

- **Create user-type roles** (e.g., `BackofficeUser`, `ReadOnly`) and assign ColumnRestriction / RowRestriction / UiRestriction rules
- **Set default masks** (sensitive-field masking applied across all user-type roles)
- **Grant mask exemptions** with an authority model and optional expiry
- **Manage organizations and users**

The dashboard is provided by `Dabbler.Security.Blazor` — no custom code needed.

---

## Step 10 — Run the scaffolded bUnit tests

```bash
dotnet test tests/MyApp.Web.Tests/
```

Expected output:

```
Passed!  - Failed: 0, Passed: 1, Skipped: 0
```

The `SampleItemListTests` class uses `Dabbler.Platform.Stubs` to register stub DI providers instead of a real database, making the test fast and isolated:

```csharp
// From the scaffolded test (§4.4 bUnit opt-in pattern):
Services.AddDabblerPlatformStubs(new StubPlatformDefinition());
Services.AddTransient<IGridDataProvider<SampleItem>, SampleItemProvider>();
```

---

## Before you deploy

> The template's defaults are for local development only. Review each setting before deploying to any shared or production environment.

| Setting | Action required |
|---------|----------------|
| `DabblerSecurity:InternalApiKey` | Generate a strong random key (≥32 chars). Store in Azure Key Vault, AWS Secrets Manager, or equivalent. |
| `DabblerSecurity:MfaEncryptionKey` | Generate a 32-byte random value and base64-encode it. Store securely. |
| `ConnectionStrings:DefaultConnection` | Switch to your production SQL Server. Enable TLS. |
| `Email.SenderType` | Change from `DevMailHog` to `Smtp`. Add `Email:SmtpHost`, `Email:SmtpPort`, `Email:FromAddress`, `Email:SmtpUser`, `Email:SmtpPass` to your production config. |
| Magic-link sign-in | Acceptable for some apps. For enterprise, add OIDC (see below). |
| `security-config.json` path | The JSON file store persists roles and restrictions. Move to a writable, backed-up location outside `ContentRootPath` on multi-instance deployments — or switch to `UseEfCoreStore()`. |

### Adding OIDC (Entra ID / DevIdp)

For teams that want Entra-ID-compatible SSO locally:

```bash
# Install the DevIdp CLI tool
dotnet tool install -g Dabbler.Security.DevIdp.Tool

# Start the local OIDC provider on https://localhost:5100
dabbler-devidp
```

Then update `Program.cs` to call `options.AddEntraId(entra => { ... })` with `Authority = "https://localhost:5100"`. See [Dabbler.Security README](../src/Libs/Dabbler.Security/README.md) for the full OIDC wiring.

---

## Next steps

| Goal | Where to go |
|------|-------------|
| Understand the platform architecture | [docs/platform-overview.md](./platform-overview.md) |
| Deep-dive a specific library | [src/Libs/Dabbler.*/README.md](../src/Libs/) |
| Add a new page with security | [Dabbler.Security README](../src/Libs/Dabbler.Security/README.md) → RegisterPages / RegisterEntities |
| Add a real CRUD entity | Copy `SampleItems.razor` + `SampleItemProvider.cs`, swap `SampleItem` for your entity |
| Add a new report | Copy `sample-items.rdf.json`, author the RDF, register a data provider |
| Configure roles and restrictions | `/admin/security` dashboard, or edit `security-config.json` directly |
| Install only security (no reports) | `dotnet new dabbler-min -o MyApp` |
| Package the platform for distribution | [docs/packaging/](./packaging/) |

---

## Troubleshooting

**App won't start / DB connection error**  
Check `appsettings.json` connection string. For LocalDB, ensure the SQL Server LocalDB instance is running: `sqllocaldb start mssqllocaldb`.

**Magic-link email not arriving in MailHog**  
Verify MailHog is running: `docker ps | grep mailhog`. Confirm the app's SMTP port is `1025` (set in `DevMailHog` sender defaults).

**Sign-in link expired**  
Magic links expire after 15 minutes (configurable via `options.MagicLinkExpiry`). Request a new link.

**`/items` shows empty grid**  
The sample data is in-memory and re-seeded on every app restart. If you clear the database or the app crashed before seeding, restart the app.

**bUnit tests fail with DI errors**  
Make sure `DabblerApp.Tests.csproj` references both `Dabbler.Platform.Stubs` (meta-package) and `Dabbler.Platform.Stubs.Components` (for `AddStubGridDataProvider`). Check the generated csproj.
