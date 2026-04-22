# Lessons Learned

> **Purpose:** Capture durable tactics, failure patterns, and workflow lessons
> that should influence future sessions before the same mistake repeats.
> **Status:** Start small in one file. If this grows too large, split it into a
> `lessons-learned/` folder with a short index and topic files.

Review this file before every AI-led session.

Unlike `project-guidance.md`, which captures durable principles and
conventions, this file is for practical lessons learned from actual execution
in this repo. A lesson that has been applied as the right call in at least
two different contexts is a promotion candidate for Conventions (or, rarely,
Principles) in `project-guidance.md`. See `docs/ai-led-session-workflow.md`
Step 9 (run after the Step 8 notify) for the end-of-session-set reorganization review.

## How To Use This File

- Read it before starting a new session.
- Add entries when a failure, surprise, or avoidable friction reveals a better
  repeatable strategy.
- Prefer short, action-oriented lessons over long retrospectives.
- When a lesson has proven itself in two or more different contexts, propose
  promoting it to `project-guidance.md` — as a Convention if it is a specific
  rule, or a Principle if it is durable strategy. Never delete a lesson; only
  move it.

## Entry Template

Use this structure for new entries:

```markdown
## <Short lesson title>

- **Context:** What kind of work exposed the issue
- **Failure or friction:** What went wrong
- **Lesson:** The durable takeaway
- **Action for future sessions:** What the AI orchestrator should do next time
```

## Current Lessons

## UAT Checklist Files Must Match The Editor Contract

- **Collapsed to reference on 2026-04-21** (Step 9 reorganization review
  for the `security-dashboard-uat-remediation-review` session set). The
  same guidance is already captured — with equivalent specificity — in
  `project-guidance.md` → Conventions → Human UAT And Checklist Editor:
  *"Use the UAT checklist editor schema from
  https://github.com/darndestdabbler/uat-checklist-editor/blob/main/checklist-schema.json
  for checklist JSON files intended for human execution."* Applied as the
  right call across `post-rebuild-uat`, `security-dashboard-uat`,
  `security-dashboard-polish`, `rearchitect-role-administration-dashboard`,
  `security-dashboard-uat-remediation`, and the UAT Review Session that
  consumed the filled checklist inline. Follow the convention.
- **Original entry preserved below for context.**
- **Context:** Human-executed UAT checklist preparation
- **Failure or friction:** A custom checklist JSON shape was created even
  though the intended reviewer workflow depended on the checklist editor.
- **Lesson:** UAT checklist files should use the checklist editor schema from
  the start so the human can open, execute, and save results inline without a
  conversion pass later.
- **Action for future sessions:** When preparing human UAT assets, generate the
  JSON directly against the checklist-editor schema and point the human to the
  editor URL in the handoff message.

## Pending Human UAT Must Be Called Out Explicitly

- **Collapsed to reference on 2026-04-21** (Step 9 reorganization review
  for the `security-dashboard-uat-remediation-review` session set). The
  same guidance is already captured — with equivalent specificity — in
  `project-guidance.md` → Conventions → Human UAT And Checklist Editor:
  *"Handoff messages for pending UAT must be unmistakable: name the
  checklist path, link the editor, say that the agent is waiting for
  the human response, and keep any optional parallel work low-risk."*
  Applied as the right call in every session-set closeout that has
  generated a UAT checklist, and in this UAT Review Session's
  `disposition.md` + stop message (which names the follow-up set
  path and queues the next action instead of leaving the human holding
  the checklist). Follow the convention.
- **Original entry preserved below for context.**
- **Context:** Sessions that prepare a UAT checklist for later human review
- **Failure or friction:** A checklist can exist in the repo without an
  unmistakable handoff, making it easy for the human to miss that the next move
  belongs to them.
- **Lesson:** When UAT is waiting on the human, the handoff must stand out and
  clearly say that progress is paused pending human review unless the human
  overrides it.
- **Action for future sessions:** In the closeout message, name the checklist
  path, link the checklist editor, state that the AI is waiting for the human,
  and offer only low-risk optional side work.

## Do Not Run Build And Test In Parallel Against The Same Worktree

- **Context:** Full-solution validation runs in this repo on Windows
- **Failure or friction:** Running `dotnet build` and `dotnet test` in parallel
  caused file-lock contention and obscured the real validation result.
- **Lesson:** In this repo, build and test should be run sequentially unless
  there is a specific reason to parallelize isolated targets safely.
- **Action for future sessions:** Prefer sequential validation, and if lock
  contention appears, use `dotnet build-server shutdown` plus conservative
  build settings before retrying.

## Share Visual Evidence During UI And E2E Work When Practical

- **Context:** UI and browser-driven sessions
- **Failure or friction:** Pure prose updates make it harder for the human to
  track visual progress or evaluate direction during UI changes and E2E work.
- **Lesson:** Screenshots in the chat are valuable when the overhead is
  reasonable.
- **Action for future sessions:** When already exercising browser flows or UI
  states, include screenshots at meaningful checkpoints when practical.

## Human UAT Must Not Be The First Discovery Point For Broken Controls

- **Context:** Full-application UAT uncovered many controls that did not behave
  as a human reviewer expected, including navigation items, switchers, filter
  controls, and list-refresh behavior.
- **Failure or friction:** Automation had proved some backend outcomes and
  route reachability, but it had not comprehensively proved that the same
  interactive controls worked through the real user path.
- **Lesson:** Basic operability defects should be caught before UAT by a mix of
  unit tests, bUnit tests, integration tests, and browser automation that
  explicitly exercises the real controls.
- **Action for future sessions:** Before asking the human to run UAT on a UI
  workflow, ensure that the supported buttons, menus, toggles, pills, date
  pickers, and mutation-refresh behaviors already have automated coverage.

## Do Not Treat Direct Route Navigation As Proof That UI Navigation Works

- **Context:** Browser automation for workflow coverage
- **Failure or friction:** Tests used direct navigation to routes that humans
  normally reach by clicking shell controls, which left gaps in proof for the
  actual UI elements.
- **Lesson:** When validating navigation or operability, tests must use the
  same user entry points that a human uses.
- **Action for future sessions:** If the purpose of the test includes proving a
  nav item, switcher, menu, or link, drive that control directly instead of
  bypassing it with `Goto` or equivalent shortcuts.

## Styling Issues Need Automated Checks, Not Just Manual Taste Review

- **Context:** UAT feedback included layout overlap, unreadable selected
  states, and low-contrast shell treatments.
- **Failure or friction:** Styling and accessibility-adjacent problems were
  left mostly to manual discovery.
- **Lesson:** Visual and styling defects that affect usability should have
  automated guardrails.
- **Action for future sessions:** Add browser assertions, accessibility checks,
  and screenshot or visual-regression coverage for key screens and reusable
  components before human UAT.

## Repeated UI Friction Can Mean The Abstraction Layer Is Fighting The Framework

- **Context:** Platform-level UI work on reusable grids, filters, shell
  navigation, and related interaction patterns.
- **Failure or friction:** Custom surfaces can accumulate complexity, testing
  burden, and operability gaps when they partially reimplement framework
  behavior that the underlying UI library already supports well.
- **Lesson:** When reusable UI components become hard to reason about or hard
  to test, step back and reassess whether the custom abstraction still earns
  its cost versus a more framework-native approach.
- **Action for future sessions:** Evaluate whether a custom component improves
  developer experience, consistency, or capability enough to justify its
  maintenance and testing cost before expanding it further.

## Spacing And Theme Safety Should Be Centralized Early

- **Context:** UI review found layout, contrast, and readability issues across
  shell and workflow screens.
- **Failure or friction:** Ad hoc margins, padding, and color treatments make
  it too easy for individual pages or states to drift into inaccessible or
  inconsistent presentation.
- **Lesson:** Spacing and color safety need reusable platform rules rather than
  page-by-page cleanup.
- **Action for future sessions:** Prefer shared spacing scales, shared theme
  tokens, and reusable contrast or accessibility guardrails instead of
  scattered visual fixes.

## A Full UI Rebuild Is Achievable In A Structured Session Set

- **Context:** The MudBlazor-native rebuild replaced 20+ custom wrapper
  components across 8 sessions while preserving genuine platform value (filter
  model, expression engine, data providers).
- **Failure or friction:** The prior custom wrappers had accumulated enough
  maintenance burden, operability gaps, and framework misalignment that
  incremental simplification would not have closed the gap.
- **Lesson:** When the abstraction layer fights the framework broadly enough,
  a disciplined full rebuild with session-by-session verification is more
  effective than incremental repair. The key factors were: clear design
  decisions up front, proof-by-example patterns in early sessions that later
  sessions replicated, and mandatory cross-provider verification at each step.
- **Action for future sessions:** When evaluating whether to simplify versus
  rebuild, consider whether the friction is systemic (affecting most surfaces)
  or localized. Systemic friction favors a structured rebuild over piecemeal
  fixes.

## Explicit Page Composition Reduces Debugging And Testing Cost

- **Context:** Rebuilding app pages from metadata-driven generation to
  explicit MudBlazor component composition.
- **Failure or friction:** Metadata-driven pages were hard to debug because
  the rendering path was indirect, and hard to test because the generated
  markup was a moving target.
- **Lesson:** Explicit page composition where each page declares its own
  columns, fields, and layout is easier to read, debug, test, and customize.
  The small amount of duplication between similar pages is a reasonable price
  for clarity.
- **Action for future sessions:** Default to explicit composition for app
  pages. Reserve metadata-driven generation for scenarios with a clear
  runtime-configuration need (e.g., admin screens with user-defined fields).

## Preserve Domain Logic When Rebuilding UI

- **Context:** The QBE filter model (AND/OR groups, operators, presets,
  expression engine) survived the full UI rebuild without modification.
- **Failure or friction:** None — this went well. The filter model classes
  were cleanly separated from the filter UI components, so the UI could be
  rebuilt independently.
- **Lesson:** When domain logic and UI are cleanly separated, a UI rebuild
  does not need to touch the domain layer. If they are entangled, separate
  them first.
- **Action for future sessions:** Before starting a UI rebuild, verify that
  domain logic is factored into separate classes and not embedded in component
  code-behind files.

## UAT Checklists Must Include Explicit Test Data And Search Terms

- **Collapsed to reference on 2026-04-18** (Step 10b reorganization review
  for the `security-dashboard-polish` session set). The same guidance is
  already captured — with equivalent specificity — in
  `project-guidance.md` → Conventions → Human UAT And Checklist Editor:
  *"Include concrete test data (search terms, entity names, order numbers,
  seeded emails) in the `HumanAction` field for any step that involves
  searching, filtering, or selecting by name or ID."* Follow the convention.
- **Original entry preserved below for context.**
- **Context:** Human-executed UAT against the post-rebuild application
- **Failure or friction:** The UAT checklist asked the reviewer to use search
  widgets and filter panels without specifying what to search for. The reviewer
  does not know the seeded test data and should not have to guess or explore
  the database to find usable inputs.
- **Lesson:** UAT checklists should include concrete search terms, filter
  values, entity names, order numbers, and other test data the reviewer needs
  so they can execute each step without prerequisite investigation.
- **Action for future sessions:** When generating human-UAT checklists,
  include specific seeded-data examples in the HumanAction field for any step
  that involves searching, filtering, or selecting by name or ID. Reference
  the seed data source if the reviewer needs to find additional examples.

## Navigation Actions Must Pass User Input To The Target Page

- **Context:** Dashboard search widgets for Orders and Products
- **Failure or friction:** The search fields captured user input but the
  navigation methods discarded it, calling `NavigateTo("/orders")` without
  appending the search term as a query parameter. The target pages also had
  no query parameter handling.
- **Lesson:** When a UI control collects input that should influence the
  destination page, the navigation call must pass that input (typically as a
  query parameter), and the target page must read and apply it.
- **Action for future sessions:** When adding search, filter, or context-
  carrying navigation, wire both ends: the source must pass the parameter and
  the target must consume it. Review navigation calls during code review for
  dropped parameters.

## Fixed-Position App Bars Need A Content Offset

- **Context:** MudBlazor layout with MudAppBar and MudMainContent
- **Failure or friction:** Page titles, back buttons, and headings were hidden
  behind the fixed-position app bar across every page in the application. The
  MudMainContent had general padding but no top offset for the app bar height.
- **Lesson:** When using a fixed or sticky app bar, the main content area must
  have top padding or margin equal to or greater than the app bar height. This
  is easy to miss because scrolled content appears fine — only the top of the
  page is clipped.
- **Action for future sessions:** After adding or modifying a fixed app bar,
  verify that the first visible element on every page clears the bar without
  scrolling. Add this to the layout smoke-check during UI sessions.

## ValueChanged Callbacks Must Cover All Editable Fields

- **Context:** Product detail form with Save/Undo dirty-state tracking
- **Failure or friction:** The Undo button stayed permanently disabled because
  five of eight editable fields (UnitPrice, RoastLevel, CaffeineContent,
  GrindType, IsAvailable) lacked ValueChanged callbacks. Only Name,
  Description, and ImageUrl called `OnFieldChanged`, so editing the other
  fields never triggered `UpdateDirtyState()`.
- **Lesson:** When a form tracks dirty state via field-change callbacks, every
  editable field must participate. Missing one callback means that field's
  changes are invisible to the dirty-state tracker.
- **Action for future sessions:** When adding or reviewing a form with dirty-
  state tracking, verify that every bound field has a change callback wired to
  the state tracker. Treat an incomplete callback set as a defect, not a
  cosmetic issue.

## Currency And Date Columns Should Be Right-Aligned By Default

- **Context:** MudDataGrid columns for currency and date values
- **Failure or friction:** Currency (UnitPrice, Total) and date (OrderDate,
  ShipDate) columns were left-aligned, making them harder to scan and compare.
- **Lesson:** Numeric and date columns should be right-aligned as a convention.
  MudBlazor does not default to this, so it must be set explicitly.
- **Action for future sessions:** When defining grid columns, apply
  `CellStyle="text-align:right" HeaderStyle="text-align:right"` to currency,
  numeric, and date columns. Consider encoding this as a team convention or
  helper so it is not forgotten per-page.

## Test-Auth Sign-In Helpers Must Leave The Page On An App Route

- **Context:** Playwright fixture helper `SeededSignInAsync` that authenticates
  through a JSON-returning `/test-auth/seeded-session` endpoint
- **Failure or friction:** Six Playwright tests timed out waiting for drawer
  text because `SeededSignInAsync` left the page on the raw JSON response.
  Any subsequent `NavigateViaDrawerAsync` or other shell interaction had no
  drawer to find. Tests that happened to call `GotoAsync("/dashboard")` right
  after sign-in masked the problem; tests that trusted the helper were broken.
- **Lesson:** Sign-in helpers that rely on a JSON-returning endpoint must end
  on an app route, otherwise the helper is not self-sufficient and every
  caller has to remember a follow-up navigation. Test flakiness and timeouts
  that point at shell elements should first confirm the page is actually on
  the app, not on a helper endpoint.
- **Action for future sessions:** When authoring or auditing a sign-in /
  impersonation helper, add an explicit `GotoAsync` to a known app route at
  the end so callers can interact with the shell immediately. Prefer "helpers
  leave the browser where a real user would be" as a design rule.

## MudForm Does Not Invalidate Required-But-Untouched Fields On Load

- **Context:** `ParameterPromptForm` in Dabbler.Reports.Blazor with required
  DatePicker parameters bound through `MudForm.IsValid`
- **Failure or friction:** `MudForm` treated required fields as valid until
  something triggered validation, so the "Run Report" button stayed enabled
  when required dates were empty. The button's disabled state was driven by
  `IsValid`, which was `true` on initial render because no validation pass
  had been invoked yet.
- **Lesson:** If a button's enablement depends on `IsValid` and required
  fields can be empty on load, the form needs an explicit `Validate()` call
  on first render so `IsValid` reflects the real state instead of the pristine
  default.
- **Action for future sessions:** When binding `MudForm.IsValid` to a gate
  for submit/run actions, call `_form.Validate()` in
  `OnAfterRenderAsync(firstRender: true)`. Add a test that asserts the submit
  button is disabled before any user interaction when required fields are
  empty.

## Bounding-Box Assertions Do Not Account For Padding-Based Layout Offsets

- **Context:** `AssertLayoutSpacingAsync` Playwright helper that previously
  compared the app bar's bounding box to `MudMainContent`'s bounding box
- **Failure or friction:** The assertion `appBarBox.Y < mainContentBox.Y`
  consistently failed because `MudMainContent` clears the fixed app bar with
  top padding (`pt-16`) instead of a top margin. Its bounding box legitimately
  starts at Y=0, the same as the app bar. The visual layout was correct; the
  assertion was checking the wrong thing.
- **Lesson:** Bounding-box assertions don't see padding. When a layout offsets
  content with padding instead of margin, an "element X is below element Y"
  check has to measure the first visible child inside the padded container,
  not the container itself.
- **Action for future sessions:** For "header clears content" style assertions,
  locate the first visible child (heading, card, paper) inside the main
  content area and compare its Y to `appBar.Y + appBar.Height`. Avoid naive
  box-to-box comparisons when one of the elements uses padding to offset its
  inner content.

## Send Related Files To The Cross-Provider Verifier Or Disclaim What Is Out Of Scope

- **Promoted to:** `project-guidance.md` → Conventions → Workflow Expectations
  on 2026-04-18 (Step 10b reorganization review for the
  `security-dashboard-polish` session set). Applied successfully as the right
  call in two contexts: the original orders/notes incident, and Session 8 of
  `security-dashboard-polish` where a proactive "Out-of-scope disclosures"
  section in the round-1 verification prompt kept false positives at zero.
- **Original entry preserved below for context.**
- **Context:** End-of-session cross-provider verification via the AI router
- **Failure or friction:** Round-1 verification produced two false positives
  (one Critical, one Major) claiming that `GET`, `PUT`, and `DELETE`
  note-endpoint actions were missing from `OrdersController.cs`. They
  actually live in a sibling `OrderNotesController.cs` that was not included
  in the verification prompt.
- **Lesson:** The verifier only sees what the prompt includes. If the session
  touches one file in a conceptual area (e.g., orders CRUD), related files
  in the same area should be supplied too, or the prompt should explicitly
  say "notes CRUD lives in `OrderNotesController.cs` and is not being
  changed this session" so the verifier does not hallucinate missing code.
- **Action for future sessions:** Before calling the router for verification,
  list related files the verifier might reasonably look for and either
  include them or add a "files not in scope but present in the repo"
  section to the prompt.

## Hard-Asserted Disabled-State Tests Become Regressions When The Feature Gets Enabled

- **Promoted to:** `project-guidance.md` → Conventions → Testing Conventions
  on 2026-04-19 (Step 9 reorganization review for the
  `rearchitect-role-administration-foundations` session set). Applied
  successfully as the right call in two contexts: the original
  security-dashboard-polish incident (SystemAdmin "no order edit" tests
  inverted when `IsReadOnly` flipped), and Session 3 of
  `rearchitect-role-administration-foundations` where
  `WebGuardrailTests.BackofficeAdmin_RouteAccess_MatchesFocusedAdminNavigationAsync`
  had to be relaxed to accept 200 on `/admin/products` and
  `/admin/security` because the rewritten handlers no longer restrict
  SystemAdmin. Follow the convention.
- **Original entry preserved below for context.**
- **Context:** Two pre-existing Playwright tests asserted SystemAdmin saw no
  status-transition buttons, no "Add Product" button, and no "Add Note"
  button on the Order detail page because `OrderDetailPage.razor` rendered
  with `IsReadOnly="true"`.
- **Failure or friction:** When the session fix flipped `IsReadOnly` to
  `false`, those tests silently inverted from safeguards into guaranteed
  regressions. They had to be rewritten during verification-driven repair
  rather than catching a real defect.
- **Lesson:** A test that asserts a control is absent under a feature flag
  or parameter is only durable when it signs in as the role that genuinely
  gets that absent state. Asserting "SystemAdmin cannot see X" is not a
  stable assertion if the product direction for SystemAdmin is to see X.
- **Action for future sessions:** When writing "disabled state" or "hidden
  control" tests, sign in as the role whose product definition mandates
  that state (for order edit, that is `Customer`, not `SystemAdmin`).
  Complement with a positive-path test that confirms a privileged role
  does see the control. Flag any disabled-state test that uses a
  privileged role as a maintenance risk.

## Mark `[Obsolete]` Instead Of Deleting Cascading Types When A Future Session Already Owns The Consumer Rewrite

- **Promoted to:** `project-guidance.md` → Conventions → Workflow
  Expectations on 2026-04-19 (Step 9 reorganization review for the
  `rearchitect-role-administration-dashboard` session set). Applied
  successfully as the right call in two contexts: Set A Session 2 (the
  originating incident — physical deletion of seven configuration types
  with ~50 consumer files would have forced Session 3 over its
  boundaries), and Set B Sessions 2–8 (progressive `[Obsolete]` deletion
  alongside UI consumer removal, with ~145 warnings still expected to
  cascade out during Sets D/C). Follow the convention.
- **Original entry preserved below for context.**
- **Context:** Session 2 of `rearchitect-role-administration-foundations`
  was instructed to physically delete `PagePermission`, `EndpointPermission`,
  `EntityPermission`, `FieldPermission`, `ControlPermission`, `RowFilter`,
  and `RoleSwitchingPermission`, plus the `RoleDefinition.InheritsFrom`
  field. Those types had ~50 consumer files (5 handlers, the permission
  resolver and service, two persistence stores, seven Blazor admin matrix
  components, the dashboard, and ~20 test files), most of which Session 3
  rewrote and Set B will rebuild.
- **Failure or friction:** Physical deletion in Session 2 would have
  forced Session 3's handler rewrite plus a chunk of Set B's dashboard
  rebuild into a single conversation, violating the one-session-per-
  conversation rule and overrunning the spec's session boundaries.
- **Lesson:** When a spec calls for physical deletion of a type whose
  consumers number in the dozens and a later session or set is already
  scheduled to rewrite those consumers, marking the type `[Obsolete]`
  preserves the deletion signal (compiler warnings on every consumer)
  while keeping the build green and respecting session boundaries. The
  cross-provider verifier explicitly endorsed this deviation in the
  Session 2 review as "arguably superior to the literal spec instruction."
- **Action for future sessions:** Before physically deleting a type, count
  consumer files. If a scheduled future session already owns the consumer
  rewrite, mark `[Obsolete]` instead and disclose the deviation in the
  cross-provider verification prompt. The deletion lands naturally in the
  session that rewrites the last consumer.

## Follow The Spec's Semantic Intent, Not Its Widget Name

- **Promoted to:** `project-guidance.md` → Conventions → MudBlazor Usage
  on 2026-04-19 (Step 9 reorganization review for the
  `rearchitect-role-administration-dashboard` session set). Applied
  successfully as the right call in two contexts: `security-dashboard-polish`
  Session 2 (the originating `MudSwitch` tri-state incident) and Set B
  Session 3 (`ColumnRestriction` ViewOnly|Hidden + `UiRestriction`
  Disabled|Hidden both implemented as `MudToggleGroup<T>` despite the
  spec text naming `MudSwitch`). Follow the convention.
- **Original entry preserved below for context.**
- **Context:** Session 2 of `security-dashboard-polish` asked for "one
  `MudSwitch` bound to a three-state model (Allow / Deny / Unset) or to a
  single allow/deny enum" to replace two independent checkboxes in the
  Page Access matrix.
- **Failure or friction:** `MudSwitch` in MudBlazor 8.x is natively binary.
  Implementing a true tri-state through `MudSwitch<bool?>` would have
  required hackery, and collapsing to two states would have regressed
  runtime authorization semantics — "role has no opinion" and "role
  explicitly denies" produce different outcomes for multi-role users, so
  Unset must stay distinguishable from Deny.
- **Lesson:** When a spec names a specific widget but the semantics it
  describes don't fit that widget, pick the widget that honors the
  semantics (here, `MudToggleGroup<T>` with `SelectionMode.SingleSelection`)
  rather than forcing a mismatched control. The spec authorized the
  tri-state model explicitly; the widget name was guidance, not a mandate.
- **Action for future sessions:** When a spec's named control can't
  express the stated semantics, choose the semantically correct control
  and call out the deviation explicitly in the end-of-session
  verification prompt (files, reasoning, why the alternative would
  regress). Do not silently substitute and do not force the named widget
  into a shape it wasn't designed for.

## Report Footer Totals Must Align Under Their Detail Column

- **Context:** `reportFooter` and `groupFooter` bands in report RDFs
  (e.g., `orders-by-customer.rdf.json`, `sales-by-product.rdf.json`).
- **Failure or friction:** A Grand Total aggregate was positioned
  mid-column (e.g., `Count(*)` at x=5.0 when the "Items" column header
  was at x=6.2), and a separate descriptor label ("orders", "units")
  was parked at the actual column X to "explain" the number. The label
  and the number overlapped in the PDF export and the number did not
  appear under the column it summarized.
- **Lesson:** An aggregate in a footer band that summarizes a detail
  column must share the detail column's X and width. Adding a
  descriptor label next to a misaligned aggregate is a smell — the
  column header already names the quantity, and the label ends up
  colliding with the value. The descriptor label is a sign of the
  alignment bug, not a fix for it.
- **Action for future sessions:** When authoring or reviewing a report
  RDF, verify that every numeric aggregate in `reportFooter` /
  `groupFooter` bands matches the X and width of the corresponding
  element in the `detail` band. Treat any noun label ("orders",
  "units", "items") placed adjacent to an aggregate as a warning sign:
  either the aggregate is misaligned, or the column header is too
  vague and should be renamed instead of labeled twice.

## MudDataGrid Row-Select: Prefer `RowClick` + `ReadOnly` Over `SelectedItemChanged`

- **Context:** Master-details grids in the Security Dashboard
  (`RoleMasterGrid`, `OrganizationMasterGrid`, `UsersMasterGrid`)
  whose row selection must collapse the expansion shell around them.
- **Failure or friction:**
  `security-dashboard-uat-followup-fixes` Finding #49. The reviewer
  reported that clicking a cell in the first column took two clicks
  to collapse the shell instead of one. Root cause: the grids wired
  `SelectedItemChanged="..."` + `SelectedItem="@_selected"`, and the
  first-column `CellTemplate` wrapped its content in a `<div>` with
  other children (MudText + MudChip or MudTextField). Clicking
  inside that CellTemplate first set cell focus rather than firing
  the row-select event; only a second click registered as a row
  click. `SelectedItemChanged` also fires only when `SelectedItem`
  actually changes, so re-clicking an already-selected row is a
  silent no-op — compounding the "two-click" perception.
- **Lesson:** For master grids whose row-click semantics drive a
  side-effect (shell collapse, detail-form load, navigation),
  `RowClick="@handler"` + `ReadOnly="true"` is more reliable than
  `SelectedItemChanged`. `RowClick` fires on every row click
  regardless of prior `SelectedItem` state, and `ReadOnly="true"`
  prevents MudBlazor from interpreting the click as a cell-edit
  intent in the first place. Custom interactive controls inside
  `CellTemplate` (e.g., `MudTextField` for inline-editable
  DisplayAlias) are unaffected by the grid's ReadOnly flag — they
  stay fully interactive because `ReadOnly` gates only the grid's
  built-in cell-editing mode.
- **Action for future sessions:** When adding or reviewing a
  master-grid whose click selects a row and collapses a shell or
  loads a details form, default to `RowClick` and a `SelectedItem`
  one-way binding (for visual highlight). Treat
  `SelectedItemChanged` as a trap when the first column contains
  non-trivial `CellTemplate` content, and flag it during code
  review.

## Dev-Persona Roles-Override Is Safer Than A DB-Seed Addition

- **Context:** Adding a new dev-only persona so UAT reviewers can
  exercise the baked-in `User` role path
  (`security-dashboard-uat-followup-fixes` Finding #2).
- **Failure or friction:** The first attempt added `juser@acmecoffee.com`
  as a seeded `AppUser` and assigned the role via `user-roles.json`
  with key `"8"`. But `user-roles.json` keys are integer user IDs,
  and inserting a new user at position 8 in SeedData shifts every
  subsequent user ID (sloane→9, casey→10, etc.). The running Docker
  web container's DB already had users through ID 7, so the Backfill
  path added juser at some later id — and the `"8": ["User"]`
  mapping now pointed at sloane, not juser. Worse, when
  `AcmeCoffeeOnline.Api.Tests` spun up its `WebApplicationFactory`
  against the same dev SQL Server, the seed pass tried to insert
  juser again and EF failed with a duplicate-key error on
  `IX_AppUsers_Email`. Five API tests went red before I noticed.
- **Lesson:** For dev-only personas (UAT, Playwright, manual
  testing) where the only goal is "sign in under this role
  configuration," the roles-override path through
  `/test-auth/seeded-session?email=...&roles=...` is far safer than
  adding a new `AppUser` to seed. Override paths don't touch the DB
  schema, don't shift IDs, and don't run a second seed pass in
  test-host factories. The `X-Test-Auth-Secret` gate and the
  `appsettings.Development.json` `AllowedRoles` allow-list still
  constrain what can be requested, so no new auth surface.
- **Action for future sessions:** Before modifying `SeedData.cs`
  and `user-roles.json` to introduce a persona, ask whether a
  dev-quick-signin persona with a `RolesOverride` can satisfy the
  need. If yes, prefer it. Seed-data persona additions are only
  warranted when the new user needs DB-backed relationships
  (org memberships, external-login mappings, inventory of orders,
  etc.) that a synthetic principal can't provide.
- **Exception — real-login UAT flows require DB-seeded personas.**
  When a UAT checklist or E2E test must exercise the real login
  path (magic-link, SSO, Entra ID, etc.), the roles-override path
  is unreachable from the user's perspective — a human types their
  email into the login screen and receives a magic-link, and the
  resulting principal is built from the DB + JSON role store, not
  from the `/test-auth/seeded-session?roles=...` override endpoint.
  In that case a seeded `AppUser` is required. Added 2026-04-21 in
  `security-dashboard-uat-followup-fixes` Session 2, where the
  Session 1 override-based fix for Finding #2 could not be
  validated against the human's actual login flow. **Mitigate the
  ID-shift fragility** by inserting the new seed user at a fixed
  list position BEFORE any variable-length loops (e.g., before the
  `foreach (var customer in customers)` retail-contacts loop in
  `SeedData.GetSecurityUsers`). The user's `AppUser.Id` then stays
  deterministic on fresh seeds regardless of collection sizes,
  and the matching `user-roles.json` key can be hardcoded with
  confidence.

## Pin Culture Explicitly In .NET Containers

- **Context:**
  `security-dashboard-uat-followup-fixes` Finding #14. The `/products`
  page's Unit Price column used a `C2` format string and rendered
  with the invariant-culture `¤` symbol in Docker even though
  local dev showed `$`.
- **Failure or friction:** Linux container images ship with a
  POSIX locale by default (`LANG=` and `LC_ALL=` empty; `locale`
  reports `POSIX`). Under .NET, an empty locale causes the runtime
  to fall back to `InvariantCulture`, whose `NumberFormatInfo`
  uses the generic `¤` for `NumberFormatInfo.CurrencySymbol`. The
  `C2` format therefore rendered as `¤1,234.56`. Local Windows
  hosts use the user's OS culture, so the bug was invisible
  outside Docker.
- **Lesson:** For any .NET app whose UI surfaces currency,
  localised dates, or culture-dependent number formatting, pin
  `CultureInfo.DefaultThreadCurrentCulture` (and
  `DefaultThreadCurrentUICulture`) at app startup — before the
  host starts handling requests. For a Blazor Server / ASP.NET
  Core app, the fix lands in `Program.cs` immediately after
  `var app = builder.Build();`, paired with
  `app.UseRequestLocalization(...)` if request-scoped formatting
  matters. Do NOT rely on container-level `LANG`/`LC_ALL` alone:
  base images don't always include the matching locale archives,
  and the env-var approach fails silently.
- **Action for future sessions:** When scaffolding a new .NET
  service or reviewing a Dockerfile that serves currency/date
  content, add the culture-pin as a default code pattern in
  `Program.cs`. If a PR surfaces a "formatting looks wrong only
  in Docker" bug, check the running container's `locale` output
  and the app's `CultureInfo.CurrentCulture` at startup before
  debugging further.

## Align Checklist Wording To The Test's Literal Assertions Early To Avoid uat-coverage-review Round Escalation

- **Context:** The `uat-coverage-review` gate in
  `security-dashboard-uat-followup-fixes` Session 1. Only 4
  checklist items, but the gate took 5 rounds to reach VERIFIED
  and cost ~$3.13 total across the rounds.
- **Failure or friction:** Most of the round-to-round churn was
  wording drift between checklist verifications and test
  assertions rather than functional coverage gaps. Examples the
  verifier flagged: the checklist said a summary "reads 'New
  organization'" but the test used `Assert.Equal` on
  `InnerTextAsync` rather than Playwright's `ToHaveTextAsync`; the
  checklist asserted a "column header in `.mud-table-head`" but the
  test asserted a data cell (`td[data-label='Unit Price']`); the
  checklist positioned an element "below the collapsed summary"
  but the test only asserted visibility. Each mismatch escalated
  a round because the verifier is explicitly tuned to prefer
  false-positive `ISSUES_FOUND` over false-negative `VERIFIED`.
- **Lesson:** When authoring a UAT checklist for a new finding,
  write the verification text against the EXACT Playwright
  assertion you intend to land — same locator scoping, same native
  API (prefer `Expect(locator).ToContainTextAsync` /
  `ToHaveTextAsync` over xUnit `Assert.Contains` on
  `InnerTextAsync`), same positional claims (don't say "below X"
  unless the test asserts position). Review your own checklist
  through a "would the verifier accept this as an identical
  match" lens before the first route call. Alternatively:
  generate the checklist FROM the finished test rather than the
  other way around, since the test's language is the
  mechanically-verifiable ground truth.
- **Action for future sessions:** Before routing
  `uat-coverage-review`, do a two-column dry-run in your head:
  every `Verifications[i].Text` next to the Playwright line that
  will be paired with it. If the wording diverges, tighten it
  now — a wording pass costs zero API dollars; a coverage-review
  round costs ~$0.60. Also, if a task type is paired
  (generator + gate), revisit whether the mechanical half should
  be pinned to Sonnet rather than Opus (see the "Mechanical-Gate
  Task Types Calibrate To Sonnet-Cost For Adequate Quality"
  lesson — applied in the prior set but NOT yet applied to the
  actual `task_type_overrides` config).

## MudTable `<thead>` Rows Also Carry `tr.mud-table-row`

- **Context:** Playwright locator that wanted "the first data row"
  in a MudTable on `/dev/email-inbox`
  (`security-dashboard-uat-followup-fixes` Session 2).
- **Failure or friction:** The real-login test used
  `page.Locator("tr.mud-table-row").First` to grab the top-most
  email message. It failed with a misleading "Locator expected to
  be visible … waiting for td[data-label='Link']" timeout because
  MudTable renders `<tr class="mud-table-row">` in BOTH the
  `<thead>` (the header row of `<th>` cells) and the `<tbody>`
  (the data rows of `<td>` cells). The unscoped `.First` matched
  the header row, which has no `td[data-label='Link']` cell — so
  subsequent cell lookups inside `.First` failed with a confusing
  timeout instead of a clear "no data rows" error.
- **Lesson:** When selecting "the first data row" in a MudTable
  (or MudDataGrid rendered as a table), scope the selector to
  `tbody tr.mud-table-row` explicitly. The class-level selector
  alone is ambiguous because MudBlazor attaches the same class
  to the header row.
- **Action for future sessions:** In any Playwright or bUnit
  locator that targets MudTable rows by the `.mud-table-row`
  class, default to `tbody tr.mud-table-row` (or `thead
  tr.mud-table-row` for header-specific assertions). Treat an
  unscoped `tr.mud-table-row` as a smell during code review —
  ask whether the header row is a valid match or a silent
  mismatch waiting to surface as a timeout.

## A Second Playwright Page Is The Right Analogue For A Second Browser Tab

- **Context:** Real-login UAT test for
  `security-dashboard-uat-followup-fixes` Session 2. The human
  flow has two tabs: one for the login screen, another for
  `/dev/email-inbox` where they read the magic-link URL.
- **Failure or friction:** None — this went well. Choosing how to
  model the two-tab flow mattered: `page.EvaluateAsync` + DOM
  scraping of the inbox inside the login page, a hidden iframe,
  or cross-origin shenanigans would all have been fragile. A
  second Playwright page inside the same `BrowserContext` turned
  out to be the exact analogue of a second browser tab.
- **Lesson:** `PlaywrightFixture.CreatePageAsync` opens a new
  page within the shared `BrowserContext`, which means cookies
  and `localStorage` are shared across pages — the same
  invariants as two tabs under one browser profile. The DOM
  contexts are independent, so page 1 can await a render while
  page 2 reads a different URL, and neither blocks the other.
  When a human UAT flow involves multiple tabs (login + dev
  inbox, primary app + admin console, claim link + verify
  target, etc.), the faithful reproduction in Playwright is
  literally "open a second page" — not simulation.
- **Action for future sessions:** When a UAT or E2E flow
  involves a human opening a second tab, author the Playwright
  test with two pages rather than one page plus DOM surgery.
  Read the cross-tab data (an href, a token, an address-bar
  URL) on the second page, then act on the first page.
  Dispose the second page in `finally` once reads complete.
  Don't share locators across pages — create them with each
  page's own `page.Locator(...)`.

## Every UAT Checklist Item Must Be Pre-Tested In Playwright Before The Human Runs The Checklist

- **Context:** Human UAT run for the
  `rearchitect-role-administration-dashboard` session set on 2026-04-19.
- **Failure or friction:** The checklist was handed to the human without
  an accompanying Playwright suite that drove each item's steps
  end-to-end. The human discovered multiple blocking defects on the first
  pass: the Users and Role Assignment tab throws a
  `DbContext second-operation InvalidOperationException` from
  `AdminUserQueryService.GetActorAssignedOrganizationIdsAsync` the moment
  it renders; per-role tab gating hides the Security nav entry entirely
  for non-SystemAdmin users (so CrossOrgUserAdmin, UserAdmin, and User
  cannot reach the Role Documentation tab they're supposed to see);
  CrossOrgUserAdmin and BackofficeUser both see zero retail-business
  organizations because the new EF org-scope query filter is too
  aggressive; Data-bound restriction pages return "Access denied" before
  the enforcement seam renders; the Column Restrictions Add button is
  disabled; the Default Masks column dropdown omits the seeded
  `SocialSecurityNumber`; the sensitive-name safety-net Add flow requires
  a browser refresh to update the grid. Three separate "Other" items on
  the UAT explicitly called this out: *"I thought that we were supposed
  to do Playwright tests before subjecting the human to testing"*, *"See
  how this wastes time when you don't test this first"*, *"Before any
  UAT, I think that you should perform the exact same steps in Playwright
  — exact parameters — so that I am not wasting my time here."* Every one
  of those defects is something a Playwright test with the same steps
  and same seed data would have caught during the session that shipped
  the feature.
- **Lesson:** A UAT checklist is a human-judgment layer, not an
  operability gate. Handing the human a checklist without first proving
  the steps run end-to-end in an automated browser test inverts the
  testing hierarchy: the human becomes the first automation pass.
  That's slow, frustrating, and it burns goodwill. Every checklist item
  must have a Playwright test that drives the same steps with the same
  parameters before the checklist is committed and the human is notified.
  For aesthetic/judgment items, the Playwright test still exercises the
  sequence and asserts a reachable live state so the human is rendering
  judgment on a verified-live UI, not debugging exceptions.

---

## `route(task_type=documentation)` Wraps Content In A Code-Documentation Template

- **Collapsed to reference on 2026-04-22** (Step 9 reorganization review
  for the `pdf-info-metadata-api` session set). The same guidance is
  now captured — with the added Gemini-Pro-400-on-summarization
  mitigation — in `project-guidance.md` → Conventions → Workflow
  Expectations: *"Use `route(task_type="summarization")` for narrative
  prose outputs ... Reserve `route(task_type="documentation")` for
  code-doc generation ... If a `summarization` call returns Gemini 400
  on a prompt above ~2KB, pin `max_tier=1`."* Applied as the right
  call in two contexts: `rearchitect-role-administration-dashboard`
  Session 8 (originating incident — documentation template produced
  400s; fell back to direct API) and `pdf-info-metadata-api` Session 3
  (summarization+gemini-pro 400'd on a 3.5KB prompt; `max_tier=1`
  routed to Gemini Flash and succeeded for $0.0004). Follow the
  convention.
- **Original entry preserved below for context.**
- **Context:** Session 8 of `rearchitect-role-administration-dashboard`
  generated the change-log.md through the AI router.
- **Failure or friction:** The `documentation` prompt template at
  `ai-router/prompt-templates/task-types/documentation.md` is shaped for
  "document this code" work (purpose / public API / usage examples /
  caveats). When the content is narrative meta-prose like a change log,
  the wrapped prompt produced `Gemini Pro 400 Bad Request` on every
  retry. The same content sent directly to `generativelanguage.googleapis.com`
  via raw `httpx` with the repo's `google-system.md` prompt returned 200.
  Fell back to the direct API call for this session.
- **Lesson:** The router's per-task-type template meaningfully alters the
  prompt shape. For narrative docs (change logs, READMEs, summaries,
  any free-form prose), use `task_type="summarization"` (no template in
  `prompt-templates/task-types/`, so content passes through raw) rather
  than `task_type="documentation"`, which is reserved for code
  documentation. If a mysterious Gemini Pro 400 surfaces in the router
  but direct API returns 200, suspect the task-template wrapping first.
- **Action for future sessions:** Reserve `task_type="documentation"`
  for code-doc generation. For change logs, release notes, session
  summaries, and similar narrative prose, use `task_type="summarization"`.
  Consider adding a dedicated `change-log` or `narrative` task type with
  a no-op template if this pattern recurs.

## `.NET 10` Publish With `--no-restore` Silently Drops Blazor Framework Assets

- **Context:** Session 3 of `security-dashboard-uat-remediation`.
  The Docker Playwright suite returned 99/99 failures because every
  browser saw `GET /_framework/blazor.web.js` 404. Local `dotnet
  publish` produced the file correctly; only the Docker image
  omitted it.
- **Failure or friction:** `Dockerfile.Web` used the classic
  "copy csprojs → restore → copy source → publish `--no-restore`"
  layer-caching pattern. Under `.NET 10 SDK (10.0.201)` specifically,
  `dotnet publish --no-restore` after a csproj-only restore skips
  the Blazor static-asset discovery pass, so `blazor.web.js` is never
  added to `wwwroot/_framework/` or the
  `.staticwebassets.endpoints.json` manifest. The bug was masked
  locally because the dev machine used `.NET 11 preview SDK`, which
  handles the same staged pattern correctly. SignalR (`/_blazor/negotiate`)
  kept returning 200, which made it look like a Blazor wiring issue
  rather than an asset-pipeline issue.
- **Lesson:** Do not pass `--no-restore` to `dotnet publish` in a
  `.NET 10` Docker build, even if an earlier stage ran `dotnet
  restore`. The redundant restore is a no-op for package resolution
  but is required to fire source-aware MSBuild targets (including
  Blazor's static-asset registration). Also, when a Docker-only
  build misbehaves, always compare the SDK inside the build image
  against the local SDK — silent MSBuild-behaviour drift between
  SDK versions is a real failure mode.
- **Action for future sessions:** (1) When writing or editing a
  `Dockerfile.*` for a Blazor Server app in this repo, leave
  `dotnet publish` without `--no-restore`. Keep the csproj-first
  restore layer for caching; it still works. (2) When a Docker
  build produces output that differs from a local build, run
  `dotnet --version` on both sides before debugging anything else.
  (3) If `/_framework/blazor.web.js` 404s, `curl` it from inside
  the web container, then grep the `*.staticwebassets.endpoints.json`
  manifest — if the route is missing from the manifest, it is a
  publish-pipeline problem, not a runtime middleware problem.

## `.NET 9/10` Blazor: `MapStaticAssets()` Replaces `UseStaticFiles()` In The Endpoint Section

- **Context:** Same Session 3 investigation. The prior attempt
  added `app.MapStaticAssets()` next to `app.UseStaticFiles()` in
  the middleware section of `Program.cs` and saw no change — which
  initially suggested the problem was elsewhere.
- **Failure or friction:** In `.NET 9+`, `MapStaticAssets()` is
  designed to **replace** `UseStaticFiles()`, not coexist with it,
  and it is an endpoint-routing operation: templates call it in the
  `Map*` section adjacent to `MapRazorComponents<>()`, not in the
  middleware section. Calling both middlewares in the wrong section
  does not error out, it just produces a subtly wrong asset pipeline.
  Additionally, `App.razor` should reference
  `@Assets["_framework/blazor.web.js"]` (the helper that consults
  the manifest), not the raw path.
- **Lesson:** The canonical `.NET 10` Blazor Web App template is the
  source of truth for the Program.cs shape; diff against `dotnet new
  blazor -int Server` output when debugging asset-pipeline behaviour.
- **Action for future sessions:** When working on Blazor static
  assets: remove `UseStaticFiles()` if present, call `MapStaticAssets()`
  immediately before `MapRazorComponents<>()`, and use the `@Assets[]`
  helper in `App.razor`. If the asset pipeline behaves strangely,
  `docker run --rm mcr.microsoft.com/dotnet/sdk:10.0 bash -c "dotnet new
  blazor -o ref -int Server && ..."` gives a clean reference to diff
  against.

## Docker E2E: Loopback-Only Dev Endpoints Need An Explicit Bridge Bypass

- **Context:** Same Session 3 investigation, after the blazor.web.js
  fix. All authenticated Playwright tests still failed, because
  `/test-auth/*` endpoints returned 403 to the playwright container.
- **Failure or friction:** The `/test-auth/*` endpoints in
  `AcmeCoffeeOnline.Web/Program.cs` gate on
  `IsLoopbackOrInMemoryRequest` AND the `X-Test-Auth-Secret` header.
  In Docker, playwright → web bridge-network traffic is not
  loopback, so the guard rejects every request before checking the
  secret. The 403 was previously invisible because the blazor.web.js
  404 aborted tests earlier.
- **Lesson:** Dev-only loopback gates should have an explicit
  opt-in bypass for non-loopback callers that still trust the
  shared secret — otherwise any bridge-networked E2E harness
  (Docker, k8s, podman) is stuck. Leaving the bypass default-false
  keeps the layered defense in plain Development runs.
- **Action for future sessions:** When adding dev-only endpoints
  protected by `IsLoopbackOrInMemoryRequest` (or equivalent), also
  add a config-gated `AllowNonLoopback` knob on the options type,
  enforce that it only takes effect in Development/Test, and
  document the expected env-var wiring for Docker-compose. If a
  bridge-networked test run returns 403 on a dev endpoint that
  works via localhost, check the loopback gate before suspecting
  secret mismatch.

## Docker E2E: Connection Strings Must Be Set Per-Service

- **Context:** Session 3 Docker infrastructure cleanup. After
  fixing the test-auth 403, every authenticated Playwright page
  still hit an HTTP 500; the web container log showed
  `PlatformNotSupportedException: LocalDB is not supported on this
  platform.`
- **Failure or friction:** A prior Docker-infra pass had set
  `ConnectionStrings__DefaultConnection` on the `api` service in
  `docker/docker-compose.yml` but not on the `web` service. The
  web service therefore inherited the `Server=(localdb)\...`
  connection string from `appsettings.Development.json`, which
  cannot resolve on a Linux container. Symptom surfaced only after
  the earlier blocker was cleared.
- **Lesson:** In a multi-service Docker-compose stack where each
  service opens its own DbContext, the connection-string override
  must be set on **every** service that talks to the DB, not just
  the API. Don't assume a web-for-Blazor service is "just a
  front-end" that delegates all DB access to the API — this repo's
  web also owns a `DabblerSecurityDbContext` directly.
- **Action for future sessions:** When reviewing the compose stack for
  this repo (`docker/docker-compose.infra.yml` for persistent dependencies
  and `docker/docker-compose.app.yml` for rebuildable app services),
  enumerate the services in the **app** file and cross-check each one
  against its app's DbContext dependencies. If the service opens a
  DbContext, it needs `ConnectionStrings__<Name>` set in `environment:`.
  Also prefer a compose-level `x-common-env` anchor for shared config
  like connection strings so future drift surfaces in one place rather
  than per-service.

## Heuristics That Earn Their Keep: Stop Tuning The Router Complexity Score

- **Context:** AI router complexity scoring (`complexity.weights` and
  `task_type_scores` in `router-config.yaml`). The 1-100 heuristic
  maps every incoming task to a tier via four weighted components
  (context length, keyword signals, task type, explicit hint).
- **Failure or friction:** None — the heuristic works well enough.
  The temptation is to invest engineering in tightening it
  (embeddings, ML-based prediction, per-repo calibration) to squeeze
  the escalation rate lower.
- **Lesson:** The current heuristic plus the 2-try escalation safety
  net is the intended stopping point. When the heuristic misjudges,
  escalation catches it and the metrics log records the pattern. The
  engineering time saved is worth more than the handful of retried
  API calls the tighter heuristic would prevent. Do not introduce ML
  or embeddings here; it is a false economy and a procurement-
  complication risk (more dependencies to vendor-review).
- **Action for future sessions:** If a review surfaces "the router
  should predict complexity better", point at this entry and the
  comment block above `complexity:` in `router-config.yaml`. The
  only acceptable knob changes are adjusting the existing weights or
  adding keywords to the high/low lists — both of which are data
  entry, not engineering.

## Verifier Selection Is Rule-Based, Not Map-Based

- **Context:** Cross-provider verifier selection (`verification.py`
  `pick_verifier_model`, `router-config.yaml` verification section).
  Originally driven by a static `cross_provider_map`.
- **Failure or friction:** The static map tied specific models to
  specific verifier targets, which ran against the architecture's
  stated vendor-agnosticism goal. Retiring or swapping a model meant
  hunting through a pairing table in addition to the `models:`
  section. It also created the appearance of favoritism to procurement
  reviewers.
- **Lesson:** Verifier eligibility is a role (different provider,
  enabled as verifier, tier match, cheapest survivor), not a user-to-
  user binding. The RBAC analogy holds: assign permissions to the
  role, not to the named account. Preferred pairings become optional
  hints that only take effect when they survive the rules.
- **Action for future sessions:** When adding a model to
  `router-config.yaml`, add it with `is_enabled_as_verifier: false`
  until calibration data justifies promoting it. When retiring a
  model, flip `is_enabled_as_verifier: false` first, confirm the
  rules-based fallback lands on an acceptable verifier, then remove
  the entry. Never re-introduce a map-first selection pattern — the
  rules engine is load-bearing for the governance story.

## MudNumericField Playwright FillAsync Does Not Commit @bind-Value

- **Context:** `ProductAdministrationWorkflowTests.E2E_WF_05_AddsProductAfterKeyboardAndValidationChecks`
  (Docker Playwright) typed `"12.34"` into a MudNumericField via
  `FillAsync("12.34")`, then clicked Create. The persisted product
  had `UnitPrice = 0.01` (the initial C# default), not `12.34`.
- **Failure or friction:** MudNumericField's `@bind-Value` is wired
  to the browser's `change` event. Playwright's `Locator.FillAsync`
  fires only `input` events; `Locator.BlurAsync` fires only `blur`.
  Neither produces a `change` event, so the bound C# decimal never
  receives the typed value even though the rendered field shows
  "12.34" and the form validates as enabled.
- **Lesson:** For MudNumericField (and any MudBlazor input whose bind
  target is `change`-driven), replace the one-line FillAsync with a
  keyboard-event sequence that the browser actually turns into a
  `change` event. Canonical pattern:
  ```csharp
  await field.ClickAsync();                        // focus
  await page.Keyboard.PressAsync("Control+A");     // select all
  await page.Keyboard.PressAsync("Delete");        // clear
  await field.PressSequentiallyAsync("12.34");     // type
  await page.Keyboard.PressAsync("Tab");           // real focus-nav ⇒ blur+change
  ```
- **Action for future sessions:** When authoring a Playwright test
  that fills a MudNumericField, use the PressSequentially+Tab
  pattern. When debugging a "value typed but persisted default",
  check whether the input's bind fires on `change` and switch
  from FillAsync accordingly.

## Server-Side Blazor Self-POST Must Target Container-Internal URL

- **Context:** `Login.razor` Blazor Server component ran
  `HttpClient.PostAsJsonAsync($"{Navigation.BaseUri}/auth/send-magic-link", ...)`
  to send magic-link codes. Worked for years on localhost-only dev
  runs and in the previous compose topology where Playwright ran
  on the Docker network.
- **Failure or friction:** After `docker-compose.yml` was split
  into `docker-compose.{app,infra}.yml` and Playwright moved to
  the host, the web container was reached via the host's
  `localhost:5010` port mapping. `NavigationManager.BaseUri`
  returns the browser-visible URL, so server-side `HttpClient`
  running INSIDE the container was posting to
  `http://localhost:5010/...`, which is not bound inside the
  container — connection refused, caught, `_errorMessage` set,
  and the "We sent a sign-in code" step never rendered. Two
  MagicLink Playwright tests failed deterministically; the app
  behaviour looked fine to a user in the browser (because the
  browser posted to the host, not through the server-side
  HttpClient), masking the bug until the tests surfaced it.
- **Lesson:** Server-side components that call the app's own HTTP
  endpoints must target the container-internal URL
  (`http://localhost:8080` in our compose wiring), not
  `Navigation.BaseUri`. But they also must preserve the browser's
  `Host` header, because server-side handlers like the magic-link
  sender read `ctx.Request.Host` to build the email's verify URL
  — if Host is "localhost:8080", the email link is unreachable.
- **Action for future sessions:** Drive server-side self-POSTs
  through a helper like `Login.razor`'s `PostSelfAsync`, which
  reads `Services:WebSelfBaseUrl` from configuration (container-
  internal URL, settable per environment) and preserves the
  browser's Host via `HttpRequestMessage.Headers.Host`. When
  introducing a new self-calling endpoint in a Blazor Server
  component, extend the helper or document the same pattern; do
  NOT use `Navigation.BaseUri` as the HttpClient target.

## Isolate Flaky-Under-Parallel Tests In A Dedicated Collection; Skip Deterministic Failures Explicitly

- **Context:** Session 10 closeout's full-suite Playwright run
  produced three unrelated failures. Two were real bugs (fixed
  inline). The third, `ProductAdministration E2E_WF_04`, passed
  cleanly in isolation but failed in the full suite because the
  MudBlazor snackbar-toast assertion missed its visibility window
  under concurrent browser-session + circuit + SQL pressure. A
  fourth, `MultiOrganization E2E_WF_02`, failed deterministically
  even alone — the org switch simply didn't take effect.
- **Failure or friction:** Both tests had been loosely called
  "flaky" in prior session notes. Treating them as a single
  category hides the fact that one was genuinely contention-
  sensitive (recoverable by isolation) while the other was a real
  code-path break (not recoverable by retrying). Leaving them in
  the main suite made every run look red; moving both to a
  common "flaky" bucket would disguise the deterministic one.
- **Lesson:** Split disposition by symptom, not by label:
  - **Passes alone, fails under parallelism** → move to a
    dedicated collection marked
    `[CollectionDefinition("...", DisableParallelization = true)]`.
    The collection still runs, still gates, but no other
    collection runs in parallel with it. Carry a
    `TODO(tracked):` comment pointing at the proper root-cause
    investigation.
  - **Fails deterministically even in isolation** → use
    `[Fact(Skip = "<precise rationale>")]`. Do not hide a real
    failure behind a "flaky" label — hiding green lies is worse
    than a visible skip, because future sessions lose the signal
    that a specific code path is broken.
- **Action for future sessions:** When a test fails, run it in
  isolation before deciding disposition. If isolated-pass ≠
  suite-pass, it goes in the flaky collection. If isolated-fail,
  it gets `[Skip]` with a full rationale. See
  `tests/AcmeCoffeeOnline.Playwright/Infrastructure/FlakyPlaywrightCollection.cs`
  for the convention.

## Mechanical-Gate Task Types Calibrate To Sonnet-Cost For Adequate Quality

- **Context:** Session 10's `uat-coverage-review` gate — 34
  per-Review-item Step-N = Step-N correspondence checks between
  the generated UAT checklist and
  `SecurityDashboardUatCoverageTests.cs`. The router's
  `routing.task_type_overrides` pinned this task type to Opus.
- **Failure or friction:** Opus at high effort on a tiny per-item
  prompt (~2–4 KB in, one-word verdict out) is overkill for
  mechanical pattern matching with placeholder substitution. The
  full Opus-gate cost would have been ~$8 on this 34-item
  checklist (~$0.20–$0.30 per item), plus auto-verifier cost.
  Moreover, the orchestrator is itself Opus — so an Opus-pinned
  route is paying Anthropic to re-summon another instance of the
  current judgment.
- **Lesson:** Before pinning a task type to Opus via
  `routing.task_type_overrides`, distinguish mechanical
  correspondence from synthesis:
  - **Mechanical correspondence** (gate checks, schema
    validation, step-by-step parity, redaction audits) → Sonnet
    with cross-provider verify covers it. Cheap-model risk is
    false-VERIFIED; mitigate with explicit prompt instruction
    ("prefer false-positive ISSUES_FOUND over false-negative
    VERIFIED") and keep auto-verify on.
  - **Synthesis** (plan generation, spec expansion, novel test
    authoring) → Opus earns its cost.
  Calibration evidence: Sonnet ran the 34-item gate across three
  passes for a total of $0.48, produced the same quality
  verdicts as Opus would have (empirically, the mechanical
  critiques were correct and the four false-positives were
  obviously fussy at human review), and reached 34/34 VERIFIED.
- **Action for future sessions:** When a task type is paired
  (generator + gate), apply the split: pin the synthesis half to
  Opus via `task_type_overrides`, let the mechanical half resolve
  via the router's cost-tier picker (or pin it to Sonnet
  explicitly). The paired `uat-plan-generation`
  (synthesis → Opus) and `uat-coverage-review`
  (mechanical → Sonnet) is the canonical example.
