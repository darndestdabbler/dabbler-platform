# Rearchitect Role Administration — Security Dashboard Rebuild

> **Purpose:** Rebuild the Security Dashboard from scratch as a
> five-tab layout (Role Master Details, Default Masks, Organization
> Master Details, Users and Role Assignment, Role Documentation)
> wired to the restriction-based model introduced by Set A.
> **Created:** 2026-04-19 *(stub — fleshed out at start of Session 1)*
> **Session Set:** `docs/session-sets/rearchitect-role-administration-dashboard/`
> **Roadmap:** `C:\Users\denmi\.claude\plans\ok-i-did-some-abstract-blossom.md`
> **Architecture doc:** [../rearchitecting-role-administration/rearchitecting-role-administration.md](../rearchitecting-role-administration/rearchitecting-role-administration.md)
> **Prerequisite:** Set A
> (`rearchitect-role-administration-foundations`) complete; its
> change log is in its folder.
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Status

**This is a stub spec.** The authoritative session breakdown is in
the roadmap file above under "Session Set B — Security Dashboard
Rebuild." Before running Session 1, the orchestrator flesh this
spec out with:

- Per-session step lists (copy from the roadmap's 7-session
  breakdown and expand).
- Concrete progress keys per step.
- Risk notes updated for any surprises found during Set A.
- A confirmation that Set A's change log lists the expected
  deliverables (delete check, migration check).

## Goals (from the roadmap)

1. Delete all old grant-based dashboard components
   (`DabblerPagePermissionMatrix`, `DabblerEndpointPermissionMatrix`,
   `DabblerEntityPermissionMatrix`, `DabblerFieldPermissionEditor`,
   `DabblerControlPermissionTab`, `DabblerRowFilterTab`,
   `DabblerRoleManager`, `DenyAllowSwitch`,
   `DabblerPermissionTriState` if it was scaffolded).
2. Five top-level tabs, each gated to the appropriate role set.
3. Role Master Details tab shows ALL roles (baked-in greyed with a
   "baked-in" chip). Sub-tabs per row: four for user-type roles,
   one (Mask Exemptions) for baked-in.
4. Default Masks tab with `FilterPanel<T>` + grid + the
   sensitive-name safety net.
5. Data-bound control enforcement seam lands here (Restriction +
   Mask enforcement on MudForm fields and MudDataGrid columns).
6. Organization and Users-and-Role-Assignment tabs wire
   `AppOrganization` and `AppUserOrganizationMembership`.
7. UAT checklist
   `rearchitect-role-administration-dashboard-uat-checklist.json`
   queued.

## Session Plan (brief — fleshed out at session 1)

1. Delete old tabs; stub five-tab layout with authorization gating.
2. Role Master Details (greyed baked-in rows, four sub-tabs for
   user-type, Mask Exemptions sub-tab for all).
3. Default Masks tab + name-pattern safety net.
4. Data-bound control enforcement helper
   (Restriction + Default-Mask + Exemption awareness).
5. Organization Master Details tab.
6. Users and Role Assignment tab.
7. Role Documentation tab + UAT checklist + change log + Step 9
   reorganization.

Expect 6–7 sessions; session 4 is the largest.
