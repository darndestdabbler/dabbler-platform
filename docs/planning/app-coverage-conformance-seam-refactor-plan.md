# App Coverage And Conformance Seam Refactor Plan

> **Purpose:** Freeze the Session 1 seam-refactor priorities for
> `docs/session-sets/app-coverage-conformance/` so Session 2 can clean up the
> highest-value app-specific proof blockers first.
> **Created:** 2026-04-15
> **Source Inputs:** `docs/planning/app-coverage-conformance-review-ledger.md`,
> `docs/planning/user-functionality-notes.md`, the current app-layer source in
> `src/Apps/`, and the existing app/API/security tests under `tests/`

---

## Prioritized Candidates

### 1. User Administration Must Stop Querying The Database From The Web Tier

- **Target:** `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Admin/UserManagement.razor`
- **Why it is mixed:** the page renders UI, resolves the current organization,
  queries memberships and roles, and mutates scoped admin assignments directly
  through `DabblerSecurityDbContext`
- **Seam to extract:** move the page to `AdminUserApiClient`-backed scope load
  and grant or revoke operations, with the API remaining the only owner of the
  admin-scope rules already centralized in
  `src/Apps/AcmeCoffeeOnline.Api/Services/AdminUserScopeService.cs`
- **Test-layer benefit:** unlocks focused bUnit proof for the page and keeps
  the rule-heavy behavior in API or integration coverage where it belongs
- **Priority:** Session 2 blocker

### 2. Main Shell Organization Loading Needs An API-First Or Session-Context Seam

- **Target:** `src/Apps/AcmeCoffeeOnline.Web/Components/Layout/MainLayout.razor`
- **Why it is mixed:** the layout owns shell rendering, dashboard factories,
  report routing, child-data loading, and also direct database lookups for
  system-admin organization options
- **Seam to extract:** replace direct `DabblerSecurityDbContext` reads with an
  API or session-context-backed organization-options provider; then isolate
  shell state loading from render-fragment factory wiring so the layout stops
  acting as both shell view and orchestration hub
- **Test-layer benefit:** enables stable bUnit proof for shell role and
  organization state while reducing the amount of infrastructure that has to be
  present for page-level tests
- **Priority:** Session 2 blocker

### 3. Order Detail Needs A Dedicated Page-State Or Workflow Seam

- **Target:** `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Orders/OrderDetail.razor`
- **Why it is mixed:** one component coordinates order load, status
  transitions, order-item add or remove behavior, note popup flow, and local
  total recalculation alongside the rendered page
- **Seam to extract:** move workflow and state transitions into a dedicated
  order-detail state object or app service, leaving the Razor component as a
  thinner rendering shell over injected dependencies and explicit events
- **Test-layer benefit:** creates unit-testable seams for workflow and
  calculation behavior plus lighter bUnit tests for rendering and event wiring
- **Priority:** Session 2 blocker

### 4. Smart-Filter Navigation Logic Should Be Unit-Testable Without Shell Hosting

- **Target:** `src/Apps/AcmeCoffeeOnline.Web/Data/AcmeCoffeeNavigationDataProvider.cs`
- **Why it is mixed:** API calls, filter interpretation, temporal rules, and
  current-user-aware audit filtering all sit together in one provider with no
  direct proof
- **Seam to extract:** isolate filter-decision helpers and current-user lookup
  boundaries so the app-specific filtering rules can be tested without a full
  shell or browser harness
- **Test-layer benefit:** fast unit coverage for a key app-navigation seam that
  is currently only indirectly exercised by smoke tests
- **Priority:** Session 3-friendly follow-on once the bigger architecture
  blockers are moving

### 5. Web Auth And Test-Helper Endpoint Wiring Should Be Split Into Smaller Mapping Units

- **Target:** `src/Apps/AcmeCoffeeOnline.Web/Program.cs`
- **Why it is mixed:** app startup, auth flows, local test helpers, magic-link
  exchange handling, organization switching, and helper functions all live in
  one large bootstrap file
- **Seam to extract:** move endpoint registration into focused extension
  methods or mapping modules for auth, test-auth diagnostics, and app health so
  later integration coverage can target narrower endpoint slices
- **Test-layer benefit:** cleaner integration seams for auth and workflow
  behavior without forcing every change through one monolithic startup file
- **Priority:** Session 2 follow-on if time permits; otherwise keep queued for
  Session 3-supporting cleanup

## Delivery Notes

- Session 2 should start with candidates 1 and 2 because they are the clearest
  architecture-conformance violations against the binding notes
- candidate 3 is the best follow-up once the API-only app boundaries are in
  place because it unlocks the largest amount of app-layer proof
- candidates 4 and 5 should not preempt the API-first refactors, but they are
  good parallel slices once the write scopes stay disjoint
- any new seam introduced in Session 2 should be named and shaped for testing
  first, not just for code movement
