# Dabbler-App Docker Assets Spec

> **Purpose:** Add a complete `docker/` asset set to the `dabbler-app`
> template so a freshly scaffolded solution can be brought up end-to-end
> with two `docker compose` commands — infrastructure (SQL Server +
> MailHog) and application (Web + Api). Today the template ships no
> `docker/` folder and the README tells consumers to run
> `docker run -d mailhog/mailhog` ad-hoc, which doesn't compose into a
> reproducible developer or CI workflow.
> **Created:** 2026-05-28
> **Session Set:** `docs/session-sets/dabbler-app-docker-assets/`
> **Prerequisite:** None. The `dabbler-app` template's four-project shape
> (`src/{Name}.Web`, `src/{Name}.Api`, `src/{Name}.Shared`,
> `tests/{Name}.Web.Tests`) is stable as of 2026-05-28.
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Session Set Configuration

```yaml
requiresUAT: false
requiresE2E: false
uatScope: none
```

> Rationale: This is a template infrastructure change. Verification is
> `dotnet new dabbler-app --name SmokeTest` + the two compose-up commands
> + an HTTP probe against the web port — fully automatable without UAT.

---

## Project Overview

The `dabbler-app` template produces a buildable four-project solution
today, but it stops short of the cross-cutting development scaffolding
that any real consumer needs: a way to run SQL Server and MailHog
without manual `docker run` commands, a way to run the Web + Api stack
in containers for integration testing, and a documented dev port.

This set ships those pieces in the template. The motivating consumer
is `Dabbler.NorthwindToBlazor` (and ultimately hundreds of Access →
Blazor migration projects to follow). Every such consumer will need
the same compose stack, the same connection-string story, and the same
predictable web port. Putting it in the template makes the work
one-and-done rather than re-authored per project.

### In scope

1. **`docker/docker-compose.infra.yml`** templated into the dabbler-app
   output. Services:
   - `db`: `mcr.microsoft.com/mssql/server:2022-latest`, `SA_PASSWORD`
     parameterized via `.env`, port `1433`, named volume for data,
     healthcheck via `sqlcmd`.
   - `mail`: `mailhog/mailhog` (or `axllent/mailpit` — Session 1
     decides), ports `1025` (SMTP) and `8025` (UI).
   - Shared docker network the app stack joins.
2. **`docker/docker-compose.app.yml`** templated into the dabbler-app
   output. Services:
   - `web`: built from `src/{Name}.Web/Dockerfile`, published on host
     port `5010`, joined to the infra network, depends on `db` healthy.
     Connection string overridden via env to point at `db:1433`.
   - `api`: built from `src/{Name}.Api/Dockerfile`, published on host
     port `5020` (or similar — Session 1 picks a default and pins it).
3. **`Dockerfile` per runnable project** (Web and Api). Multi-stage:
   SDK build stage → runtime stage on `mcr.microsoft.com/dotnet/aspnet`
   matching the template's `<TargetFramework>`.
4. **Web port pinned to 5010** in both `launchSettings.json` (local
   `dotnet run`) and compose, so the documented URL is the same
   whichever startup path the consumer takes. Api gets a parallel
   pinned port.
5. **Connection-string story.** The default `DefaultConnection` in the
   templated `appsettings.json` is the LocalDB string today
   (`Server=(localdb)\\mssqllocaldb;...`). Keep that as the
   `dotnet run` default. Add `appsettings.Docker.json` (or env-var
   override documented in the generated README) that points at
   `Server=db,1433;...;TrustServerCertificate=True` for the compose
   path. Compose's `web` and `api` services set
   `ASPNETCORE_ENVIRONMENT=Docker` or pass the connection string by
   env so the override fires without code changes.
6. **Health endpoints.** Add a minimal `/_health` endpoint on both
   Web and Api so compose `healthcheck` blocks have something to hit
   and integration tests can probe readiness deterministically.
7. **`.env.example`** in the templated `docker/` folder with the
   parameterized values (SA password, optional ports). `.gitignore` an
   `.env` next to it.
8. **Generated README updates.** The dabbler-app template's `README.md`
   (the one that lands in the scaffolded solution) gets a new section
   documenting the compose startup path: `docker compose -f
   docker/docker-compose.infra.yml up -d` → `docker compose -f
   docker/docker-compose.app.yml up --build -d` → browse
   `http://localhost:5010`. The existing `dotnet run` path stays
   documented as the alternative.
9. **dabbler-platform's own instruction files updated.** The template
   surface change must land in the right places so future AI
   orchestrators know it exists:
   - `templates/Dabbler.Templates/content/dabbler-app/README.md` (the
     templated README — see #8).
   - The top-level repo `README.md` section that lists what the
     template produces.
   - `CLAUDE.md`, `AGENTS.md`, `GEMINI.md` template-related sections
     where they enumerate the scaffold's output.
   - `docs/scaffolding-templates/` (or wherever the template inventory
     lives — Session 1 confirms the path) updated to list the new
     compose assets as part of the dabbler-app surface.

### Out of scope

- Production-grade compose (TLS termination, prod-grade secret
  management, replica sets). The compose files target dev / CI /
  integration-test workflows only.
- Kubernetes manifests, Helm charts, or any non-compose orchestrator.
- Image publishing to a registry. Images are built locally from the
  Dockerfiles; the template doesn't push.
- Compose for the `dabbler-min` template. That template targets a
  narrower minimal-deps scenario; if it needs docker assets later
  that's a separate spec.
- Cross-platform compose tweaks (Linux-host vs. Windows-host
  differences beyond what `docker compose` already abstracts).
- Database seeding strategy. The compose stack brings up an empty
  SQL Server; consumers do their own seeding (the Dabbler.Security
  schema is auto-migrated at app start per the existing template).

---

## Sessions

### Session 1 of 2: Compose, Dockerfiles, Ports, Connection String

**Steps:**

1. Read `docs/planning/project-guidance.md` and
   `docs/planning/lessons-learned.md`.
2. Pick the mail service (MailHog vs. Mailpit) and document the
   choice. Pin `db` image tag, mail image tag, Web port, Api port,
   SA password convention.
3. Author `docker-compose.infra.yml` and `docker-compose.app.yml`
   under `templates/Dabbler.Templates/content/dabbler-app/docker/`.
   Use template-variable placeholders for `{Name}` where the .NET
   templating engine substitutes the user's project name.
4. Author `Dockerfile` under
   `templates/Dabbler.Templates/content/dabbler-app/src/{Name}.Web/`
   and `.../src/{Name}.Api/`. Multi-stage, copy the
   `{Name}.Shared.csproj` ahead of the Web/Api csproj so the layer
   cache is well-behaved.
5. Update `launchSettings.json` in both Web and Api templates to
   pin the chosen ports.
6. Update the templated `appsettings.json` and add
   `appsettings.Docker.json` with the docker-aware connection
   string. Update compose env to select `Docker` environment.
7. Add minimal `/_health` endpoint to Web and Api. Wire the compose
   `healthcheck` blocks to the new endpoint.
8. Author `.env.example`. Update template's `.gitignore` to exclude
   `docker/.env`.
9. `dotnet build && dotnet test` against the template's own test
   suite to confirm no regressions.
10. End-of-session verification + commit + push + notify.

**Creates:**
- `templates/Dabbler.Templates/content/dabbler-app/docker/docker-compose.infra.yml`
- `templates/Dabbler.Templates/content/dabbler-app/docker/docker-compose.app.yml`
- `templates/Dabbler.Templates/content/dabbler-app/docker/.env.example`
- `templates/Dabbler.Templates/content/dabbler-app/src/{Name}.Web/Dockerfile`
- `templates/Dabbler.Templates/content/dabbler-app/src/{Name}.Api/Dockerfile`
- `templates/Dabbler.Templates/content/dabbler-app/src/{Name}.Web/appsettings.Docker.json`
- `docs/session-sets/dabbler-app-docker-assets/session-reviews/session-001.md`

**Touches:**
- `templates/Dabbler.Templates/content/dabbler-app/src/{Name}.Web/Program.cs` (health endpoint).
- `templates/Dabbler.Templates/content/dabbler-app/src/{Name}.Api/Program.cs` (health endpoint).
- `templates/Dabbler.Templates/content/dabbler-app/src/{Name}.Web/Properties/launchSettings.json` (port pin).
- `templates/Dabbler.Templates/content/dabbler-app/src/{Name}.Api/Properties/launchSettings.json` (port pin).
- `templates/Dabbler.Templates/content/dabbler-app/.gitignore` (docker/.env).
- `templates/Dabbler.Templates/content/dabbler-app/.template.config/template.json` (if asset includes/excludes need updating).

**Ends with:** A fresh `dotnet new dabbler-app --name SmokeTest`
followed by `docker compose -f docker/docker-compose.infra.yml up -d`
and `docker compose -f docker/docker-compose.app.yml up --build -d`
brings the stack up cleanly and `http://localhost:5010/_health`
returns 200.

---

### Session 2 of 2: Docs, Template Inventory, Verification

**Steps:**

1. Read the Session 1 review.
2. Update the templated `README.md` with the compose startup section
   (#8 in scope).
3. Update dabbler-platform's own consumer-facing docs: top-level
   `README.md` template-features section, `CLAUDE.md` / `AGENTS.md` /
   `GEMINI.md` template-output references, and any
   `docs/scaffolding-templates/` (or equivalent) inventory doc. Use
   `route(task_type="documentation")` for the refresh passes.
4. End-to-end smoke proof: `dotnet new dabbler-app --name SmokeTest`
   in a temp directory; bring up both compose stacks; HTTP-probe the
   web `:5010/_health` endpoint and the MailHog UI on `:8025`. Tear
   down. Record the smoke transcript inside this session's review.
5. `dotnet build && dotnet test` the template's own test suite.
6. End-of-session verification.
7. Closeout: `disposition.json`, `change-log.md`,
   `next-session-set-recommendation.md`, commit + push +
   `mark_session_complete()` + notify.

**Creates:**
- `docs/session-sets/dabbler-app-docker-assets/session-reviews/session-002.md`
- `docs/session-sets/dabbler-app-docker-assets/change-log.md`
- `docs/session-sets/dabbler-app-docker-assets/disposition.json`

**Touches:**
- `templates/Dabbler.Templates/content/dabbler-app/README.md` (compose
  startup section).
- Top-level `README.md`.
- `CLAUDE.md`, `AGENTS.md`, `GEMINI.md` (template-output sections).
- `docs/scaffolding-templates/` (or wherever the inventory lives).

**Ends with:** A future AI orchestrator scaffolding a dabbler-app
solution sees the compose assets documented in every place it would
look (template README, repo README, AGENTS / CLAUDE / GEMINI files,
template inventory doc), and the smoke proof in the session review
confirms the path works.

---

## Stop Rules

| Stop rule | Signal | Pivot |
|---|---|---|
| **SQL Server image won't start under the targeted compose version** | `db` service exits before healthcheck passes on a clean machine | Stop. Check the image's required Compose version + SA_PASSWORD complexity rules; surface to the human if a non-default workaround is needed. |
| **`dotnet new dabbler-app` smoke proof fails for a reason that's not template logic** | `dotnet new` errors on missing template assets or template-engine substitution glitches after Session 1's authoring | Stop. Investigate the `.template.config/template.json` includes/excludes; do not paper over with file-by-file workarounds. |
| **Health endpoint conflicts with Dabbler.Security's auth pipeline** | `/_health` requires auth or 302-redirects under the security wiring in `Program.cs` | Resolve via an `AllowAnonymous` attribute or auth-bypass route; do not move the health endpoint to a non-standard path. |
| **Docker port 5010 already in use on Northwind dev machine** | This is the consumer-side problem, not the template's — but if `5010` is widely contested, reconsider the default | Document the port-override path in the generated README; do not change the default without consumer-side justification. |

---

## Why now

The downstream consumer `Dabbler.NorthwindToBlazor` is mid-Set-001
and blocked on these docker assets to complete its first session's
spec. More importantly, the user has signalled this template will
serve "hundreds of Access databases" worth of consumer projects.
Authoring compose files once at the template level pays back every
new project that adopts dabbler-app — the alternative is N copies of
the same compose stack drifting independently in each consumer.
