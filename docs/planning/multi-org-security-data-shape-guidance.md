# Multi-Org Security Data-Shape Guidance

> **Purpose:** Record the Session 2 data-shape bridge from the current
> sample data into the organization-aware security model.
> **Created:** 2026-04-15
> **Source Inputs:** `docs/planning/multi-org-security-decision-ledger.md`,
> `docs/planning/multi-org-security-architecture-baseline.md`, and
> `docs/session-sets/multi-org-security/spec.md`

---

## Session 2 Outcome

Session 2 introduces a concrete organization-aware identity shape in
`Dabbler.Security`:

- `AppOrganization` represents the internal organization plus retail-business
  organizations
- `AppUserOrganizationMembership` links a user to each organization they
  belong to and records the default organization
- `AppUserRole` now supports organization-scoped role assignments by attaching
  a role to a membership, while still allowing a global `SystemAdmin`
  assignment

This is the accepted persistence contract for later active-organization,
permission, admin-flow, and row-filter work.

## Seed Mapping Guidance

- the configured default internal organization key remains `internal` unless
  an environment or app configuration deliberately overrides it
- seeded internal staff receive an internal-organization membership, with that
  membership marked as their default context
- each existing sample `Customer` record is treated as the current bridge for
  one retail-business organization during seeding
- seeded retail-business contacts receive a membership for the retail-business
  organization that corresponds to their current sample `Customer` record
- the seeded regional sales rep is modeled as an internal-staff user with
  `BackofficeUserAdmin` assignments across multiple retail-business
  memberships
- the seeded dual-membership user proves that one email address may hold
  different roles in different organizations

## Legacy Bridge Notes

- current sample business records still use the existing `Customer` bridge in
  the Acme application domain
- current registration and external-login provisioning paths can still create
  single-organization bridge users until Session 3 and Session 4 complete the
  active-organization runtime and organization-aware admin flows
- `BackofficeUserAdmin` remains the technical bridge role name for the
  organization-scoped `UserAdmin` behavior accepted in planning

## Follow-On Implementation Expectation

- Session 3 should read the active organization from authenticated runtime
  context and evaluate organization-scoped roles through the membership model
- Session 4 should expose and constrain organization-aware administration using
  the same membership contract without reopening the seed or schema model
