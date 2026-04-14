# Change Log: `rebuild-consolidation`

`rebuild-consolidation` is complete. The six-session rebuild moved the
AcmeCoffeeOnline app, required Dabbler libraries, and the AI-led workflow
assets into `dabbler-platform` as the canonical development home.

## Final Outcome

- `Dabbler.Platform.sln` now owns the rebuilt Web, API, and Shared projects.
- Required libraries now live in-repo under `src/Libs/`, including
  `Dabbler.Security`, `Dabbler.Security.Blazor`, `Dabbler.Components`,
  `Dabbler.Reports.Core`, `Dabbler.Reports.Export`, and
  `Dabbler.Reports.Blazor`.
- Docker-based local and CI-like workflow assets, root instruction files,
  router tooling, durable guardrail docs, and a reusable `template-seed/`
  bootstrap folder now live in the same repo.
- `dabbler-platform` is the active code home; `dabbler-demo`,
  `dabbler-security`, and `dabbler-components` are now reference/archive repos.

## Session Highlights

- Session 1 captured the baseline architecture, parity targets, lessons learned,
  and fragile patterns that should not carry forward.
- Session 2 created the integrated repo skeleton, centralized build/package
  setup, Docker stack, and initial xUnit/Playwright foundation.
- Session 3 consolidated the security libraries into `src/Libs/`, aligned them
  to the unified build, and verified the imported security suites plus the
  existing API/Web foundation tests.
- Session 4 moved the canonical workflow assets into the repo and added startup,
  auth, configuration, and smoke-test guardrails.
- Session 5 imported the required component/report libraries and rebuilt the
  AcmeCoffeeOnline application surface on integrated source references.
- Session 6 added meaningful E2E coverage, created the first local template
  seed, documented cutover/guardrails/auth modes, and prepared the next
  follow-on session artifacts.

## Final Validation

- `dotnet build Dabbler.Platform.sln` passed with zero errors and zero warnings.
- `dotnet test Dabbler.Platform.sln --no-build` passed with 338 tests:
- `AcmeCoffeeOnline.Web.Tests`: 5
- `AcmeCoffeeOnline.Api.Tests`: 1
- `Dabbler.Security.Tests`: 207
- `Dabbler.Security.Blazor.Tests`: 120
- `AcmeCoffeeOnline.Playwright`: 5

## Follow-On Work

- `docs/session-sets/platform-post-cutover-hardening/` was created as the first
  ready-to-run post-rebuild session set.
- `docs/planning/ops-observability-input.md` captures lower-confidence
  observability follow-up work.
- `docs/planning/library-expansion-input.md` captures lower-confidence library
  migration follow-up work.
- `docs/planning/multi-organization-user-administration-input.md` logs the new
  user-organization requirement and explicitly notes that it needs its own
  dedicated future session set rather than being folded into the completed
  rebuild work.
