# Guiding Principles

> **Purpose:** Consolidate the durable architecture, testing, UI, workflow,
> and human-UAT guidance that should carry forward across this repo and future
> projects built from the same platform direction.
> **Primary sources:** `docs/planning/user-functionality-notes.md`,
> `docs/planning/testing-hierarcy.pdf`,
> `docs/planning/phase-2-platform-roadmap.md`,
> `docs/platform-guardrails.md`,
> `docs/dev-test-auth-modes.md`, and
> `docs/planning/reusable-platform-stub-guide.md`

This document is the short, durable guidance layer.

Use it as the first-stop reference before changing architecture, testing
strategy, workflow assets, or human-UAT conventions.

Review this document before every AI-led session.

Session-set specs may narrow the scope for a particular effort, but they should
not silently violate these principles without explicit human approval.

## Architecture Foundations

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

## Security And Organization Model

- Internal users should use SSO through Entra ID or the accepted equivalent.
- External-user auth may use simpler options when appropriate, but it must stay
  explicit and well-documented.
- Treat authorization as a runtime system that governs:
  users-to-roles, API capabilities, UI functionality, and page access.
- Support organization and role switching as a first-class concept:
  `SystemAdmin` can switch freely, while other users can switch only among
  their valid organizations.
- Keep seeded-session and dev-inbox helpers Development or Test only,
  loopback-only, and secret-guarded.

## UI And Reusable Platform Principles

- The web application layer is Blazor Server.
- WCAG 2.1 AA is the default accessibility bar for reusable components and
  application pages.
- The long-term goal is not just to make one demo app work. It is to validate
  reusable components and libraries that can support many future apps while
  keeping the developer experience as simple as possible.
- Use DI seams and reusable provider abstractions instead of one-off test hacks
  whenever the platform already has a stable contract.
- Treat DI-toggled stub behavior as a supported platform feature, not just a
  test trick. It should help:
  bUnit, stub/demo hosts, early design review, and future generated apps.
- Prefer built-in MudBlazor capabilities when they already satisfy the intended
  product and platform need. Custom Dabbler abstractions should earn their
  existence by clearly improving consistency, supported capability, testability,
  or developer experience.
- Do not build and maintain custom wrapper surfaces that merely duplicate
  MudBlazor behavior without adding durable platform value.
- Keep grids, forms, reports, and navigation aligned with the durable intent in
  `user-functionality-notes.md`:
  filter-first search experiences, detail forms with validation and tab order,
  report output beyond flat tables, and clear top/left navigation patterns.
- The demo app should be presentation-worthy and broad enough to prove the
  platform without turning into product-specific scaffolding.
- Standardize spacing and layout rhythm through reusable tokens, conventions, or
  helper abstractions rather than ad hoc per-page margin and padding choices.
- Theme and styling changes should go through reusable, testable guardrails when
  practical so foreground/background combinations, selected states, and dynamic
  color changes remain readable and accessible.
- Use explicit page composition rather than metadata-driven UI generation. Each
  page should declare its own MudDataGrid columns, MudForm fields, and layout.
  This makes pages easier to read, debug, test, and customize.
- Use `MudDataGrid<T>` directly with `ServerData` callbacks, column definitions,
  sorting, and paging. Do not wrap it in a custom component unless a specific,
  justified need arises.
- Apply `Bordered="true"` to all MudDataGrid instances for visual column
  separation. Right-align currency and numeric columns with
  `CellStyle="text-align:right; max-width:130px"` and a matching HeaderStyle.
  Right-align date columns using the same pattern. The max-width keeps values
  close to their headers in flexible layouts.
- Use `MudForm` directly for detail pages with MudBlazor input components and
  built-in validation. Child entity grids belong in `MudTabs`. Popup editing
  uses `MudDialog` with `MudForm` content.
- Use `MudLayout`, `MudAppBar`, `MudDrawer`, `MudNavMenu`, and `MudMainContent`
  for the application shell. Navigation is route-based with clean URLs, browser
  history support, and standard Blazor routing.
- The QBE filter model (AND/OR groups, operators, presets, expression engine,
  filter summary) is genuine platform value and should be preserved. The filter
  UI should be built from MudBlazor input components via `FilterPanel<T>`, not
  from custom wrapper components.

## Testing Hierarchy

Testing should follow this order of preference:

1. Strict unit tests
2. Integration tests
3. bUnit tests
4. True end-to-end tests
5. Human UAT through the checklist editor

### Strict Unit Tests First

- Use strict unit tests for any logic that can be isolated from I/O, runtime
  infrastructure, or browser concerns.
- Structure code so the unit-testable portions are factored into methods,
  services, or helpers that can be exercised without integration setup.
- Do not keep mixed-responsibility methods intact when the pure-logic portion
  can be split out and tested directly.

### Integration Tests Second

- When strict unit tests are not possible, prefer integration tests for
  server-side or service-boundary behavior.
- Integration tests should cover API behavior, persistence, security
  boundaries, and non-UI seams that still need realistic infrastructure.
- They should not become a substitute for unit tests when logic could have been
  isolated earlier.

### bUnit For UI-Seam Proof

- For UI-related work where integration tests are inadequate, use bUnit before
  escalating to browser-level testing.
- Structure UI code so rendering, validation, visible-state decisions, and
  component interactions can be isolated behind bUnit-testable seams.
- DI-toggled stub behavior is encouraged when it makes bUnit tests simpler,
  cleaner, and more representative of the supported platform shape.
- bUnit should carry as much UI verification load as is realistically possible
  before resorting to browser automation.
- Reusable UI components must have interaction-contract coverage for the
  behaviors they expose: click handling, keyboard handling, disabled states,
  validation timing, selection state, and visible feedback.
- Do not treat markup-presence tests as sufficient proof for interactive
  components. If a button, chip, toggle, menu, calendar icon, or similar
  control exists for user interaction, automated tests should exercise the
  interaction itself.

### True E2E For Real Browser And Data Proof

- Use true end-to-end tests when the lower layers cannot adequately prove the
  behavior, and to build confidence in meaningful user workflows.
- True E2E means more than route reachability. It includes:
  input validation behavior, tab order and keyboard movement between controls,
  navigation with and without route parameters, common user action sequences,
  and the ultimate effect of important workflows in the database.
- Representative E2E coverage is acceptable only when lower layers are already
  strong. E2E should not become the only proof layer.
- Docker-backed execution remains the preferred baseline for true E2E.
- When practical during UI and E2E work, provide screenshots to the human so
  they can see visual state and interaction progress instead of relying only on
  prose summaries.
- E2E tests must use the same user entry points that a human uses when the goal
  is to validate navigation or operability. Do not bypass the control under
  test with direct route navigation and then treat that as proof that the
  navigation control works.
- For supported user workflows, automation should prove not only persistence
  outcomes but also visible UI outcomes: route changes when expected, active
  state changes, refreshed grid contents after add or delete, snackbar or
  banner feedback, denial states, and keyboard-operable control flow.
- Human UAT should not be the first line of discovery for basic broken
  controls. Buttons, menus, toggles, date pickers, pills, and other primary
  interactions should already be covered by automated tests before human UAT
  begins.

### Styling And Accessibility Must Be Tested Too

- Styling defects that materially affect usability or readability should be
  caught before human UAT whenever practical.
- Reusable components and key workflow pages should have automated checks for
  visible focus, selected-state contrast, readable text against backgrounds,
  layout overlap, and other WCAG 2.1 AA-adjacent presentation risks.
- Use a combination of component tests, browser-based assertions, accessibility
  tooling, and screenshot or visual-regression evidence for high-value screens.
- If a visual treatment is important enough to carry user meaning, it is
  important enough to validate automatically.
- Styling standards should be encoded centrally where practical: shared spacing
  rules, shared color tokens, and reusable contrast-safe styling helpers are
  preferred over one-off fixes at individual call sites.

### Human UAT Last

- Human UAT is a confidence and judgment layer, not a replacement for the
  automated hierarchy above.
- Use it for clarity, coherence, trust, visual quality, and other judgments
  that automation cannot score well.
- For UI-facing work, capture screenshot evidence when practical so the human
  can see styling and interaction changes evolve across sessions.
- Keep human UAT focused on trust, clarity, and workflow feel after automation
  has already proved basic operability. If human UAT starts finding numerous
  broken controls, the automation strategy needs correction before more human
  testing continues.

## Human UAT And Checklist Editor

- Use the UAT checklist editor schema from
  `https://github.com/darndestdabbler/uat-checklist-editor/blob/main/checklist-schema.json`
  for checklist JSON files intended for human execution.
- Open and run those files through
  `https://darndestdabbler.github.io/uat-checklist-editor/`.
- Save human review results inline in the checklist JSON itself. Do not create
  a separate empty `*-findings.json` scaffold for pending review.
- After the human completes the checklist, extract findings or summaries from
  the revised checklist JSON only when a downstream artifact is useful.
- When a session creates or refreshes a human-UAT checklist and that review has
  not yet been completed, treat that pending human step as a blocker for new
  sessions unless the human explicitly overrides it.
- Handoff messages for pending UAT must be unmistakable:
  name the checklist path, link the editor, say that the agent is waiting for
  the human response, and keep any optional parallel work low-risk.

## Documentation And Workflow Expectations

- Before every session, read this file and
  `docs/planning/lessons-learned.md`.
- Keep `README.md`, technical docs, user docs, and XML comments current enough
  to describe the validated system rather than historical assumptions.
- Save verifier output raw and never edit session-review files after they are
  written.
- Log every AI-led session step in the active session set.
- Keep auth, browser coverage, and workflow-asset changes aligned with
  `docs/platform-guardrails.md` and `docs/dev-test-auth-modes.md`.
- AI instruction documents should reference this file directly so future agent
  runs inherit the same durable expectations.
- When the human gives a session instruction or decision that appears durable
  enough to guide future sessions or future projects, ask whether it should be
  incorporated into this document.
- When a failure reveals a reusable strategy, recommend a corresponding update
  to `docs/planning/lessons-learned.md`.

## Precedence

When guidance appears in multiple places, apply it in this order:

1. Explicit human direction in the current conversation
2. Session-specific spec requirements
3. This guiding-principles document
4. Supporting planning notes and roadmaps

If those sources conflict in a meaningful way, stop and confirm with the
human before proceeding.
