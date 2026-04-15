# E2E Layered Coverage Matrix

> **Purpose:** Freeze layer ownership and the final workflow-to-layer map for
> the approved representative Playwright subset.
> **Created:** 2026-04-15
> **Source Inputs:** `docs/planning/e2e-workflow-traceability-rules.md`,
> `docs/planning/e2e-representative-workflow-catalog.md`,
> `docs/planning/app-coverage-conformance-accepted-baseline.md`,
> `docs/planning/multi-org-security-accepted-baseline.md`,
> `tests/AcmeCoffeeOnline.Playwright/SmokeTests.cs`, and
> `tests/AcmeCoffeeOnline.Web.Tests/WebGuardrailTests.cs`.

---

## Coverage Matrix

| Concern | Unit / Integration | bUnit | Playwright | Later UAT |
| --- | --- | --- | --- | --- |
| Active-organization claims, scoped permissions, and row filters | Primary owner. API and service tests prove allowed versus denied scope, role assignment, and row-filter semantics. | Secondary owner for auth-driven rendering states. | Only prove the final shell and route experience after live switching. | Spot-check the persona story still feels coherent. |
| Shell navigation composition | Not a primary owner. | Own component rendering and conditional menu composition with stubbed auth state. | Own real navigation changes after sign-in and org switching. | Confirm wording and discoverability still make sense for humans. |
| Auth entry paths | Not a primary owner beyond service plumbing. | Not a primary owner. | Own the real magic-link sign-in journey and one seeded-session fast lane. | Confirm later real-environment sign-in confidence where needed. |
| Admin user-scope actions | Own grant, revoke, and unauthorized boundaries in API and service tests. | Own grid-state and message rendering with stubbed responses. | Own the end-to-end switcher plus grid action plus persisted-result story. | Human-check only if later UX concerns remain. |
| Product-admin mutation | Own create, update, and toggle behavior in API and service tests. | Can own dialog rendering if component-level regressions appear later. | Own one UI-driven toggle and one representative modal validation path. | Human-check only if future polish or accessibility concerns remain. |
| Orders query and detail scoping | Own row filtering and order state logic. | Own component rendering and detail-state transitions where isolated. | Own one real customer journey from auth to order list to detail. | Confirm the selected order story is understandable. |
| Validation boundaries | Own rule definitions and API rejection behavior. | Own component-level validation rendering. | Own one representative blocked-submit path where browser wiring matters. | Confirm feedback language is clear, not only technically correct. |
| Keyboard and focus behavior | Not a primary owner. | Can simulate focus in narrow cases, but with lower confidence. | Primary automation owner for one representative modal or tab-order path. | Human-only spot check for perceived usability if needed. |

## Final Workflow Mapping

| Workflow Id | Main Concern | Existing Lower-Layer Proof | What Playwright Adds |
| --- | --- | --- | --- |
| `E2E-WF-01` | Scoped admin mutation | `tests/AcmeCoffeeOnline.Api.Tests/AdminUserScopeTests.cs` and `tests/AcmeCoffeeOnline.Web.Tests/Pages/Admin/UserManagementPageTests.cs` | Proves the real shell switch, page refresh, action button, snackbar, and persisted role assignment in one path |
| `E2E-WF-02` | Role-aware navigation after org switch | `tests/AcmeCoffeeOnline.Web.Tests/Services/ShellSessionStateLoaderTests.cs`, `tests/AcmeCoffeeOnline.Web.Tests/Data/AcmeCoffeeNavigationDataProviderTests.cs`, and `tests/AcmeCoffeeOnline.Web.Tests/WebGuardrailTests.cs` | Proves visible navigation and route outcomes actually change in the browser after switching scope |
| `E2E-WF-03` | Real customer orders journey | accepted row-filter baseline plus existing sign-in and orders smoke coverage | Proves the user reaches scoped order detail through the UI, not only by route access and helper-backed API assertions |
| `E2E-WF-04` | Product-availability mutation | current API and service product proof plus helper-driven Playwright toggle | Replaces helper-only mutation with a user-driven browser click and backend confirmation |
| `E2E-WF-05` | Validation and keyboard wiring | `ProductDto` annotations and product API proof | Proves modal focus landing, keyboard travel, blocked submit, and successful create from the real dialog |

## Frozen Scope Rules

- Playwright owns complete browser journeys, not full business-rule coverage.
- Each approved workflow must add either multi-organization confidence, real
  auth confidence, browser-only interaction confidence, or backend-truth
  confidence after a UI-driven mutation.
- If a scenario is already strong at unit, integration, or focused bUnit level
  and does not add one of those browser-specific gains, it stays out of the
  Playwright subset.
- UAT should inherit this same representative set rather than reopening scope
  from scratch.
