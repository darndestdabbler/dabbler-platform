# App Coverage And Conformance Review Ledger

> **Purpose:** Freeze the Session 1 review verdict, coverage matrix, and queue
> decisions for `docs/session-sets/app-coverage-conformance/`.
> **Created:** 2026-04-15
> **Source Inputs:** `docs/planning/distributor-acceptance-baseline.md`,
> `docs/planning/multi-org-security-accepted-baseline.md`,
> `docs/planning/phase-2-platform-roadmap.md`,
> `docs/planning/user-functionality-notes.md`, the current app and test
> surfaces under `src/Apps/`, and the remaining downstream Phase 2 session-set
> specs

---

## Session 1 Verdict

The current `app-coverage-conformance` spec still matches the accepted
distributor and multi-organization baselines.

No downstream spec text changes are warranted yet.

The durable Session 1 outcomes are:

- keep `app-coverage-conformance` focused on app-specific proof and
  architecture conformance rather than re-opening reusable-library contract
  work that already belongs to earlier Phase 2 sets
- treat direct web-tier database access as the highest-priority conformance gap
  because `docs/planning/user-functionality-notes.md` requires app data access
  to flow through the REST service layer
- treat the existing browser, API, and security tests as a strong baseline for
  auth, organization-aware scope, and representative mutation proof, but not as
  sufficient proof for app-specific Blazor page logic
- organize later work around explicit app-layer seams and missing proof instead
  of adding more browser-only coverage by default

## Current Strengths

- `tests/AcmeCoffeeOnline.Playwright/SmokeTests.cs` already proves seeded
  sign-in, real magic-link sign-in, permission gating, focused admin
  navigation, and one backend-confirmed product mutation
- `tests/AcmeCoffeeOnline.Web.Tests/WebGuardrailTests.cs` already proves shell
  render, auth layout separation, seeded-session guardrails, authenticated
  organization switching, and stub-host availability
- `tests/AcmeCoffeeOnline.Api.Tests/AdminUserScopeTests.cs` already proves
  organization-scoped admin elevation limits and representative row-filter
  behavior through the API and service layer
- `tests/Dabbler.Security.Tests/` already carries lower-layer proof for active
  organization, permission evaluation, row filters, and session context

## Coverage And Conformance Matrix

| Capability Area | Current Implementation Surface | Current Proof | Gap / Risk | Recommended Next Layer |
| --- | --- | --- | --- | --- |
| Architecture conformance | `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Admin/UserManagement.razor`, `src/Apps/AcmeCoffeeOnline.Web/Components/Layout/MainLayout.razor` | Code inspection only | The web app still queries `DabblerSecurityDbContext` directly for admin scope and system-admin organization choices, which violates the REST-service-layer rule from `docs/planning/user-functionality-notes.md` and blocks clean app-layer proof. | Session 2 API-first refactor plus focused API and bUnit proof |
| Authentication and organization switching | Auth endpoints in `src/Apps/AcmeCoffeeOnline.Web/Program.cs`, shell organization controls in `src/Apps/AcmeCoffeeOnline.Web/Components/Layout/MainLayout.razor` | Web guardrail tests, Playwright smoke tests, and security-layer tests | The cross-layer behavior is already broadly proven, but app-specific shell rendering and state transitions are still only indirectly covered. | Targeted bUnit coverage after shell seams are cleaned up |
| Organization-scoped user administration | `src/Apps/AcmeCoffeeOnline.Api/Services/AdminUserScopeService.cs`, `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Admin/UserManagement.razor` | API tests cover scope rules; browser smoke covers route access | The UI page duplicates scope-building and role-mutation behavior against the database instead of consuming the API seam that already exists. | Session 2 refactor to `AdminUserApiClient`, then bUnit/UI proof in Session 3 |
| Order workflow and detail behavior | `src/Apps/AcmeCoffeeOnline.Api/Services/OrderService.cs`, `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Orders/OrderDetail.razor` | Representative browser mutation proof and service-backed runtime behavior | `OrderDetail.razor` mixes rendering, status workflow, item mutations, note coordination, and client-side total updates without app-layer unit or bUnit proof. | Extract page-state seams in Session 2, then unit plus bUnit coverage in Session 3 |
| Navigation smart filters and dashboard harness behavior | `src/Apps/AcmeCoffeeOnline.Web/Data/AcmeCoffeeNavigationDataProvider.cs`, dashboard setup in `src/Apps/AcmeCoffeeOnline.Web/Components/Layout/MainLayout.razor` | Indirect proof through shell guardrails and browser navigation | App-specific filter behavior and dashboard wiring are not directly unit-tested, so regressions could slip past smoke coverage. | Unit tests for navigation/filter logic and focused bUnit shell proof |
| Reports and app-level reporting surfaces | `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Reports/OrdersByCustomer.razor`, `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Reports/SalesByProduct.razor` | Reusable report-surface tests exist in library test projects | The distributor app’s report routes and provider wiring do not yet have app-specific proof for role-aware access, data shaping, or representative page behavior. | Add app-level bUnit or integration proof after higher-priority seam cleanup |
| Distributor workflow breadth vs. notes-driven expectations | Dashboard, products, customers, orders, admin, auth, reports, and `/platform/stub` surfaces | Mixed smoke, API, and lower-layer proof | `docs/planning/user-functionality-notes.md` also calls for CSV export, named query/report reuse, richer search-form behavior, broader report output options, and stronger page-level proof. Some of that is deferred platform scope, and some is not yet evidenced by the app. | Use Sessions 2-3 to distinguish true app gaps from intentionally deferred or reusable-platform capabilities |

## Queue And Parallelization Decisions

### Cross-Set

No new cross-set parallelization is safe before `app-coverage-conformance`
completes.

`e2e-workflow-design` depends on the accepted app-proof baseline that this set
will produce, and every later Phase 2 set still inherits that dependency
chain.

### Intra-Set

The existing `app-coverage-conformance` parallelization note remains
appropriate.

Once this review ledger and the seam-refactor plan are frozen:

- Session 2 should tackle the architecture-conformance blockers first,
  especially API-only admin and shell seams
- after those seams are stabilized, disjoint Session 3 proof work can proceed
  in parallel across separate modules such as navigation-provider unit tests,
  order-detail app-layer proof, and report or admin page coverage

### Coordination Rules

- Session 2 should treat the REST-service-layer rule as fixed and should not
  preserve new direct `DbContext` access in app UI surfaces
- Session 2 should prefer extracting app-facing clients or state services over
  embedding more orchestration logic into large Razor files
- Session 3 should add the lowest viable test layer first: unit for pure app
  logic, integration or API proof for service boundaries, and bUnit where the
  behavior is page-specific
- later E2E and UAT sets should inherit the proof matrix here instead of using
  browser coverage to compensate for missing lower-layer seams

## Watchpoints

- `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Admin/UserManagement.razor`
  and `src/Apps/AcmeCoffeeOnline.Web/Components/Layout/MainLayout.razor` are
  the clearest architecture-conformance regressions because they mix UI and
  direct database access
- `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Orders/OrderDetail.razor`
  is the highest-risk app page for hidden state regressions because it bundles
  several workflows into one component
- `src/Apps/AcmeCoffeeOnline.Web/Data/AcmeCoffeeNavigationDataProvider.cs`
  carries app-specific behavior that is important to the shell experience but
  currently lacks direct proof
- Session 3 should avoid turning the proof plan into a browser-heavy backlog;
  the stated goal is a deliberate test pyramid, not more smoke tests by
  default
