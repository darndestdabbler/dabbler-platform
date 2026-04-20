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

- Human UAT is where confidence and judgment live. It is not where basic
  operability defects should first be discovered.
- Pending human UAT blocks downstream sessions unless the human explicitly
  overrides the pause.
- Capture screenshot evidence when practical so the human can see styling and
  interaction changes evolve across sessions.

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

### Human UAT And Checklist Editor

- Use the UAT checklist editor schema from
  `https://github.com/darndestdabbler/uat-checklist-editor/blob/main/checklist-schema.json`
  for checklist JSON files intended for human execution.
- Open and run those files through
  `https://darndestdabbler.github.io/uat-checklist-editor/`.
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

### Code Style

- Namespaces: `AcmeCoffeeOnline.Web`, `AcmeCoffeeOnline.Api`,
  `AcmeCoffeeOnline.Shared`, and `Dabbler.*` for integrated libraries.
- PascalCase for public members; `_camelCase` for private fields.
- Nullable reference types enabled.
- All event handlers return `Task` and use the `Async` suffix.

### Workflow Expectations

- Before every session, read this file and `docs/planning/lessons-learned.md`.
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
