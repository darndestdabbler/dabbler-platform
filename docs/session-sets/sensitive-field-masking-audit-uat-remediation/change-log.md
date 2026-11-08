# Change Log — sensitive-field-masking-audit-uat-remediation

> **Status:** COMPLETE (2026-05-01)
> **Sessions:** 2 of 2 closed
> **Authored:** at end of Session 2 per the session-set authoring guide and memory `feedback_change_log_end_of_set_only`.

---

## What this set delivered

This set was the UAT-remediation pass for the sensitive-field-masking and audit work originally shipped in Set D (`sensitive-field-masking-audit`, closed 2026-04-27 with a checklist that the human couldn't actually execute). The set ran in two distinct sessions:

### Session 1: Real-login UAT remediation (DSL-pipeline redo)

The original 2026-04-28 attempt at this session ran under the now-retired `*UatCoverageTests.cs` + `uat-coverage-review` pattern. It was reset on 2026-04-30 and re-executed under the DSL pipeline that `uat-workflow-dsl-alignment` shipped (W0 compile + W2 runner + UAT/E2E spec-declared scope). Session 1 closed 2026-05-01.

**What changed in code:**
- [src/Apps/AcmeCoffeeOnline.Api/Data/DatabaseInitializer.cs](../../../src/Apps/AcmeCoffeeOnline.Api/Data/DatabaseInitializer.cs) — codified an idempotent UPDATE backfill for `Customer.SocialSecurityNumber` paired with the existing ALTER, so pre-existing dev DBs converge on the seed-equivalent value the same way fresh-volume installs do. Replay-safe via the `IS NULL` guard.
- [src/Libs/Dabbler.Security.Blazor/Components/Restriction/RestrictedFormField.razor](../../../src/Libs/Dabbler.Security.Blazor/Components/Restriction/RestrictedFormField.razor) — fixed the initial-render race where the unrestricted `ChildContent` (editable input) flashed on screen for one render cycle before the async binder resolved. Real defense-in-depth fix; affected any user on a slow render or congested SignalR circuit.
- [ai-router/uat_runner/cli.py](../../../ai-router/uat_runner/cli.py) — added `--reset-app-state` CLI flag and a `RequiresAppStateReset: true` checklist field. When either is set, the runner force-recreates the web + api containers before the test runs, eliminating state-pollution across runs caused by `SecurityConfigurationManager` persisting UAT-2's MaskExemption grant to the container's writable layer.
- The masking checklist itself ([sensitive-field-masking-audit-uat-remediation-uat-checklist.json](sensitive-field-masking-audit-uat-remediation-uat-checklist.json)) was reauthored against the catalog with visible-text labels and the role-switch-driven flow. `RequiresAppStateReset: true` set so the runner auto-resets.
- Repo-local platform fixes: layered `PlaywrightFixture.WaitForQuietAsync` probe; un-stubbed `PlaywrightFixtureUatAdapter`; `dabbler-tracked-actions.js` MutationObserver auto-bootstrap (so `data-blazor-ready` fires regardless of which layout hosts the page); `ComponentCatalogCrawler` records visible text and aria-label separately and prefers visible text for `userVisibleLabel`; crawler follows `target="_blank"` links; `CustomerDataProvider.FindSearchFilter` reads `CompositeFilter.Filters`; `<HierarchyColumn />` added to `ComplianceReviewTab` so `<ChildRowContent>` actually has an expand affordance.

**What changed in the sibling repo (`dabbler-uat-dsl`):**
- `FOLLOW LINK IN NEW TAB` verb (grammar/AST/validator/emitter), with active-page tracking via a `currentPage` local in emitted code.
- Anchored visible-text regex with whitespace tolerance + IgnoreCase for `CLICK BUTTON / LINK / TAB / MENU ITEM`.
- `.mud-popover-open.Last`-scoped emits for `CLICK MENU ITEM` and `OPEN DROPDOWN`.
- Row locators via `Filter(HasText)` chains; `EXPAND ROW` chevron click; `Click + Clear + PressSequentially` for inputs; `VERIFY SNACKBAR` filtered by `HasText`; `Exact: true` on all `GetByLabel` calls.
- 199 unit tests pass.

**What was reverted:**
- Speculative role-switch propagation (`X-App-Switched-Role` header from `UserIdentityHandler`, gate in `InternalUserMiddleware`, `MaskExemption` integration in `PermissionService`, API-side `RoleSwitching` config block) was attempted mid-session and reverted in full at end-of-session. The current data shape has zero `AppFieldPermission` Hidden rows for `Customer.SSN`, so the role-switch gate doesn't change observable masking behavior — the Web's `RestrictionBinder` is the sole authority. The gate added attack-surface complexity without delivering observable defense. Reintroducing it is appropriate when an actual `AppFieldPermission Hidden` row needs API-side enforcement against role-switched users; that's a future security-hardening session set.

**Cross-provider verification:** 3 rounds (R1 4 issues, R2 1, R3 1 Major resolved by checklist-field auto-detect). Final state verified.

### Session 2: User Administration vs. Users-and-Role-Assignment audit

A discovery/audit session resolving the human's open question about the relationship between `/admin/users` and the Security Dashboard's "Users and Role Assignment" tab.

**Output:** [user-admin-audit.md](user-admin-audit.md) — disposition doc with full catalogue tables for both surfaces, per-task availability matrix, severity-tagged gap list, and three remediation options (differentiate / consolidate / defer).

**Key findings:**
- The two surfaces are *not* redundant; they serve different authorization scopes (`/admin/users` is org-membership-scoped UserAdmin elevation only; the dashboard tab is global role assignment).
- Their *names* mislead: "/admin/users" reads as the broader surface but is actually narrower; the "tab inside a dashboard" phrasing reads as smaller but is broader.
- **No admin-driven user-creation flow exists.** `/register` is `[AllowAnonymous]` self-service for retail-business customers; admins have no path to create backoffice users.

**Recommendation:** Option 1 — differentiate explicitly with cross-links + clearer titles + canonical UserAdmin surface + add-user stub. The disposition includes 6 concrete components and an honest sizing note acknowledging that even Option 1 requires shared-library changes (just smaller than Option 2).

**Cross-provider verification:** 3 routed calls (Gemini Pro draft sanity-review at $0.0114, gpt-5-4 R1 ISSUES_FOUND at $0.0840, gpt-5-4 R2 VERIFIED at $0.0694; total $0.1648).

**Suggested follow-on session sets:**
- `admin-users-cross-links` — implements Option 1 components 1-5 (rename, canonical UserAdmin surface, org-context-aware cross-links both ways, authorization-correct visibility).
- `admin-user-creation-flow` — implements Option 1 component 6 (admin-driven backoffice user creation with role + org-membership assignment in the same workflow).

---

## Durable knowledge promoted

### `lessons-learned.md`

- **Restriction-Binder Components Must Render Nothing Until First Resolution Completes** — the structural pattern derived from `RestrictedFormField` to prevent unrestricted-default leaks. Apply to any future `Restricted*` / `Authorized*` / mask-aware component.
- **State-Mutating UATs Pollute The Container's Writable Layer Across Runs** — the diagnostic technique and the durable runner-side fix (`RequiresAppStateReset: true` checklist field + `--reset-app-state` CLI flag).

### `project-guidance.md`

- **Catalog classifier — visible text wins, aria-label is the icon-only fallback** — promoted to the Human UAT And Checklist Editor section. Locks in the rationale for the catalog crawler's classification rule.
- **`--reset-app-state` and `RequiresAppStateReset: true`** — documented in the UAT Runner section as the canonical signal for state-mutating checklists.

### Step 9 reorg review (Session 2 — last session of set)

Three Session-2 candidates surveyed and declined as not yet meeting the two-context promotion threshold:
- The "naming-suggests-broader-scope" anti-pattern (one-context occurrence).
- The "two surfaces grant the same sensitive action" audit smell (one-context).
- The disposition-doc shape (catalogue → matrix → gap list → ranked options) (one-context).

If a future audit uses any of these patterns again, promote at that point.

---

## Known follow-on items

- **`uat-dsl-verify-input-value` (sibling-repo session set)** — proposal at [docs/session-sets/uat-dsl-verify-input-value/spec.md](../uat-dsl-verify-input-value/spec.md) covering both `VERIFY INPUT … has value matching "<regex>"` and tightening of `OPEN DROPDOWN` / `CLICK MENU ITEM` emit patterns to handle MudSelect's click-handler-attach race.
- **`admin-users-cross-links`** and **`admin-user-creation-flow`** — sized in the Session 2 disposition.

---

## Compatibility

- W0 compiler version: as shipped by `uat-workflow-dsl-alignment` (no change needed in this set).
- Catalog schema: 1.1 (the version the W0 toolchain consumes); the `userVisibleLabel`-vs-`ariaLabel` separation is now enforced by the crawler and consumed by the W0 emitter and the `uat-script-author` prompt template.
- Container baseline: any `--build --force-recreate` run picks up the codified SSN backfill and the `RestrictedFormField` render contract automatically.

---

## What this set did NOT do

- DevIdp restoration (prerequisite, separate set).
- Net-new masking/audit features (Set D's territory).
- The User Administration UI changes recommended by Session 2 (intentionally deferred to follow-on sets per spec).
- An API-side gate for the X-App-Switched-Role header (speculative work that was reverted in Session 1 because the current data shape doesn't need it).
