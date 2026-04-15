# CLAUDE.md -- {{PROJECT_NAME}}

Read `README.md` for the repo layout and `docs/ai-led-session-workflow.md` for
the session-set workflow.

## Rules

- one session only
- never skip external verification
- never edit saved session-review files
- log every step
- use `ai-router/` for reasoning work, not filesystem work
- keep the starter contract narrowed and workflow-focused instead of implying
  broader historical shell, dashboard, query/report, or security defaults
- do not treat `DabblerCatalogPage`, `DabblerQueryReportPage<T>`,
  `PageViewMode`, `DabblerSecurityNav`, catalog/usage-tracking provider
  wiring, or favorites/recent-items widgets as starter defaults
- treat query CSV export and multiselect lookup support as explicit app-level
  choices, and keep report defaults centered on PDF export without default
  print/zoom controls

## Build And Test

Replace these with the repo's real commands:

```bash
dotnet build
dotnet test
```
