# Accepted Multi-Organization Security Baseline

> **Purpose:** Capture the accepted organization-aware security baseline after
> `docs/session-sets/multi-org-security/` so later Phase 2 work can rely on one
> durable handoff.
> **Created:** 2026-04-15
> **Source Inputs:** `docs/planning/multi-org-security-decision-ledger.md`,
> `docs/planning/multi-org-security-architecture-baseline.md`,
> `docs/planning/multi-org-security-data-shape-guidance.md`,
> `docs/planning/multi-organization-user-administration-input.md`,
> `docs/planning/distributor-acceptance-baseline.md`, the completed
> `docs/session-sets/multi-org-security/activity-log.json`, the Session 2-4 raw
> verifier outputs, and routed Session 5 documentation and analysis.

---

## Summary

The repo now has an accepted organization-aware security baseline for identity,
role assignment, runtime authorization, row filtering, and user
administration.

One user may belong to multiple organizations, may hold different roles in
different organizations, and now operates through an explicit active-
organization context unless they are using global `SystemAdmin` capability.

## Accepted Security Baseline

- `AppOrganization` and `AppUserOrganizationMembership` are the accepted
  persistence bridge for organization membership and default-organization
  behavior.
- `AppUserRole` now supports membership-scoped role assignments while
  preserving the global `SystemAdmin` bridge for cross-organization
  administration.
- The default internal organization remains configurable, with `internal` as
  the accepted seeded and runtime default for internal-staff and admin-safe
  starting context.
- Organization-aware services and APIs derive scope from authenticated runtime
  context instead of trusting freely supplied organization parameters.

## Seeded Personas And Role Model

- internal-staff users are seeded with an `internal` organization membership
  and default context
- retail-business contacts are seeded against the retail-business organization
  that corresponds to the current `Customer` bridge record
- the regional sales rep is the accepted seeded persona for multi-organization
  `UserAdmin` behavior across multiple retail businesses
- the dual-membership user proves that one email address may hold different
  roles in different organizations
- `BackofficeUserAdmin` remains the current technical bridge role name for the
  accepted organization-scoped `UserAdmin` behavior

## Active-Organization And Switching Model

- single-organization users enter the app with that organization activated
  automatically
- multi-organization users must establish an active organization before
  entering the main app experience
- the main shell now exposes a persistent organization switcher for users with
  more than one available organization
- switching organizations refreshes the authenticated principal so effective
  roles, permissions, and visible data all move to the new active context

## Row-Filter And Admin Guardrails

- organization-owned data such as customer and order views now follow the
  active organization by default
- non-`SystemAdmin` sessions with an active organization but no matching
  organization-owned scope fall back to deny-all behavior rather than broad
  visibility
- organization-scoped `UserAdmin` actions are limited to the active
  organization where the current user holds that authority
- `SystemAdmin` remains the only unrestricted cross-organization administrator
  and bypasses the normal organization-scoped row-filter and admin limits
- seeded-session diagnostics and guarded organization-switch helpers now expose
  enough session detail to keep active-organization behavior testable and
  diagnosable in Development and Test

## What This Baseline Does Not Change

- current technical bridge identifiers such as `Customer`, `BackofficeUser`,
  `BackofficeUserAdmin`, and `AcmeCoffeeOnline` project or namespace names stay
  in place until a later set deliberately changes them with supporting proof
- shared reference entities such as products and role definitions remain global
  rather than becoming organization-scoped in this set
- the accepted auth modes and auth-entry paths remain the same; the change here
  is that post-authentication session state is now organization-aware

## Downstream Implications

- `app-coverage-conformance` can treat organization-aware identity, switching,
  admin scoping, and row-filter behavior as implemented baseline instead of
  roadmap intent
- E2E design, Playwright implementation, and later UAT can use the seeded
  regional sales rep, dual-membership user, and `SystemAdmin` personas as the
  accepted security proof story
- documentation work should describe organization-aware security and switching
  as current platform behavior, while still calling out the preserved technical
  bridge names

## Queue Rebaseline

No downstream Phase 2 spec edits are required from this closeout.

The implemented security model matches the earlier decision ledger, the
architecture baseline, and the current downstream queue assumptions closely
enough that later sets can proceed without reordering or additional spec text.

## Catch-Up Decision

No Session 6 is warranted.

The completed work delivered the planned durable outcomes inside the original
five sessions:

- organization-aware membership and seeded personas
- active-organization runtime context and permission evaluation
- organization-scoped admin flows plus shell-level organization switching
- focused proof for switching, admin limits, and representative row filtering
