# DabblerApp

Generated from the **dabbler-app** template — a full Blazor Server app with Dabbler platform libraries.

## Before you run

### 1. Database

Update the connection string in `appsettings.json` to point to your SQL Server instance:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=DabblerApp_Dev;Trusted_Connection=True;TrustServerCertificate=True"
}
```

The app runs `Database.MigrateAsync()` at startup to create the security schema and seeds `admin@local.test` with the SystemAdmin role. No manual `dotnet ef database update` is needed.

### 2. Email (magic-link sign-in)

By default the app sends magic-link emails to **MailHog** on `localhost:1025`.
Start MailHog before running the app:

```bash
# Docker
docker run -d -p 1025:1025 -p 8025:8025 mailhog/mailhog

# Or Mailpit (modern alternative)
docker run -d -p 1025:1025 -p 8025:8025 axllent/mailpit
```

Browse to <http://localhost:8025> to see delivered magic-link emails.

### 3. Seeded admin user

On first run, the app seeds **admin@local.test** in the security database. To sign in:

1. Navigate to `/login`
2. Enter `admin@local.test`
3. Click **Send sign-in link**
4. Open MailHog at <http://localhost:8025>, find the email, and click the link

## Before you deploy to production

Replace these settings in `appsettings.json` before any production deployment:

| Setting | What to do |
|---------|------------|
| `DabblerSecurity.InternalApiKey` | Replace with a strong random key |
| `DabblerSecurity.MfaEncryptionKey` | Replace with a securely-generated 32-byte base64 key |
| `ConnectionStrings.DefaultConnection` | Use your production SQL Server |
| `Email.SenderType` | Switch to `Smtp` and configure `Email.SmtpHost`, etc. |

## What's included

| Feature | Location |
|---------|----------|
| Magic-link sign-in | `Components/Pages/Login.razor` |
| Sample query-list page (server-paged grid) | `Components/Pages/SampleItems.razor` |
| Sample report (with PDF export) | `Components/Pages/SampleReport.razor` |
| Security dashboard | `/admin/security` (Dabbler.Security.Blazor) |
| In-memory sample data | `Data/SampleItemSeed.cs` |

## Running tests

```bash
dotnet test DabblerApp.Tests/
```

Tests use `Dabbler.Platform.Stubs` for DI stub providers — no running app needed.
