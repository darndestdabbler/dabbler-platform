# Dabbler Platform

This repository is the integrated development home for the AcmeCoffeeOnline
rebuild, the required Dabbler libraries, and the AI-led workflow assets used
to run session-set development from one place.

## Current foundation

The repo currently includes:

- the rebuilt AcmeCoffeeOnline Web, API, and Shared projects
- consolidated `Dabbler.Security` and `Dabbler.Security.Blazor` source projects
- integrated `Dabbler.Components` and Dabbler Reports source projects under `src/Libs/`
- the migrated Acme shell, auth pages, catalog/customer/order flows, and initial report surfaces
- central build and package management
- Docker Compose for web, api, sql server, MailHog, and Playwright
- xUnit and Playwright coverage, including self-starting smoke tests plus meaningful auth, permission, and backend-confirmed browser flows
- repo-root AI workflow assets: `ai-router/`, `AGENTS.md`, `CLAUDE.md`, and `GEMINI.md`
- durable cutover/guardrail docs and a reusable `template-seed/` bootstrap folder

## Project layout

- `src/Apps/AcmeCoffeeOnline.Web/` - rebuilt Blazor Server host
- `src/Apps/AcmeCoffeeOnline.Api/` - rebuilt REST API host
- `src/Apps/AcmeCoffeeOnline.Shared/` - shared DTOs, enums, and models
- `src/Libs/` - integrated Dabbler libraries used by the rebuild
- `tests/` - xUnit, bUnit, and Playwright coverage
- `docker/` - compose and container definitions
- `ai-router/` - router code, prompt templates, and workflow utilities
- `docs/` - workflow docs and session-set artifacts
- `template-seed/` - lightweight local seed for repeating the AI-led workflow in a future repo

## Local commands

```bash
dotnet build Dabbler.Platform.sln
dotnet test Dabbler.Platform.sln
docker compose -f docker/docker-compose.yml up --build --abort-on-container-exit playwright
```
