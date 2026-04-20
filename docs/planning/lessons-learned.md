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
- **Action for future sessions:** When generating a UAT checklist,
  in the same session also add a `*UatCoverageTests.cs` file under
  `tests/AcmeCoffeeOnline.Playwright/` with one test per Review item.
  Each test (a) signs in as the named persona via
  `PlaywrightFixture.SeededSignInAsync`, (b) executes the `HumanAction`
  steps with the exact parameters the human will use, and (c) asserts
  the objective portion of `Expectation` — route reached, required
  controls present/enabled, expected persist/refresh behavior observed,
  no unhandled circuit exception. Treat a checklist shipped without
  this coverage as a session-closeout defect. Promoted as a Principle
  under Human UAT Philosophy and as a Convention under Human UAT And
  Checklist Editor on 2026-04-19 (same-day promotion after a single
  sharp context because the cost of the missing discipline is high and
  the rule is unambiguous).

## `route(task_type=documentation)` Wraps Content In A Code-Documentation Template

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
