# Multi-Org Security Decision Ledger

> **Purpose:** Freeze the Session 1 defaults, queue decisions, and
> implementation guardrails for `docs/session-sets/multi-org-security/`.
> **Created:** 2026-04-15
> **Source Inputs:** `docs/planning/multi-organization-user-administration-input.md`,
> `docs/planning/user-functionality-notes.md`,
> `docs/planning/distributor-acceptance-baseline.md`,
> `docs/planning/distributor-domain-glossary.md`, and the remaining unstarted
> Phase 2 session-set specs

---

## Session 1 Verdict

The current `multi-org-security` spec still matches the accepted distributor
baseline, the logged multi-organization planning input, and the remaining
Phase 2 queue.

No downstream spec text changes are warranted yet.

The durable Session 1 outcomes are:

- keep the default-organization concept configurable, with `internal` as the
  initial implementation default
- freeze `SystemAdmin` as the only global cross-organization administrator
- treat the regional sales rep as a seeded persona with multi-organization
  `UserAdmin`, not as a new role
- make active organization a first-class runtime context instead of an
  optional UI hint

## Frozen Defaults

### Default Organization

- the application must support a configurable default organization identifier
- the initial setting and implementation fallback should be `internal`
- `internal` is the baseline context for internal staff and the default
  organization used when later sessions need an internal or admin-safe
  starting point

### Roles And Personas

- `SystemAdmin` remains global and bypasses organization-scoped filtering and
  role limits
- `UserAdmin` becomes organization-scoped and can administer users only within
  organizations where the current user holds that role
- the regional sales rep is an internal-staff persona who also carries
  `UserAdmin` membership across multiple retail businesses
- user-facing distributor terminology may advance ahead of technical renames;
  existing identifiers such as `Customer`, `BackofficeUser`, and
  `SystemAdmin` remain acceptable implementation bridges until a later session
  intentionally changes them

### Active Organization

- users with one organization should enter the app with that organization set
  automatically
- users with multiple organizations should choose an active organization
  immediately after authentication before entering the main app shell
- users with multi-organization membership must be able to switch
  organizations later through a persistent shell-level affordance
- changing the active organization must refresh role, permission, and row-
  filter context immediately

## Initial Scope Freeze

### Organization-Scoped First

- organization membership
- organization-scoped role assignments
- retail-business records carried today through the `Customer` bridge
- orders and similar transactional records
- user-created business records that represent organization-owned data

### Global First

- core user identity and login record
- role definitions
- products and similar shared reference entities
- cross-organization system-administration capability

### Deferred For Later Sessions

- broad technical renames of legacy entity names
- a larger reference-data partitioning exercise beyond the initial global
  versus organization-scoped split above
- any product-specific distributor customization that would narrow the
  platform story

## Queue And Parallelization Decisions

### Cross-Set

No downstream Phase 2 session set is safe to start early.

`app-coverage-conformance` and every later set still depends on the accepted
multi-organization baseline that this session set will establish.

### Intra-Set

Before the Session 1 defaults are frozen, no implementation overlap is safe.

After Session 2 freezes the membership shape and seeded persona baseline, a
narrow overlap becomes acceptable:

- Session 3 backend-focused active-organization and permission work may
  proceed in parallel with the Session 4 shell-level organization-switching UI
  work when the edits stay on disjoint files and both slices treat this ledger
  plus the Session 2 membership contract as fixed input

The rest of Session 4 remains blocked on Session 3 because admin-limit proof
and organization-aware row-filter proof depend on the implemented runtime
security context.

### Coordination Rules

- Session 2 should treat the configurable `internal` default and the active-
  organization timing rules here as fixed
- Session 3 should not add explicit `organizationId` query parameters to
  normal end-user data-fetch endpoints when the active organization is already
  part of the authenticated runtime context
- Session 4 should make organization switching visible and testable without
  reopening the role model or introducing a second parallel security context

## Watchpoints

- migration from the current user and record shape into organization-aware
  membership is the main delivery risk for Session 2
- Session 3 has the highest regression risk because permission evaluation and
  row filtering must become organization-aware without breaking the global
  `SystemAdmin` escape hatch
- shell-level switching must refresh effective permissions and visible data
  promptly enough that users cannot act in stale organization context
- organization-aware filtering will need indexing and query-shape review once
  the concrete persistence model lands
