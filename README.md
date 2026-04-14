# Dabbler Platform

This repository is the integrated home for the AcmeCoffeeOnline rebuild and the
supporting Dabbler libraries.

## Session 2 foundation

Session 2 establishes:

- a single solution with app and test projects
- central build and package management
- Docker Compose for web, api, sql server, MailHog, and Playwright
- xUnit test project defaults
- a smoke Playwright path that proves the web app can reach the API in a
  containerized stack

The real Dabbler library move is intentionally deferred to Session 3 to avoid
placeholder project churn.

## Project layout

- `src/Apps/AcmeCoffeeOnline.Web/` - rebuilt Blazor Server host foundation
- `src/Apps/AcmeCoffeeOnline.Api/` - rebuilt REST API foundation
- `src/Libs/` - reserved for Dabbler library consolidation in Session 3
- `tests/AcmeCoffeeOnline.Api.Tests/` - xUnit API smoke/integration tests
- `tests/AcmeCoffeeOnline.Web.Tests/` - xUnit web smoke/integration tests
- `tests/AcmeCoffeeOnline.Playwright/` - browser smoke coverage
- `docker/` - compose and container definitions

## Local commands

```bash
dotnet build Dabbler.Platform.sln
dotnet test Dabbler.Platform.sln
docker compose -f docker/docker-compose.yml up --build --abort-on-container-exit playwright
```
