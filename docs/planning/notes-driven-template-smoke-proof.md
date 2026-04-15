# Notes-Driven Template Smoke Proof

> **Purpose:** Record the smoke-proof evidence that `template-seed/` now holds
> the narrowed starter contract and note whether the remaining session-set
> queue needs any follow-up.
> **Created:** 2026-04-15
> **Source Inputs:** `template-seed/README.md`,
> `template-seed/AGENTS.md`, `template-seed/CLAUDE.md`,
> `template-seed/GEMINI.md`,
> `template-seed/docs/session-sets/starter-feature/spec.md`, and
> `tests/AcmeCoffeeOnline.Web.Tests/TemplateSeedSmokeTests.cs`

---

## Smoke-Proof Coverage

The template seed now has repo-enforced smoke proof for the workflow-only
starter contract.

`tests/AcmeCoffeeOnline.Web.Tests/TemplateSeedSmokeTests.cs` verifies that:

- the template still ships the expected workflow assets, placeholder tooling,
  and `ai-router/` files
- the template does not quietly grow application scaffolding such as `src/`,
  `tests/`, or `docker/`
- the bootstrap contract in `template-seed/README.md` keeps the focused shell,
  dashboard, query, detail, popup, report, and single-select lookup story
  explicit
- the root instruction templates keep the narrowed default surface visible,
  including the absence of compatibility-only catalog/query-report/admin
  defaults
- the starter session-set spec continues to require deliberate opt-ins for
  query CSV export, multiselect lookup workflows, and any broader advanced
  surface

This smoke proof is intentionally documentation-centered because the template
remains a workflow bootstrap, not an app scaffold.

## Queue Rebaseline

The remaining session-set queue does not need a new simplification follow-up
based on the template work.

- no direct omission was exposed in the retained notes-driven platform contract
- the new lesson is durability, not simplification: keep testing the template's explicit negative
  defaults so future doc edits cannot silently widen the starter contract
- that lesson belongs in the current repo test suite, not in an added catch-up
  session for this set

## Catch-Up Decision

No Session 4 should be appended to
`docs/session-sets/notes-driven-template-encoding/spec.md`.

The current three sessions fully cover the audit, guidance update, smoke-proof,
queue rebaseline, and closeout work for this set.
