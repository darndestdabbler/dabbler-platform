# E2E Workflow Traceability Rules

> **Purpose:** Freeze the Session 1 traceability rules, layer ownership, and
> workflow-record expectations for
> `docs/session-sets/e2e-workflow-design/`.
> **Created:** 2026-04-15
> **Source Inputs:** `docs/planning/e2e-workflow-scope-ledger.md`,
> `docs/planning/app-coverage-conformance-accepted-baseline.md`,
> `docs/planning/multi-org-security-accepted-baseline.md`,
> `docs/planning/distributor-acceptance-baseline.md`,
> `docs/planning/user-functionality-notes.md`, and the current browser and
> lower-layer proof posture in `tests/`

---

## Traceability Goal

Every representative workflow must show why it belongs in Playwright, what
lower layers already prove, and which backend-truth or browser-only outcomes
still require an end-to-end lane.

Session 2 and Session 3 should be able to trace each final workflow from:

- business or persona intent
- start route and navigation path
- validation or branching behavior
- organization-aware security expectation
- backend-truth confirmation
- owning proof layers outside Playwright

## Layer Ownership Matrix

| Test Layer | Navigation | Validation | Multi-Organization Behavior | Backend Truth |
| --- | --- | --- | --- | --- |
| Unit | Not a primary owner | Owns pure rule and state-transition logic where it can be isolated cleanly | Owns pure organization-aware logic in state or service helpers | Not a primary owner |
| Integration / API | Not a primary owner beyond endpoint flow | Owns API-level validation, rejected commands, and persistence-facing rules | Owns the main proof for scoped permissions, row filtering, and active-organization semantics | Owns the primary truth checks against persisted state and returned data |
| bUnit | Owns component-level navigation affordances, conditional rendering, and stub-backed UI states | Owns rendered validation messaging and blocked-submit behavior at the component level | Owns shell and page rendering behavior driven by stubbed auth or organization context | Not a primary owner because stub-backed state is not persistence truth |
| Playwright | Owns complete browser journeys across pages, redirects, focus changes, and real auth context | Owns one representative happy path and one representative error path where real browser wiring matters | Owns the end-to-end persona experience when switching organizations or exercising scoped admin behavior | Owns secondary confirmation for critical mutations after UI-driven actions |
| UAT | Owns human confirmation that a selected journey feels coherent and usable | Owns spot-check confirmation that validation feedback is understandable | Owns a small number of human-confirmed persona flows | Owns only selective manual or helper-backed confirmation when UI feedback is not enough |

## Required Traceability Fields For Each Workflow Record

Every workflow captured in Session 2 or Session 3 should include:

- `Workflow Id`
- `Persona`
- `Auth Mode`
- `Primary Routes`
- `Business Goal`
- `Why Playwright`
- `Preconditions And Seed Data`
- `Navigation Expectations`
- `Validation Expectations`
- `Organization And Permission Expectations`
- `Backend-Truth Checks`
- `Lower-Layer Coverage Already Present`
- `Browser-Only Checks`
- `Out Of Scope`

## Navigation Rules

- record the actual entry route and the important intermediate route or shell
  transitions instead of only the final destination
- distinguish navigation proved today by smoke or guardrail tests from the
  richer navigation sequence the workflow intends to prove
- note any required tab opening, return navigation, organization-switch
  redirect, or shell refresh behavior that only a real browser can prove
- tie keyboard-navigation expectations to specific controls or interaction
  points rather than to generic accessibility language

## Validation Rules

- record only representative validation boundaries that materially increase
  confidence in the user journey
- prefer one real invalid-input branch per major workflow instead of trying to
  enumerate every field rule through Playwright
- identify whether lower layers already prove the underlying rule so the
  Playwright branch is justified as wiring proof rather than rule duplication
- call out success messaging or blocked-submit behavior only when it matters to
  the overall workflow outcome

## Multi-Organization Rules

- every workflow that depends on organization context must name the active
  organization at start and at finish
- role expectations must be written in the accepted seeded-persona language,
  while tolerating the current technical bridge role names where needed
- workflows that switch organizations must state what should change afterward:
  visible navigation, manageable users, filtered data, or allowed mutations
- organization-aware admin flows must distinguish scoped `UserAdmin` behavior
  from `SystemAdmin` bypass behavior

## Backend-Truth Rules

- backend-truth checks are mandatory for critical mutations and optional for
  read-only flows
- use API or database confirmation to prove persisted state, scoping, or side
  effects that the UI alone cannot confirm reliably
- do not attach backend-truth checks to every workflow by default; reserve them
  for the mutations and filtered views where they materially improve confidence
- when a workflow already has strong integration proof, explain why the
  Playwright-level backend check still earns its keep

## Lower-Layer Mapping Rules

- each workflow must name the lower-layer tests or accepted baselines that
  already prove adjacent behavior
- if bUnit or integration already owns most of the confidence story, the
  Playwright workflow must explain the remaining browser-specific gap
- if no lower-layer ownership can be identified, Session 2 should treat that as
  a possible gap in the pyramid rather than silently expanding Playwright scope

## Scenario-Record Quality Gate

A workflow record is ready for later implementation only when:

- the persona, routes, and business goal are specific
- the reason it belongs in Playwright is explicit
- navigation, validation, organization, and backend-truth expectations are all
  named or intentionally marked not applicable
- adjacent lower-layer proof is identified
- out-of-scope detail is explicit enough to prevent opportunistic expansion
