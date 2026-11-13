# Change Log — component-catalog-grounded-authorship

> **Set status:** Complete (2 of 2 sessions)
> **Started:** 2026-04-28
> **Completed:** 2026-04-28
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

## What this set delivered

The set landed a runtime-crawled component catalog plus a `uat-script-author`
routed task type, eliminating the label-guessing failure mode that produced six
issues on a single checklist item in the `sensitive-field-masking-audit` UAT.
Specifically:

1. **Component-catalog crawler** (`tools/ComponentCatalogCrawler/`, .NET 8
   console app, Playwright.NET) — boots against the running compose stack,
   authenticates as each defined role via the existing
   `SeededSessionAuth` pattern, walks every `@page`-discovered route, and
   scrapes the in-page DOM (an `EvaluateAsync` walk that avoids the
   `IAccessibility.SnapshotAsync` `System.Text.Json` 64-deep limit triggered
   by `/admin/security`'s MudBlazor tree). The result is per-role manifests
   plus a merged `docs/component-catalog/index.json` (2,267 entries across
   five roles and 21 routes) conforming to the `Dabbler.Uat.Dsl` repo's
   `schema/component-catalog.json`.

2. **`uat-script-author` routed task type** in
   `ai-router/router-config.yaml` (Sonnet at medium effort, complexity 60,
   not in `auto_verify_task_types` because validation is delegated to the
   W0 `dabbler-uat-dsl validate` CLI). The H1 prompt template lives in
   `ai-router/prompt-templates/task-prompts.md` and forbids label
   invention: every quoted label in the emitted checklist must match a
   `userVisibleLabel` or `ariaLabel` entry in the catalog for the active
   route × role, and the DSL verb's element type must match the catalog
   `elementRole`.

3. **Re-authored masking-remediation UAT checklist** at
   `docs/session-sets/sensitive-field-masking-audit-uat-remediation/sensitive-field-masking-audit-uat-remediation-uat-checklist.json`.
   Six Review items (UAT-1..UAT-6, step counts 16/16/16/17/17/16,
   `IsJudgmentItem: true` on UAT-5). Every catalog-grounded label is
   verbatim from the catalog: tab names like `"ROLE MASTER DETAILS"` and
   `"COMPLIANCE REVIEW"` (uppercase per render), grid columns like
   `"Display alias"` and `"Account Email"`, and the role-switcher as
   `CLICK ICON BUTTON with aria-label "System Admin, switch role"` (the
   catalog's `IconButton` mapping is authoritative).

4. **Suppressions document** at `tools/uat-suppressions.json` covering
   (a) the `/login` and `/dev/email-inbox` pre-auth catalog gap (the
   crawler authenticates first by design), (b) a category-wide
   `UnknownRoute` suppression for click-driven navigation transitions that
   the W0 `SemanticValidator` does not yet follow (the validator only
   updates its route cursor on `NAVIGATE TO` statements), and (c) a
   wildcard suppression for closed pre-catalog UAT checklists that pre-date
   the catalog ground truth.

5. **`docs/planning/project-guidance.md`** updated under
   *Conventions → Human UAT And Checklist Editor* with two new bullets:
   catalog-grounded authoring is mandatory for new UAT checklists (manual
   authoring deprecated; new checklists go through
   `route(task_type="uat-script-author")`), and the W0 click-navigation
   validator limitation is documented with the future-enhancement path.
   `docs/ai-led-session-workflow.md`'s task-type table grew a row for
   `uat-script-author` plus a paragraph explaining when to prefer it over
   `uat-plan-generation`.

## Sessions

### Session 1 of 2 — Crawler scaffold, catalog schema, first manifest
- Scaffolded `tools/ComponentCatalogCrawler/`, added to
  `Dabbler.Platform.sln`.
- Implemented `RouteEnumerator`, `SeededSessionAuth`,
  `AccessibilityTreeScraper`, `CrawlerOrchestrator`.
- Ran the crawler against the Docker compose stack: 21 routes, 5 roles,
  2,267 unique entries.
- Validated the manifest against `JsonCatalogAdapter.FromJson`.
- Cross-provider verification ran via `gpt-5.4` (the Anthropic API account
  hit zero credit balance mid-session; verification stayed cross-provider
  by going to OpenAI directly).
- Round 1 found two Major issues; Round 2 returned `VERIFIED, 0 issues`.

### Session 2 of 2 — Task type, prompt template, re-author checklist, planning docs
- Added `uat-script-author` to `ai-router/router-config.yaml`
  (`task_type_overrides`, `task_type_scores`, `task_type_params`,
  `delegation.always_route_task_types`).
- Authored the H1 prompt section in
  `ai-router/prompt-templates/task-prompts.md` (using the existing
  single-file convention rather than the spec's referenced
  `ai-router/prompts/uat_script_author.md` path — disclosed in the
  verification prompt).
- Routed the catalog-grounded re-author call to Sonnet. The call timed
  out three times at the 300s default timeout. Per the user's explicit
  redirect ("if you are not getting a response from an API, just run it
  yourself"), the orchestrator authored the checklist directly using the
  catalog as the literal label source.
- Ran `dabbler-uat-dsl validate` against the new checklist with the full
  `index.json` catalog. 47 errors, all `UnknownRoute`, all attributable
  to the two documented categories (pre-auth gap + click-driven cursor
  limitation). NO label-mismatch, element-type-mismatch, or
  unknown-label errors — every catalog-grounded label resolved cleanly,
  which is the proof-of-concept the set was meant to land.
- Created `tools/uat-suppressions.json`.
- Updated `docs/planning/project-guidance.md` and
  `docs/ai-led-session-workflow.md`.

## Routing summary

Session 1 routed 3 calls totalling $0.1744. Session 2 routing summary
will appear in the cost report at session close — the planned
`uat-script-author` route call to Sonnet timed out before producing
metered output (logged in the activity log as a deviation), so Session 2
is dominated by orchestrator-side direct work plus the cross-provider
session-verification call.

## What this set did not do

- Did not crawl into dialogs, drawers, or popovers — only surface-level
  routes. Mask Exemption sub-panel content, Compliance Review tab
  internals, and role-switch popover items remain catalog gaps and are
  noted inline.
- Did not extend W0 `dabbler-uat-dsl` (e.g., the `--suppressions` flag,
  click-driven cursor inference, pre-auth crawler pass). Those are
  upstream W0 enhancements scheduled to land alongside the
  `uat-runner-and-triage-feedback` (W2) set.
- Did not re-author closed pre-catalog UAT checklists. They are
  grandfathered via `tools/uat-suppressions.json`.
- Did not wire `dabbler-uat-dsl validate` as a CI gate. CI integration
  lands with W2.

## Next set (planned)

Per `project_uat_truthful_pipeline.md` (W1→W2→W3), the next set is
`uat-runner-and-triage-feedback` (W2): a Python orchestrator that
invokes the W0 CLI to compile the catalog-grounded checklist into
runnable Playwright code, executes via `dotnet test`, captures
`.trx` results, and routes failure triage through a 4-class taxonomy
with auto-apply only for `CATALOG_STALE`. Confirm or surface a deviation
at the end of this session via the next-session-set recommendation.
