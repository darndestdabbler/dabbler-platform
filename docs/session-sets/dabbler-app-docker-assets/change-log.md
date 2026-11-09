# Change Log — dabbler-app-docker-assets

**Closed:** 2026-05-28
**Sessions:** 2 of 2

---

## Summary

Added a complete Docker/compose asset set to the `dabbler-app` template so a freshly scaffolded solution can be brought up end-to-end with two `docker compose` commands — infrastructure (SQL Server + Mailpit) and application (Web + Api).

---

## Changes

### Session 1 — Compose, Dockerfiles, Ports, Connection String

**Template files created:**

| File | Description |
|---|---|
| `docker/docker-compose.infra.yml` | SQL Server + Mailpit infra stack (start once, leave up) |
| `docker/docker-compose.app.yml` | Web + Api app stack with image tags |
| `docker/.env.example` | SA password template; `.env` is gitignored |
| `src/{Name}.Web/Dockerfile` | Multi-stage .NET 10 Blazor Server image |
| `src/{Name}.Api/Dockerfile` | Multi-stage .NET 10 Web API image |
| `src/{Name}.Web/appsettings.Docker.json` | Docker-path connection string override |
| `.dockerignore` | Excludes `.git`, `**/bin`, `**/obj`, `docker/.env` from build context |
| `nuget.config` | Wires `packages/` as a local NuGet feed for Dabbler.* packages |
| `packages/.gitkeep` | Keeps the `packages/` directory tracked in the template |

**Template files updated:**

| File | Change |
|---|---|
| `src/{Name}.Web/Properties/launchSettings.json` | Web port pinned to 5010 |
| `src/{Name}.Api/Properties/launchSettings.json` | Api port pinned to 5020 |
| `src/{Name}.Web/Program.cs` | `/_health` endpoint with `.AllowAnonymous()` |
| `src/{Name}.Api/Program.cs` | `/_health` endpoint |
| `.template.config/template.json` | `NameLower` symbol for lowercase Docker image names |
| `.gitignore` | `docker/.env` entry |

**Key decisions:**

| Decision | Choice | Rationale |
|---|---|---|
| Mail service | Mailpit (`axllent/mailpit:latest`) | Actively maintained successor to MailHog; same ports |
| Web port | 5010 | Spec requirement |
| Api port | 5020 | Spec requirement |
| SA password | `MSSQL_SA_PASSWORD` via `.env` | Parameterized; avoids hardcoded secrets |
| Docker image naming | `NameLower` template symbol | Docker requires lowercase image names |
| `--no-restore` on publish | Omitted | .NET 10 lesson: omitting `--no-restore` is required for Blazor static-asset discovery |

---

### Session 2 — Docs, Template Inventory, Verification

**Template files updated:**

| File | Change |
|---|---|
| `templates/Dabbler.Templates/content/dabbler-app/README.md` | Dual-path startup section: Docker Compose (recommended) + `dotnet run` |
| `templates/Dabbler.Templates/content/dabbler-app/.dockerignore` | **Bug fix:** removed `packages/*.nupkg` and `packages/*.snupkg` exclusions that prevented the local NuGet feed from reaching the Docker build context |

**Platform docs updated:**

| File | Change |
|---|---|
| `README.md` | Added note that `dabbler-app` ships a split compose stack |
| `CLAUDE.md` | Added `templates/Dabbler.Templates/` bullet to Solution Structure |
| `AGENTS.md` | Same |
| `GEMINI.md` | Same |

**Bug fix (`.dockerignore`):**

The Session 1 `.dockerignore` incorrectly excluded `packages/*.nupkg` and `packages/*.snupkg` from the Docker build context. Docker applies `.dockerignore` before transferring the build context, so `COPY packages/ packages/` inside the Dockerfile would receive an empty directory, causing `NU1101` for every Dabbler.* package. Discovered during the Session 2 smoke proof; fixed by removing the two incorrect lines.

---

## Smoke Proof Result (Session 2)

```
dotnet new dabbler-app --name SmokeTest -o C:/tmp/SmokeTest  → success
docker compose -f docker/docker-compose.infra.yml up -d      → SQL Server + Mailpit started
docker compose -f docker/docker-compose.app.yml up --build -d → api + web built and started
curl http://localhost:5010/_health  → {"status":"healthy"}
curl http://localhost:5020/_health  → {"status":"healthy"}
curl http://localhost:8025          → HTTP 200 (Mailpit UI)
docker compose ... down             → clean teardown
```

Both sessions verified: Session 1 by gpt-5-4 (VERIFIED), Session 2 by gpt-5-4 (VERIFIED).
