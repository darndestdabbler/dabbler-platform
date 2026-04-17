# Lessons Learned

> **Purpose:** Capture durable tactics, failure patterns, and workflow lessons
> that should influence future sessions before the same mistake repeats.
> **Status:** Start small in one file. If this grows too large, split it into a
> `lessons-learned/` folder with a short index and topic files.

Review this file before every AI-led session.

Unlike `guiding-principles.md`, which captures broad cross-project rules, this
file is for practical lessons learned from actual execution in this repo.

## How To Use This File

- Read it before starting a new session.
- Add entries when a failure, surprise, or avoidable friction reveals a better
  repeatable strategy.
- Prefer short, action-oriented lessons over long retrospectives.
- When a lesson becomes broad enough to guide future projects, ask the human
  whether it should also be promoted into `guiding-principles.md`.

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
