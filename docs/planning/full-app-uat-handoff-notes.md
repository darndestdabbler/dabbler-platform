# Full-App UAT Handoff Notes

> **Purpose:** Hand off the stabilized representative Playwright suite to
> `docs/session-sets/full-app-uat/`.
> **Created:** 2026-04-16
> **Source Inputs:** `docs/planning/e2e-persona-route-matrix.md`,
> `docs/planning/e2e-representative-suite-baseline.md`, and the final
> `docs/session-sets/e2e-workflow-implementation/` validation evidence.

---

## Why This Handoff Exists

The representative automated suite is now stable enough for the later
`full-app-uat` set to use as its workflow-selection baseline instead of
reopening route entry points or persona framing.

The downstream UAT work should inherit the frozen route and persona matrix from
`docs/planning/e2e-persona-route-matrix.md`, then spend human attention on
clarity, coherence, and other checks that automation cannot judge well.

## Recommended Human-Confirmed Subset

Keep the human-confirmed subset small and inherit all five representative
workflow IDs in the established order:

- `E2E-WF-04`: confirm that the `/admin/products` availability toggle remains
  understandable and that the browser feedback around the state change is clear
  to a human reviewer.
- `E2E-WF-05`: confirm that the add-product dialog feels coherent to navigate,
  that the validation messaging is understandable, and that the keyboard path
  feels intentional rather than merely technically passable.
- `E2E-WF-02`: confirm that switching from internal scope to the retail
  customer scope makes the active organization obvious and that the route
  transition feels trustworthy to a human user.
- `E2E-WF-01`: confirm that the `/admin/users` warning state is understandable
  before the user switches into an allowed organization, and that the allowed
  scope feels clearly distinguished after the switch.
- `E2E-WF-03`: confirm that the real magic-link sign-in experience is clear
  end to end and that the orders-to-detail journey feels coherent in the signed
  customer scope.

## Human-Only Add-Ons

The route and persona matrix already identifies the most valuable human-only
checks to layer on top of the automated subset:

- clarity of the active-organization label after switching scope
- clarity of the `/admin/users` warning state before the allowed scope is
  chosen
- clarity of product-dialog validation messaging and keyboard flow
- clarity of the real magic-link sign-in experience

## Execution Notes

The handoff validation evidence at closeout is:

- `dotnet build Dabbler.Platform.sln` succeeded with preview-SDK notices and
  `12` warnings.
- `dotnet test Dabbler.Platform.sln` passed `430/430`.
- `dotnet test tests/AcmeCoffeeOnline.Playwright/AcmeCoffeeOnline.Playwright.csproj --filter "FullyQualifiedName~E2E_WF_"` passed `5/5`.

Trace workflow ownership back to the current Playwright files rather than
recreating the inventory from scratch:

- `tests/AcmeCoffeeOnline.Playwright/ProductAdministrationWorkflowTests.cs`
  owns `E2E-WF-04` and `E2E-WF-05`.
- `tests/AcmeCoffeeOnline.Playwright/MultiOrganizationWorkflowTests.cs` owns
  `E2E-WF-02` and `E2E-WF-01`.
- `tests/AcmeCoffeeOnline.Playwright/CustomerWorkflowTests.cs` owns
  `E2E-WF-03`.
- `tests/AcmeCoffeeOnline.Playwright/SmokeTests.cs` remains the supporting
  baseline for shell, auth, permission, and focused-navigation proof.

## Scope Guard For Full-App UAT

Use the representative Playwright suite as the baseline for workflow selection,
not as a reason to expand into new end-to-end scenarios.

The later UAT set should confirm a small subset of already-implemented routes
and personas, add only a few intentionally human-only checks, and rely on the
existing automation for backend-truth proof that humans do not need to retake
manually.
