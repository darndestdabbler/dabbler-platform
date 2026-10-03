# DabblerApp

Generated from the **dabbler-min** template — a single-project Blazor Server starter with Dabbler Security only (magic-link auth, cookie auth, role-based authorization). No reports, no sample CRUD page, no test project.

> **Looking for a multi-project topology (Web + API + Shared + Tests)?** Use `dotnet new dabbler-app` instead. If your organization's architecture standards require physically separate Web Application and Web API projects, `dabbler-min` will not satisfy that rule by design — it's the incremental-adoption path for prototypes, spikes, and single-tier internal tools, not a production-application starter.

## Project layout

Single project. Auth endpoints live in `Program.cs` alongside the Blazor Server host by design — this is the lightest possible Dabbler Security adopter shape.

```
DabblerApp/
  DabblerApp.csproj
  Program.cs              # Blazor Server + Dabbler.Security wiring + /auth/* minimal-API endpoints
  appsettings.json
  appsettings.Development.json
  security-config.json    # JSON file store for roles, restrictions, masks
  _Imports.razor
  Components/
    App.razor
    Routes.razor
    Layout/               # MainLayout + AuthLayout
    Pages/                # Home, Login
    Shared/               # AccessDeniedCard
  wwwroot/app.css
```

## Before you run

### 1. Database

Update the connection string in `appsettings.json` to point to your SQL Server instance:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=DabblerApp_Dev;Trusted_Connection=True;TrustServerCertificate=True"
}
```

The app calls `Database.MigrateAsync()` at startup to create the security schema and seeds `admin@local.test` with the SystemAdmin role on first run.

### 2. Email (magic-link sign-in)

By default the app sends magic-link emails to **MailHog** on `localhost:1025`.
Start MailHog before running the app:

```bash
docker run -d -p 1025:1025 -p 8025:8025 mailhog/mailhog
```

Browse to <http://localhost:8025> to read the emails it catches.

### 3. Sign in

1. Run the app, navigate to `/login`
2. Enter `admin@local.test`, click **Send sign-in link**
3. Open MailHog, click the link in the email
4. You're now signed in with the `SystemAdmin` baked-in role; `/admin/security` is reachable.

## Before you deploy

Replace these settings in `appsettings.json` before any production deployment:

| Setting | What to do |
|---------|------------|
| `DabblerSecurity:InternalApiKey` | Generate a strong random key (≥32 chars) and store it in your secret manager. |
| `DabblerSecurity:MfaEncryptionKey` | Generate a 32-byte random value, base64-encode it, store securely. |
| `ConnectionStrings:DefaultConnection` | Switch to your production SQL Server, enable TLS. |
| `Email:SenderType` | Switch from `DevMailHog` to `Smtp` and configure SMTP host/port/credentials. |

## Adding CRUD pages

Use the [`Dabbler.CrudSlice` wrapper](https://github.com/darndestdabbler/dabbler-platform/blob/master/tools/Dabbler.CrudSlice/README.md). For a single-project consumer like this one, point both `--web-root` and `--api-root` at the project root. Generated controllers, services, and Blazor pages will live in `Controllers/`, `Services/`, and `Components/Pages/{Entity}/` respectively.

## Outgrowing `dabbler-min`

When the app gets large enough to justify a separate API tier, a shared contracts library, or a dedicated test project, scaffold a fresh `dabbler-app` solution and migrate. That's a deliberate growth path, not a gap — `dabbler-min` is the minimal adoption ramp for Dabbler Security.
