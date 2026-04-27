# GEMINI.md -- {{PROJECT_NAME}}

Read `README.md` for the repo layout. Required reading before every session:

- `docs/planning/project-guidance.md`
- `docs/planning/lessons-learned.md`
- `docs/planning/session-set-authoring-guide.md` (how to author specs, the
  Session Set Configuration block, When-UAT/When-E2E heuristics)
- `docs/ai-led-session-workflow.md` (the authoritative workflow)

The human starts a session with one of the trigger phrases listed in
`docs/ai-led-session-workflow.md` -> **Trigger Phrases**. That section
is the canonical list; do not re-list trigger phrases here.

## Rules

- execute one session at a time
- log every completed step
- use `route()` for analysis, documentation, and verification tasks
- run external session verification before committing
- obey the spec's Session Set Configuration block at runtime; do not
  re-litigate `requiresUAT` / `requiresE2E` mid-session
- keep the starter contract narrowed and workflow-focused instead of implying
  broader historical shell, dashboard, query/report, or security defaults
- do not treat `DabblerCatalogPage`, `DabblerQueryReportPage<T>`,
  `PageViewMode`, `DabblerSecurityNav`, catalog/usage-tracking provider
  wiring, or favorites/recent-items widgets as starter defaults
- treat query CSV export and multiselect lookup support as explicit app-level
  choices, and keep report defaults centered on PDF export without default
  print/zoom controls

## Build And Test

Replace these placeholders before use:

```bash
dotnet build
dotnet test
```
