# Dabbler Platform

This repository is the integrated development home for the AcmeCoffeeOnline
rebuild, the required Dabbler libraries, and the AI-led workflow assets used
to run session-set development from one place.

## Current foundation

The repo currently includes:

- the rebuilt AcmeCoffeeOnline Web, API, and Shared projects
- a **MudBlazor-native UI layer** using MudBlazor components directly for
  layout, navigation, grids, forms, dialogs, and theming — replacing the
  prior custom wrapper abstractions
- consolidated `Dabbler.Security` and `Dabbler.Security.Blazor` source projects
- integrated `Dabbler.Components` library retaining the QBE filter model,
  expression engine, and data provider abstractions while delegating UI
  rendering to MudBlazor
- Dabbler Reports source projects under `src/Libs/`
- route-based navigation with `MudLayout`, `MudAppBar`, `MudDrawer`, and
  `MudNavMenu` replacing the prior shell/workspace/tab model
- explicit page composition: each page declares its own MudDataGrid columns,
  MudForm fields, and layout rather than generating UI from metadata
- central build and package management
- Docker Compose for web, api, sql server, MailHog, and Playwright
- xUnit, bUnit, and Playwright coverage including conformance tests, filter
  panel interaction tests, accessibility assertions, and browser workflows
- repo-root AI workflow assets: `requirements.txt`, `ai_router/`,
  `uat_runner/`, `AGENTS.md`, `CLAUDE.md`, and `GEMINI.md`
- durable cutover/guardrail docs

## Project layout

- `src/Apps/AcmeCoffeeOnline.Web/` — rebuilt Blazor Server host with
  MudBlazor-native pages and layout
- `src/Apps/AcmeCoffeeOnline.Api/` — rebuilt REST API host
- `src/Apps/AcmeCoffeeOnline.Shared/` — shared DTOs, enums, and models
- `src/Libs/` — integrated Dabbler libraries (Components, Security, Reports)
- `tests/` — xUnit, bUnit, and Playwright coverage
- `docker/` — compose and container definitions
- `ai_router/` — operator-tuned `router-config.yaml` and the
  append-only `router-metrics.jsonl` cost telemetry. The router
  itself is `pip install dabbler-ai-router` (declared in
  `requirements.txt`); the canonical source lives at
  [darndestdabbler/dabbler-ai-orchestration](https://github.com/darndestdabbler/dabbler-ai-orchestration)
  and the canonical extension's **Dabbler: Install ai-router**
  command handles the install for new clones.
- `uat_runner/` — platform-specific UAT runner package (CLI, triage,
  remediations, test cases). Calls into the canonical router via
  `import ai_router`.
- `docs/` — workflow docs and session-set artifacts

## Local commands

```bash
dotnet build Dabbler.Platform.sln
dotnet test Dabbler.Platform.sln

# E2E (Playwright runs from the host against the app containers):
docker compose -f docker/docker-compose.infra.yml up -d        # sqlserver + mailhog (start once, leave up)
docker compose -f docker/docker-compose.app.yml   up --build -d # api + web (rebuild after code changes)
dotnet test tests/AcmeCoffeeOnline.Playwright/
```

The compose stack is split into two files. `docker-compose.infra.yml` runs
the persistent dev dependencies (SQL Server, MailHog, and the `dabbler-dev`
network); start it once and leave it up across sessions. `docker-compose.app.yml`
runs the rebuildable app services (api, web) with explicit `image:` tags so
rebuilds replace the tagged images instead of orphaning dangling layers.
Playwright is intentionally **not** a compose service — run it from the
host (`dotnet test` or VS Code Test Explorer) against `http://localhost:5010`.
