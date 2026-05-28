# DabblerApp

Generated from the **dabbler-app** template — a multi-project starter with a Blazor Server web app, a Web API starter, a shared library, and bUnit tests.

## Solution layout

- `DabblerApp.slnx` — solution file
- `src/DabblerApp.Web/` — Blazor Server host with Dabbler security, sample CRUD UI, and sample report
- `src/DabblerApp.Api/` — starter Web API host for app-specific endpoints
- `src/DabblerApp.Shared/` — shared contracts and models
- `tests/DabblerApp.Web.Tests/` — bUnit smoke tests

## Before you run

Choose the startup path that fits your workflow:

- **Docker Compose** (recommended) — fully containerised SQL Server, Mailpit, Web and Api. One-time setup, then two `docker compose` commands.
- **`dotnet run`** — connect to a local SQL Server (LocalDB or named instance) and run Mailpit separately.

---

### Docker Compose path

The template ships a `docker/` folder with a split compose stack — infrastructure (SQL Server + Mailpit) runs persistently; the app stack rebuilds on demand.

#### Prerequisites

- Docker Desktop (or Docker Engine + Compose plugin)
- The Dabbler.* NuGet packages. These ship in the **dabbler-platform** repo's `pack-output/` folder and are produced by running `dotnet pack` in that repo. Copy the `.nupkg` and `.snupkg` files from `<dabbler-platform>/pack-output/` into `packages/` at the root of this solution before building the Docker images. The `packages/` directory is a local NuGet feed wired into `nuget.config`; it must be populated before `docker compose ... up --build` runs the Docker build.

#### Start

```bash
# 1. Copy the env template and set your SA password
cp docker/.env.example docker/.env
#   Edit docker/.env and replace the placeholder SA password with a strong value.

# 2. Populate the local NuGet feed (one-time, or after a dabbler-platform update)
cp <dabbler-platform>/pack-output/*.nupkg  packages/
cp <dabbler-platform>/pack-output/*.snupkg packages/

# 3. Start the persistent infrastructure (SQL Server + Mailpit)
docker compose -f docker/docker-compose.infra.yml up -d

# 4. Build and start the app services
docker compose -f docker/docker-compose.app.yml up --build -d
```

Once up:

| Service | URL |
|---------|-----|
| Web app | <http://localhost:5010> |
| Web health | <http://localhost:5010/_health> |
| Api | <http://localhost:5020> |
| Api health | <http://localhost:5020/_health> |
| Mailpit UI | <http://localhost:8025> |

The compose files set `ASPNETCORE_ENVIRONMENT=Docker`, which activates `appsettings.Docker.json` inside the containers. That file overrides the connection string to point at the `db` container — no manual `appsettings.json` edits needed.

The app runs `Database.MigrateAsync()` at startup and seeds `admin@local.test`. To sign in, navigate to <http://localhost:5010/login>, request a sign-in link for `admin@local.test`, then open the Mailpit UI to retrieve the link.

#### Rebuild after code changes

```bash
docker compose -f docker/docker-compose.app.yml up --build -d
```

#### Tear down

```bash
# Stop the app stack (keep infra up for the next run)
docker compose -f docker/docker-compose.app.yml down

# Stop everything including SQL Server and Mailpit
docker compose -f docker/docker-compose.infra.yml down
```

---

### `dotnet run` path

#### 1. Database

Update the connection string in `src/DabblerApp.Web/appsettings.json` to point to your SQL Server instance:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=DabblerApp_Dev;Trusted_Connection=True;TrustServerCertificate=True"
}
```

The app runs `Database.MigrateAsync()` at startup to create the security schema and seeds `admin@local.test` with the SystemAdmin role. No manual `dotnet ef database update` is needed.

#### 2. Email (magic-link sign-in)

Start Mailpit (or MailHog) before running the app:

```bash
docker run -d -p 1025:1025 -p 8025:8025 axllent/mailpit
```

Browse to <http://localhost:8025> to see delivered magic-link emails.

#### 3. Seeded admin user

On first run, the app seeds **admin@local.test** in the security database. To sign in:

1. Navigate to `/login`
2. Enter `admin@local.test`
3. Click **Send sign-in link**
4. Open Mailpit at <http://localhost:8025>, find the email, and click the link

## Before you deploy to production

Replace these settings in `src/DabblerApp.Web/appsettings.json` before any production deployment:

| Setting | What to do |
|---------|------------|
| `DabblerSecurity.InternalApiKey` | Replace with a strong random key |
| `DabblerSecurity.MfaEncryptionKey` | Replace with a securely-generated 32-byte base64 key |
| `ConnectionStrings.DefaultConnection` | Use your production SQL Server |
| `Email.SenderType` | Switch to `Smtp` and configure `Email.SmtpHost`, etc. |

## What's included

| Feature | Location |
|---------|----------|
| Magic-link sign-in | `src/DabblerApp.Web/Components/Pages/Login.razor` |
| Sample query-list page (server-paged grid) | `src/DabblerApp.Web/Components/Pages/SampleItems.razor` |
| Sample report (with PDF export) | `src/DabblerApp.Web/Components/Pages/SampleReport.razor` |
| Security dashboard | `/admin/security` (Dabbler.Security.Blazor) |
| In-memory sample data | `src/DabblerApp.Web/Data/SampleItemSeed.cs` |
| Starter API host | `src/DabblerApp.Api/Program.cs` |
| Shared contracts library | `src/DabblerApp.Shared/` |

## Running tests

```bash
dotnet test tests/DabblerApp.Web.Tests/
```

Tests use `Dabbler.Platform.Stubs` for DI stub providers — no running app needed.
