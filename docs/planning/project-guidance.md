# Project Guidance — Dabbler Platform

> **Purpose:** The single source of truth for durable strategic commitments
> (Principles) and specific rules and patterns (Conventions) that apply to
> this repo and future projects built from the same platform direction.
> **Primary sources:** `docs/planning/testing-hierarchy.pdf`,
> `docs/planning/phase-3-platform-roadmap.md`

Read this file before every session. Use it as the first-stop reference
before changing architecture, testing strategy, UI patterns, workflow assets,
or human-UAT conventions.

A session's section of the session plan may narrow the scope for a particular
effort, but it should not silently violate anything here without explicit
human approval.

The file has two top-level sections:

- **Principles** — durable strategic commitments (the *why* and *what*). Slow
  to change; every principle should explain the reasoning behind itself.
- **Conventions** — specific rules, patterns, values, and code styles (the
  *how*). Faster to change; often promoted from successful lessons or
  demoted from over-general principles.

When a principle implies a specific rule, put the principle under Principles
and the concrete rule under Conventions. Reorganization candidates —
promotions from Lessons to Conventions/Principles, demotions, relocations —
are proposed to the human, who accepts or rejects each.

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
  source. Do not add sibling-repo coupling unless the session plan explicitly calls for
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
  roles must explicitly switch to operate under each one. Recording
  the active role on transactional writes is deferred: the
  `transactional-system-columns` set was narrowed to temporal tables
  only on 2026-05-14 (GPT-5.4 and Gemini Pro both flagged `RoleId` on
  every business row as a session-attribute that needs a dedicated
  design before committing it to the schema). Follow-on specs
  `org-scoping-global-filter` and `actor-audit-columns` will define
  the correct stamping model.
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
- **Composition over configuration for CRUD page composites.** When a CRUD
  page variant (different routing model, different action buttons, different
  filter sophistication) needs a reusable UI primitive, the right abstraction
  is a small focused helper the page adopts à la carte — not a whole-page
  config-driven composite. A config DSL that re-implements the Blazor renderer
  accepts all variation as enums (impossibly broad) or rejects them (forces
  every page into one rigid shape). The platform's CRUD helpers
  (`EditSession<T>`, `<DirtyAwareActionRow>`, `<DeclarativeFilterPanel>`) are
  the reference: each page wires them independently instead of being generated
  from a `MasterDetailsConfig<TEntity>`. This principle was validated by two
  independent cross-provider reviews (GPT 5.4 and Gemini Pro) and by the
  abandoned `ConfigurableMasterDetails<TEntity>` prototype in the
  `composable-crud-helpers` set; the verdict converged: "the composite picked
  the wrong axis of variation."

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
the feedback loop. The Conventions under *Human UAT And Checklist
Editor* carry the procedural enforcement.

- **Step N in the UAT checklist corresponds to Step N in the E2E test.
  Identical actions. Identical parameters. Identical verifications.
  Not "equivalent" — identical.** Under the DSL-driven pipeline this
  is structural: the checklist is the W0 compiler input, the
  Playwright `.cs` is the compiler output, and a single DSL step
  produces exactly one Playwright step. Drift is a compiler bug, not
  a discipline failure.
- **Parameter values are shared, not parallel.** The persona email,
  entity name, search term, URL, role name, and expected outcome the
  human will act on are the same literal values the Playwright test
  will drive — because the Playwright test was compiled from the same
  DSL checklist. The structured-`Steps` `TestData` object travels
  with the checklist into the compiled output.
- **The compiled test passes before the human is notified.**
  Sequencing matters: author the DSL checklist, compile via W0, run
  the compiled `.cs` with `dotnet test` to a green deterministic pass,
  commit, then notify the human. Notifying against a red run is the
  failure mode this principle exists to prevent.
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
  starting, dispose of every finding — Blocker, Substantive, Minor or
  Out of scope — in a session of its own that ends with the next
  action queued. Skipping it is how UAT findings sit unaddressed for
  weeks or get folded opportunistically into unrelated work, losing
  the human's signal.
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
  the UAT signal loses fidelity. Before authoring a checklist, walk through
  the flow as the user will: what do they see, in what order, with
  what labels? Write the checklist from that perspective; wire the
  test to the same visible surface.

### Precedence

When guidance appears in multiple places, apply it in this order:

1. Explicit human direction in the current conversation
2. The session's section of the session plan
3. This document
4. Supporting planning notes and roadmaps

If those sources conflict in a meaningful way, stop and confirm with the human
before proceeding.

---

## Conventions

### Composite Shell Components

- Prefer a single generic component with capability parameters over multiple
  mode-specific variants. `MasterDetailExpansionShell<TRow, TEntity, TLink>`
  is the reference: three independent boolean knobs (`ShowGrid`, `ShowLinkData`,
  `TargetEditability`) cover Add-Only, Search+Update, Pick-or-Add, and M:N-with-
  payload from one component rather than four variants. A wrapper dialog
  (`PickOrAddDialog`) is the correct composition unit for modal invocation.
  Apply this pattern when a new composite UI control needs to serve more than
  one workflow shape — name the capability parameters, not the mode.
- When a composite form hosts multiple EF entities (e.g., target entity + join-
  table payload), use separate dirty-state trackers per section (`_entityDirty`,
  `_linkDirty`) rather than a single flag. The action label and save handler
  read `AnyDirty = _entityDirty || _linkDirty`; each EF write is gated on its
  own flag. This pattern was first used in `MasterDetailExpansionShell` Session 4.
- `MudBlazor 8.x` places `data-testid` attributes directly on the `<input>`
  element inside `MudTextField`, not on the wrapper div. Playwright locators
  that use `GetByTestId("foo")` already resolve to the `<input>`; calling
  `.Locator("input")` on the result looks for a child input and returns nothing.
  Use `ToHaveValueAsync` (not `ToContainTextAsync`) when asserting input values.
  Also: `MudTextField ReadOnly="true"` in MudBlazor 8.x does not set the HTML
  `readonly` attribute. Use toggle-button text (e.g., "Edit record" vs "Lock
  record") as the Playwright proxy for read-only state; `Not.ToBeEditableAsync`
  produces false failures.

### CRUD Form Conventions

These rules apply to every Blazor form managed by an `EditSession<T>` and a
`<DirtyAwareActionRow>`. They are the codified form of the composable-crud-helpers
set's form-component conventions, validated across Products, Orders, and
Customers detail pages.

- **Form fields bind with `Value`/`ValueChanged`, never `@bind-Value`.** Two-way
  `@bind-Value` bypasses `EditSession<T>`'s dirty tracking because it does not
  call `NotifyChanged()`. Every field must use the explicit form:
  `Value="@_record.Field" ValueChanged="@((T v) => { _record.Field = v; _session.NotifyChanged(); })"`.
- **`<DirtyAwareActionRow>` owns the Save/Cancel/Delete action row.** Pages must
  not add their own Save, Cancel, or Delete buttons outside the action row. Page-
  specific affordances (Undo, Toggle Availability, Status workflow buttons) go in
  the `CustomActions` slot; the row's primary buttons remain Save, Cancel-or-Close,
  and Delete only.
- Required form fields get `Required="true"` + `RequiredError`.
- Layout uses `MudGrid` + `MudItem` with project breakpoints (`xs="12" md="6"`).
- On a successful server save, call `_session.CommitFrom(updated)` (not just
  `Commit()`). `CommitFrom` updates the snapshot baseline so a subsequent Undo
  restores to the last saved value, not the original construction-time value.
- Use `EditSession<T>` for any detail page that maintains a dirty snapshot. Flat
  DTOs with public read/write scalar/enum/string/nullable properties are the
  supported shape; get-only collections and EF navigation properties are
  out of scope — use a dedicated compare-and-copy strategy for those.
- Nested grid row forms (`MasterDetailExpansionShell` `DetailsActions` slot) also
  render `<DirtyAwareActionRow>` per row form. The parent order's action row
  is the sole server commit; the per-row form mutates the parent's in-memory
  collection only.

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
- When a session plan names a specific MudBlazor widget but the semantics it
  describes don't fit that widget, choose the widget that honors the semantics
  rather than forcing a mismatched control. `MudSwitch` is natively binary in
  MudBlazor 8.x — use `MudToggleGroup<T>` with
  `SelectionMode.SingleSelection` for tri-state or multi-state selection
  (e.g., `ColumnRestriction` ViewOnly|Hidden, `UiRestriction` Disabled|Hidden,
  Allow|Deny|Unset). Call out the deviation explicitly in the step's report
  notes so the reviewer sees the reasoning.

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
  — migration scope for that library is deferred until the session plan explicitly adds
  the dependency.
- **Playwright tests that create unique-key records (users, emails, etc.) must
  use a static run-unique suffix to avoid collisions across repeated
  `dotnet test` invocations.** For `dotnet test` and CI runs without a
  container reset, embed a static
  `RunId = Guid.NewGuid().ToString("N")[..8]` field in the test class and
  append it to any email or name that must be unique in the DB (e.g.,
  `$"e2e-newuser-{RunId}@test.local"`). Promoted 2026-05-11 from
  `admin-user-creation-flow` Session 3, where UAT-1 and UAT-4 Playwright tests
  failed on the second run because prior-run emails already existed in the
  database.
- **Testing MudBlazor dialog components in bUnit: store the
  `IRenderedComponent<MudDialogProvider>` ref and use it as the search root.**
  `TestContext.Find()`, `FindComponent()`, `InvokeAsync()`, and `WaitForState()`
  are extension methods on `IRenderedFragment`, not on `TestContext`. MudBlazor
  renders dialog content inside `MudDialogProvider`, not inside the `MudDialog`
  component itself. The correct pattern: store the provider ref from
  `ctx.RenderComponent<MudDialogProvider>()` during test setup, then after
  calling `IDialogService.ShowAsync`, search for dialog elements via
  `provider.Find(...)`, `provider.FindComponent<T>()`, etc. Tests that call
  `ctx.Find(...)` directly will fail with a compile error. Pattern established
  in `admin-user-creation-flow` Session 3
  (`CreateBackofficeUserDialogTests.cs`). Promoted 2026-05-11.

### Human UAT And Checklist Editor

- **Catalog-grounded authoring is mandatory for new UAT checklists.** Every
  user-visible label, caption, text, tab name, scope descriptor, and column
  header in a new UAT checklist must come from the component catalog at
  `docs/component-catalog/index.json` for the route × role active in that
  step. The catalog is produced by `tools/ComponentCatalogCrawler/` (a .NET
  console app that crawls the running compose stack as each role and
  scrapes accessibility-tree labels). Re-crawl on demand before authoring a
  new checklist; commit the diff so UI changes are visible in PR review.
  Select every label verbatim from the catalog; inventing one is a defect.
  If a needed label is missing from the catalog, re-crawl it or mark the
  affected item `IsJudgmentItem: true` — never fabricate a label that the W0
  compiler will faithfully turn into broken Playwright code.

  **W0 toolchain integration.** Validation and compilation are delegated to
  the `dabbler-uat-dsl` CLI (W0) from the sibling repo. The DSL syntax,
  CLI flags, JSON output shapes, exit codes, and the catalog +
  suppressions data contracts are documented in the sibling repo —
  do not restate them here. See:
  - [dabbler-uat-dsl/docs/cli-reference.md](https://github.com/darndestdabbler/dabbler-uat-dsl/blob/main/docs/cli-reference.md) — `validate` / `compile`, every flag (`--catalog`, `--suppressions`, `--active-role`, `--emitter`, `--output`), JSON output shapes, exit codes.
  - [dabbler-uat-dsl/docs/data-contracts.md](https://github.com/darndestdabbler/dabbler-uat-dsl/blob/main/docs/data-contracts.md) — component-catalog schema (1.0 / 1.1 with `isWatermarked`) and suppressions schema.
  - [docs/COMPATIBILITY.md](../COMPATIBILITY.md) — the supported W0 Core / Cli / Emitter / catalog-schema combination.

  Integration rules that live here (not in the sibling docs):
  - **Always invoke `validate` and `compile` with
    `--suppressions tools/uat-suppressions.json` and
    `--active-role <name>`.** The active-role flag is what makes
    post-auth label probes hit the concrete-role catalog slice rather
    than the AnyRole `*` wildcard (which silently returns zero hits and
    surfaces as cascading `UnknownRoute` on the route the script just
    navigated to). Closed pre-catalog checklists are grandfathered via
    `tools/uat-suppressions.json`.
- **Click-driven navigation does not advance W0's route cursor.** Until
  W0 ships route-transition inference (or a `VERIFY URL matches "..."`
  route-cursor sigil), click-driven UAT scripts produce cascading
  `UnknownRoute` errors after the first click-driven transition.
  Mitigations are recorded per-checklist in `tools/uat-suppressions.json`
  with a `rationale` per entry; the underlying limitation and the
  glob-match rules are documented at
  [dabbler-uat-dsl/docs/cli-reference.md → Known limitations](https://github.com/darndestdabbler/dabbler-uat-dsl/blob/main/docs/cli-reference.md#known-limitations)
  and
  [dabbler-uat-dsl/docs/data-contracts.md → Suppressions](https://github.com/darndestdabbler/dabbler-uat-dsl/blob/main/docs/data-contracts.md#suppressions).
- Use the UAT checklist editor schema from
  `https://github.com/darndestdabbler/uat-checklist-editor/blob/main/checklist-schema.json`
  for checklist JSON files intended for human execution.
- Open and run those files through
  `https://darndestdabbler.github.io/uat-checklist-editor/`.
- **MANDATORY: each `Review` item is a self-contained, runnable user
  journey starting from a defined precondition (typically a fresh
  sign-in via `/login`).** Concretely:
  - **No item may consist solely of `VERIFY` steps.** Every Review
    item must include the human-action sequence (`NAVIGATE`, `CLICK`,
    `OPEN DROPDOWN`, `CLICK INPUT … and enter …`, etc.) that drives
    the UI to the state being verified. A reader who jumps into any
    single Review item — not just the first — must be able to execute
    the whole thing without remembering state from prior items.
  - **Include the full sign-in flow in `Steps[]` when authentication
    is part of the user-observable journey.** The masking-audit-uat-
    remediation set's items 1–12 are the canonical pattern: `NAVIGATE
    TO "/login"` → `CLICK INPUT "Email address" and enter "…"` →
    `CLICK BUTTON "Send sign-in link"` → `VERIFY TEXT "We sent a
    sign-in code to"` → `NAVIGATE TO "/dev/email-inbox"` → `CLICK
    LINK with text "Open"` → continue into the post-login scenario.
    Repeat this preamble in every item that depends on sign-in. The
    cost is duplication; the benefit is items that read top-to-
    bottom as the human will actually run them.
  - **Use `Feedback` (Review-level, free-text) — not `HumanAction`
    or `Expectation` — for human-facing prose.** W0 parses
    `HumanAction` and `Expectation` as DSL and rejects free prose
    there with cascading grammar errors. Put persona, ItemLabel
    rationale, judgment-item notes, and any "what to look for" prose
    in `Feedback` so the editor renders it for the reviewer without
    breaking the W0 validator.
  - **Anti-pattern (do not repeat): single-`VERIFY` items.** A
    Review item with one step like `VERIFY TEXT contains "X"` reads
    to the human as "verify this thing somehow" with no instruction
    on how they got to a page where X is visible. These items break
    the "self-contained journey" contract and create the impression
    that UAT is rote inspection rather than guided execution.
    Surfaced as the explicit failure mode in 2026-05-11 (admin-
    users-cross-links Session 1 closeout) — the first-draft
    checklist had four single-`VERIFY` items and was rejected by
    the human reviewer.
- **Emitted test wrapper conflict with DSL-driven login.**
  When a checklist's `Steps[]` start with `NAVIGATE TO "/login"`, the
  W0-emitted Playwright test wrapper's auto-inserted
  `SeededSignInAsync(persona)` collides with the DSL-driven login:
  the persona is already signed in before the DSL navigates to
  `/login`, so the login form is missing and the DSL's `CLICK INPUT
  "Email address"` step times out. Until W0 adds an
  opt-out (e.g., `--no-pre-sign-in` or auto-detect-DSL-login),
  consumer-side options for UAT that needs human-equivalent login
  are:
  - (a) Author the human-readable checklist with full sign-in flow
    in `Steps[]` (the right answer for the human), accept that the
    compiled-test pass is gated on the W0 enhancement, and
    rely on the bUnit / hand-authored Playwright coverage for the
    automated correctness proof in the meantime.
  - (b) Use SeededSignInAsync (test setup, out-of-scope of the UAT)
    and start the DSL after login, accepting weaker human-facing
    instructions.
  - (c) Hand-author the Playwright test (masking-audit pattern) so
    the test exercises the full magic-link flow in `Steps[]` and
    the auto-emitted wrapper's SeededSignInAsync is only used for
    setup state mutations (not for the human-equivalent steps).
  Prefer (a) for new checklists; record the compiled-test status in
  the session's notes and surface the W0 gap upstream.
- **MANDATORY: every UAT step must be performable by the human using
  only user-visible surfaces, and the matching Playwright step is the
  W0-compiled output of that DSL step.** Under the DSL-driven
  pipeline, the three-step authoring discipline that previously
  demanded hand-authoring the checklist, hand-authoring the E2E test,
  and cross-verifying parity is now structural: the checklist is
  authored in DSL, W0 compiles it into the Playwright `.cs` test, and
  the compiled test's green run is the parity proof. Test-framework backdoors
  (`SeededSignInAsync(roles=)` for sign-in steps, `/test-auth/*`
  shortcuts for navigation, direct `security-config.json` edits for
  permission-grant steps, EF API pokes for things the user performs
  through the UI) remain forbidden for any step that the human is
  also expected to perform; they are acceptable only for test setup
  of state that is out-of-scope of the UAT.

  When a session has UAT but the supported sign-in path or any
  other user-visible surface is not yet wired up, BLOCK the UAT until
  that surface works. Surface this when the session plan is written or
  at the start of the session, not after generating an unrunnable
  checklist.

- **The UAT/E2E DSL — sibling-repo references.** Every step in a UAT
  checklist must express its action using a verb from the closed DSL
  (the W0 compiler reads the DSL and emits the matching Playwright
  step; closed sets that retain hand-authored
  `// begin STEP NNN` blocks expressed the same DSL inline). The
  verb list, structured `VERIFY` forms, scope (`in <scope>`) syntax,
  the quoted-string requirement, the locator-discipline rules (no
  `data-testid` / xpath / DOM-index, row identification by
  column-value match, no composites), and the closed-list extension
  policy live in the sibling repo:
  - [dabbler-uat-dsl/docs/language-reference.md](https://github.com/darndestdabbler/dabbler-uat-dsl/blob/main/docs/language-reference.md) — verb list, structured `VERIFY` forms, scopes, locator rules.
  - [dabbler-uat-dsl/grammar/DabblerUatDsl.g4](https://github.com/darndestdabbler/dabbler-uat-dsl/blob/main/grammar/DabblerUatDsl.g4) — canonical ANTLR4 grammar (the parser is authoritative when this and the prose disagree).

  Do not restate the verb list, structured forms, or design rules
  here. The integration rules that DO belong in dabbler-platform are:

  **Watermark pairing rule.** `WAIT FOR ACTIONS QUIET` is **required
  after every interaction verb on routes whose catalog entry has
  `isWatermarked: true`** (the page component inherits from
  `Dabbler.Testing.Blazor.Components.TrackedComponentBase`; see the
  Testing Conventions entry on `TrackedComponentBase`). Do NOT emit
  after pure navigation, pure verification, or on routes whose catalog
  entry has `isWatermarked: false`. Checklist authors must follow
  this rule by hand. The watermark mechanism, the
  `TrackedComponentBase` base class, and `WaitForQuietAsync` itself are
  application-side instrumentation that lives in
  `src/Libs/Dabbler.Testing.Blazor/`; the `isWatermarked` flag is
  produced by `tools/ComponentCatalogCrawler/` per route × role.

  **Verb-list extensions go through dabbler-uat-dsl, not here.** When a
  new verb is needed, propose the change in the sibling repo per its
  closed-list extension policy ([language-reference.md → Closed-list
  extension policy](https://github.com/darndestdabbler/dabbler-uat-dsl/blob/main/docs/language-reference.md#closed-list-extension-policy));
  the consumer-side adoption (catalog crawler updates, smoke proof)
  lands as its own dabbler-platform session.

  **Catalog classifier — visible text wins, aria-label is the icon-only
  fallback.** `tools/ComponentCatalogCrawler/AccessibilityTreeScraper.cs`
  records each control's `userVisibleLabel` from the rendered
  `innerText` first (what the human reads) and only falls back to
  `aria-labelledby` / `<label for=>` / `aria-label` when no visible
  text is present. When a control has both visible text and an
  aria-label that differs (e.g., button "Save" with `aria-label="Save
  changes to record"`), the aria-label is stored separately in the
  `ariaLabel` field but **does not** become the locator key. Human UAT
  steps must match what the human sees on screen, so locator probes
  resolve `(role, userVisibleLabel)`. Icon-only controls (no visible
  text) are reclassified with the aria-label as `userVisibleLabel`
  and `elementRole=IconButton`. Do not change this policy without
  reviewing the `sensitive-field-masking-audit-uat-remediation`
  Session 1 rationale — the prior aria-label-first behavior caused
  `GetByRole(Name=...)` to silently match the ARIA accessible name
  (which prefers aria-label) instead of the user-visible caption,
  producing checklists the human could not run.

- **UAT↔E2E pairing under the DSL pipeline.** New checklists do not
  get a separate `*UatCoverageTests.cs` paired to the checklist by
  `// begin STEP NNN` comments. The DSL checklist is the single
  source: W0 compiles it directly into the Playwright `.cs` test, so
  parity is structural, and the compiled test's green run is the
  coverage proof.

  *Legacy pattern (closed work only):* checklists closed before
  2026-04-30 used a hand-authored `*UatCoverageTests.cs` whose
  methods were named by `E2ETestReference` and whose bodies were
  delimited by zero-padded `// begin STEP NNN` comments paired
  1:1 to the checklist's `Steps[].StepNumber`. Those files
  remain in place unchanged; do not retroactively rewrite them.

- **MANDATORY: validate the generated checklist against the canonical schema
  before committing.** The schema lives at
  `https://raw.githubusercontent.com/darndestdabbler/uat-checklist-editor/main/checklist-schema.json`
  — fetch it, run a JSON-Schema validation pass against the checklist file,
  and fix any validation errors before the human is notified.
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
    schema. Under the DSL pipeline the checklist is its own source of
    truth (W0 compiles it directly into the `.cs` test); the
    `E2ETestReference` field is optional and may name the
    W0-compiled method when a backwards-compatible cross-reference is
    desired. A judgment-item rationale belongs in `Feedback` (e.g.,
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
- **Generate a new UAT checklist for each session that needs one.** Do not
  re-use or re-run a checklist from an earlier session — each should have
  its own inline result record so audit history stays scoped and
  unambiguous.
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
- **Compiled-test proof per UAT item is mandatory.** Before
  committing a checklist to the repo and notifying the human, the
  W0-compiled Playwright test (output of
  `dabbler-uat-dsl compile`) must run green with `dotnet test`. That
  green run is the coverage proof for every functional `Review` item.
  Items flagged `IsJudgmentItem: true` with a one-sentence
  justification still require sequence-reachability — the compiled
  test must drive the UI to the state the human will judge.
  Checklists whose compiled test does not pass are a session-closeout
  defect — re-run the pipeline before the session closes.

  **W0 applies to Blazor web app UI checklists only.** The W0
  DSL grammar covers Blazor web app interactions (NAVIGATE TO, CLICK
  BUTTON, VERIFY TEXT, etc.) and the component catalog covers web
  app routes × roles. For sessions whose UAT items cover
  **developer experience only** — documentation quality, CLI tooling,
  NuGet package metadata, VS template UX — the W0 compiler has
  nothing to compile. These
  items should be authored as `IsJudgmentItem: true` with no DSL
  `Steps[]`. Automated reachability proof comes from prior sessions'
  tests (e.g., consumer smoke tests, SkippableFact Playwright tests).
  Record the W0 non-applicability as an explicit scope deviation in
  the session's notes. This exception does NOT apply to a mixed
  checklist that includes both web app UI items and developer-experience
  items — those items must be separated, with UI items getting W0
  coverage and devex items getting the judgment-item treatment.
  Established in `packaging-and-template-readiness` Session 11
  (2026-05-15).

  **The compiled test is a build artifact.** The Playwright `.cs` is
  the output of `dabbler-uat-dsl compile`; do not hand-author it. The
  hand-authored `*UatCoverageTests.cs` pattern survives only in closed
  work.

---

### Code Style

- Namespaces: `AcmeCoffeeOnline.Web`, `AcmeCoffeeOnline.Api`,
  `AcmeCoffeeOnline.Shared`, and `Dabbler.*` for integrated libraries.
- PascalCase for public members; `_camelCase` for private fields.
- Nullable reference types enabled.
- All event handlers return `Task` and use the `Async` suffix.

### Workflow Expectations

- Keep `README.md`, technical docs, user docs, and XML comments current enough
  to describe the validated system rather than historical assumptions.
- Keep auth, browser coverage, and workflow-asset changes aligned with the
  security and auth guidance in this document.
- AI instruction documents (AGENTS.md, and CLAUDE.md through it) reference
  this file so future agent runs inherit the same durable expectations.
- When the human gives a session instruction or decision that appears durable
  enough to guide future sessions or future projects, ask whether it should
  be incorporated into this document.
- When a failure reveals a reusable strategy, recommend a corresponding
  update to `docs/planning/lessons-learned.md`.
- Before physically deleting a type whose consumers are referenced by a
  later session, count the consumer files. If a scheduled
  future session already owns the consumer rewrite, mark the type
  `[Obsolete]` instead of deleting it. The compiler warnings surface on
  every consumer so no reference slips through, the build stays green, and
  the deletion lands naturally in the session that rewrites the last
  consumer. Disclose the deviation in the step's report notes. Applies
  whenever consumer count and session boundaries would
  otherwise force a single session to own a disproportionate rewrite.
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
  console-log + dump instrumentation pinpointed the layer; a cross-provider
  second-opinion analysis ($0.0145) used the captured trace to converge on
  the correct fix. The instrumentation cost was small and worth keeping in
  the repo afterward (the JS-shim console logs and the `DumpQuietDiagnosticsAsync`
  helper remain). Applies to any platform behavior that misbehaves under load
  or non-deterministically — observability before iteration.
- **Ship a checkout-free consumer-acquisition smoke gate in the same
  session that publishes or changes a distributable artifact.** When a
  session makes a `Dabbler.*` artifact externally consumable (a `dotnet
  new` template, a `dotnet tool`, a NuGet library) or changes how one is
  acquired, prove it from a real consumer's position **in the same
  session**: acquire the artifact exactly the way an external consumer
  would — template install into an isolated `--debug:custom-hive`; `dotnet
  tool install --tool-path <dir> --add-source <feed> --version <pinned>`;
  package restore from the local pack feed — then generate/build against a
  consumer fixture and assert it **builds green**. A pack→install→use→build
  smoke cycle catches acquisition-time and render-time breakage (missing
  packaged files, wrong `ToolCommandName`, version skew between paired
  artifacts, un-bundled project-reference dependencies) that unit tests on
  the source project structurally cannot see — they exercise the source
  tree, not the published artifact. Promoted 2026-06-20 from `packaging-and-template-readiness` Sessions 8
  & 9 (template content authored without a consumer smoke; 7 quality issues
  found late) and `access-migration-generator-consumption` Session 2 (the
  `Dabbler.CrudSlice.Tool` consumer-validation gate acquired the packaged
  tool checkout-free via `--tool-path`/`--add-source` and built a generated
  CRUD slice green in the same session). Generalizes the former
  template-only lesson to any distributable artifact.
