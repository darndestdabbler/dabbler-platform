# Dabbler Platform

This repository is the integrated development home for the AcmeCoffeeOnline
rebuild, the required Dabbler libraries, and the AI-led workflow assets used
to run session-set development from one place.

## Current foundation

The repo currently includes:

- the rebuilt AcmeCoffeeOnline Web and API foundations
- consolidated `Dabbler.Security` and `Dabbler.Security.Blazor` source projects
- central build and package management
- Docker Compose for web, api, sql server, MailHog, and Playwright
- xUnit and Playwright test foundations
- repo-root AI workflow assets: `ai-router/`, `AGENTS.md`, `CLAUDE.md`, and `GEMINI.md`

## Project layout

- `src/Apps/AcmeCoffeeOnline.Web/` - rebuilt Blazor Server host
- `src/Apps/AcmeCoffeeOnline.Api/` - rebuilt REST API host
- `src/Libs/` - integrated Dabbler libraries used by the rebuild
- `tests/` - xUnit, bUnit, and Playwright coverage
- `docker/` - compose and container definitions
- `ai-router/` - router code, prompt templates, and workflow utilities
- `docs/` - workflow docs and session-set artifacts

## Local commands

```bash
dotnet build Dabbler.Platform.sln
dotnet test Dabbler.Platform.sln
docker compose -f docker/docker-compose.yml up --build --abort-on-container-exit playwright
```
