# Workflow Template Seed

This folder is the smallest reusable starter for the AI-led workflow used in
`dabbler-platform`.

It includes:

- a ready-to-copy `ai-router/` folder
- root instruction-file templates for Codex, Claude, and Gemini
- the shared workflow doc
- a starter session-set spec skeleton
- a placeholder-replacement script for bootstrapping a new repo

## Application Bootstrap Contract

This seed stays workflow-focused. It does not scaffold application projects,
layouts, pages, or default `Program.cs` wiring.

If a new repo uses this seed to bootstrap notes-driven Dabbler work, treat the
following as the starter contract:

- start from focused shell, dashboard, query, detail, popup, report, and
  single-select lookup workflows instead of restoring broad compatibility-era
  surfaces
- do not silently scaffold `DabblerCatalogPage`,
  `DabblerQueryReportPage<T>`, `PageViewMode`, `DabblerSecurityNav`, or broad
  catalog-browsing defaults
- do not assume default `IArtifactCatalogProvider` or
  `IUsageTrackingProvider` wiring, and do not make favorites/recent-items
  widgets part of the starter dashboard by default
- treat query CSV export as an explicit app-level opt-in rather than a
  universal starter behavior
- preserve the accepted PDF-export report baseline without implying print or
  zoom controls by default
- add `DabblerMultiComboBox<T>` only when a concrete scaffolded workflow
  genuinely needs multiselect lookup behavior

Any advanced or compatibility-only capability should be a deliberate follow-up
decision in the session plan, not an inherited template default.

## Placeholders

Replace these tokens after copying the seed into a new repo:

- `{{PROJECT_NAME}}`
- `{{REPO_ROOT}}`
- `{{APP_NAME}}`
- `{{SESSION_SET_NAME}}`

Run:

```powershell
.\scripts\replace-placeholders.ps1 -ProjectName "My Project" -AppName "MyApp" -SessionSetName "initial-bootstrap"
```

## Next Steps

1. Create a Python virtual environment and install `ai-router/requirements.txt`.
2. Set `ANTHROPIC_API_KEY` and `GEMINI_API_KEY` as user environment variables.
3. Rename the starter session-set folder if needed.
4. Update the root instruction files with repo-specific build/test commands and
   keep the bootstrap contract explicit.
5. Ask the orchestrator to "Start the next session".
