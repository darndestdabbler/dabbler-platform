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
