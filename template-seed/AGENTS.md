# AGENTS.md -- {{PROJECT_NAME}} (Codex)

Read `README.md` for the repo layout. Required reading before every session:

- `docs/planning/project-guidance.md`
- `docs/planning/lessons-learned.md`
- `docs/planning/session-set-authoring-guide.md` (how to author specs, the
  Session Set Configuration block, When-UAT/When-E2E heuristics)
- `docs/ai-led-session-workflow.md` (the authoritative workflow)

The human starts a session with one of the trigger phrases listed in
`docs/ai-led-session-workflow.md` -> **Trigger Phrases**. That section
is the canonical list; do not re-list trigger phrases here.

## Project Rules

- work from the integrated source inside this repo
- do not add sibling-repo dependencies unless a session spec requires it
- log every session step with `SessionLog`
- use `route()` for reasoning tasks and external session verification
- complete exactly one session per conversation
- obey the spec's Session Set Configuration block at runtime; do not
  re-litigate `requiresUAT` / `requiresE2E` mid-session
- keep the narrowed starter contract explicit instead of silently restoring
  broader historical platform defaults
- do not treat `DabblerCatalogPage`, `DabblerQueryReportPage<T>`,
  `PageViewMode`, `DabblerSecurityNav`, catalog/usage-tracking provider
  wiring, or favorites/recent-items widgets as starter defaults
- treat query CSV export and multiselect lookup support as explicit workflow
  choices, and keep report defaults centered on PDF export without default
  print/zoom controls

## Build And Test

Update these commands for the new repo before the first real session:

```bash
dotnet build
dotnet test
```

<!-- Canonical session procedure lives in docs/ai-led-session-workflow.md.
     Do not duplicate it here. -->
