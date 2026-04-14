# Multi-Organization User Administration Input

## Status

This is a logged planning input, not an active session set yet.

A dedicated follow-on session set is still needed for this work because it
touches identity, authorization, record ownership, UI state, and admin flows.

## Requirement Summary

- introduce a user-organization concept
- provide a default organization such as `internal`, ideally configurable
- allow one user to belong to multiple organizations
- allow a user to hold different roles in different organizations
- allow user administrators to manage users only for organizations where they
  hold the user-administrator role
- allow those organization-scoped user administrators to elevate another user
  within that same organization to user administrator
- preserve global, cross-organization privileges for system administrators
- allow system administrators to manage reference entities such as product lists
- assume a single email address may span multiple organizations
- plan for records to carry both `organization_id` and `user_id` where needed
- allow users assigned to multiple organizations to switch organizations in the UI
- allow system administrators and developers to switch both organization and role at any time

## Why This Needs Its Own Session Set

- data ownership and row filtering will need an organization-aware model
- role assignment changes move from user-wide to organization-scoped
- the current seeded-session, role-switching, and permission helpers will need
  new organization context
- admin UI and navigation behavior will need organization-aware filtering and
  elevation rules
- login/session behavior may need an organization selector or a post-login
  organization context switch

## Likely Session-Set Scope

- define organization entities, default organization behavior, and migration rules
- redesign user-role assignment storage around organization membership
- update permission evaluation to combine organization context with role context
- add organization switching to the UI and developer/system-admin tooling
- define record-model and API expectations for `organization_id` plus user identity
- add tests for organization-scoped admin actions, cross-organization system-admin behavior,
  and organization switching

## Open Questions

- whether the default organization should be fixed as `internal` or configurable by environment
- whether organization selection should happen at login, after login, or both
- which entities become organization-scoped first and which remain global reference data
- whether developer role-switching should stay separate from system-admin capabilities or be unified
