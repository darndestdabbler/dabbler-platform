# Transactional System Columns

> **Purpose:** Add `OrganizationId` and `RoleId` as system columns on
> every transactional entity (Product, Order, OrderItem, Customer,
> …). Stamp both on insert from the active `UserSessionContext`.
> Apply a global EF query filter to scope reads by organization
> (except for SystemAdmin / CrossOrgUserAdmin / CrossOrgMonitor).
> Add temporal tracking (`SysStart`, `SysEnd`) to match the
> `Dabbler.Security` convention.
> **Created:** 2026-04-19 *(stub — fleshed out at start of Session 1)*
> **Session Set:** `docs/session-sets/transactional-system-columns/`
> **Roadmap:** `C:\Users\denmi\.claude\plans\ok-i-did-some-abstract-blossom.md`
> **Prerequisite:** Set A complete (for the role model + the
> active-role concept). Sets B and D can be in flight or complete —
> they're independent of this one.
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Status

**This is a stub spec.** The authoritative session breakdown is in
the roadmap file under "Session Set C — System Columns on
Transactional Entities." Before running Session 1, the
orchestrator should flesh this spec out with detailed step lists
per session, progress keys, and updated risk notes.

## Goals (from the roadmap)

1. New Principle in `project-guidance.md`: transactional entities
   carry `Id`, `OrganizationId`, `UserId`, `RoleId`, `SysStart`,
   `SysEnd`.
2. EF migrations add `OrganizationId`, `RoleId` to every
   transactional table in `AcmeCoffeeOnline.Web.Data` and
   `AcmeCoffeeOnline.Api`. Existing rows backfilled to the
   internal org's id and a sentinel role id (see session 2 spec).
3. `SaveChanges` interceptor stamps `OrganizationId` + `RoleId`
   from the active `UserSessionContext` on insert.
4. Global EF query filter
   (`modelBuilder.Entity<T>().HasQueryFilter(...)`) scopes reads to
   the active organization, bypassed for SystemAdmin /
   CrossOrgUserAdmin / CrossOrgMonitor.
5. Temporal-table support (`IsTemporal`) configured on each
   transactional entity.
6. Existing manual org-filter logic (e.g., in `CustomerService.cs`)
   removed where the global filter covers it.
7. Integration tests prove the stamping + filter flow end-to-end.
8. UAT checklist queued.

## Session Plan (brief — fleshed out at session 1)

1. Design + Principle. Pick marker interface vs. base class for
   transactional entities. Document the six-column convention.
2. EF migrations + `SaveChanges` interceptor + temporal tracking.
3. Global query filter + removal of duplicated manual org-scoping.
4. Integration tests + UAT checklist + change log + Step 9
   reorganization.

Expect 3–4 sessions.
