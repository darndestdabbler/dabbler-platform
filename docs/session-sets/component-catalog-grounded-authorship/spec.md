# Component Catalog and Grounded Authorship Spec

> **Purpose:** Establish a runtime-crawled catalog of every user-visible
> interactive element in `AcmeCoffeeOnline.Web` keyed by route × role ×
> user-visible label. The catalog is consumed by the `Dabbler.Uat.Dsl`
> compiler's semantic-validation pass (W0) so AI sessions can no longer
> author UAT scripts that reference labels not present in the rendered UI —
> the failure mode that caused `sensitive-field-masking-audit` UAT 1 to
> surface six issues on a single checklist item.
> **Created:** 2026-04-28
> **Session Set:** `docs/session-sets/component-catalog-grounded-authorship/`
> **Prerequisite:** `uat-dsl-compiler` (W0) complete. The compiler defines
> the catalog schema (`schema/component-catalog.json` in
> `../dabbler-uat-dsl/`) and consumes the catalog via its
> `JsonCatalogAdapter`. This set produces data conforming to that schema
> and integrates the compiler's CLI into the authoring workflow.
> Additionally, `sensitive-field-masking-audit` complete; both code
> sessions of `sensitive-field-masking-audit-uat-remediation` committed to
> master.
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Session Set Configuration

```yaml
requiresUAT: false
requiresE2E: false
uatScope: none
```

> Rationale: This set builds the component-catalog crawler (a console app)
> and adds an `ai-router` task type for catalog-grounded UAT script
> authoring. No Blazor UI surface, API endpoint, or user-facing behavior
> changes. Per the platform addendum, tooling and ai-router changes are
> `requiresUAT: false` / `requiresE2E: false`. The set's quality bar is:
> the crawler produces a catalog conforming to the W0-defined schema; the
> W0 compiler's `validate` subcommand passes against the re-authored
> remediation checklist with zero unsuppressed errors.

---

## Project Overview

### What this set delivers

1. **`tools/ComponentCatalogCrawler/`** — a .NET console app
   (Playwright.NET) that boots against the running compose stack,
   authenticates as each defined role, visits every routable page, and
   scrapes the accessibility tree to produce a catalog manifest.

2. **`docs/component-catalog/index.json`** — the merged catalog
   conforming to `Dabbler.Uat.Dsl`'s
   `schema/component-catalog.json`. Checked into the repository; diff on
   re-crawl makes UI changes visible in PR review.

3. **`ai-router/router-config.yaml` additions** — a `uat-script-author`
   task type whose system prompt requires that every label in the emitted
   checklist be present in the catalog. Forbids label invention.

4. **`ai-router/prompts/uat_script_author.md`** — the catalog-grounded
   authoring prompt; references the `Dabbler.Uat.Dsl` grammar's quoted-string
   convention and structured VERIFY forms.

5. **Re-authored
   `sensitive-field-masking-audit-uat-remediation-uat-checklist.json`** —
   produced via the new `uat-script-author` task type and validated via
   `dabbler-uat-dsl validate`. This is the proof-of-concept that the
   catalog + compiler eliminates Step 2/9-class errors before they reach
   the human.

6. **`docs/planning/project-guidance.md` addition** — catalog-grounded
   authorship documented as a Convention; reference to the `Dabbler.Uat.Dsl`
   compiler as the validation gate.

### What this set does not do

- Does not build a separate lint tool — the `Dabbler.Uat.Dsl` compiler's
  `validate` subcommand from W0 fills that role.
- Does not change any Blazor component, page, API endpoint, or UI behavior.
- Does not build the runner + triage feedback loop (W2, next set).
- Does not crawl into dialogs, drawers, or popovers — only surface-level
  routes visible without clicking anything.
- Does not wire validation as a required CI gate in this set; CI integration
  is documented but not enforced until W2 wires the runner.

---

## Design Commitments (Decided Up Front)

### Crawler technology: .NET console app, Playwright.NET

The `tests/AcmeCoffeeOnline.Playwright/Infrastructure/PlaywrightFixture.cs`
seeded-session auth pattern (`X-Test-Auth-Secret` header,
`SeededSignInAsync`) must be replicated. A .NET console app can share the
same auth logic without duplication.

### Accessibility-tree scraping (not CSS DOM parsing)

Use Playwright's `IPage.Accessibility.SnapshotAsync()` to capture the
computed accessibility tree of each page. Rationale: the tree gives us the
ARIA role (what `GetByRole` sees) and the accessible name (what
`GetByText`/label sees), and automatically excludes hidden elements.
MudBlazor renders standard ARIA-compliant HTML; the tree captures it
correctly without MudBlazor-specific CSS queries.

### Catalog schema is owned by `Dabbler.Uat.Dsl`

The schema lives at `../dabbler-uat-dsl/schema/component-catalog.json` (W0
deliverable). This set produces data conforming to that schema. The
crawler's output records match the schema field-for-field. Schema fields
recap (defined by W0):

```
{
  "routePath": "/customers",
  "pageTitle": "Customers",
  "role": "BackofficeUser",
  "elementRole": "button",
  "userVisibleLabel": "Add Customer",
  "ariaLabel": "Add Customer",
  "testId": "add-customer-btn",
  "htmlTag": "button",
  "scope": null
}
```

ARIA-role → DSL `elementRole` mapping (defined in W0; recorded here for
crawler reference): button, link, tab, iconButton (button + non-null
ariaLabel + empty innerText), checkbox, switch, input (textbox/spinbutton),
select (combobox/listbox), menuItem, gridRow.

### Roles to crawl

All distinct roles in `src/Apps/AcmeCoffeeOnline.Web/user-roles.json`:
`SystemAdmin`, `BackofficeUser`, `CrossOrgMonitor`, `CrossOrgUserAdmin`,
`User`. The crawl runs each role separately so role-gated UI surfaces are
captured.

### Route discovery: static scan of `@page` directives

Scan `.razor` files under
`src/Apps/AcmeCoffeeOnline.Web/Components/Pages/` for `@page "..."` lines.
Parameterize dynamic segments (`{id}`) with seeded entity IDs from the
test API (first customer, first order, etc.). Skip routes with
unresolvable parameters with a logged warning.

### Catalog committed to source control

`docs/component-catalog/index.json` and per-role manifests are committed.
Git diff on re-crawl is the primary mechanism for detecting UI drift.
Re-crawl is on demand (e.g., before authoring a new UAT checklist), not in
CI — CI cost of booting the app on every push is not warranted yet.

### Validation is delegated to the W0 compiler

This set does NOT build a separate Python lint. Validation is invoked
via:
```bash
dabbler-uat-dsl validate <checklist.json> --catalog docs/component-catalog/index.json
```
Exit code 0 = pass; exit code 1 = structured error list on stderr. The
compiler's near-match suggestions ("step says 'Email'; catalog has
'Email address' on this route — did you mean…?") are exactly the
Step 2-class affordance we need.

---

## Session Plan

### Session 1 of 2: Crawler scaffold, catalog schema, first manifest

**Steps:**

1. Read `docs/planning/project-guidance.md` and `docs/planning/lessons-learned.md`.
2. Confirm prerequisite: `../dabbler-uat-dsl/` exists, `dotnet build`
   succeeds there, and `schema/component-catalog.json` is present. If
   not, halt and notify the human — W0 must complete first.
3. Create `tools/ComponentCatalogCrawler/ComponentCatalogCrawler.csproj`:
   .NET 8 console app referencing `Microsoft.Playwright`. Add to
   `Dabbler.Platform.sln`.
4. Implement `RouteEnumerator.cs`: scan
   `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/**/*.razor` for
   `@page "..."` directives. For dynamic segments (e.g.,
   `@page "/customers/{id}"`), emit a parameterized variant using
   seeded data IDs (query the API with `X-Internal-Api-Key` to get a
   known ID per entity type). Returns `List<(string routePath, string
   resolvedPath)>`.
5. Implement `SeededSessionAuth.cs`: authenticate via
   `POST /test-auth/seeded-session` with `X-Test-Auth-Secret:
   "acme-local-seeded-session"`. Same as `PlaywrightFixture`. Returns
   an `IPage` with the session established.
6. Implement `AccessibilityTreeScraper.cs`: walk
   `page.Accessibility.SnapshotAsync()` recursively. Emit
   `CatalogElement` records matching the W0 schema. Apply the
   ARIA-role → `elementRole` mapping. De-duplicate by
   `(routePath, role, elementRole, userVisibleLabel)` so the catalog
   records unique labels, not every occurrence.
7. Implement `CrawlerOrchestrator.cs`: for each role × route,
   authenticate, navigate, wait for `[data-testid="main-shell"]`
   visible, scrape, collect. Write `docs/component-catalog/{role}.json`
   per role and merge to `docs/component-catalog/index.json`.
8. Run the crawler:
   ```bash
   docker compose -f docker/docker-compose.infra.yml up -d
   docker compose -f docker/docker-compose.app.yml up --build -d
   dotnet run --project tools/ComponentCatalogCrawler/ -- \
     --output docs/component-catalog/
   ```
9. Validate the manifest against the W0 schema:
   ```bash
   dabbler-uat-dsl validate-catalog \
     --catalog docs/component-catalog/index.json
   ```
   (If the W0 CLI doesn't expose `validate-catalog`, use `validate`
   against a trivial sample checklist and confirm the catalog loads
   without parse errors. The full schema validation is implicit in the
   adapter's load step.)
10. Spot-check the output: `/customers` under `BackofficeUser` shows
    an "Add Customer" or equivalent button; `/admin/security-dashboard`
    under `SystemAdmin` shows tabs; `User` shows a restricted set vs.
    `SystemAdmin`.
11. `route(task_type="session-verification")` covering crawler code +
    a manifest sample.
12. Commit. Commit message:
    `Add component catalog crawler and initial manifest`.

**Creates:** `tools/ComponentCatalogCrawler/` (full project), `docs/component-catalog/index.json`, `docs/component-catalog/{role}.json` per role.
**Touches:** `Dabbler.Platform.sln`.
**Ends with:** Catalog manifest exists, conforms to the W0 schema, and a spot-check confirms the expected entries.
**Progress keys:** `session-1/read-planning`, `session-1/prereq-check`, `session-1/crawler-project`, `session-1/route-enumerator`, `session-1/auth`, `session-1/scraper`, `session-1/orchestrator`, `session-1/crawl-run`, `session-1/schema-validate`, `session-1/spot-check`, `session-1/verification`, `session-1/commit`.

---

### Session 2 of 2: ai-router task type, re-author remediation checklist, change log, Step 9 reorg

**Steps:**

1. Read planning docs.
2. Add `uat-script-author` task type to `ai-router/router-config.yaml`:
   ```yaml
   uat-script-author:
     model: sonnet
     effort: medium
     prompt_template: prompts/uat_script_author.md
   ```
3. Author `ai-router/prompts/uat_script_author.md`:
   - Receives: the session-set spec, the UAT/E2E DSL grammar reference
     (link to `../dabbler-uat-dsl/grammar/DabblerUatDsl.g4`), and the
     full `docs/component-catalog/index.json` injected as context.
   - Rules:
     - All values must be double-quoted string literals (per the W0
       grammar requirement).
     - Every label, caption, text, and scope value must match a catalog
       entry verbatim for the active route.
     - The DSL verb's element type must match the catalog `elementRole`
       (a link is `CLICK LINK`, not `CLICK BUTTON`).
     - Prefer structured VERIFY forms (per the W0 list). Free-form
       VERIFY is allowed but flagged.
     - FORBIDDEN: inventing a label not in the catalog. If no catalog
       entry exists, STOP and report the gap; do not emit the step.
4. Re-author the
   `sensitive-field-masking-audit-uat-remediation` UAT checklist via
   `route(task_type="uat-script-author")`. Supply:
   - The remediation set's spec.
   - The catalog (`docs/component-catalog/index.json`).
   - The current checklist as a reference for intended scenario coverage.
5. Validate the new checklist:
   ```bash
   dabbler-uat-dsl validate \
     docs/session-sets/sensitive-field-masking-audit-uat-remediation/sensitive-field-masking-audit-uat-remediation-uat-checklist.json \
     --catalog docs/component-catalog/index.json
   ```
   Iterate authoring until exit 0 with zero errors. If the catalog is
   genuinely missing an entry the checklist needs (not a guessing
   error — a real catalog gap), re-run the crawler with a targeted pass
   for that route × role and re-validate.
6. Manual compare against the original checklist — confirm the
   scenario coverage is preserved and that:
   - Step 2 label now matches the rendered `"Email address"`.
   - Step 9 uses `CLICK LINK` (was described as a button).
   - Steps 13/15/16 use structured VERIFY forms that exercise
     user-visible text, not DOM properties (e.g., `VERIFY TEXT in
     <ssn-cell-scope> matches "<masked-pattern>"`).
7. Save the re-authored checklist to its target path under
   `docs/session-sets/sensitive-field-masking-audit-uat-remediation/`.
8. Backfill suppressions for closed-set checklists that the W0 compiler
   flags. Suppression mechanism is owned by W0 (use the CLI's
   `--suppressions <path>` flag). Add a `tools/uat-suppressions.json`
   to dabbler-platform listing grandfathered checklists with rationale
   `"Pre-catalog checklist; closed set; not retroactively updated"`.
   Validation in CI uses this suppression file.
9. Update `docs/planning/project-guidance.md`:
   - Convention: catalog-grounded authoring is required for all new
     `requiresUAT: true` sets.
   - Reference: link to `dabbler-uat-dsl` repo for the grammar.
   - Workflow note: the `uat-script-author` task type is the only
     supported way to author a UAT checklist; manual authoring is
     deprecated.
10. Update `docs/ai-led-session-workflow.md` Step 5 (UAT generation):
    add the conditional that catalog-grounded authoring is invoked when
    `requiresUAT: true`.
11. Generate
    `docs/session-sets/component-catalog-grounded-authorship/change-log.md`
    via `route(task_type="summarization")`.
12. Full build:
    ```bash
    dotnet build Dabbler.Platform.sln
    dotnet test Dabbler.Platform.sln
    ```
13. Cross-provider verification.
14. Commit. Commit message:
    `Catalog-grounded uat-script-author + remediation checklist re-author`.
15. Send `send_session_complete_notification()`. Step 9 reorg review:
    - Promotion candidate: with W0 + W1 both shipped, the catalog +
      compiler combination has been used in two contexts (the original
      authoring + the re-author validation). Propose promoting the
      catalog crawler skeleton to `../dabbler-ai-orchestration` after
      one more app uses it.
    - Lessons-learned candidate: "Catalog-grounded authoring eliminates
      label-guessing class of UAT failures." Worth documenting if not
      already implied by the convention update.

**Creates:** `ai-router/prompts/uat_script_author.md`, `tools/uat-suppressions.json`, re-authored `sensitive-field-masking-audit-uat-remediation-uat-checklist.json`, `change-log.md`.
**Touches:** `ai-router/router-config.yaml`, `docs/planning/project-guidance.md`, `docs/ai-led-session-workflow.md`, possibly `docs/component-catalog/index.json` (targeted re-crawl).
**Ends with:** The re-authored remediation checklist passes `dabbler-uat-dsl validate` with zero unsuppressed errors. Human can now proceed with the remediation set's UAT against a catalog-grounded checklist.
**Progress keys:** `session-2/read-planning`, `session-2/router-task-type`, `session-2/prompt-template`, `session-2/reauthor-checklist`, `session-2/validate-pass`, `session-2/manual-compare`, `session-2/save-checklist`, `session-2/suppressions`, `session-2/guidance-update`, `session-2/workflow-update`, `session-2/change-log`, `session-2/build-and-test`, `session-2/verification`, `session-2/commit-push-notify`, `session-2/step-9-reorg`.

---

## What This Set Preserves

| Existing Asset | Status |
|---|---|
| Existing UAT checklists in closed sets | Unchanged; covered by suppressions |
| `sensitive-field-masking-audit-uat-remediation` code | Unchanged |
| `ai-router/router-config.yaml` existing task types | Unchanged; new task type appended |
| All existing Playwright and unit tests | Must continue passing |
| `Dabbler.Uat.Dsl` library and grammar | Unchanged; consumed as a NuGet package or via local pack |

## Risk Notes

- **Accessibility tree gaps.** Some MudBlazor components may not expose
  accessible names (e.g., icon-only buttons without explicit
  `aria-label`). The crawl emits them with `userVisibleLabel: ""`. The
  W0 validator will fail when a UAT step tries to name an empty-label
  element. That's a feature: it surfaces the missing label in the
  source component for fix.

- **Dynamic-route parameterization.** If a seeded entity does not exist
  for a parameterized route (e.g., no seeded orders), the crawler will
  fail navigation. Session 1 must verify seeded data covers all
  parameterized routes or skip unresolvable routes with a logged
  warning.

- **Compose stack must be running.** Sessions 1 and 2 require the app
  stack. Standard pattern: `docker compose ... up -d` before; `down` is
  the human's choice. Document this in the crawler's README.

- **Large catalog file.** ~5 roles × ~20 routes × ~30 elements ≈ 3,000
  records. ~500 KB JSON. Fine for now; W0 adapter loads to memory
  without streaming. Re-evaluate if app surface area grows.

- **`Dabbler.Uat.Dsl` consumption mechanism.** v1 uses local `dotnet
  pack` + a `nuget.config` pointing at the `bin/Debug/` folder of
  `../dabbler-uat-dsl/`. Acceptable for local development; CI adoption
  requires either a private NuGet feed or building the .nupkg in CI.
  Defer that decision until W2 wires CI execution.

- **The remediation checklist re-author may change wording.** The new
  checklist is a new artifact; the human reviews via the manual-compare
  step (Session 2 step 6) before UAT begins.
