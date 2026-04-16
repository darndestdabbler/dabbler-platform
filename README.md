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
- repo-root AI workflow assets: `ai-router/`, `AGENTS.md`, `CLAUDE.md`, and
  `GEMINI.md`
- durable cutover/guardrail docs and a reusable `template-seed/` bootstrap folder

## Project layout

- `src/Apps/AcmeCoffeeOnline.Web/` — rebuilt Blazor Server host with
  MudBlazor-native pages and layout
- `src/Apps/AcmeCoffeeOnline.Api/` — rebuilt REST API host
- `src/Apps/AcmeCoffeeOnline.Shared/` — shared DTOs, enums, and models
- `src/Libs/` — integrated Dabbler libraries (Components, Security, Reports)
- `tests/` — xUnit, bUnit, and Playwright coverage
- `docker/` — compose and container definitions
- `ai-router/` — router code, prompt templates, and workflow utilities
- `docs/` — workflow docs and session-set artifacts
- `template-seed/` — lightweight local seed for repeating the AI-led workflow
  in a future repo

## Local commands

```bash
dotnet build Dabbler.Platform.sln
dotnet test Dabbler.Platform.sln
docker compose -f docker/docker-compose.yml up --build --abort-on-container-exit playwright
```
