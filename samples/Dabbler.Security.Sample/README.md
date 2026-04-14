# Dabbler.Security Sample

This sample demonstrates `Dabbler.Security` with two authentication paths living side by side:

- OIDC sign-in for internal users through the local DevIdp
- Magic-link sign-in for external users, with TOTP MFA enforced for selected roles

After either login path completes, all users land in the same `AppUser`/role/permission pipeline.

## Docker Quick Start

Docker is the recommended way to run the full end-to-end sample, including the browser-based OIDC flow.

```bash
docker compose -f docker/docker-compose.yml up -d --build
```

Services:

| Service | URL |
|---------|-----|
| SampleApp.Web | `http://localhost:5001` |
| SampleApp.Api | `http://localhost:5002` |
| DevIdp | `http://localhost:5100` |
| Mailpit | `http://localhost:8025` |

Reset the sample state after the containers are healthy:

```bash
curl -X POST http://localhost:5002/seed
curl -X POST http://localhost:5001/seed-config
```

The login page at `http://localhost:5001/login` shows both the OIDC button and the magic-link form.

## Local Run Notes

You can still run the projects directly for debugging:

```bash
dotnet run --project samples/Dabbler.Security.Sample/Dabbler.Security.DevIdp
dotnet run --project samples/Dabbler.Security.Sample/SampleApp.Api
dotnet run --project samples/Dabbler.Security.Sample/SampleApp.Web
```

Default local URLs from launch settings:

| Project | URL |
|---------|-----|
| SampleApp.Web | `http://localhost:5084` |
| SampleApp.Api | `http://localhost:5005` |
| DevIdp | `https://localhost:5100` |

Magic-link development works fine with `dotnet run`. For the full seeded OIDC browser flow, Docker is the most faithful path right now because the seeded DevIdp client registration for the sample uses the Docker callback URI `http://localhost:5001/signin-EntraId`.

## Development HTTP vs Production HTTPS

The Docker-based development flow intentionally allows HTTP for local realism and simpler browser automation. Those allowances are development-only:

- `RequireHttpsMetadata` is disabled only in Development for the sample OIDC client
- the DevIdp disables transport-security enforcement only in Development
- the sample apps post-configure their cookie schemes to use `SameAsRequest` only in Development

The library still defaults to secure cookie behavior and HTTPS-oriented OIDC settings for non-development environments, so deployment is not being simplified in a way that hides production requirements.

## Seeded Users

### Magic Link

| Email | Roles | Notes |
|-------|-------|-------|
| `sysadmin@sample.local` | `SystemAdministrator` | No MFA |
| `admin@sample.local` | `UserAdministrator`, `Manager` | Seeded verified TOTP for tests |
| `editor@sample.local` | `Editor` | Seeded verified TOTP for tests |
| `viewer@sample.local` | `Viewer` | No MFA |
| `partner@external.com` | `Viewer` | External user, no MFA |

Magic-link emails are delivered to Mailpit at `http://localhost:8025`.

### OIDC / DevIdp

| Email | Password | Roles |
|-------|----------|-------|
| `admin@contoso.local` | `Pass123!` | `UserAdministrator`, `Manager` |
| `jane@contoso.local` | `Pass123!` | `Editor` |
| `viewer@contoso.local` | `Pass123!` | `Viewer` |

The sample also supports OIDC auto-provisioning. A DevIdp user who is not already present in the SampleApp database is created on first sign-in and receives the default `Viewer` role.

## DevIdp Notes

The local DevIdp is an OpenIddict-based development identity provider that emits Entra-style claims such as `oid`, `preferred_username`, `name`, `email`, and `roles`.

Key sample client registration:

| Client ID | Secret | Redirect URI |
|-----------|--------|--------------|
| `dabbler-security-sample` | `sample-secret-dev-only` | `http://localhost:5001/signin-EntraId` |

For Docker/browser testing, SampleApp.Web uses the DevIdp through `http://host.docker.internal:5100` inside the container so the host browser can still complete the login flow through `http://localhost:5100`.
