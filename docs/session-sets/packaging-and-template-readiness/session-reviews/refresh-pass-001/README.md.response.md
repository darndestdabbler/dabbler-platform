# Refresh response for `README.md`

- Model: gemini-pro
- Input tokens: 1803
- Output tokens: 899
- Cost: $0.0112

---

```markdown
# Dabbler Platform

This repository contains the Dabbler Platform, a reference implementation for building modern Blazor applications. It includes the `AcmeCoffeeOnline` application, the core `Dabbler.*` libraries, and AI-led workflow assets for session-set development.

## Platform Foundation

- **Blazor + .NET:** Rebuilt `AcmeCoffeeOnline` Web (Blazor Server), API (ASP.NET Core), and Shared projects.
- **MudBlazor-Native UI:** A direct, composable UI layer using MudBlazor components for layout, navigation, forms, and data grids. This replaces prior custom abstractions and metadata-driven UI generation.
- **Integrated Dabbler Libraries:**
  - `Dabbler.Components`: Retains the QBE filter model, expression engine, and data providers while delegating all UI rendering to MudBlazor.
  - `Dabbler.Security`: Provides a flexible authorization model with 5 built-in roles, JSON-configurable user-type roles, and sensitive-field masking.
  - `Dabbler.Reports`: Source projects for reporting infrastructure.
- **AI-Led Workflow Tooling:** Integrated assets for an AI-assisted development and UAT process, including an operator-tuned AI router and a local UAT runner.
- **Comprehensive Testing:** xUnit, bUnit, and Playwright test suites covering API/unit conformance, component interactions, accessibility, and E2E browser workflows.
- **Containerized Development:** A Docker Compose stack for the web app, API, SQL Server, and MailHog.

## Project Layout

- `src/Apps/AcmeCoffeeOnline.Web/` — Blazor Server host with MudBlazor-native pages.
- `src/Apps/AcmeCoffeeOnline.Api/` — ASP.NET Core REST API host.
- `src/Apps/AcmeCoffeeOnline.Shared/` — DTOs, enums, and shared models.
- `src/Libs/` — Integrated `Dabbler.*` libraries (Components, Security, Reports).
- `tests/` — xUnit, bUnit, and Playwright test projects.
- `docker/` — Docker Compose files and container definitions.
- `ai_router/` — Operator-tuned configuration (`router-config.yaml`) and cost telemetry (`router-metrics.jsonl`) for the `dabbler-ai-router`.
- `uat_runner/` — Local UAT runner package for executing test cases against the application.
- `docs/` — Key workflow documents, including `ai-led-session-workflow.md`.
- `AGENTS.md`, `CLAUDE.md`, `GEMINI.md` — Agent-specific instructions for the AI workflow.
- `requirements.txt` — Python dependencies for the AI tooling, including `dabbler-ai-router`.

## Getting Started

The AI tooling requires Python. Install dependencies using pip:
```bash
pip install -r requirements.txt
```

### Local Development & Testing

```bash
# Build and run unit/integration tests
dotnet build Dabbler.Platform.sln
dotnet test Dabbler.Platform.sln

# Run the UAT test suite
python -m uat_runner.cli
```

### End-to-End Testing with Playwright

Playwright tests run from the host against the application running in Docker containers.

```bash
# 1. Start infrastructure (do this once and leave it running)
docker compose -f docker/docker-compose.infra.yml up -d

# 2. Build and run the applications (re-run after code changes)
docker compose -f docker/docker-compose.app.yml up --build -d

# 3. Run Playwright tests from the host
dotnet test tests/AcmeCoffeeOnline.Playwright/
```

The Docker Compose stack is split into two files:
- `docker-compose.infra.yml`: Persistent dev dependencies (SQL Server, MailHog).
- `docker-compose.app.yml`: Rebuildable app services (API, Web) that run on the `dabbler-dev` network.
```

---
**Verification (gpt-5-4-mini, openai):** ISSUES_FOUND

**Issue 1** [? / ?]: S FOUND.

The response is broadly aligned with the platform truth, but it is not fully consumer-accurate yet:

- It removes the stale sibling-repo / vendored-router framing correctly, but it **under-specifies the current UI and security model**. The platform truth explicitly calls out:
  - composable CRUD helpers: `EditSession<T>`, `DirtyAwareActionRow`, `DeclarativeFilterPanel`
  - default-permissive restrictions (`Column` / `Row` / `Ui`)
  - sensitive-field masking via `DefaultMask` and per-role `MaskExemption`
  None of that appears in the rewrite.
- It **fails to identify the docs of record** for workflow mechanics and authoring:
  - `docs/ai-led-session-workflow.md` is the execution SSOT
  - `docs/planning/session-set-authoring-guide.md` is the authoring guide
- The UAT section is directionally correct, but it’s **missing the important packaging/toolchain context** from platform truth: the W0 toolchain comes from the sibling `dabbler-uat-dsl` repo as NuGet packages, and the runner is invoked via `python -m uat_runner.cli`.
- The opening “reference implementation for building modern Blazor applications” is **added marketing/scoping** not supported by the platform truth. It’s not a hard error, but it’s extra and unnecessary.

So: the rewrite is cleaner than the original, but it still needs a material refresh to fully match the current platform state.
