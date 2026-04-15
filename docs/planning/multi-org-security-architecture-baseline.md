# Multi-Org Security Architecture Baseline

> **Purpose:** Capture the Session 1 architecture decisions that later
> `multi-org-security` sessions will implement.
> **Created:** 2026-04-15
> **Source Inputs:** `docs/planning/multi-org-security-decision-ledger.md`,
> `docs/planning/multi-organization-user-administration-input.md`,
> `docs/planning/user-functionality-notes.md`, and
> `docs/planning/distributor-acceptance-baseline.md`

---

## Architecture Baseline

The accepted Phase 2 model is organization-aware, not user-wide-role-aware.

One user may belong to multiple organizations, may hold different roles in
different organizations, and must operate inside an explicit active-
organization context unless they are using global `SystemAdmin` capability.

This baseline intentionally preserves the current technical bridge names until
later implementation work proves a rename is worthwhile.

## Data Model Decisions

- add or formalize an `Organization` entity that can represent either a retail
  business or the configurable internal organization
- add an organization-membership join shape linking user identity to
  organization membership and organization-scoped role assignment
- treat organization-scoped role assignment as data attached to membership or
  an equivalent join-level association rather than to the global user record
- require `OrganizationId` on transactional and organization-owned business
  records
- keep shared reference entities such as products and role definitions global
  unless a later session explicitly proves they must become organization-aware

## Runtime Security Decisions

- store the current active organization in the authenticated session or
  equivalent request context that both UI and API boundaries can consume
- evaluate permission checks against the active organization plus the current
  user's membership and role assignments for that organization
- apply organization-aware row filtering by default where the entity is
  organization-owned
- preserve explicit bypass behavior for `SystemAdmin` so global admin flows,
  diagnostics, and reference-data management remain cross-organization

## Login And Switching Decisions

- if a user belongs to one organization, set that organization as active
  automatically after authentication
- if a user belongs to multiple organizations, require an organization
  selection step before entering the main app experience
- expose a persistent shell-level organization switcher for users with
  multiple memberships
- refresh effective role and data-visibility state immediately after an
  organization switch

## API And Seed Guidance

- organization-aware query endpoints should derive the active organization from
  authenticated runtime context rather than from a freely supplied end-user
  query parameter
- seed at least one dual-membership user with different roles across
  organizations
- seed at least one regional sales rep persona with `UserAdmin` access across
  multiple retail businesses
- keep `internal` configurable, but seed it as the initial default-organization
  story for internal staff and admin behavior

## Implementation Guardrails

- do not let distributor-story wording force premature namespace, project, or
  route renames in this set
- keep developer and system-admin tooling provable, but do not let debugging
  affordances become alternate production authorization paths
- update tests alongside each security-shape change so organization-aware
  behavior becomes provable incrementally rather than only at the end
