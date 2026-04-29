# Project Guidance — Dabbler Platform

> **Purpose:** The single source of truth for durable strategic commitments
> (Principles) and specific rules and patterns (Conventions) that apply to
> this repo and future projects built from the same platform direction.
> **Primary sources:** `docs/planning/testing-hierarchy.pdf`,
> `docs/planning/phase-3-platform-roadmap.md`

Read this file before every AI-led session. Use it as the first-stop reference
before changing architecture, testing strategy, UI patterns, workflow assets,
or human-UAT conventions.

Session-set specs may narrow the scope for a particular effort, but they should
not silently violate anything here without explicit human approval.

The file has two top-level sections:

- **Principles** — durable strategic commitments (the *why* and *what*). Slow
  to change; every principle should explain the reasoning behind itself.
- **Conventions** — specific rules, patterns, values, and code styles (the
  *how*). Faster to change; often promoted from successful lessons or
  demoted from over-general principles.

When a principle implies a specific rule, put the principle under Principles
and the concrete rule under Conventions. At the end of every session set the
orchestrator proposes reorganization candidates — promotions from Lessons to
Conventions/Principles, demotions, relocations — and the human accepts or
rejects each (see `docs/ai-led-session-workflow.md` Step 9, post-notify).

---

## Principles

### Architecture

- Use SQL Server as the primary database platform.
- Prefer temporal tables for durable auditing. When practical, keep the history
  tables in a separate `history` schema while preserving the base table name.
- Keep entity conventions consistent: surrogate primary key `Id`,
  `OrganizationId`, `UserId`, `SysStart`, and `SysEnd`.
- Route all database access through the REST service layer. The web app should
  not quietly become a second data-access layer.
- Keep the API in a separately deployable project and use Entity Framework Core
  for persistence and querying.
- Stay on the current .NET LTS line unless the human explicitly approves a
  change.
- Use the integrated libraries under `src/Libs/` as the default platform
  source. Do not add sibling-repo coupling unless a spec explicitly calls for
  it.

### Security And Organization Model

- Internal users should use SSO through Entra ID or the accepted equivalent.
- External-user auth may use simpler options when appropriate, but it must stay
  explicit and well-documented.
- Treat authorization as a runtime system that governs:
  users-to-roles, API capabilities, UI functionality, and page access.
- Authorization is built around five baked-in roles defined in code
  (`SystemAdmin`, `CrossOrgUserAdmin`, `CrossOrgMonitor`, `UserAdmin`,
  `User`) plus zero or more app-defined **user-type** roles. Baked-in
  roles carry compiled privilege rules that handlers consult before any
  configuration lookup. Applications cannot alter a baked-in role's
  privileges; they may only set a per-deployment `DisplayAlias`, or
  register additional non-user-type roles through the
  `IBakedInRoleProvider` DI contract.
- User-type roles are default-permissive and shaped entirely by explicit
  **restrictions**: `ColumnRestriction` (view-only or hidden),
  `RowRestriction` (filter expression limiting visible records), and
  `UiRestriction` (disabled or hidden page or control). User-type roles
  are configured in JSON; baked-in roles are not.
- **Role inheritance is not supported.** A user assigned to multiple
  roles must explicitly switch to operate under each one, and every
  transactional entity records the `RoleId` under which each write was
  made (see the `transactional-system-columns` session set).
- Sensitive-field masking is modelled separately from restrictions as
  platform-wide **Default Masks** plus per-role **Mask Exemptions** with
  optional `ExpiresAt` and explicit authority rules (separation of
  duties). Masks render even when the stored value is null, so presence
  or absence of a sensitive field never leaks through an empty-looking
  unmasked control.
- **Baked-in roles bypass DefaultMask enforcement.** The `RestrictionBinder`
  always returns `IsMasked = false` for baked-in roles (SystemAdmin,
  CrossOrgUserAdmin, CrossOrgMonitor, UserAdmin, User) — they see clear
  values regardless of any DefaultMask rule. Only user-type roles (those
  defined in `security-config.json` `UserTypeRoles`) are subject to
  DefaultMask rules and mask exemption checks. This distinction affects
  E2E test design: tests verifying masking behaviour must use a user-type
  role (e.g., BackofficeUser), not a baked-in role.
- **For compliance-accountable classifications the business already
  owns — masking targets, retention classes, PII flags, and similar —
  trust the business operator to populate them and rely on the internal
  auditor as the backstop. Do not build or maintain auto-detection
  tooling (name-scanners, "suggested items" safety-nets, auto-flag
  heuristics) to second-guess the business.** Such tooling adds a
  second system that can also be wrong without removing the auditor
  review, and it imposes permanent code, UI, and test cost for a
  speculative gain. This stance is specific to items a business role
  is already accountable for under an external review process; it is
  not a general rejection of security detection (dependency scanning,
  test guardrails, automated access reviews, etc. remain encouraged).
- Every role — baked-in and app-defined — has a canonical immutable
  `Name` used in handlers, claims, and audit records, plus an optional
  `DisplayAlias` shown in the dashboard, role switcher, and audit UI.
- Support organization and role switching as a first-class concept:
  `SystemAdmin` can switch freely, while other users can switch only among
  their valid organizations.
- Keep seeded-session and dev-inbox helpers Development or Test only,
  loopback-only, and secret-guarded.

### UI Philosophy

- The web application layer is Blazor Server.
- WCAG 2.1 AA is the default accessibility bar for reusable components and
  application pages.
- The long-term goal is not just to make one demo app work. It is to validate
  reusable components and libraries that can support many future apps while
  keeping the developer experience as simple as possible.
- Use DI seams and reusable provider abstractions instead of one-off test hacks
  whenever the platform already has a stable contract.
- Treat DI-toggled stub behavior as a supported platform feature, not just a
  test trick. It should help bUnit, stub/demo hosts, early design review, and
  future generated apps.
- Prefer built-in MudBlazor capabilities when they already satisfy the intended
  product and platform need. Custom Dabbler abstractions should earn their
  existence by clearly improving consistency, supported capability, testability,
  or developer experience.
- Do not build and maintain custom wrapper surfaces that merely duplicate
  MudBlazor behavior without adding durable platform value.
- Use explicit page composition rather than metadata-driven UI generation. Each
  page should declare its own MudDataGrid columns, MudForm fields, and layout.
  This makes pages easier to read, debug, test, and customize.
- Keep domain logic (filter models, expression engines, data providers)
  factored separately from UI components so UI can be rebuilt independently.
- The demo app should be presentation-worthy and broad enough to prove the
  platform without turning into product-specific scaffolding.

### Testing Hierarchy

Testing follows this order of preference:

1. Strict unit tests
2. Integration tests
3. bUnit tests
4. True end-to-end tests
5. Human UAT through the checklist editor

**Strict unit tests first.** Use them for any logic that can be isolated from
I/O, runtime infrastructure, or browser concerns. Structure code so the
unit-testable portions are factored into methods, services, or helpers that
can be exercised without integration setup.

**Integration tests second.** When strict unit tests are not possible, prefer
integration tests for server-side or service-boundary behavior. They should
cover API behavior, persistence, security boundaries, and non-UI seams that
still need realistic infrastructure.

**bUnit for UI-seam proof.** For UI-related work where integration tests are
inadequate, use bUnit before escalating to browser-level testing. Structure
UI code so rendering, validation, visible-state decisions, and component
interactions can be isolated behind bUnit-testable seams. DI-toggled stub
behavior is encouraged when it makes bUnit tests simpler, cleaner, and more
representative of the supported platform shape. Reusable UI components must
have interaction-contract coverage — not just markup-presence.

**True E2E for real browser and data proof.** Use true end-to-end tests when
the lower layers cannot adequately prove the behavior, and to build confidence
in meaningful user workflows. True E2E means more than route reachability —
input validation behavior, tab order, navigation with and without route
parameters, common user action sequences, and the ultimate effect of
important workflows in the database.

**Styling and accessibility must be tested too.** Styling defects that
materially affect usability should be caught before human UAT whenever
practical. Reusable components and key workflow pages should have automated
checks for visible focus, selected-state contrast, readable text against
backgrounds, layout overlap, and other WCAG 2.1 AA-adjacent presentation
risks.

**Human UAT last.** Human UAT is a confidence and judgment layer, not a
replacement for the automated hierarchy. Use it for clarity, coherence, trust,
visual quality, and other judgments that automation cannot score well. If
human UAT starts finding numerous broken controls, the automation strategy
needs correction before more human testing continues.

### Human UAT Philosophy

> Human UAT reviews judgment, not correctness. Every functional claim in
> a UAT checklist must have E2E test coverage that exercises the same
> sequence and verifies the same outcome. Items that genuinely cannot
> be automated must be flagged as judgment items with justification.

This principle is the answer to the governance question *"what stops an
AI agent from shipping broken code?"* The answer is **not** *"the human
will catch it in UAT."* Human UAT is the judgment backstop; automated
E2E tests are the correctness backstop. Handing a human a checklist
full of untested steps inverts the testing hierarchy and makes the
human the first automation pass — slow, frustrating, and corrosive to
the feedback loop. See the `E2E Coverage Before UAT` section and rule
11 of the authoritative rules list in `docs/ai-led-session-workflow.md`
for the procedural enforcement.

- **Step N in the UAT checklist corresponds to Step N in the E2E test.
  Identical actions. Identical parameters. Identical verifications.
  Not "equivalent" — identical.** The discipline is mechanical on
  purpose: if "equivalent" were enough, every drift point between
  prose and automation would re-surface as a human surprise. A
  Playwright test that clicks a row with a different label, or
  navigates via a different entry point, or asserts against a
  paraphrased expectation, does not satisfy this discipline even when
  it passes.
- **Parameter values are shared, not parallel.** The persona email,
  entity name, search term, URL, role name, and expected outcome the
  human will act on are the same literal values the Playwright test
  drove. The structured-`Steps` `TestData` object is the shared source
  of truth; the Playwright fixture reads from the same values.
- **E2E passes before the human is notified.** Sequencing matters: the
  Playwright suite runs to green first, the checklist is generated
  from the green suite, `uat-coverage-review` returns VERIFIED on the
  pair, then the commit lands, then the human is notified. Generating
  a checklist against a red suite and asking the human to triage is
  the failure mode this principle exists to prevent.
- **Judgment items still get sequence coverage.** An item flagged
  `IsJudgmentItem: true` is exempt from the matching-step requirement
  but not from the reachability requirement — a test must still drive
  the UI to the state the human will be evaluating, so the human is
  rendering judgment on a verified-live screen rather than debugging
  an exception. A one-sentence justification accompanies every
  judgment flag.
- Human UAT is where confidence and judgment live. It is not where
  basic operability defects should first be discovered.
- Pending human UAT blocks downstream sessions unless the human
  explicitly overrides the pause.
- Capture screenshot evidence when practical so the human can see
  styling and interaction changes evolve across sessions.
- **A filled UAT checklist does not review itself.** Between the
  human finishing the checklist and any downstream remediation work
  starting, run a dedicated **UAT Review Session** (see the next
  bullet). Skipping it is how UAT findings sit unaddressed for weeks
  or get folded opportunistically into an unrelated set, losing the
  human's signal.
- **UAT Review Session (set type).** A lightweight 1-session set
  whose single deliverable is a disposition for every finding in a
  just-completed UAT checklist.
  - **Input:** the filled `<set-slug>-uat-checklist.json` committed
    at the end of the prior set's closeout.
  - **Steps:**
    1. Read every Review item where `Passes == false` or `Feedback`
       is non-empty (including auto-generated "Other" rows).
    2. Classify each finding as one of:
       **Blocker** (ship-stopping, must remediate before any other
       set starts), **Substantive** (real defect, becomes scope of
       a new remediation set), **Minor** (polish or style, fold into
       an adjacent planned set's spec), or **Out of scope** (scope
       creep or rejected suggestion, documented with rationale).
    3. Produce one of three outputs depending on severity: a stub
       spec for a new remediation set, a one-commit micro-fix plan
       if findings are small enough, or a signoff note if nothing
       requires action.
    4. Commit the disposition artifact alongside a short
       review-summary commit message.
  - **Ends with:** the next action queued — either a named next set,
    a named next single-session fix, or an explicit "no remediation
    needed" signoff. The human is not left holding the checklist.
  - **Orchestrator guidance:** classification is mechanical pattern
    matching (group + severity + suggested disposition) and fits
    Sonnet cleanly; Opus is only needed when a finding is genuinely
    ambiguous about whether it's Substantive or Minor, or when a
    Blocker candidate needs architectural judgment to size.
- **Every UAT checklist item must be proven executable by automation
  before the checklist is handed to the human.** For each Review item
  there must be a Playwright test (or equivalent browser-level test)
  that performs the same steps with the same parameters and asserts
  that the described expectation is reached. Items whose expected
  outcome is a subjective design or aesthetic judgment still require
  a Playwright test that executes the full sequence and asserts that
  nothing throws, no required control is missing, and the UI reaches
  the state the human will be evaluating. This is not optional.
  UAT is confidence and judgment, not first-pass operability
  verification; sending a human to discover a null-reference
  exception or a missing nav entry wastes their time and undermines
  the checklist's purpose.
- **E2E tests (and the UAT checklists derived from them) describe
  what the user sees, not what is happening behind the scenes.**
  When a role carries a `DisplayAlias`, the checklist step refers
  to the alias — the canonical name may appear in parentheses or
  in `TestData` for cross-reference, but the human-facing action
  text uses the label the human will actually read. When a
  currency column renders `$1,234.56`, the test asserts against
  the formatted string, not the underlying decimal. When the
  user never sees themselves "log in with role X" (because the
  system assigns the role through organization switching, role
  switching, or seeded impersonation that surfaces the role only
  on a subsequent screen), the checklist's step ordering mirrors
  the user's sensory timeline — the "role is active" verification
  lands at the screen where the role first becomes visible, not
  at login. This principle is the filter that catches UI-vs-model
  drift: if the checklist says "sign in as CrossOrgUserAdmin" but
  the user sees "Back Office Administrator" in the role switcher,
  the checklist has drifted away from the human's experience and
  the UAT signal loses fidelity. Before routing
  `uat-plan-generation` or hand-authoring a checklist, walk through
  the flow as the user will: what do they see, in what order, with
  what labels? Write the checklist from that perspective; wire the
  test to the same visible surface.

### Precedence

When guidance appears in multiple places, apply it in this order:

1. Explicit human direction in the current conversation
2. Session-specific spec requirements
3. This document
4. Supporting planning notes and roadmaps

If those sources conflict in a meaningful way, stop and confirm with the human
before proceeding.

---

## Conventions

### MudBlazor Usage

- Use `MudDataGrid<T>` directly with `ServerData` callbacks, column
  definitions, sorting, and paging. Do not wrap it in a custom component
  unless a specific, justified need arises.
- Apply `Bordered="true"` to all MudDataGrid instances for visual column
  separation.
- Right-align currency and numeric columns with
  `CellStyle="text-align:right; max-width:130px"` and a matching HeaderStyle.
  Right-align date columns using the same pattern. The max-width keeps values
  close to their headers in flexible layouts.
- Use `MudForm` directly for detail pages with MudBlazor input components and
  built-in validation. Child entity grids belong in `MudTabs`. Popup editing
  uses `MudDialog` with `MudForm` content.
- Use `MudLayout`, `MudAppBar`, `MudDrawer`, `MudNavMenu`, and
  `MudMainContent` for the application shell. Navigation is route-based with
  clean URLs, browser history support, and standard Blazor routing.
- When a session spec names a specific MudBlazor widget but the semantics it
  describes don't fit that widget, choose the widget that honors the semantics
  rather than forcing a mismatched control. `MudSwitch` is natively binary in
  MudBlazor 8.x — use `MudToggleGroup<T>` with
  `SelectionMode.SingleSelection` for tri-state or multi-state selection
  (e.g., `ColumnRestriction` ViewOnly|Hidden, `UiRestriction` Disabled|Hidden,
  Allow|Deny|Unset). Call out the deviation explicitly in the end-of-session
  verification prompt so the verifier sees the reasoning.

### Filter UI

- Preserve the QBE filter model (AND/OR groups, operators, presets, expression
  engine, filter summary) as platform value.
- Build filter UI from MudBlazor input components via `FilterPanel<T>`, not
  from custom wrapper components.

### Styling And Spacing

- Standardize spacing and layout rhythm through reusable tokens, conventions,
  or helper abstractions rather than ad hoc per-page margin and padding
  choices.
- Theme and styling changes should go through reusable, testable guardrails
  when practical so foreground/background combinations, selected states, and
  dynamic color changes remain readable and accessible.
- If a visual treatment is important enough to carry user meaning, it is
  important enough to validate automatically.
- Use a combination of component tests, browser-based assertions,
  accessibility tooling, and screenshot or visual-regression evidence for
  high-value screens.

### Testing Conventions

- Factor pure-logic portions out of mixed-responsibility methods so they can
  be strict-unit-tested. Do not keep mixed-responsibility methods intact when
  the pure-logic portion can be split out.
- Integration tests should not become a substitute for unit tests when logic
  could have been isolated earlier.
- bUnit should carry as much UI verification load as is realistically
  possible before resorting to browser automation.
- Do not treat markup-presence tests as sufficient proof for interactive
  controls. If a button, chip, toggle, menu, calendar icon, or similar
  control exists for user interaction, automated tests should exercise the
  interaction itself.
- E2E tests must use the same user entry points that a human uses when the
  goal is to validate navigation or operability. Do not bypass the control
  under test with direct route navigation and then treat that as proof that
  the navigation control works.
- Disabled-state and hidden-control tests should sign in as the role whose
  product definition mandates that absence. Asserting that a privileged role
  (for example, SystemAdmin) cannot see a control is brittle: when the product
  expands that role's access, the safeguard inverts into a guaranteed
  regression. Pair every "control absent" test with a positive-path test
  where a permitted role sees the control, and prefer to drive absence
  assertions through the role whose deny semantics are intrinsic to the
  model.
- For supported user workflows, automation should prove not only persistence
  outcomes but also visible UI outcomes: route changes when expected, active
  state changes, refreshed grid contents after add or delete, snackbar or
  banner feedback, denial states, and keyboard-operable control flow.
- Docker-backed execution is the preferred baseline for true E2E.
- When practical during UI and E2E work, provide screenshots to the human so
  they can see visual state and interaction progress.
- **Components with async handlers that drive a Blazor render that a Playwright
  test must wait through should inherit `TrackedComponentBase` and wrap those
  handlers with `TrackAction`.** Test-side synchronization with the
  watermark goes through the DSL `WAIT FOR ACTIONS QUIET` verb (see the
  closed verb list under *Human UAT And Checklist Editor*) — emit it
  after every interaction on a route whose catalog entry has
  `IsWatermarked: true`. This includes async user-action handlers
  (clicks, switches, dialog opens) AND async data-loading callbacks (search
  debouncers, paging callbacks, filter changes) — anywhere a Blazor render is
  driven by an async handler that the test needs to synchronize against.
  Promoted 2026-04-29 from playwright-sync-watermark-and-playbook Session 5,
  which extended the watermark from `ToggleAvailabilityAsync` (user action) to
  `OnSearchAsync` (data-loading callback) to close a click-during-search-rerender
  race. `TrackedComponentBase` lives in `src/Libs/Dabbler.Testing.Blazor/Components/`
  and is registered via `builder.Services.AddTrackedActions(builder.Configuration)`.
  Action tracking is controlled by `Testing:ActionTracking:Enabled` in
  `appsettings.json`; it is `false` by default (no production overhead) and
  `true` in `appsettings.Development.json`. **Important: `TrackAction`'s JS-interop
  calls go through `TryInvokeJsAsync` and silently no-op if `IJSRuntime` is not
  yet dispatchable** (prerender boundary, intermediate render passes). This is a
  test-time signal; missing the watermark in those frames degrades to "no signal"
  rather than failing the page render. Without this guard, an early-fire of a
  TrackAction-wrapped event handler turns into an HTTP 500 — observed in
  Session 5 when the un-hardened wrap broke `/admin/products` for non-SystemAdmin
  roles. Note: `Dabbler.Security.Blazor` does not reference `Dabbler.Testing.Blazor`
  — migration scope for that library is deferred until a spec explicitly adds
  the dependency.

### Human UAT And Checklist Editor

- **Catalog-grounded authoring is mandatory for new UAT checklists.** Every
  user-visible label, caption, text, tab name, scope descriptor, and column
  header in a new UAT checklist must come from the component catalog at
  `docs/component-catalog/index.json` for the route × role active in that
  step. The catalog is produced by `tools/ComponentCatalogCrawler/` (a .NET
  console app that crawls the running compose stack as each role and
  scrapes accessibility-tree labels). Re-crawl on demand before authoring a
  new checklist; commit the diff so UI changes are visible in PR review.
  Authoring goes through `route(task_type="uat-script-author")` — that task
  type's prompt template (in `ai-router/prompt-templates/task-prompts.md`)
  forbids label invention and routes the call to Sonnet by default. Manual
  hand-authoring of UAT checklists is deprecated; if the orchestrator
  authors directly (e.g., when the API call times out), it must still
  select every label verbatim from the catalog. Validation is delegated to
  the `dabbler-uat-dsl validate <checklist> --catalog <path>` CLI from the
  sibling `dabbler-uat-dsl` repo (W0). Closed pre-catalog checklists are
  grandfathered via `tools/uat-suppressions.json`.
- The W0 SemanticValidator currently updates its `currentRoute` cursor only
  on `NAVIGATE TO` statements; click-driven navigation (CLICK LINK,
  CLICK ROW that lands on a detail page) does NOT propagate the new route.
  Click-driven UAT scripts therefore produce cascading `UnknownRoute`
  errors after the first click-driven transition. This is a known W0
  limitation; mitigations are documented in `tools/uat-suppressions.json`
  and a future W0 enhancement (route-transition inference from click
  semantics, or a `VERIFY URL matches "..."` route-cursor sigil) will
  remove the suppression. Until then, validate output should be triaged
  with the suppressions file.
- Use the UAT checklist editor schema from
  `https://github.com/darndestdabbler/uat-checklist-editor/blob/main/checklist-schema.json`
  for checklist JSON files intended for human execution.
- Open and run those files through
  `https://darndestdabbler.github.io/uat-checklist-editor/`.
- **MANDATORY: every UAT step must be performable by both the human and
  the E2E test using only user-visible surfaces.** A three-step authoring
  discipline applies, in order, with no shortcuts:
  1. **Write the UAT step using the UAT/E2E DSL** (closed verb list
     defined below). Each step is one primitive verb parameterised by
     visible labels and visible text only — never by testid, xpath, CSS
     class, or DOM index. The verb list is closed and may only be
     extended with explicit human permission committed in the same patch
     as the new verb's first use.
  2. **Write the E2E test driving those same explicit steps with the same
     locators.** Each Playwright step maps 1:1 to a numbered checklist
     step. **Test-framework backdoors are forbidden when the step under
     test is something a human is also expected to perform.** That
     includes: `SeededSignInAsync(roles=)` overrides for sign-in steps,
     `/test-auth/*` shortcuts for navigation steps, direct
     `security-config.json` edits for permission-grant steps, EF API
     pokes for things the user is supposed to do through the UI. Such
     shortcuts are acceptable ONLY for test setup of state that is
     out-of-scope of the UAT (e.g., pre-seeding inventory the human
     accepts as given).
  3. **Cross-verify every step is reachable by both sides.** For every
     numbered step, confirm the human can perform it in a real browser
     and the E2E test can perform it through user-visible locators.
     If either side cannot, the step is inadequate — fix the UI gap
     (e.g., assign the persona the missing role, add the missing
     button, restore the missing page) or remove the step. Do NOT
     paper over a UI gap with a developer shortcut.
  When a session set has UAT but the supported sign-in path or any
  other user-visible surface is not yet wired up, BLOCK the UAT until
  that surface works. Surface this at spec authoring or at the start
  of the closing session, not after generating an unrunnable checklist.

- **The UAT/E2E DSL — closed verb list.** Every step in a UAT
  checklist and every `// begin STEP NNN` block in the matching E2E
  test must express its action using exactly one of these verbs.
  Each interaction verb may take an optional `in <scope>` qualifier;
  `<scope>` is a labelled region (e.g. `in the dialog titled "Add
  Mask Exemption"`, `in the Admin drawer group`, `in the expanded
  row for "Customer 1"`, `in the "Mask Exemptions" tab`). Locators
  inside a scope are still by visible label only.

  **Lifecycle / plumbing**
  - `NAVIGATE TO <URL>`
  - `WAIT FOR <visible state>` — e.g. *"the page displays `X`"*,
    *"the URL matches `<pattern>`"*, *"the snackbar shows `Y`"*
  - `WAIT FOR ACTIONS QUIET` — no parameters; compiles to
    `await fixture.WaitForQuietAsync(page)`. **Required after every
    interaction verb on routes whose catalog entry has
    `IsWatermarked: true`** (the page component inherits from
    `Dabbler.Testing.Blazor.Components.TrackedComponentBase`; see the
    Testing Conventions entry on `TrackedComponentBase`). Do not emit
    after pure navigation, pure verification, or on routes whose
    catalog entry has `IsWatermarked: false`. The
    `uat-script-author` prompt template enforces this rule
    automatically; hand-authored checklists must follow it manually.
  - `PRESS KEY <Tab|Enter|Escape|...>`

  **Interaction**
  - `CLICK BUTTON [in <scope>] with caption <caption>`
  - `CLICK ICON BUTTON [in <scope>] with aria-label <label>`
    *(icon-only controls — chevrons, edit pencils, close X's)*
  - `CLICK LINK [in <scope>] with text <text>`
  - `CLICK TAB [in <scope>] with name <name>`
  - `CLICK MENU ITEM [in <scope>] with label <label>`
  - `CLICK INPUT [in <scope>] with label <label> and enter <value>`
  - `CLICK CHECKBOX [in <scope>] with label <label>`
    *(distinct from switch — different ARIA + binary)*
  - `CLICK SWITCH [in <scope>] with label <label>`
  - `CLICK SWITCH [in <scope>] in column with header <header>`
  - `OPEN DROPDOWN [in <scope>] with label <label> and select item whose text = <text>`
  - `PICK DATE [in <scope>] with label <label> and choose <date>`
  - `SCROLL <grid name> to row where <col header> = <value> [and <col header> = <value> ...]`
  - `CLICK ROW in <grid name> where <col header> = <value> [and ...]`
  - `EXPAND ROW in <grid name> where <col header> = <value> [and ...]`

  **Verification** (see "Structured VERIFY Forms" below for formal syntax)
  - `VERIFY <user-visible property or event>` — text content, URL
    match, control enabled/disabled, presence/absence of element-by-
    label, snackbar text. Never verify by testid or DOM internals.

  **String Values Must Be Double-Quoted**
  - All user-visible labels, captions, text values, and scope
    descriptors must be double-quoted string literals in checklist and
    E2E DSL steps. Keywords like `with`, `in`, `and`, `where`, etc.
    appear bare. Examples:
    - ✓ `CLICK BUTTON in "Add Mask Exemption" dialog with caption "Save"`
    - ✗ `CLICK BUTTON in Add Mask Exemption dialog with caption Save`
  - This makes the grammar unambiguous: keywords inside quoted values
    are never tokenized as structural keywords.

  **Structured VERIFY Forms** (Formal, Executable)
  - Structured VERIFY steps compile to deterministic Playwright
    assertions. Free-form VERIFY statements (see below) remain
    supported for backward compatibility but emit `// PENDING` comments
    in generated test code (human-only, not automatically asserted).
  - Structured forms and their Playwright mappings:
    - `VERIFY ELEMENT [in <scope>] with label <label> is visible`
    - `VERIFY ELEMENT [in <scope>] with label <label> is not visible`
    - `VERIFY ELEMENT [in <scope>] with label <label> is enabled`
    - `VERIFY ELEMENT [in <scope>] with label <label> is disabled`
    - `VERIFY TEXT [in <scope>] equals <expected>`
    - `VERIFY TEXT [in <scope>] contains <expected>`
    - `VERIFY TEXT [in <scope>] matches <regex>`
    - `VERIFY URL matches <pattern>`
    - `VERIFY SNACKBAR shows <text>`
    - `VERIFY ROW in <grid name> where <col> = <value> exists`
    - `VERIFY ROW in <grid name> where <col> = <value> does not exist`
  - Legacy free-form `VERIFY <text>` continues to be accepted and
    emits a `// PENDING: <text>` comment pointing to the structured
    alternative. New checklists should prefer structured forms.
  - The canonical ANTLR grammar and latest structural forms are
    published at:
    https://github.com/darndestdabbler/dabbler-uat-dsl/blob/main/grammar/DabblerUatDsl.g4

  **DSL design rules:**
  - **Locators are by visible label or visible text** — never by
    testid, xpath, CSS class, or DOM index. The verifier rejects any
    step whose locator is not user-visible.
  - **Row identification is always by column-value match** — never
    by row index ("the first row," "the third row"). Column-value
    matches stay stable across data shuffles.
  - **`in <scope>` is uniform across interaction verbs** — same
    syntax in UAT prose and in `// begin STEP NNN` E2E commentary,
    so the verifier can mechanically pair them.
  - **Composites don't exist.** "Sign in" is 5–8 primitive steps.
    That's the point. If the human can't follow each one in a real
    browser, the test is inadequate.
  - **Extending the verb list requires explicit human permission**
    committed in the same patch as the new verb's first use, with
    a real session-set example. No speculative verbs.

- **Step-block alignment between UAT and E2E.** The
  `uat-coverage-review` gate must mechanically pair UAT steps to
  E2E steps. The pairing scheme:
  - **Each UAT Review item names its E2E test method** in the
    existing `E2ETestReference` property (form: `Class.Method` or
    fully-qualified `Namespace.Class.Method`). The verifier resolves
    that reference to a real `[Fact]` (or `[Theory]`) method in the
    Playwright source. Reject any non-judgment Review item with no
    method match.
  - **Each step within an item is numbered** 1, 2, 3, ... via the
    existing `StepNumber` property. Step numbers are zero-padded to
    three digits when referenced from E2E code (`STEP 001`,
    `STEP 002`, ..., `STEP 042`).
  - **Inside the method body, `// begin STEP NNN` comments delimit
    each step's code**, with identical numbering and order to the
    UAT item's `Steps[].StepNumber`. The comment is the alignment
    marker — the verifier walks both files and pairs comment-to-step
    on N. Mismatched count or out-of-order numbering is `ISSUES_FOUND`.

    ```csharp
    [Fact]
    public async Task SomeName_DescriptiveAsync()
    {
        // begin STEP 001
        await page.GotoAsync("http://localhost:5020/login");

        // begin STEP 002
        await page.GetByLabel("Email").FillAsync("admin@acmecoffee.com");

        // begin STEP 003
        await page.GetByRole(AriaRole.Button,
            new() { Name = "Send sign-in code" }).ClickAsync();
        // ...
    }
    ```

  - **The verifier walks `E2ETestReference` → method match →
    step-block pairing** and flags any DSL violation (testid locator,
    row index, framework backdoor on a human-equivalent step) as
    `ISSUES_FOUND`. The `uat-coverage-review` prompt template drives
    this pairing mechanically.

- **MANDATORY: validate the generated checklist against the canonical schema
  before committing.** The schema lives at
  `https://raw.githubusercontent.com/darndestdabbler/uat-checklist-editor/main/checklist-schema.json`
  — fetch it, run a JSON-Schema validation pass against the checklist file,
  and fix any validation errors before the orchestrator notifies the human.
  Required field shapes that have caused two prior incidents:
  - Root object: `ProjectName` (string, required), `Review` (array, required).
    `ReleaseLabel`, `DocumentDate`, `Configuration` are optional. **Do NOT use**
    `SessionSetSlug`, `Title`, `Description`, `CreatedDate`, `Version`, or
    `Reviews` (plural) — these are not in the schema and the editor rejects
    files that contain them.
  - Each `Review[i]` item requires `FunctionalArea`, `Subarea`, `ItemLabel`.
    The schema also accepts `HumanAction`, `Expectation`, `Steps`, `TestData`,
    `E2ETestReference`, `IsJudgmentItem`, `Passes`, `Feedback`, `IsOtherItem`.
    **Do NOT use** `Id`, `Title`, or `JudgmentJustification` — not in the
    schema. UAT↔E2E pairing uses `E2ETestReference` (resolves to the
    matching `[Fact]` method) plus `// begin STEP NNN` comment markers in
    the method body — see the Step-block alignment convention. A
    judgment-item rationale belongs in `Feedback` (e.g.,
    `"Feedback": "Judgment item — <rationale>"`).
  - Each `Steps[j]` item requires `StepNumber` (integer) and `Action` (string).
    Optional `Verifications` is an array of **objects** each with a `Text`
    property — **NOT** an array of plain strings, and **NOT** containing
    `HumanAction` (the step's action goes in `Action`, not `HumanAction`).
    `E2ETestReference` belongs at the Review level, not on individual steps.
  - Verifications support `Text` (required), optional `VerificationMethod`
    (`"ui"` | `"api"` | `"sql"`), `Query`, and `ExpectedValue`.
  - `additionalProperties` is `false` at the root, on each Review item, on
    each Step, and on each Verification — extra fields cause validation
    failures.
  After fixing structural shape, the editor still requires the checklist to
  be browsable end-to-end — open it locally before notifying the human.
- Save human review results inline in the checklist JSON itself. Do not
  create a separate empty `*-findings.json` scaffold for pending review.
- **Generate a new UAT checklist per session set.** Name the file
  `{session-set-slug}-uat-checklist.json` and place it inside that session
  set's folder. Do not re-use or re-run a checklist from an earlier session
  set — each set should have its own inline result record so audit history
  stays scoped and unambiguous.
- Include concrete test data (search terms, entity names, order numbers,
  seeded emails) in the `HumanAction` field for any step that involves
  searching, filtering, or selecting by name or ID. The reviewer should not
  have to guess or explore the database.
- When a session creates or refreshes a human-UAT checklist and that review
  has not yet been completed, treat the pending human step as a blocker for
  new sessions unless the human explicitly overrides it.
- Handoff messages for pending UAT must be unmistakable: name the checklist
  path, link the editor, say that the agent is waiting for the human
  response, and keep any optional parallel work low-risk.
- **Playwright pre-testing per UAT item is mandatory.** Before
  committing a checklist to the repo and notifying the human, each
  functional `Review` item must have a corresponding Playwright test
  that:
  1. Signs in as the persona named in the step (reuse
     `PlaywrightFixture.SeededSignInAsync`).
  2. Executes the same navigation and interaction steps the checklist
     describes, using the same parameter values — the ones carried in
     the item's `TestData` object or, for legacy items, embedded in
     the `HumanAction` text.
  3. Asserts the objective portion of the expectation: the expected
     route was reached, the required controls are present and enabled,
     the described persist/refresh behaviour occurred, no unhandled
     exception bubbled to the circuit, and no required data is missing
     from the UI.

  **Preferred authoring shape for items paralleling an E2E test:**
  use the extended checklist schema's structured `Steps` array with
  numbered steps, each step's `Verifications` list, and
  `E2ETestReference` set to the fully-qualified test method name.
  The step numbers, actions, parameters, and verifications in the
  checklist are *identical* to those in the matching Playwright test —
  see `Principles → Human UAT Philosophy` for the rationale. Legacy
  `HumanAction`/`Expectation` items remain valid and render in the
  editor unchanged; prefer the structured form whenever parallelism
  with an E2E test exists.

  **Authoring and verification go through the router.** Use
  `route(task_type="uat-plan-generation")` to produce the structured
  checklist from the session set's spec and the Playwright coverage
  file. Before committing, call
  `route(task_type="uat-coverage-review")` with the generated
  checklist and the Playwright file — it returns VERIFIED only when
  every non-judgment item has a matching E2E step with identical
  action, parameters, and verifications, **AND** every step in the
  checklist passes per-step reachability: a real human in a browser
  can perform the step using only user-visible surfaces. The
  `uat-coverage-review` prompt must explicitly request this
  reachability check; ISSUES_FOUND on any developer-shortcut
  dependency (test-only sign-in overrides, direct config edits for
  steps the human is supposed to perform, etc.) blocks the handoff.

  Group the Playwright tests in a `*UatCoverageTests.cs` file (or
  equivalent) under `tests/AcmeCoffeeOnline.Playwright/` and
  cross-reference the UAT item in a comment on each test method so
  future changes stay in sync. If a test is infeasible because the
  expectation is purely aesthetic, mark the checklist item
  `IsJudgmentItem: true` with a one-sentence justification — the
  test still covers the step sequence and asserts reachable-state so
  the human is rendering judgment on a verified-live UI. Checklists
  shipped without this coverage are a session-closeout defect —
  re-run the pre-test step before marking the session complete.
  
---

### UAT Runner (W2)

When a session set declares `requiresUAT: true`, the orchestrator invokes
the UAT runner before proceeding to build/test and notifying the human.
The runner:

- Compiles the catalog-grounded checklist via the W0 compiler.
- Runs the compiled test via `dotnet test`.
- Classifies failures via a four-class taxonomy (APP_FUNCTIONAL_BUG,
  CATALOG_STALE, SEED_DATA_GAP, AMBIGUOUS_LOCATOR).
- Auto-applies CATALOG_STALE remediations and re-runs.
- Files human-review issues for the other three classes.

The runner is invoked via:

```bash
# Invoked as a script — the package directory uses a hyphen
# (`ai-router/`), which prevents the `-m ai_router.uat_runner.cli` form.
# `cli.py` bootstraps `ai_router` via importlib at startup. A repo-wide
# rename is tracked as its own session set.
python ai-router/uat_runner/cli.py \
  --checklist docs/session-sets/{slug}/{slug}-uat-checklist.json \
  --catalog docs/component-catalog/index.json \
  --session-set {slug}
```

Exit codes:

- `0` = Pass OR auto-recovered CATALOG_STALE failures only.
- `1` = Compilation failed OR human-review issues filed.

If the runner exits 1, resolve filed issues before build/test and human
notification. See `docs/ai-led-session-workflow.md` → **Sub-step: UAT
Runner** for the remediation procedure for each class.

---

### Code Style

- Namespaces: `AcmeCoffeeOnline.Web`, `AcmeCoffeeOnline.Api`,
  `AcmeCoffeeOnline.Shared`, and `Dabbler.*` for integrated libraries.
- PascalCase for public members; `_camelCase` for private fields.
- Nullable reference types enabled.
- All event handlers return `Task` and use the `Async` suffix.

### Workflow Expectations

- Before every session, read this file, `docs/planning/lessons-learned.md`,
  and `docs/planning/session-set-authoring-guide.md`. The authoring guide
  is the source of truth for *spec* authoring (slug naming, sizing, the
  Session Set Configuration block, When-UAT/When-E2E heuristics);
  `docs/ai-led-session-workflow.md` remains the source of truth for
  *execution* mechanics (the 10-step procedure, verification, delegation).
- Keep `README.md`, technical docs, user docs, and XML comments current enough
  to describe the validated system rather than historical assumptions.
- Save verifier output raw and never edit session-review files after they are
  written.
- Log every AI-led session step in the active session set.
- Keep auth, browser coverage, and workflow-asset changes aligned with the
  security and auth guidance in this document.
- AI instruction documents (CLAUDE.md, AGENTS.md, GEMINI.md) reference this
  file and `docs/ai-led-session-workflow.md` so future agent runs inherit the
  same durable expectations.
- **Session-state.json is the single source of truth for in-progress
  detection.** Call `register_session_start()` at Step 1 before the first
  `log_step()`, and `mark_session_complete()` at Step 8. External tooling
  (VS Code Session Set Explorer, manager dashboards) flips state from
  this file; do not infer in-progress from activity-log presence.
- **Author `ai-assignment.md` and the next-orchestrator /
  next-session-set recommendations via routed analysis — never
  self-opine.** The orchestrator's own opinion of which model could have
  been cheaper is the precise thing the routed analysis is designed to
  displace.
- **Obey the spec's Session Set Configuration block at runtime.** Rules
  #9, #10, #11 in the workflow doc (pending UAT blocking, one checklist
  per set, E2E before UAT) are conditional on the spec's `requiresUAT`
  and `requiresE2E` flags. Do not re-litigate those flags during a
  session — if a flag is wrong, surface it at the Step 9 reorganization
  review.
- **Token-window budget overrides via the `— maxout <engine>` suffix.**
  When the human appends `— maxout Claude` (or another engine) to a
  trigger phrase, override `ai-assignment.md` recommendations for that
  session and push routing to the named engine's frontier model. The
  one constraint that survives any maxout: `session-verification` is
  always cross-provider — never routed back to the orchestrator's own
  provider.
- When the human gives a session instruction or decision that appears durable
  enough to guide future sessions or future projects, ask whether it should
  be incorporated into this document.
- When a failure reveals a reusable strategy, recommend a corresponding
  update to `docs/planning/lessons-learned.md`.
- Before calling `route(task_type="session-verification")`, include related
  files the verifier might reasonably look for, or disclose them in an
  out-of-scope section of the prompt. Missing context produces false positives
  about "missing" code that actually lives in a sibling file; the verifier
  only sees what the prompt includes.
- Before physically deleting a type whose consumers are referenced by a
  later session or session set, count the consumer files. If a scheduled
  future session already owns the consumer rewrite, mark the type
  `[Obsolete]` instead of deleting it. The compiler warnings surface on
  every consumer so no reference slips through, the build stays green, and
  the deletion lands naturally in the session that rewrites the last
  consumer. Disclose the deviation in the end-of-session verification
  prompt. Applies whenever consumer count and session boundaries would
  otherwise force a single session to own a disproportionate rewrite.
- When the orchestrator model matches the routing target for a given
  task type, handle the task in-conversation rather than calling
  `route()`. A `route()` call into the orchestrator's own model gives
  fresh context (occasionally useful for stochastic independence) but
  also pays the provider's API cost to summon another instance of the
  same judgment. The non-negotiable exception is `session-verification`,
  which MUST go through the router regardless, because the whole point
  is a cross-provider second opinion. Complements the existing
  Delegation Discipline criteria in `docs/ai-led-session-workflow.md`.
- When a session set spans two repos (e.g., a library repo and a
  consuming repo), designate one repo as authoritative for each
  session. The consuming repo's `activity-log.json` carries **mirror
  entries** for sessions executed in the sibling — each entry's
  description prefixed with `[Mirror of sibling <path>]` and pointing
  at the sibling's authoritative log — and copies the sibling's
  `session-NNN.md` review plus `session-NNN-issues.json` into its
  own `session-reviews/` and `issue-logs/` directories for
  traceability. The spec's per-session step list is written once in
  each repo's spec but describes only the work that repo owns;
  cross-repo sessions reference the sibling instead of duplicating.
  Promoted 2026-04-22 from the `pdf-info-metadata-api` set (Session
  set spec trim-and-restore experience, twice-applied in this one
  set: spec trimmed to 2 sessions then restored to 3 with mirror
  entries when the sibling's Session 2 completed).
- Use `route(task_type="summarization")` for narrative prose outputs
  (change logs, session summaries, release notes). Reserve
  `route(task_type="documentation")` for code-doc generation — its
  prompt template wraps content in a "document this code" frame that
  mis-fits free-form prose and produces Gemini 400s. If a
  `summarization` call returns Gemini 400 on a prompt above ~2KB,
  pin `max_tier=1` (forces Gemini Flash, which is more forgiving on
  long prose). Promoted 2026-04-22 from the
  `rearchitect-role-administration-dashboard` Session 8 incident
  (documentation template produced 400s) and the `pdf-info-metadata-api`
  Session 3 mitigation (summarization+gemini-pro 400'd on a 3.5KB
  prompt; `max_tier=1` landed it on Gemini Flash cleanly).
- **Diagnostic instrumentation BEFORE speculative fixes when the failure
  mode is non-obvious.** When a flaky or intermittent failure has a
  non-obvious cause, add observable instrumentation FIRST (browser console
  capture, server logs, structured per-step diagnostics, on-failure dumps)
  and run a single observation pass before changing any code under test.
  Then make exactly ONE targeted change based on what the instrumentation
  reveals. Promoted 2026-04-29 from playwright-sync-watermark-and-playbook
  Sessions 1 and 5 — the same failure (cumulative-flake-investigation.md
  `data-actions-in-flight=null`) was guess-and-patched for hours in Session 1
  by a Claude instance and again at the start of Session 5 by another Claude
  instance until the diagnostic-first discipline was applied. ~30 minutes of
  console-log + dump instrumentation pinpointed the layer; the routed
  second-opinion analysis ($0.0145) used the captured trace to converge on
  the correct fix. The instrumentation cost was small and worth keeping in
  the repo afterward (the JS-shim console logs and the `DumpQuietDiagnosticsAsync`
  helper remain). Applies to any platform behavior that misbehaves under load
  or non-deterministically — observability before iteration.
- **Cross-provider session-verification needs a fallback ladder, not
  same-provider escalation.** When the preferred cross-provider verifier
  fails (HTTP 503, timeout, garbled response) and the only remaining
  capability-matched option is from the orchestrator's own provider, the
  router should prefer (a) a tier-2 cross-provider verifier (e.g., GPT-5.4
  Mini) over (b) a tier-3 same-provider verifier (e.g., Opus when the
  orchestrator is also Anthropic). If neither cross-provider option is
  available, the router should surface "no cross-provider available;
  orchestrator should self-verify" rather than silently same-provider
  escalating. Promoted 2026-04-29 — same issue hit twice in
  `playwright-sync-watermark-and-playbook`: Session 2 actuals flagged that
  session-verification at complexity ~70 lands on tier-3 Opus by default
  (same-provider when orchestrator is Anthropic, $0.24 wasted), and
  Session 5 close hit it again when Gemini Pro 503'd and the router
  escalated to Opus ($1.07 same-provider penalty). Per
  `feedback_self_verify_when_no_cross_provider.md` the human-preferred
  fallback is self-verification when no cross-provider option exists.
  **Note (2026-04-29):** the human is actively working on routing-config
  fixes for this and related issues; the convention captures the desired
  end-state behavior so future orchestrators can plan around it while the
  config-side fix lands.
