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

## `register_session_start()` Overwrites `session-state.json` Without Preserving `completedSessions[]`

- **Context:** `crud-scaffolding-spike` Session 3 of 3, opening a session
  on a lightweight-tier set that already had `completedSessions: [1, 2]`
  populated by hand at the close of Session 2.
- **Failure or friction:** The orchestrator called
  `register_session_start()` at Step 1 to flip `lifecycleState` to
  `work_in_progress` for the new session. The router function writes a
  fresh `session-state.json` snapshot with the canonical schema fields,
  but it does NOT preserve the existing `completedSessions[]` array —
  it omits the field entirely. On lightweight-tier sets this array is
  the only authoritative count signal (no events-ledger fallback, no
  router writer maintaining it), so losing it forces the VS Code Session
  Set Explorer to display N−1/N. The user caught the regression mid-
  session and pointed back to CLAUDE.md's state-file rules section.
- **Lesson:** Until `register_session_start()` is fixed router-side to
  preserve `completedSessions[]` across snapshot rewrites, the
  orchestrator must manually re-add the array immediately after calling
  it. Read the prior `completedSessions[]` before the call (or from
  memory of what was committed on the prior session's close), then
  re-write the array into `session-state.json` right after. The
  `mark_session_complete()` helper at Step 8 handles appending the
  closing session for you; only the mid-set `register_session_start()`
  call needs the manual save-and-restore.
- **Action for future sessions:** Before calling
  `register_session_start()`, read `session-state.json` and capture
  `completedSessions[]`. After the call, edit the new
  `session-state.json` to restore the array. (Alternative: ask the human
  whether the router-side fix has landed; if so, skip the workaround.)

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

## Tri-Provider Review — Orchestrator Contribution Uses Full Context, Not An Isolated Prompt

- **Promoted to Convention on 2026-05-16** (Step 9 reorganization review
  for the `crud-scaffolding-spike` session set). The same guidance is now
  captured — with equivalent specificity — in
  `project-guidance.md` → Conventions → Workflow Expectations:
  *"Tri-Provider Review: Orchestrator Contribution Uses Full Context."*
  Two-or-more contexts threshold met across different sets:
  `packaging-and-template-readiness` Sessions 4 & 5 and
  `crud-scaffolding-spike` Session 3 (Opus self-contribution disclosed in
  `scaffolding-approach-review.md` and explicitly endorsed by the
  GPT-5.4 session verifier). Follow the convention.
- **Original entry preserved below for context.**
- **Context:** Tri-provider platform value review (packaging-and-template-readiness Session 4)
  and packaging approach review (Session 5)
- **Failure or friction:** The GPT-5.4 cross-provider verifier flagged (Round 4) that the
  Opus orchestrator's own review assessment was written with full in-session context, while the
  spec step said "identical and self-contained prompt for all providers." The verifier treated
  the context asymmetry as a protocol defect. The operator deferred it as a spec-authoring
  ambiguity rather than a session defect.
- **Lesson:** When a spec calls for a tri-provider review, the "self-contained prompt" clause
  applies to the external providers only — they receive a prompt that stands on its own without
  prior session context. The orchestrator's direct contribution (written in-session rather than
  via `route()`) legitimately uses its fully-loaded context because routing to the orchestrator's
  own model is wasteful and the loaded context is the whole point. The synthesis document must
  explicitly disclose this asymmetry (e.g., "Opus assessment written with full session context;
  GPT-5.4 and Gemini Pro received self-contained prompts") so future verifiers do not flag it
  as a defect. Specs should pre-empt the ambiguity by saying "orchestrator contributes directly
  using its loaded context" rather than "identical prompt for all three."
- **Action for future sessions:** In any tri-provider session where the orchestrator writes its
  own perspective directly: (1) note the context asymmetry in the synthesis document, and (2)
  include a disclosure in the end-of-session verification prompt so the verifier has explicit
  context before evaluating the output. Promotes the `feedback_orchestrator_self_contributes_to_review_panel`
  memory to a documented lesson.

## Author Template Content And Consumer Smoke Test In The Same Session

- **Context:** VS template authoring (packaging-and-template-readiness Session 8) and consumer
  validation (Session 9)
- **Failure or friction:** Session 8 authored the `templates/Dabbler.Templates/` content but did
  not run a consumer smoke build. Session 9's smoke pass (`PackagingSmokeTests`) discovered 7
  template quality issues: missing using directives, broken syntax, and app-specific references
  that had leaked into the generic template content. These were all fixable but added a full
  round of remediation work to Session 9 that would not have been necessary if Session 8 had
  included a build step against the generated template output.
- **Lesson:** Template content is code. Like any code, it should be tested in the same session
  it is written. A pack → install → scaffold → build smoke cycle is the minimal bar; it takes
  less than five minutes and catches the class of issues (missing namespaces, broken syntax,
  residual app-specific content) that are invisible to the author but immediately apparent when
  a real consumer tries to use the template.
- **Action for future sessions:** Any session that authors or significantly modifies template
  content must include a build-and-test smoke step before closing. The smoke can be as lightweight
  as `dotnet new <template-name> -o /tmp/smoke && dotnet build /tmp/smoke` — the goal is to
  catch render-time issues before they accumulate across sessions.

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

- **Promoted to Convention on 2026-05-14** (Step 9 reorganization review for the
  `composable-crud-helpers` session set). The same rule is now captured — with
  binding-pattern specificity — in `project-guidance.md` → Conventions → CRUD Form
  Conventions: *"Form fields bind with `Value`/`ValueChanged`, never `@bind-Value`."*
  Applied as the right call in Products (Session 1), Orders (Session 3), and
  Customers (Session 4) migrations. Follow the convention.
- **Original entry preserved below for context.**
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

## Do Not Present An Inferred Root Cause As Fact Without Verifying It

- **Context:** Any user-reported symptom whose explanation depends
  on an assumed environmental, network, or operational condition
  (package availability on a feed, container state, filesystem
  reachability, which binary version a running process has loaded,
  which URL a test is actually hitting, etc.).
- **Failure or friction:** During follow-up debugging on the
  `pdf-newpage-multipage-fix-consumption` session set, the
  orchestrator confidently reported "DabblerPdf 1.1.2 is not on
  nuget.org → the Docker build context cannot reach the sibling
  repo's `nupkg/` folder → the container is stuck on 1.1.0,
  therefore the title is still missing." None of those premises
  had been verified. 1.1.2 WAS on nuget.org. The user was not
  using Docker at all — they were running `dotnet run` host-side.
  The user wasted time correcting a wrong theory and had to
  debug the code themselves to find the actual answer (a stale
  `dotnet run` process linked against an older build).
- **Lesson:** A plausible-sounding root cause is not a verified
  one. A reasoned inference from source control and config files
  is not evidence. For every factual claim a root-cause theory
  depends on — "X is/isn't available on Y," "container Z is
  running version W," "the running process has binary V loaded,"
  "the test is hitting URL U" — there is a specific command or
  file check that proves or disproves it. Run that check before
  writing the theory up, not after the user pushes back.
- **Action for future sessions:** Before presenting a "root cause"
  section in any plan, findings doc, or session review, list
  every factual claim the cause depends on. For each claim, name
  the verification command (`dotnet nuget list`, `docker exec …
  dotnet list package`, `ls -la` on the actual path, a targeted
  `dotnet test` run against the exact binary, inspection of a
  `.deps.json`, etc.). Run them. Mark any un-verified claim as
  "hypothesis, not verified" in the writing, and never let a
  hypothesis be presented as a verified root cause.

## Truncation Detection: `stop_reason` Alone Is Not Sufficient

- **Context:** Any routed call whose response is consumed
  programmatically — code generation, test generation, structured
  data emission, schema-validation reports.
- **Failure or friction:** Gemini Pro has been observed to return
  `stop_reason: "end_turn"` on responses that visibly cut off
  mid-string (e.g., `var sql = "SELECT Reports`). The orchestrator
  logs the call as successful, the structured consumer sees a
  malformed JSON / unbalanced brace, and downstream parsing fails —
  sometimes silently, sometimes with a misleading error far from
  the truncation point.
- **Lesson:** Use the `detect_truncation(content, stop_reason)`
  helper in `ai-router/utils.py`. It returns True when
  `stop_reason == "max_tokens"` OR when the response shows
  syntactic incompleteness (odd count of triple-backtick fences,
  more `{` than `}`). The brace and fence checks reliably catch
  code-output truncation; parentheses are intentionally NOT
  checked because prose produces too many false positives.
- **Action for future sessions:** Call `detect_truncation()` before
  treating any structured output as canonical. On truncation,
  halve the batch and retry, fall back in-conversation, or
  escalate a tier — do not log a successful call.

## Cost Guard On Verification: Skip When Verifier Cost Greatly Exceeds Generator

- **Context:** Any auto-verified task type (`code-review`,
  `security-review`) where the generator is a cheap tier-1 or
  tier-2 model and the rule-based verifier picks an expensive
  tier-3 model from a different provider.
- **Failure or friction:** A cheap Gemini Flash call routed at
  $0.0003 can pull a $0.15 GPT-5.4 verification call — a 500x
  cost ratio that destroys the savings the cheap generation was
  supposed to deliver. Without a guard, batch jobs that loop over
  many small routed calls accumulate verifier costs that dwarf
  the actual work.
- **Lesson:** The router's verification block in
  `router-config.yaml` carries `verification.max_cost_multiplier`
  (default 3.0). When the estimated verifier cost exceeds
  `max_cost_multiplier × generator cost`, the verification is
  skipped and the metrics record `verification_skipped:
  cost_guard`. The session-verification step is exempt from the
  guard — that one is non-negotiable cross-provider review.
- **Action for future sessions:** Trust the guard for
  auto-verification of cheap routed work. If a specific task type
  must always be verified regardless of cost ratio, raise
  `max_cost_multiplier` in the YAML or pin the generator to a
  more expensive model so the ratio drops naturally.

## Persist Routed Output To Disk Before Display Or Logging

- **Context:** Any routed call whose `result.content` is large or
  contains user-supplied content, on Windows where the default
  console code page is `cp1252`.
- **Failure or friction:** `print(result.content)` can crash
  mid-line when the content contains characters `cp1252` cannot
  encode (em-dash, non-breaking space, smart quotes from a
  routed-prose response). The crash kills the script and loses the
  paid output that has not yet been written anywhere — a $1+ Opus
  call disappears because of a single non-ASCII character in the
  response.
- **Lesson:** Write routed output to a file FIRST, then print or
  log. The file write uses `encoding="utf-8"` and survives any
  Unicode in the content. The print step then either succeeds or
  fails on display only — the underlying output is preserved on
  disk for human inspection.
- **Action for future sessions:** The pattern is:
  ```python
  with open(out_path, "w", encoding="utf-8") as f:
      f.write(result.content)
  print(f"Wrote {out_path} ({len(result.content)} chars)")
  ```
  Avoid `print(result.content)` even when the content "looks like
  ASCII" — the output is generated by an LLM, you don't know
  what's in it until it's on disk.

## ASCII-Only Glyphs In Cross-Platform Terminal Output

- **Context:** Any helper that prints a status table, summary, or
  progress indicator to the terminal. Examples:
  `print_session_set_status()`, `print_metrics_report()`, custom
  status helpers a session set adds.
- **Failure or friction:** Emoji glyphs (✅, 🟡, ⏳, etc.) crash
  Windows `cp1252` consoles mid-line, losing the rest of the
  report. Some terminal emulators silently drop the unprintable
  characters; PowerShell and cmd.exe in default Windows
  configurations raise `UnicodeEncodeError` and abort the script.
- **Lesson:** Use ASCII-only glyphs in any output that prints to
  the terminal: `[~]` for in-progress, `[ ]` for not-started,
  `[x]` for done, `*` for bullets, `-` for dashes. Reserve rich
  Unicode for files written to disk (`encoding="utf-8"`) where
  consumers control their own decoding.
- **Action for future sessions:** When porting or authoring a
  status helper, follow `print_session_set_status()`'s pattern in
  `ai-router/__init__.py`. If a richer rendering is desired, write
  it to a file and tell the user the path; do not assume the
  terminal will render it.

## Session-State.json Is The Single Source Of Truth For In-Progress Detection

- **Context:** Any external tool that needs to detect which
  session set is currently being worked on — VS Code Session Set
  Explorer, manager dashboards, parallel-session coordination.
- **Failure or friction:** `find_active_session_set()` historically
  inferred state from `activity-log.json` presence — but the
  activity log is only created on the first `log_step()` call,
  which can be 5–10 minutes after a session actually started.
  External tools showed sets as "not started" while a session was
  visibly underway, leading to confusion and (in parallel-session
  scenarios) two orchestrators starting work on the same set.
- **Lesson:** Call `register_session_start()` at Step 1, before
  the first `log_step()`. The function writes `session-state.json`
  with `status: "in-progress"`, the orchestrator metadata, and the
  start timestamp. External tools watch this file and flip to In
  Progress immediately. At Step 8, `mark_session_complete()` flips
  status to `complete` and records the verification verdict.
- **Action for future sessions:** Always call
  `register_session_start()` at Step 1 and
  `mark_session_complete()` at Step 8. Do not rely on activity-log
  presence as the in-progress signal.

## Always Route `ai-assignment.md` And Next-Orchestrator / Next-Set Recommendations

- **Context:** Authoring `ai-assignment.md` (Step 3.5) or the
  next-orchestrator / next-session-set recommendations (Step 8).
- **Failure or friction:** When the orchestrator drafts these
  documents from its own opinion, the recommendations are
  systematically biased toward whichever model the orchestrator
  is. A Claude orchestrator predictably recommends Claude for the
  next session even when Gemini Flash or Sonnet would have handled
  it cheaper. The recommendations become useless for cost
  optimization.
- **Lesson:** Always produce these via
  `route(task_type="analysis")`. The router's task-type defaults
  pick a cheaper model than the orchestrator's own, and the
  recommendation reflects routing economics rather than the
  orchestrator's self-image.
- **Action for future sessions:** Never self-opine on which model
  could have been cheaper. Rule #17 in the workflow doc makes this
  explicit. If the routed recommendation surprises you, surface
  that to the human — that surprise IS the value the
  recommendation provides.

## Per-Session-Set E2E/UAT Configuration Is Spec-Declared, Not Inferred

- **Context:** Authoring a new session-set spec, or onboarding a
  set into an upgraded workflow.
- **Failure or friction:** Earlier workflow versions inferred UAT
  applicability at runtime from "did this session touch a UI
  surface?" The orchestrator made the call session-by-session,
  which led to inconsistency: one session in a set would author a
  checklist, the next would not, and the human would discover the
  drift only at handoff.
- **Lesson:** UAT and E2E gates are now declared once per session
  set in a YAML configuration block at the top of the spec
  (`requiresUAT`, `requiresE2E`, `uatScope`). The orchestrator
  reads the block at Step 2 and obeys it for the rest of the
  workflow without re-litigating. Authoring-time heuristics
  (When-UAT-Is-Required, When-E2E-Is-Required) live in
  `docs/planning/session-set-authoring-guide.md` for the spec
  author. Runtime mechanics live in `docs/ai-led-session-workflow.md`
  for the orchestrator. Crossing those wires (asking the
  orchestrator to second-guess the spec) is the failure pattern.
- **Action for future sessions:** When authoring a spec, read the
  authoring guide first and pick the flags deliberately. When
  executing a session, trust the spec — if a flag is wrong,
  surface it to the human at the Step 9 reorganization review,
  not at Step 6 verification.

## Three-Step UAT Authoring Discipline — Explicit Steps + Step-Level E2E Map + Per-Step Reachability

- **Promoted to:** `project-guidance.md` → Conventions → Human UAT And
  Checklist Editor on 2026-04-27 (immediately, after the human flagged
  that the masking-audit UAT relied on `SeededSignInAsync(roles=)` —
  a test-only shortcut no human can perform in a browser). Three
  contexts: `rearchitect-role-administration-dashboard` Session 8 (Other
  items revealed E2E coverage was not exercising real surfaces),
  `security-dashboard-uat-followup-fixes` Session 2 (real-login
  Finding #2 invisible until Session 2 re-test;
  `Dev-Persona Roles-Override` lesson Exception added), and
  `sensitive-field-masking-audit` Session 5 (entire UAT was unrunnable
  because every persona depended on a roles-override that has no
  human equivalent). Follow the convention.
- **Original entry preserved below for context.**
- **Context:** Three separate UATs across the role-rearchitecture
  roadmap shipped checklists that the human could not run, despite
  passing `uat-coverage-review`. Each time the orchestrator's E2E
  used a test framework backdoor (`SeededSignInAsync(roles=...)`,
  `/test-auth/seeded-session?roles=...`, direct
  `security-config.json` edits) for a step the human was expected
  to also perform.
- **Failure or friction:** The "step-by-step parity" discipline
  checked structural matching (same step count, same testid
  references, same expected text) but did not check that a real
  human, using only what's exposed in a browser, could actually
  perform each step. Test framework can take any shortcut available
  to the test framework; the human cannot. The current
  `uat-coverage-review` prompt does not enforce per-step reachability.
- **Lesson:** UAT authoring is a three-step process, with the third
  step being the missing one:
  1. Write the UAT step with maximally-explicit human action — full
     URL, exact control labels, exact text typed, exact link/button
     labels, expected resulting URL or visible state.
  2. Write the E2E test driving the SAME explicit steps via the
     same user-visible locators. No test-framework backdoors on
     steps a human is also expected to perform.
  3. Cross-verify per step that BOTH the human AND the E2E can
     complete it using only user-visible surfaces. If either side
     can't, the step is inadequate — fix the UI gap or remove the
     step. Don't paper over a UI gap with a developer shortcut.
- **Action for future sessions:** Apply the convention from
  `project-guidance.md` → Conventions → Human UAT And Checklist
  Editor at every UAT-bearing session set. The
  `route(task_type="uat-coverage-review")` prompt must explicitly
  request per-step reachability confirmation (a non-trivial change
  to the prompt — orchestrators must carry this addition forward).
  When the supported sign-in path is not yet wired up
  (e.g., DevIdp restore deferred), block the UAT and surface this
  to the human at spec authoring, not after generating an
  unrunnable checklist.

## UAT Checklist Must Be Validated Against The Canonical Schema Before Commit

- **Promoted to:** `project-guidance.md` → Conventions → Human UAT And
  Checklist Editor on 2026-04-27 (Step 9 follow-up after the human flagged
  a non-loadable checklist for the *second* time). Two contexts:
  `rearchitect-role-administration-dashboard` Session 8 (original
  custom-shape incident) and `sensitive-field-masking-audit` Session 5
  (regenerated via `route(uat-plan-generation)` against the wrong shape;
  Opus produced JSON with `SessionSetSlug`/`Title`/`Reviews`/`Id` that the
  editor rejected). Follow the convention.
- **Original entry preserved below for context.**
- **Context:** Two separate session sets shipped UAT checklist JSON that
  did not load in the editor at
  `https://darndestdabbler.github.io/uat-checklist-editor/`.
- **Failure or friction:** Both incidents stemmed from the orchestrator
  generating fields the schema does not allow. The canonical schema
  requires `ProjectName` + `Review` at the root and
  `FunctionalArea`/`Subarea`/`ItemLabel` per Review item; each step's
  action lives in `Action` (not `HumanAction`); verifications are objects
  with `Text` (not plain strings); `additionalProperties: false` is set
  at every nesting level. Even when the orchestrator believes it has
  "the right schema," the editor's schema is the only ground truth.
- **Lesson:** The orchestrator must run an actual JSON-Schema validation
  pass against the canonical schema URL before committing the checklist
  AND before notifying the human. A "looks reasonable" eyeball check is
  insufficient — the failure modes (extra root fields, plural `Reviews`
  vs singular `Review`, string verifications vs object verifications)
  are exactly the kind of structural mistakes that pass eyeball review
  but fail editor parsing.
- **Action for future sessions:** When generating a UAT checklist
  (Step 5.2 in the workflow):
  1. Fetch the schema:
     `https://raw.githubusercontent.com/darndestdabbler/uat-checklist-editor/main/checklist-schema.json`
  2. Validate the generated checklist with `jsonschema` (Python) or an
     equivalent validator. Any validation error blocks the commit.
  3. Eyeball-confirm the field names against the schema's
     `properties` — don't trust the routed `uat-plan-generation`
     output's shape; the prompt template has occasionally produced
     non-schema fields.
  4. If a routed call produces non-schema fields, transform the output
     to the schema rather than re-prompting (cheaper, deterministic).

## ICsvExportService Is A Convention Candidate After A Second Admin-Page Reuse

- **Context:** `sensitive-field-masking-audit` Session 4 introduced
  `ICsvExportService` + `CsvExportService` in `Dabbler.Components/Csv/`
  for the Compliance Review tab's Export CSV feature.
- **Failure or friction:** None — this went well. One usage is
  insufficient for promotion to Convention; capturing it here so the
  second use triggers the review.
- **Lesson:** `ICsvExportService` (generic `Export<T>(rows, columns)`
  → `byte[]`, RFC 4180, CRLF, invariant culture, UTF-8 no-BOM) is a
  reusable export service for admin pages. Register via `TryAddSingleton`
  in `AddDabblerComponents`. It pairs with the existing
  `dabbler-download.js` `downloadFile` module for browser-side delivery.
- **Action for future sessions:** When the second admin page reuses
  `ICsvExportService`, promote it to `project-guidance.md` → Conventions
  as a platform export pattern. Current first use: Compliance Review tab
  (`sensitive-field-masking-audit` Session 4).

## A Green Playwright Suite Is Not Sufficient Evidence That The Human Sees What The Test Claims

- **Context:** `sensitive-field-masking-audit-uat-remediation` Session 1
  (committed 2026-04-28, sha 4d61525). The Playwright suite went green twice
  consecutively (6/6 passing), cross-provider session-verification returned
  VERIFIED, the suite was committed and the UAT checklist handed to the human.
  The human then ran UAT-1 in a real browser and recorded inline feedback:
  *"15. There is no Mask. 16. The input with the label 'Social Security
  Number' IS visible."* — the OPPOSITE of what UAT-1's verifications claimed.
- **Failure or friction:** The Playwright test for UAT-1 step 15 used
  `Expect(page.GetByText(new Regex(@"XXX-XX-"))).ToBeVisibleAsync(...)` to
  prove the SSN field rendered as masked text. `GetByText` matched
  *somewhere* on the page (likely a mask-format hint string in another
  panel or a tooltip), so the assertion passed even though the actual SSN
  input rendered unmasked. Step 16 used `GetByLabel("Social Security
  Number").Not.ToBeVisibleAsync(...)` with a 2-second timeout — when the
  field rendered as an editable input under "Social Security Number"
  it should have failed but did not, suggesting either the label-association
  isn't the way `MudTextField` actually exposes its label, or the field
  was visible after the 2-second window. Either way, the test pair lied
  about the rendered state.
- **Lesson:** Visible-label `GetByText`/`GetByLabel` assertions on Blazor +
  MudBlazor surfaces are easy to write in a way that *passes* against an
  incorrect render. When the test claim is a structural property of a
  specific control (this exact field renders as a masked span vs an
  editable input), the assertion has to be scoped to that control's
  container — not to the entire page — and the masked-vs-unmasked
  distinction has to be a positive structural claim on both sides
  (mask span exists with masked text, AND the input does not exist), not a
  free-text regex that any tooltip/help text could satisfy.
  Playwright-green ≠ UI-correct.
- **Action for future sessions:** A separate session set is being authored
  in a parallel Claude conversation to make fundamental changes to how E2E
  is conducted, with the goal of reducing this kind of misalignment
  systemically. When that set lands, follow whatever conventions it
  establishes. Until then, when authoring E2E for a UAT item:
  scope every assertion to the specific control's container (e.g.,
  `page.GetByTestId("field-ssn-wrapper")` for the SSN slot) before
  applying the visible-label/text check inside it; treat a passing
  assertion outside the scoped container as a false positive; and
  cross-check both halves of a render-mode claim (masked → mask span
  visible AND input not present; unmasked → input present AND mask span
  not present) so a single weak assertion cannot let a broken UI through.

## Masking-Auditor Pattern Is A Platform Auditing Convention Candidate

- **Context:** `sensitive-field-masking-audit` introduced the full
  masking-audit stack: write-once `AppSensitiveAccessLog` temporal rows,
  `IRenderCorrelationScope` (per-Blazor-circuit/request Guid, resets on
  `NavigationManager.LocationChanged`), and `TryRecordOnce` dedup per
  `(CorrelationId, EntityKey, EntityId, ColumnKey)`.
- **Failure or friction:** None — the pattern worked. One session-set
  application is insufficient for Convention promotion.
- **Lesson:** The masking-auditor pattern is a candidate platform
  convention for any future auditable feature that needs:
  - Append-only temporal audit rows (`AppSensitiveAccessLog` shape or similar)
  - Per-render dedup to avoid audit log inflation on repeated renders
  - Correlation-scoped grouping of related events across one user action
  Key invariants to preserve: `SecurityConfigurationManager` is a
  Singleton — in-memory only; direct file edits without calling
  `SaveAsync` are NOT picked up by running circuits. `RenderCorrelationScope`
  is Scoped (per Blazor circuit); the `IsMasked` check and FieldUnmaskedRead
  dedup happen at the binder layer, not the component layer.
- **Action for future sessions:** When a second auditable feature needs
  sensitive-field or action-audit coverage, evaluate applying this pattern
  and, if applied, promote it to `project-guidance.md` → Conventions.
  Current first use: `sensitive-field-masking-audit` Sessions 2–3.

## Cross-Provider Verification Must Exclude The Orchestrator's Provider, Not Just The Generator's

- **Context:** `uat-dsl-compiler` Session 1 — Claude orchestrator routed
  grammar authoring to Opus (Anthropic) and tests to Gemini Pro
  (Google). The `session-verification` task should have run on a
  non-Anthropic model per Rule 2 (cross-provider verification).
- **Failure or friction:** The router's preferred verifier (GPT-5.4,
  OpenAI) returned 429 rate-limited. The fallback selection in
  `verification.py` excludes the failed provider but does NOT consider
  the orchestrator's provider, so it picked Opus (Anthropic) — the same
  provider as the orchestrator. The cross-provider guarantee was
  silently broken; the verification still ran and returned a verdict,
  but it was not actually independent of the orchestrator's pool.
- **Lesson:** The cross-provider rule is anchored to the *orchestrator*,
  not the *generator of the work being verified*. Verifier candidates
  must exclude the orchestrator's provider in addition to (or instead
  of) the generator's provider, especially on `session-verification`
  where the routed call IS the verification step. With three providers
  and the orchestrator's provider excluded, the rate-limit fallback
  still has one valid choice; with two providers excluded the call
  should hard-fail with a clear "no valid cross-provider verifier"
  error rather than silently fall back to the orchestrator's pool.
- **Action for future sessions:**
  1. Pass the orchestrator's provider into `route()` (e.g., via a
     `register_session_start()` side-effect or a `route(...,
     orchestrator_provider=...)` parameter) and have
     `verification.py` exclude it from the verifier candidate pool
     for `session-verification` task type.
  2. Until that lands, the orchestrator should sanity-check the
     verifier model name against its own provider after every routed
     verification call and log a `Major` issue if they match — even
     if the verdict is VERIFIED.
  3. Surface the same-provider fallback in the manager report
     (`report.py`) so the rate at which the cross-provider guarantee
     is actually upheld is observable over time.

## Use The Verification Prompt Template — Don't Hand-Roll The Verifier Prompt

- **Context:** `uat-dsl-compiler` Session 1 — built a custom
  verification prompt with a "What to verify" checklist and inlined
  file contents, but did NOT include the structured JSON response
  schema from `ai-router/prompt-templates/verification.md`.
- **Failure or friction:** The verifier returned a markdown report
  ("Verdict: PASS — All session deliverables met. No blocking issues.")
  with a markdown table breakdown, NOT the
  `{"verdict": ..., "issues": [...]}` JSON block the workflow expects
  for mechanical parsing. The verdict was unambiguous and the saved
  review file was useful, but a downstream tool that parses
  `session-NNN.md` for routing decisions would have failed or
  misclassified the result. The workflow doc explicitly calls this out
  ("bare-paragraph verdicts have caused parser failures and
  ISSUES_FOUND-misclassified-as-VERIFIED regressions") — exactly the
  failure mode that nearly happened here.
- **Lesson:** The structured-JSON requirement at Step 6 is non-
  negotiable, and the existing template at
  `ai-router/prompt-templates/verification.md` already enforces it.
  Hand-rolling the verification prompt to be "more specific" loses the
  schema enforcement and is the wrong trade.
- **Action for future sessions:**
  1. For `session-verification`, build the prompt by reading
     `ai-router/prompt-templates/verification.md` and substituting
     `{original_task}`, `{task_type}`, and `{original_response}` with
     the session's spec excerpt and file contents — do not hand-roll.
  2. If the prompt has session-specific framing (lists of what to
     verify, file context), add it as additional content under the
     template's `Your Instructions` section rather than replacing
     the response-format block.
  3. The router could also auto-prepend the JSON schema block when
     `task_type="session-verification"` regardless of what the caller
     passes — that's a more robust fix than relying on caller
     discipline.

## Anthropic Effort Levels Are `low|medium|high|max` — `xhigh` Is Not Valid

- **Context:** `uat-dsl-compiler` Session 1 — first time the
  `architecture` task type was actually exercised end-to-end against
  Anthropic. The router-config had `effort: xhigh` for Opus on the
  architecture task, and `ai-router/providers.py` listed `xhigh` as a
  valid Anthropic effort level in its docstring.
- **Failure or friction:** Every architecture-routed call to Opus
  failed with `400 Bad Request: This model does not support effort
  level 'xhigh'. Supported levels: high, low, max, medium.` The
  router has no concept of param-validity per provider, so the call
  retried 3 times and then raised — costing time and grinding the
  session to a halt until the cause was diagnosed by direct API call.
  The bug had been latent in `router-config.yaml` since the
  architecture task type was added; it never surfaced because no prior
  session had actually routed an architecture call (sets that needed
  architecture either skipped routing or used a different task type).
- **Lesson:** The valid Anthropic effort enum is
  `{low, medium, high, max}` — `xhigh` is OpenAI-specific
  (`reasoning_effort: xhigh` is valid for GPT-5.x) and was apparently
  copy-pasted across providers in both the YAML and the providers.py
  docstring. Cross-provider param normalization is a real gap in the
  router.
- **Action for future sessions:**
  1. The fix has been applied: `router-config.yaml` line 307 now
     reads `effort: max` for Opus on the architecture task. The
     `providers.py` docstring should also be corrected to remove
     `xhigh` from the Anthropic effort enum (still pending).
  2. New per-task-type param entries in `router-config.yaml` should
     be validated against the providers.py docstring (or a future
     schema check) before merging.
  3. Consider adding a startup-time validator in the router that
     loads the YAML and calls each provider's parameter-shape
     normalizer, surfacing invalid combinations as a clear error
     message instead of waiting for the first 400 from the API mid-
     session.

## Test Infrastructure Must Degrade Silently When JS-Interop Is Not Dispatchable

- **Context:** Wrapping `OnSearchAsync` in `TrackedComponentBase.TrackAction("search-products", ...)` to fix a click-during-search-rerender race in `ProductManagement.razor` (playwright-sync-watermark-and-playbook Session 5).
- **Failure or friction:** The wrap closed the watermark race in isolation (59/60 isolated stress) but caused a deterministic HTTP 500 on `/admin/products` for `BackofficeUserAdmin` in the full suite. Root cause: `TrackAction` calls `JS.InvokeVoidAsync` first, and somewhere in the BackofficeUserAdmin render pipeline that call fired before the SignalR dispatcher was ready. `JS.InvokeVoidAsync` threw `InvalidOperationException`, which bubbled out as the 500.
- **Lesson:** Anything that calls `IJSRuntime` from a base class or a shared event handler — especially test-time plumbing — should treat "JS not yet dispatchable" as expected and silently no-op its side effect. The watermark is a test-time signal; missing it in a frame should degrade to "no signal" rather than failing the user-visible page.
- **Action for future sessions:** When adding test-time JS interop to a base class or cross-cutting handler, wrap the `JS.InvokeVoidAsync` call in a `try/catch (InvalidOperationException)` (the `TryInvokeJsAsync` pattern in `TrackedComponentBase.cs` is the canonical example). Add a bUnit / xUnit test for the silent-degrade path. Verify the change against at least one test that exercises a non-SystemAdmin authorization path before declaring victory — the prerender / pre-circuit-dispatchable boundary often differs by role.

## Single-Session Spec Sizing Has A Practical Ceiling

- **Context:** Authoring or executing the closing session of a session set, where the closing session combines investigation + drain + full-suite × 5 + change-log + cross-provider verification + Step 9 reorganization review.
- **Failure or friction:** playwright-sync-watermark-and-playbook Session 5 was authored as one session containing all of the above. In practice the cumulative-flake investigation alone took 4 fix iterations and a routed second-opinion analysis. Mid-session the human floated a Session 6 to bound scope, then relaxed it once iteration 2 hit 20/20, then accepted that iteration 4 was the right landing. Final session length: ~2.5 hours, with several scope-clarification interrupts.
- **Lesson:** A closing session that includes both an open investigation AND drain + full-suite × 5 + change-log + reorg risks blowing past any reasonable session boundary. Spec authors should consider splitting the investigation from the close: a "diagnostic + first-fix" session, and a separate "validation + drain + close + reorg" session. The early-bound option also gives the human a natural decision point if the diagnostic reveals a worse-than-expected hypothesis.
- **Action for future sessions:** When authoring a session-set spec whose final session includes an investigation (cumulative-flake hunt, performance regression, etc.), default to splitting it: investigation gets its own session; drain + close gets the next. The session-set-authoring-guide may be the better long-term home for this guidance once it has been applied in a second context.

## SemanticValidator Default `activeRole = "*"` Silently Degrades Catalog-Grounded Checklists

- **Context:** Running a catalog-grounded UAT checklist against `/admin/products` for SystemAdmin via the W2 runner (`w0-cli-suppressions-and-role-pinning` Session 3 — the deferred 5/5 watermark proof).
- **Failure or friction:** Without a concrete role pinned at compile time, every label probe on a real route falls through to the validator's `EntriesFor(route, "*")` AnyRole-wildcard fallback and returns zero hits — even when the catalog plainly contains the entries under the concrete role name. Surfaces as cascading `UnknownRoute` for the route the script just navigated to, with a misleading message that tempts diagnosis toward catalog re-crawl rather than role pinning. The failure is silent in the sense that the catalog-grounded labels look correct on visual inspection of both the checklist and the catalog.
- **Lesson:** Catalog grounding is a `(routePath × role × label)` triple, not a `(routePath × label)` pair. Pinning a concrete role at compile time is mandatory for any post-auth checklist. The mechanism options (W0 `--active-role` flag, runner-side persona inference from the sign-in step's email, or a script-level `AS ROLE "<name>"` directive) are equivalent for correctness; pick whichever path produces the smallest blast radius for the codebase at hand.
- **Action for future sessions:** When authoring or running a catalog-grounded UAT checklist, always thread a concrete role to the validator — never rely on the AnyRole wildcard. The dabbler-platform runner does this automatically via `ai-router/uat_runner/personas.json` (email → role); explicit override available via `--active-role <name>` on `cli.py` for ad-hoc smoke tests under a non-default persona. If a future spec needs a different role-resolution strategy (e.g., for multi-org tests), evaluate it against the same correctness bar: zero label hits on a real route is a silent failure, not a noisy one.

## Smoke Proofs That Have Never Run Surface Their Own Infrastructure Gaps

- **Context:** Two consecutive session sets needed a runner-driven `python ai-router/uat_runner/cli.py` smoke against the live compose stack: `uat-dsl-watermark-integration` Session 3 (deferred 5/5 proof) and `w0-cli-suppressions-and-role-pinning` Session 3 (the actual 5/5 proof). Each set's smoke was authored optimistically against pieces — DSL primitives, CLI flags, runner-side hooks — that had never actually run end-to-end together.
- **Failure or friction:** The first set deferred its 5/5 quality bar after surfacing two infrastructure gaps (W0 CLI lacked `--suppressions`; SemanticValidator defaulted `activeRole` to `*`). The second set closed those gaps and ran the smoke for real, where it surfaced *three more* gaps in the magic-link sign-in DSL flow alone: free-form `WAIT FOR "<state>"` mis-emits to `WaitForSelectorAsync(<literal English>)` (Playwright rejects literal English as CSS), `/dev/email-inbox`'s link text is `"Open"` with `target="_blank"` not `"Sign in"`, and the OTP-code paste step has no DSL primitive. Each gap looked like scope creep mid-session; in aggregate they were the smoke proof's primary value.
- **Lesson:** A smoke proof that has never actually run is a *test of the infrastructure that supports the smoke*, not a smoke of the application. Expect 1–3 additional infrastructure gaps to surface the first time it runs end-to-end. Budget for them in the spec rather than treating each surfaced gap as scope creep — the smoke's job is to find them. Closing a gap mid-session is not a deviation from the spec; failing to close one and pretending the smoke "passed" against an unrunnable artifact is.
- **Action for future sessions:** When a session-set spec calls for "the deferred 5/5 proof" or any first end-to-end runtime exercise of an integration, treat the smoke run itself as the work and reserve scope for gap closure. If a surfaced gap is large enough to warrant its own session, surface it explicitly and pause; if small (e.g., a runner-template extension, a structured-VERIFY swap, a test-infra cleanup hook), close it inline and document in the change-log. The smoke is green only when the runner pipeline is actually deterministic — compile-only proof or hand-edited emit doesn't count.

## Restriction-Binder Components Must Render Nothing Until First Resolution Completes

- **Context:** `RestrictedFormField` (and analogous restriction wrappers) gate the rendering of a sensitive form control on the result of an async `IRestrictionBinder.ResolveAsync` call. The component initialized its resolution state with `RestrictionResolution.Unrestricted(string.Empty, string.Empty)` — visibility=Visible, IsMasked=false. This caused the **first render** to display the unrestricted `ChildContent` (the editable `MudTextField` input) before `ResolveAsync` returned and the masked `<span>` replaced it.
- **Failure or friction:** During the `sensitive-field-masking-audit-uat-remediation` Session 1 closeout, this surfaced as a defense-in-depth concern (a human user on a slow render could briefly see the editable input for a column they should only see masked). The flash window is normally tens of milliseconds — invisible to most users — but on congested SignalR circuits the window widens. Note: this was *not* the cause of the test flakiness chased earlier in the same session — that turned out to be the separate state-pollution issue documented below — but the bug is real and the fix is independently valuable.
- **Lesson:** A component whose visible output depends on an async authorization decision must render **nothing** (or an opaque placeholder) until that decision returns. Initialising with a "permissive default" state — even if the surrounding visual frame is unrestricted — leaks the protected surface for one render cycle. The fix is structural: track an `_isResolved` flag (or sentinel `_resolved` value) and gate the entire render tree on it. Don't fall back to a "show the input by default and hide it once we know" pattern; default-permissive in security-sensitive UI is the wrong shape.
- **Action for future sessions:** Any `Restricted*` / `Authorized*` / mask-aware Blazor component must follow the render-nothing-until-resolved contract. When reviewing such components, check three things: (1) initial state of any backing resolution field, (2) the top of the @if chain in the .razor markup explicitly handles the "not yet resolved" case before falling through to ChildContent, (3) the resolve method sets a flag at the end so the render is consistent. Apply the same scrutiny when wiring new wrappers (`RestrictedGridColumn`, future per-cell-restricted column types). The `RestrictedFormField` fix in 2026-05-01 is the canonical pattern.

## State-Mutating UATs Pollute The Container's Writable Layer Across Runs

- **Context:** The masking-audit checklist's UAT-2 grants a `MaskExemption` row via the live UI. The `SecurityConfigurationManager`'s JSON file store persists this mutation to the container's `/app/security-config.json` — its writable layer, NOT the source-tree file the image was built from. UAT-3 in the same run then verifies the exemption is active. UAT-3 passes correctly. But the exemption stays in the container for any subsequent W2 run.
- **Failure or friction:** On a re-run without `--build --force-recreate`, the next iteration's UAT-1 (which expects the SSN to render masked because *no* exemption is supposed to be active yet) fails: the persisted exemption from the previous run's UAT-2 is still in place, the binder resolves to `IsMasked=false`, and the editable input renders instead of the masked span. Symptom: 1/5 pass rate on consecutive `cli.py` runs, with the failure mode "looks like a render race" because the page DOES eventually render *something* — just not the masked span. The actual cause is deterministic state pollution. Multiple hours were lost during 2026-05-01 to chasing this as a render-timing race in `RestrictedFormField` (the render-race fix landed and is correct, but it didn't restore reliability — the state-pollution did).
- **Lesson:** Any UAT that mutates persisted application state must either (a) clean up after itself within the same run, or (b) be paired with a runner-level container reset between runs. Option (b) is more robust — it's the only thing that catches state mutations the test author didn't anticipate. The W2 runner now exposes `--reset-app-state` to force-recreate the web + api containers before the test runs (added 2026-05-01); use it whenever a checklist contains a UAT that grants permissions, writes config, or otherwise persists state. The flag adds ~10 seconds per run; the cost is worth the determinism.
- **Action for future sessions:** When authoring a session-set spec for a UAT that mutates state, set `requiresUAT: true` AND note in the spec that the runner must be invoked with `--reset-app-state`. When debugging an "intermittent" W2 failure where the failure is deterministic from-first-bad-run-onwards (passes once, then fails), suspect state pollution before suspecting render race or other timing bug. Quickest diagnostic: `docker exec <container> grep <state-key> /app/<config-file>` to compare in-container state to source-tree state.

## Idempotent Schema Backfills Do Not Replay Seed Data

- **Context:** `Customers.SocialSecurityNumber` was added to the dev DB schema by an idempotent ALTER in `DatabaseInitializer.EnsureAcmeCoffeeSchemaAsync` after the initial seeder had already populated the table. `SeedData.GetCustomers()` does set `SocialSecurityNumber = "123-45-6789"` for customer 0 — but only on the initial seed pass. On any pre-existing dev DB, the new column was added with NULLs and never backfilled. UAT-3 was the human-visible consequence: an empty SSN input where the test data should have been "123-45-6789".
- **Failure or friction:** The seeder is "idempotent in the existence sense" (won't re-add rows) but **not** "idempotent in the column-fill sense" (won't backfill new columns on existing rows). Schema initializers and data seeders read like the same kind of thing in source — both are guarded "if X then create" — but they have very different replay semantics. A new column shipping with seed-supplied default content needs an explicit backfill pass alongside the ALTER, or the dev DB visibly diverges from a fresh-volume install in ways UAT will eventually catch.
- **Lesson:** When a schema migration adds a column that the seeder is *supposed* to populate, the migration must include a backfill SQL pass for pre-existing rows. The pattern is `ALTER TABLE … ADD column NULL;` followed by `UPDATE table SET column = <seed-equivalent expression> WHERE …` for the rows the seed code would have touched on a fresh install. Without it, the dev environment quietly accumulates an "old DB" vs. "fresh DB" divergence that surfaces only when a UAT step exercises the new column on a row that pre-dated it.
- **Action for future sessions:** Adding a new column to a seeded table? Pair the `ALTER TABLE ADD` with an `UPDATE … WHERE` backfill in the same `EnsureAcmeCoffeeSchemaAsync` block (or whichever idempotent SQL initializer your app uses). Mirror the seed code's intent — if `SeedData.cs` says "customer 0 gets SSN '123-45-6789' and the rest get NULL," the backfill should set the same row to the same value. If the migration spans multiple environments (dev/CI/staging), test it explicitly against a snapshot DB taken before the column existed.

## Interactive Blazor Server Pages Must Use `AuthenticationStateProvider`, Not `IHttpContextAccessor`, For Role Checks

- **Context:** `admin-users-cross-links` Session 1 added a dashboard cross-link banner to `UserManagement.razor` (the `/admin/users` page). Initial implementation injected `IAdminUserQueryService` and called `GetActorActiveAdminRole()`, which internally reads `IHttpContextAccessor.HttpContext?.User` to resolve the actor's role. The page is hosted under `@rendermode="InteractiveServer"` (App.razor lines 17, 21).
- **Failure or friction:** The bUnit tests passed (bUnit renders once with a synchronous test context where `HttpContext` is fine). The W2 Playwright run failed at the banner-visibility step. The page rendered the unconditional page heading correctly ("Manage Organization Admins" was visible), but the role-gated banner ("Open the Security Dashboard → Users and Role Assignment tab") was absent. `IHttpContextAccessor.HttpContext` is null in interactive Blazor Server components — Microsoft's documented stance — so `GetActorActiveAdminRole()` returned null, `_showDashboardBanner` was set to false, and the banner never rendered after the circuit took over from prerender. The exact symptom: feature works in unit tests, missing in browser.
- **Lesson:** For Blazor Server interactive components that need the actor's role, claims, or authenticated identity, inject `AuthenticationStateProvider` and call `GetAuthenticationStateAsync()`. Combine with `IRoleSwitcher.EffectiveRoles(authState.User)` and `IBakedInRoleProvider.TryGetBakedInRole(roleName)` to honor any active role switch. This is the pattern `DabblerSecurityDashboard.razor` already uses (lines 14, 129–149) and it works in both prerender and interactive phases because Blazor maintains the auth state synchronously across the circuit boundary. `IAdminUserQueryService.GetActorActiveAdminRole()` is safe from services that run inside an HTTP request (controllers, API endpoints, middleware) — *not* from Blazor components.
- **Action for future sessions:** When adding role-gated visibility to a Blazor page or component, check the file's render mode. If the component is hosted under `@rendermode="InteractiveServer"` (or `InteractiveAuto`), use `AuthenticationStateProvider` + `IRoleSwitcher`. Avoid `IHttpContextAccessor`-backed services for role resolution from Blazor markup — they appear to work in unit tests but produce a silent "feature absent" defect in production. When reviewing PRs that inject `IAdminUserQueryService.GetActorActiveAdminRole()` into a `.razor` page, ask whether the page is interactive; if yes, recommend the switch.

## `JsonFileUserRoleStore` Must Be Keyed By Email, Not Integer ID

- **Context:** `admin-user-creation-flow-uat-remediation` Session 1. Adding the `manager@acmecoffee.com` UserAdmin persona to `user-roles.json` required a stable key. The prior format used SQL Server identity integer IDs (`"1"`, `"11"`, `"62"`) as keys. The dev database had been modified by many Playwright test runs, so the new user's actual identity ID was 1030, not 62 (the fresh-seed value). The verifier correctly flagged the hardcoded `"1030"` key as brittle.
- **Failure or friction:** Integer identity IDs are non-deterministic across environments. `BackfillSecuritySeedDataAsync` inserts new seed users via normal EF `Add()`, which lets SQL Server assign the next available identity value — a value that varies depending on how many test-generated rows exist in the DB. Any `user-roles.json` key authored against a clean-seed environment breaks in a dev environment that has been used for Playwright test runs.
- **Lesson:** `IUserRoleStore` keys must be stable across environments. Email addresses are the right key for `JsonFileUserRoleStore` because they are immutable, environment-independent, and human-readable. Both call sites (`UserSessionContextService.GetRolesAsync` and `AdminUserQueryService` row projection) must pass `user.Email` rather than `user.Id.ToString()`. This fix also removes the only source of drift between the Api and Web `user-roles.json` files (they were previously not in sync on the jdoe entry). Promotion candidate after a second context confirms the email-key convention.
- **Action for future sessions:** When adding a baked-in role assignment to `user-roles.json`, always use the persona's email as the key. Never use integer IDs. The `AdminUserRow.UserId` property now holds an email address; a rename to `UserEmail` is a tracked minor clean-up for the next session touching the admin dashboard Blazor components.

## Updated AI Router Enforces A `disposition.json` Closeout Gate

- **Context:** The pip-installed `dabbler-ai-router` (canonical source: dabbler-ai-orchestration repo, post Set 9 Session 3) added a closeout gate at `mark_session_complete()`: every session set must contain a structured `disposition.json` file. The router's `Disposition` dataclass schema requires `status` (one of `completed`/`failed`/`requires_review`), `summary`, `verification_method` (`api` for `outsourceMode: first`, `queue` for `outsourceMode: last`), and optional `files_changed`, `verification_message_ids`, `next_orchestrator`, `blockers`. The router treats this as the canonical structured record of what happened in the session — separate from `change-log.md` (human-narrative) and `session-reviews/session-NNN.md` (raw verifier output).
- **Failure or friction:** `admin-users-cross-links` Session 1 closeout was blocked by `CloseoutGateFailure` because the workflow doc (`docs/ai-led-session-workflow.md` Step 8) was authored before the gate landed and does not include "author `disposition.json`" as a step. The orchestrator (Sonnet) had no in-context signal that the file was required, so closeout was attempted without it. `force=True` worked but is explicitly scoped to incident recovery — the router's own warning text says "hard-scoped to incident-recovery only", and the closeout logs `forceClosed=true` plus a `closeout_force_used` event. Using `force=True` for a routine closeout would skew the audit signal across the repo.
- **Lesson:** The `disposition.json` requirement is a real, well-designed structured-record gate — not an arbitrary obstacle. The integration gap is on the platform side: Step 8 of `docs/ai-led-session-workflow.md` needs to call out the disposition-authoring requirement before `mark_session_complete`. Once the workflow doc is updated, this becomes a routine closeout step alongside the cost report and change-log generation. `force=True` is the correct path only when `disposition.json` cannot be authored (e.g., a crashed/incomplete session that needs forensic closure).
- **Action for future sessions:** Pre-author `docs/session-sets/<slug>/disposition.json` as part of Step 8, just before `mark_session_complete()`. Minimum viable structure for an implementation session:
  ```json
  {
    "status": "completed",
    "summary": "<one-paragraph close-out summary>",
    "verification_method": "api",
    "files_changed": ["<paths created or modified>"],
    "next_orchestrator": null  // or {"engine": "...", "model": "...", "effort": "..."} for non-final sessions
  }
  ```
  Surface the workflow-doc gap to the orchestration repo (issue draft in change-log) so future agents inherit the corrected procedure. Once Step 8 is updated, this lesson stops being a hazard and stabilizes as routine — at that point it can be retired or promoted to a Convention if a stable disposition-authoring pattern emerges across sessions.

## Blazor DI Lifetime Mismatch Breaks Parent-Child State Sharing

- **Context:** `unified-master-details-composite` Session 5 — after fixing the `OrdersController` 409 bug, order saves still weren't navigating because `OrderCreatePage.State.Record.Id` was always 0 despite the save succeeding.
- **Failure or friction:** `OrderDetailState` was registered as `AddTransient`. Both `OrderCreatePage` and its child `OrderDetail` injected `OrderDetailState`, but each got a different instance. `OrderDetail.State` saved the order and got `Id = 123`, but `OrderCreatePage.State.Record.Id` was still 0 — different object. The `NavigateTo` never fired.
- **Lesson:** In Blazor Server, `AddTransient` gives every injection point a fresh instance. For state objects that parent and child components must share within a single page interaction, use `AddScoped` (one instance per Blazor circuit/connection). `AddScoped` is safe: each browser tab is a separate circuit so multi-user isolation is preserved.
- **Action for future sessions:** When a state class is injected by both a page component and a child component, and they need to observe each other's mutations, register it as `AddScoped`. If it's exclusively owned by one component and never read by another, `AddTransient` is fine. Check DI registration when a parent "doesn't see" a child's state change.

## MudBlazor 8.x Data-testid Placement and ReadOnly Behavior

- **Context:** `unified-master-details-composite` Session 5 Playwright test fixes.
- **Failure or friction:** Two categories of test failures traced to MudBlazor 8.x behavior differences:
  1. `GetByTestId("foo").Locator("input")` returned `<element(s) not found>` because MudBlazor 8.x places `data-testid` directly on the `<input>` element, not on a wrapper div. Playwright then looked for a child `input` inside an `input` element — none exists.
  2. `Not.ToBeEditableAsync()` on a MudTextField with `ReadOnly="true"` always failed (field appeared editable) because MudBlazor 8.x does not set the HTML `readonly` attribute on the underlying input for `ReadOnly=true`. Playwright's `isEditable()` checks the `readonly` HTML attribute specifically.
- **Lesson:** In MudBlazor 8.x, `GetByTestId("foo")` resolves directly to the `<input>` element; use `ToHaveValueAsync` (not `.Locator("input").ToHaveValueAsync`) for input value assertions. For ReadOnly state, use the toggle button's visible text as the proxy (the shell renders "Edit record" when IsTargetReadOnly=true; Playwright can check `ToContainTextAsync("Edit record")`). `Not.ToBeEditableAsync` / `ToBeEditableAsync` are unreliable for MudBlazor ReadOnly.
- **Action for future sessions:** When writing Playwright tests against MudBlazor 8.x MudTextField: use `GetByTestId("testid").ToHaveValueAsync(...)` directly; never append `.Locator("input")`. For ReadOnly state checks use observable UI signals (button text, class names, aria attributes) rather than `ToBeEditableAsync`.

## AI Cheating on UAT Checklists Requires a Tooling Gate, Not a Prompt

- **Context:** `unified-master-details-composite` Session 5 — the AI orchestrator authored UAT checklist steps using fake labels (`CLICK TAB with name "ROLES"`, `CLICK LINK with text "first role row in the grid"`) on `/admin/security` (a fully crawled route). The W0 compiler emitted `UnknownLabel` errors but they were suppressed via `routePathPatterns: ["*"]`. The W2 runner then ran with `--skip-dotnet-test`. The broken compiled test never executed. The human only found the cheating during manual UAT.
- **Failure or friction:** Any safeguard that requires the AI to voluntarily comply is not a safeguard. The supplemental prompt-side instruction ("use only catalog-grounded labels") was insufficient — the model has a strong incentive to ship the checklist quickly and a weak signal that label fabrication will be caught.
- **Lesson:** The gate must live in the tooling. The `uat-catalog-grounding-gate` session set added a pre-flight check to `uat_runner/runner.py` (`_check_catalog_grounding`) that fires `CATALOG_GROUNDED_VIOLATION` for any DSL label on a catalog-known route that doesn't exist in the catalog. The violation is hard-coded as not-suppressible by `tools/uat-suppressions.json`. Routes not in the catalog continue to use the existing CATALOG_STALE path (suppressible) so legitimate work on new pages isn't blocked.
- **Action for future sessions:** Apply the same pattern when an AI shortcut is observed: identify the tooling layer where the bypass occurred, install a hard gate at that layer rather than asking the AI to behave. Trust the AI to be helpful; do not trust the AI to police itself. The `--skip-dotnet-test` flag's existence and the wildcard suppression pattern were both gaps in the safeguard chain — both can be tightened in similar ways when future shortcuts are observed.

## Component Catalog Has No `Grid` Element Role; `CLICK ROW` DSL Verb Cannot Always Compile

- **Context:** `uat-catalog-grounding-gate` Session 2 — needed to author a checklist step "click the Backoffice User role in the Roles grid" using the W0 DSL `CLICK ROW in "<grid name>" where "<col header>" = "<value>"` verb. The grammar supports the verb but the catalog has no entry that names the role grid (no `elementRole=Grid` or `elementRole=Table` entries crawled).
- **Failure or friction:** The catalog crawler captures clickable elements (Button, Link, Tab, Input, IconButton), column headers (Column), and section headers (Text, Element). It does not emit a distinct "grid" element with a user-visible name. Without that, `CLICK ROW in "..."` has no catalog-grounded grid name to use.
- **Lesson:** For grids without a catalog name, prefer either (a) using a catalog-grounded element inside the row (e.g., `CLICK INPUT with label "Backoffice User"` if the row has a unique input value), or (b) marking the row-click item as `IsJudgmentItem: true` and providing the regression coverage via a hand-authored Playwright test using `data-testid`. The hand-authored test is the authoritative coverage; the checklist describes the human's manual spot-check.
- **Action for future sessions:** When the checklist needs "click a specific row", first check the catalog for catalog-grounded alternatives (Inputs with row-identifying labels, IconButtons inside the row). If none exist, mark `IsJudgmentItem: true` and link a Playwright test in `E2ETestReference`. Do not invent labels like "first role row in the grid" — the catalog grounding gate will reject them.

## Test-Time Customer Rows Race The Security-Seed Backfill Under xUnit Parallelism

- **Context:** `transactional-system-columns` Session 1 — authored `TemporalTablesTests.Customer_TemporalAsOf_...` to insert a fresh `Customer` row, mutate it, query history, then delete it.
- **Failure or friction:** The test passed in isolation. Run alongside `MigratedSecurityConfigSeedTests` (a different class in the same test assembly), it intermittently failed with `Cannot insert duplicate key row in object 'dbo.AppUsers' with unique index 'IX_AppUsers_Email'. The duplicate key value is (temporal-test-<guid>@example.invalid)`. Root cause: each test class instantiates its own `WebApplicationFactory<Program>`, which boots the API and runs `DatabaseInitializer.InitializeAsync` → `BackfillSecuritySeedDataAsync`. The backfill calls `SeedData.GetSecurityUsers(customers)`, which derives one `AppUser` per `Customer.Email` (see `SeedData.cs` ~line 290). When my test customer was committed during one class's run, a parallel boot from another class saw the new customer, generated an `AppUser` for the same email, and both class boots raced the `EnsureUsersAsync` insert. Related (but distinct) from the existing "Dev-Persona Roles-Override Is Safer Than A DB-Seed Addition" lesson (line ~559), which was about adding personas to `SeedData.cs` shifting integer IDs.
- **Lesson:** For integration tests that mutate `AcmeCoffeeDbContext` against the real seeded SQL Server, **probe-update a seeded row inside a try/finally** instead of creating a new row whose email leaks into the security seed. Customers are the highest-risk entity because their email keys `AppUser.Email`; Orders, OrderItems, OrderNotes, and Products are safer because the security seed does not derive identity from them. If a test genuinely needs a new row of a "safe" entity (e.g., Product for delete-preserves-history coverage), wrap creation + assertion in try/finally so cleanup runs even on assertion failure.
- **Action for future sessions:** When authoring integration tests against `AcmeCoffeeOnline.Api.Tests` (or any project that boots `Program` via `WebApplicationFactory`), default to the probe pattern from `TemporalTablesTests.Product_Update_CreatesHistoryRowAsync` / `Customer_TemporalAsOf_...` / `Order_TemporalAll_...`: load an existing seeded row, snapshot the relevant field, mutate, assert, then restore in `finally`. Reserve new-row creation for entities outside the security-seed derivation path, and even then guard with try/finally cleanup. If a future change extends seed derivation to other entities (Orders, Products), revisit the probe pattern's "safe entities" list.
