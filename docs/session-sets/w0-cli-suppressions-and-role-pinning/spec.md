# W0 CLI Suppressions And Role Pinning Spec

> **Purpose:** Close the two W0 infrastructure gaps that prevented
> `uat-dsl-watermark-integration` Session 3 from exercising the spec's
> "5/5 PASS" quality bar against the live compose stack: (1) the W0 CLI
> does not yet expose a `--suppressions <path>` flag, so suppressed
> compiles cannot emit code; (2) the `SemanticValidator` defaults
> `activeRole` to `"*"` while catalog entries store concrete role names,
> causing every label probe on a real route to return zero hits even
> when the catalog plainly contains the entries.
> **Created:** 2026-04-29
> **Session Set:** `docs/session-sets/w0-cli-suppressions-and-role-pinning/`
> **Prerequisite:** `uat-dsl-watermark-integration` complete (delivers the
> watermark catalog flag, the `WAIT FOR ACTIONS QUIET` verb, and the
> updated `uat-script-author` prompt; this set unblocks the runtime
> proof those pieces could not deliver).
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Session Set Configuration

```yaml
requiresUAT: false
requiresE2E: false
uatScope: none
```

> Rationale: This set changes the W0 compiler/CLI and the UAT runner's
> persona handling — test infrastructure with no user-visible behavior
> change. Per the platform addendum, test-only changes and library
> refactors with no observable behavior change are `requiresUAT: false` /
> `requiresE2E: false`. Session 3's runner-driven smoke (5 iterations
> against `/admin/products`) is the set's primary quality bar; a fresh
> UAT checklist authored on a watermarked page must compile to E2E code
> and run deterministically through `python ai-router/uat_runner/cli.py`.

---

## Project Overview

### What this set delivers

1. **`--suppressions <path>` on the W0 CLI.** Both `validate` and
   `compile` modes accept the flag. When supplied, errors that match a
   suppression entry in the file are downgraded from `errors` to
   `warnings` in the JSON output. `compile` proceeds to emit code (and
   exits 0) when every remaining error is matched by a suppression;
   `validate` returns `valid: true` with a populated `warnings` array
   under the same condition. The suppressions file format is the
   existing schema at `tools/uat-suppressions.json` in `dabbler-platform`
   — this set does not change the schema, only the W0 CLI's ability to
   honor it natively.

2. **Validator role context.** Either (a) a script-level `AS ROLE
   "<name>"` directive at the top of a checklist, OR (b) runner-side
   persona detection that infers the role from the first sign-in step's
   email address and passes `activeRole` to `SemanticValidator`. The
   choice is decided in Session 2's read-step after a brief audit of
   which path makes the catalog grounding clearer end-to-end. The firm
   contract: by the end of Session 2, a checklist whose pre-auth flow
   signs in as `admin@acmecoffee.com` validates against
   `role=SystemAdmin` (not `role=*`) on every post-auth step.

3. **5/5 runner-driven smoke proof on `/admin/products`.** Re-author
   the smoke checklist that
   `uat-dsl-watermark-integration/smoke/uat-dsl-watermark-integration-uat-checklist.json`
   captured but could not run, point it at the running compose stack,
   and run it 5 times via `python ai-router/uat_runner/cli.py`. Pass
   bar: 5/5 deterministic. The emitted `*.cs` from the W0 compile must
   contain `await fixture.WaitForQuietAsync(page);` after each
   interaction step on `/admin/products`.

### What this set does not do

- Does not change the suppressions schema in
  `tools/uat-suppressions.json`. The W0 CLI learns to read the existing
  schema natively; the runner's existing parser stays in place as a
  fallback for already-shipped scripts.
- Does not migrate any existing UAT checklists. Closed sets stay
  grandfathered via the existing all-suppressed entry. New checklists
  authored after this set lands automatically pick up role pinning via
  whichever Session 2 mechanism wins.
- Does not change the watermark mechanism, the catalog crawler, the
  `WAIT FOR ACTIONS QUIET` verb, or the `uat-script-author` prompt.
  Those are the load-bearing primitives this set's runtime proof
  validates.
- Does not retire the existing `EmitWatermarkWaits` implicit-emission
  flag (the transitional debt Session 2 of `uat-dsl-watermark-integration`
  flagged). That cleanup is its own focused set; this one needs the
  flag in place to get to a green smoke.

---

## Design Commitments (Decided Up Front)

### Suppressions semantics

A suppression entry that matches a W0 error downgrades the error to a
warning. The compile step emits code if and only if all remaining
errors after downgrade are zero. The CLI's exit code reflects the
post-downgrade error count, not the pre-downgrade count, so CI gates
(and the runner) can rely on exit 0 = compile succeeded with zero
hard errors.

### Role context: directive vs. persona inference

Both options are viable. The Session 2 audit decides, but the firm
preferences:

- **Prefer `AS ROLE "<name>"` directive** if the audit shows that
  authors already think in terms of "I'm writing this as the
  SystemAdmin persona" before they write the first NAVIGATE TO. The
  directive is a single-line addition at the top of a checklist's
  Steps array (or at the Review level); it makes the role explicit in
  the artifact.
- **Prefer runner-side persona detection** if the audit shows that
  every checklist already has an unambiguous "sign in as
  `<email>`" step early in the flow, and the email-to-role mapping is
  already authoritative elsewhere (e.g., seeded user fixtures). The
  runner reads the email, looks up the role, passes it to the
  validator at compile time. No grammar change needed.

The decision goes in Session 2's activity log with the audit
reasoning. Whichever wins, the other becomes a tracked candidate for
a future set if it turns out to be needed too.

### Smoke target reuse

Session 3's smoke uses the exact checklist
`uat-dsl-watermark-integration-uat-checklist.json` already committed
in the prior set's `smoke/` folder, copied (not symlinked) into this
set's smoke folder so the prior set stays immutable. Any tweaks
needed to make it run cleanly (e.g., adjusting catalog labels that
shifted in a re-crawl, or adding the new role directive if Session 2
chose that path) land as a fresh authored checklist via routed
`uat-script-author`, NOT as hand-edits to the prior set's artifact.

---

## Cross-repo orchestration

This set spans `dabbler-platform` and `dabbler-uat-dsl`. Per the
established cross-repo convention (and the
`dabbler-uat-dsl/CLAUDE.md` instructions): the spec, activity log,
ai-assignment, session-reviews, and change-log all live in
`dabbler-platform` here. Code commits land in whichever repo the
session's work touches.

| Session | Authoritative repo for code | Touches sibling? |
|---|---|---|
| 1 | `dabbler-uat-dsl` | no |
| 2 | `dabbler-uat-dsl` (validator + grammar/visitor); possibly `dabbler-platform` (runner) | maybe — depends on Session 2's audit decision |
| 3 | `dabbler-platform` | no — uses Sessions 1+2's already-published W0 1.2.0 |

Sessions 1 and 2's commit messages must reference the
dabbler-platform spec by slug; Session 3 must reference both prior
sibling commit SHAs.

---

## Session Plan

### Session 1 of 3: W0 CLI `--suppressions` flag

> **Code work happens in `dabbler-uat-dsl`.** Spec/activity-log work
> in `dabbler-platform`.

**Steps:**

1. Read planning docs (`project-guidance.md`, `lessons-learned.md`,
   `session-set-authoring-guide.md`) and the
   `uat-dsl-watermark-integration` smoke-result + suppressions JSON for
   context.
2. Audit `dabbler-uat-dsl/src/Dabbler.Uat.Dsl.Cli/` to find the CLI
   argument parser and the validate/compile entry points. Audit
   `tools/uat-suppressions.json` (in dabbler-platform) for the
   schema the runner already reads.
3. Add `--suppressions <path>` to both `validate` and `compile`
   subcommands. When the flag is supplied, the CLI loads and parses
   the suppressions file, builds an in-memory matcher, and applies it
   to the W0 ValidationError list before reporting.
4. Update the JSON output: matched errors move from `errors` to a new
   `warnings` array (same shape as errors); the `valid: true/false`
   verdict and the `compile.success` flag respect post-downgrade counts
   only.
5. Add unit tests in `dabbler-uat-dsl/tests/` covering: no flag (no
   change in behavior), flag with matching suppression (downgrade),
   flag with non-matching suppression (still error), flag with
   wildcard match (`*` errorKind, `*` routePath).
6. Pack new local NuGet (1.2.0) for the CLI as well as Core +
   Emitters.Playwright.CSharp if any types changed (likely just the
   CLI; Core's ValidationError shape is unchanged).
7. `dotnet build && dotnet test` in dabbler-uat-dsl.
8. `route(task_type="session-verification", max_tier=2)` —
   cross-provider.
9. Commit (in `dabbler-uat-dsl`). Commit message:
   `Add --suppressions <path> to W0 CLI validate/compile`.
   Reference the dabbler-platform spec slug.

**Creates:** New CLI option handler, suppressions matcher class, unit
tests.
**Touches:** CLI entry point, validate/compile JSON serialization,
`pack-output/`.
**Ends with:** `dabbler-uat-dsl validate <chk> --suppressions <path>`
returns `valid: true` when every error matches a suppression. Same
for compile, plus the emitted `.cs` is produced.
**Progress keys:** `session-1/read-planning`, `session-1/cli-audit`,
`session-1/flag-impl`, `session-1/json-output`, `session-1/unit-tests`,
`session-1/pack`, `session-1/build-and-test`, `session-1/verification`,
`session-1/commit`.

---

### Session 2 of 3: Validator role context

> **Code work mostly in `dabbler-uat-dsl`** (validator + grammar +
> visitor if the directive path wins) and possibly the runner in
> `dabbler-platform` (if the persona-inference path wins).

**Steps:**

1. Read planning docs + this spec + Session 1's outcome.
2. Audit: pull 3–5 recent UAT checklists (post-`component-catalog-grounded-authorship`)
   and inspect how they identify the active persona — explicit step
   text, structured `TestData`, an existing email-in-step convention,
   or none. Decide directive vs. persona inference and log the
   reasoning in activity-log.
3. **If directive path wins:** add `AS ROLE "<name>"` to the grammar
   as a one-shot lifecycle prelude (parses once at the top of a
   checklist's Steps; subsequent `AS ROLE` statements update the
   role cursor). Update parser, AST, visitor, and SemanticValidator
   to honor `currentRole`.
4. **If persona-inference path wins:** update
   `ai-router/uat_runner/runner.py` to scan the first ~5 steps for a
   `CLICK INPUT with label "Email address" and enter "<email>"`
   pattern, look up the role from a small fixtures map (seeded user
   emails → roles, lives at `ai-router/uat_runner/personas.json` or
   similar), and pass `activeRole` to the W0 CLI via a new
   `--active-role <name>` flag. Add the flag on the W0 CLI side too;
   that's a smaller validator change than the directive path.
5. Add unit tests covering the chosen path's happy path, the absence
   of role context (still falls back to `*`), and a role that is not
   in the catalog (returns a clear error rather than silent miss).
6. Pack updated local NuGet (1.2.0 → 1.2.1 if directive path; or
   leave 1.2.0 alone if only the runner changed and the CLI flag
   landed in 1.2.0 above).
7. `dotnet build && dotnet test` in whichever repos changed.
8. `route(task_type="session-verification", max_tier=2)` —
   cross-provider.
9. Commit. Reference both repos' slugs / SHAs as needed.

**Creates:** New grammar rule + AST node (directive path) OR new
runner module + W0 `--active-role` flag (persona-inference path).
Unit tests for either.
**Touches:** SemanticValidator (always); grammar + parser + visitor
(directive path only); runner.py + personas.json + W0 CLI
(persona-inference path only).
**Ends with:** A checklist signed in as `admin@acmecoffee.com`
validates against `role=SystemAdmin` on every post-auth step, with
no manual `--active-role` invocation needed for the runner happy
path.
**Progress keys:** `session-2/read-planning`, `session-2/audit`,
`session-2/decision`, `session-2/impl`, `session-2/unit-tests`,
`session-2/pack`, `session-2/build-and-test`,
`session-2/verification`, `session-2/commit`.

---

### Session 3 of 3: Re-run the watermark smoke (the deferred 5/5 proof)

**Steps:**

1. Read planning docs + this spec + Sessions 1–2 outcomes.
2. Bump `Directory.Packages.props` (and any consumer csproj if
   needed) to consume W0 1.2.0/1.2.1 from the sibling pack-output.
3. Re-crawl `docs/component-catalog/index.json` against the running
   compose stack (defensive: ensures the smoke's labels are current).
4. Author a fresh UAT checklist for `/admin/products` via routed
   `uat-script-author` — same scenario as
   `uat-dsl-watermark-integration/smoke/`'s authored output (toggle
   the Available switch on a product row, verify quiet completion).
   Save under this set's `smoke/` folder; do not edit the prior set.
5. Validate and compile via the W0 CLI with `--suppressions
   tools/uat-suppressions.json` (and `--active-role SystemAdmin` if
   Session 2 chose that path). Inspect the emitted `.cs`: confirm
   `await fixture.WaitForQuietAsync(page);` is present after each
   interaction step on `/admin/products`. The double-emission
   (implicit + explicit) is acceptable here — that's
   `EmitWatermarkWaits` debt, tracked separately.
6. Run the runner: `python ai-router/uat_runner/cli.py
   --checklist <path> --catalog docs/component-catalog/index.json
   --session-set w0-cli-suppressions-and-role-pinning`. Five
   iterations. **Quality bar: 5/5 PASS.**
7. If any iteration fails, follow the cumulative-flake investigation
   discipline (diagnostic instrumentation FIRST, max two iterations,
   then route to analysis). Per
   `feedback_fix_broken_tests_no_carry_forward.md` — fix or skip
   with rationale; never silently tolerate.
8. Generate `change-log.md` via `route(task_type="summarization",
   max_tier=1)`.
9. `dotnet build && dotnet test` in `dabbler-platform` (full
   solution excluding Playwright suite).
10. `route(task_type="session-verification", max_tier=2)` —
    cross-provider.
11. Commit. Commit message:
    `W0 suppressions+role-pinning complete; 5/5 watermark smoke green`.
12. Mark complete; send `send_session_complete_notification()`.
13. Step 9 reorganization review:
    - Candidate: promote a `lessons-learned.md` entry on validator
      role context — likely strong (this set's own existence justifies
      it: missing role pinning silently degraded every catalog-grounded
      checklist).
    - Candidate: extend the `Catalog-Grounded UAT Checklist Authoring`
      Convention to mention the `--suppressions` and role-pinning
      mechanism so authors know the workflow includes those pieces.
    - Candidate: lessons-learned entry on infrastructure-gap
      debugging during smoke proofs (deferred from
      `uat-dsl-watermark-integration` Step 9 as one-context-only;
      Session 3 closing this set is the second context, making it a
      promotion candidate).

**Creates:** `change-log.md`, fresh smoke checklist + compiled `.cs`
under `smoke/`, run logs.
**Touches:** `Directory.Packages.props`, possibly `nuget.config` if a
new feed entry is needed, `docs/component-catalog/index.json`
(re-crawl), `docs/planning/project-guidance.md` and
`docs/planning/lessons-learned.md` (if Step 13 accepts).
**Ends with:** Set complete. The 5/5 runner-driven smoke that
`uat-dsl-watermark-integration` Session 3 deferred is green; the
watermark integration is fully proven end-to-end.
**Progress keys:** `session-3/read-planning`, `session-3/nuget-bump`,
`session-3/recrawl`, `session-3/smoke-author`,
`session-3/smoke-compile`, `session-3/smoke-run`,
`session-3/change-log`, `session-3/build-and-test`,
`session-3/verification`, `session-3/commit-push-notify`,
`session-3/step-9-reorg`.

---

## What This Set Preserves

| Existing Asset | Status |
|---|---|
| `tools/uat-suppressions.json` schema | Unchanged; W0 CLI learns to read it natively |
| Existing UAT checklists (closed sets) | Unchanged; grandfathered via the existing all-suppressed entry |
| `uat-script-author` prompt template | Unchanged; the watermark HR#8 already covers everything this set needs |
| `WAIT FOR ACTIONS QUIET` verb | Unchanged; this set's smoke proves it |
| `TrackedComponentBase` watermark mechanism | Unchanged |
| Component catalog schema (1.1) | Unchanged |
| `EmitWatermarkWaits` implicit-emission flag | Unchanged (separate cleanup set tracked) |

## Risk Notes

- **Decision deferral in Session 2.** The directive-vs-persona-
  inference choice is left to a Session 2 audit rather than pre-
  decided. Both paths are viable; the audit is short. If the audit
  surfaces a third path (e.g., the catalog itself carries a "preferred
  default role" field), the orchestrator should flag the surprise and
  pause for the human rather than improvise.

- **Smoke target health.** The Session 3 smoke depends on
  `/admin/products` being in a passing state. Session 5 of
  `playwright-sync-watermark-and-playbook` left it at 59/60 isolated.
  If a regression has landed between then and Session 3's start, the
  smoke target may need to switch to another watermarked page
  (`/admin/customers/*`, `/admin/orders/*`). Decide during Session 3's
  read-step.

- **Cross-repo coordination.** Sessions 1 and 2 commit code in
  `dabbler-uat-dsl`; Session 3 consumes the published packages. If
  Session 2 lands a CLI flag change, Session 3 must wait for the
  pack-output bump before its compose-stack run can pick it up. The
  spec assumes the sibling repo's `pack-output/` is the consumed
  source (per the existing `nuget.config`); if that ever changes,
  Session 3 needs to update too.

- **Grandfathered checklists must not regress.** The
  all-suppressed entry in `tools/uat-suppressions.json` covers every
  pre-catalog UAT checklist. Session 1's CLI flag must respect that
  entry's `errorKind: *` + `routePathPatterns: *` semantics or the
  closed sets will start failing in CI. Add an explicit unit test for
  the wildcard-everything case.

- **The `EmitWatermarkWaits` double-emission is acceptable here.**
  Session 3's smoke will show two `WaitForQuietAsync` calls (one
  implicit from the existing flag, one explicit from the new verb).
  Don't treat that as a smoke failure. Removal is its own focused set
  and is intentionally out of scope.
