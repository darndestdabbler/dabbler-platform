# Notes-Driven Template Contract Audit

> **Purpose:** Turn the accepted notes-driven app baseline into the explicit
> bootstrap contract for `template-seed/` without breaking the guardrail that
> keeps the template workflow-focused.
> **Created:** 2026-04-14
> **Source Inputs:** `docs/planning/notes-driven-app-acceptance-baseline.md`,
> `docs/planning/notes-driven-library-compatibility-notes.md`,
> `docs/planning/notes-driven-library-contract-audit.md`,
> `docs/platform-guardrails.md`, and the current `template-seed/` assets

---

## Current Template Status

`template-seed/` is still a workflow bootstrap, not an application starter.

Today it includes:

- the reusable `ai-router/` folder
- root instruction-file templates for Codex, Claude, and Gemini
- the shared AI-led session workflow document
- a starter session-set `spec.md`
- placeholder replacement tooling

This matches the template guardrail in `docs/platform-guardrails.md`: keep the
template workflow-focused and prefer placeholders plus small bootstrap scripts
over product-specific scaffolding.

## Contract Alignment Verdict

The current template is aligned with the narrowed platform contract by
omission.

- it does not scaffold application code, shell wiring, or Dabbler UI defaults
- it therefore does not currently reintroduce retired or compatibility-only
  surfaces
- it also does not yet make the accepted negative defaults explicit for new
  repos, which leaves too much room for starter guidance to drift back toward
  the broader historical platform story

Session 2 should close that documentation and guidance gap without turning
`template-seed/` into a full product or app scaffold.

## Session 2 Update List

### `template-seed/README.md`

- add an "Application Bootstrap Contract" section that explains the template
  seeds workflow assets only
- add the accepted notes-driven negative defaults for future app scaffolding
- make clear that app bootstrap work must deliberately choose any advanced or
  compatibility-only capability instead of inheriting it silently

### `template-seed/docs/session-sets/starter-feature/spec.md`

- add a standards section that frames the starter session set around retained
  core surfaces and explicit opt-ins
- forbid obsolete or compatibility-only defaults in starter session guidance,
  including `DabblerCatalogPage`, `DabblerQueryReportPage<T>`, broad catalog
  browsing assumptions, and dashboard favorites/recent-items defaults
- state that advanced behaviors such as query CSV export and multiselect
  lookups require deliberate justification in a concrete workflow

### `template-seed/AGENTS.md`

- add a rule that future sessions must follow the narrowed platform contract
- state that obsolete or compatibility-only surfaces are not starter defaults

### `template-seed/CLAUDE.md`

- add the same narrowed-contract rule set used by the other instruction files
- make explicit that reasoning and verification work should not imply older
  default shell, dashboard, query/report, or security composition

### `template-seed/GEMINI.md`

- add the same narrowed-contract rule set used by the other instruction files
- keep the guidance workflow-focused rather than app-scaffold-oriented

### `template-seed/docs/ai-led-session-workflow.md`

- review for any wording that could imply generic starter repos should grow a
  broader historical platform surface by default
- keep the workflow document generic unless a cross-reference to the bootstrap
  contract is needed for clarity

## Explicit Template Limitations

These limits should be documented instead of left implicit:

- `template-seed/` is not a starter application; it does not provide Blazor
  projects, layouts, pages, or `Program.cs` wiring
- the template does not preconfigure `IArtifactCatalogProvider` or
  `IUsageTrackingProvider`, so new repos must not treat catalog browsing,
  usage tracking, `DabblerTopMenuBar`, favorites, or recent-items widgets as
  starter defaults
- starter guidance must not imply a default `DabblerSecurityNav`; focused,
  app-specific admin navigation remains the accepted baseline
- query CSV export is an explicit app-level opt-in, not a universal starter
  behavior
- report guidance should preserve the accepted PDF-export baseline without
  implying print or zoom controls
- `DabblerMultiComboBox<T>` remains supported but should not be treated as part
  of the minimal starter surface unless a scaffolded workflow genuinely needs
  multiselect lookup behavior
- starter guidance must not reintroduce `DabblerCatalogPage`,
  `DabblerQueryReportPage<T>`, `PageViewMode`, or similar
  compatibility-only/obsolete surfaces as default building blocks

## Risks To Avoid In Session 2

- turning `template-seed/` into product scaffolding would violate the repo's
  template-discipline guardrail
- reintroducing compatibility-era defaults in docs or starter session plans
  would undo the audit, simplification, and app-alignment outcomes
- leaving the negative defaults undocumented would make the template look
  neutral while still allowing future repos to drift back toward unsupported
  shell, dashboard, query/report, and security assumptions

## Session 1 Conclusion

Session 1 should be considered complete when the template contract is explicit:
`template-seed/` stays workflow-focused, and Session 2 has a clear, bounded
update list for making the accepted notes-driven defaults visible in bootstrap
docs and starter session guidance without inventing a full application
scaffold.
