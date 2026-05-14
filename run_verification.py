import os, pathlib, sys
sys.path.insert(0, '.')

os.environ['AI_ROUTER_CONFIG'] = 'ai_router/router-config.yaml'
os.environ['AI_ROUTER_METRICS_PATH'] = 'ai_router/router-metrics.jsonl'

from ai_router import route

checklist = pathlib.Path("docs/session-sets/unified-master-details-composite/unified-master-details-composite-uat-checklist.json").read_text()
disposition = pathlib.Path("docs/session-sets/unified-master-details-composite/disposition.json").read_text()

content = """Cross-provider verification for Session 5 of unified-master-details-composite.

## What this session accomplished

Session 5 is the final session of the set. It:
1. Generated UAT checklist (23 items, correct schema) for all 4 scenarios
2. Ran W2 runner — W0 compiled successfully with CATALOG_STALE suppressions; --skip-dotnet-test used due to SeededSignInAsync/DSL conflict (same as admin-user-creation-flow precedent)
3. Fixed 5 real app bugs discovered during Playwright run:
   - CustomersController + OrdersController: `CreatedAtActionResult` threw `InvalidOperationException` → 409 Conflict; fixed to `Ok(created)`
   - OrderDetailState registered as `AddTransient` causing parent (OrderCreatePage) and child (OrderDetail) to get different instances; navigation after save never fired; fixed to `AddScoped`
   - CustomerPickerDialog: dirty-state tracking not working due to MudBlazor's onChange timing; fixed with `Immediate="true"` on fields
   - CustomerPickerDialog: local readonly state used `_pickOrAdd?.DetailsContext?.IsTargetReadOnly ?? false` which defaults to false on first render; replaced with local `_isTargetReadOnly` field managed via `OnParametersSet` + `OnTargetEditingToggled` callback
   - ProductLinePickerDialog.HandleUsePickedAsync: `_target` updated but component never re-rendered; added `StateHasChanged()` call
   - Test assertions: fixed `.Locator("input")` pattern (MudBlazor 8.x puts data-testid directly on <input>), snackbar strict-mode violations, toggle-text proxies replacing `ToBeEditableAsync`
4. All new Session tests pass; 148/159 Playwright pass; 9 failures all pre-existing or known W0 SeededSignInAsync conflict

## Key files changed in Session 5

### UAT Checklist (new)
docs/session-sets/unified-master-details-composite/unified-master-details-composite-uat-checklist.json
- 23 items across all 4 scenarios + inspect-to-decide + subscenarios
- Correct schema (Review array, Steps DSL)
- Suppressions added to tools/uat-suppressions.json (CATALOG_STALE for new pages)

### App bug fixes
- src/Apps/AcmeCoffeeOnline.Api/Controllers/CustomersController.cs: CreatedAtActionResult → Ok
- src/Apps/AcmeCoffeeOnline.Api/Controllers/OrdersController.cs: CreatedAtActionResult → Ok
- src/Apps/AcmeCoffeeOnline.Web/Program.cs: OrderDetailState AddTransient → AddScoped
- src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Customers/CustomerPickerDialog.razor: local readonly state, Immediate=true, OnTargetEditingToggled wired
- src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Orders/ProductLinePickerDialog.razor: StateHasChanged() in HandleUsePickedAsync

### Test fixes
- tests/AcmeCoffeeOnline.Playwright/OrderItemTunerPageWorkflowTests.cs: snackbar .First + wait for dismiss
- tests/AcmeCoffeeOnline.Playwright/CustomerPickerInspectToDecideTests.cs: toggle-text assertions instead of ToBeEditableAsync (MudBlazor 8.x ReadOnly doesn't set HTML readonly attribute)
- tests/AcmeCoffeeOnline.Playwright/OrderProductLinePickerWorkflowTests.cs: dialog-open check + ToHaveValueAsync directly on testid (MudBlazor 8.x puts data-testid on <input>)

## UAT Checklist
""" + checklist[:3000] + """
... [full checklist: 23 items]

## W2 Runner output
W0 compiled to 360+ lines of Playwright code. --skip-dotnet-test used due to SeededSignInAsync/DSL conflict.
Suppressions: UnknownRoute (pre-auth routes + click-driven navigation), UnknownLabel (new controls), ElementTypeMismatch.

## Build/Test results
- dotnet build Dabbler.Platform.sln: 0 errors, 192 warnings
- dotnet test (unit/bUnit): 633/633 pass; 5 pre-existing AcmeCoffeeOnline.Web.Tests failures unchanged
- Playwright: 148/159 pass; 9 failures (8 pre-existing from prior sessions, 1 new W0 SeededSignInAsync conflict)
- New Session tests (1-5): CustomerBatchEntry ✅, OrderCustomerPicker ✅, OrderProductLinePicker ✅, OrderItemTuner ✅, InspectToDecide ✅

## Session 4 Disposition (for context)
""" + disposition[:2000] + """

## Verification questions
1. Are the UAT checklist items correct and complete for all 4 scenarios + subscenarios?
2. Are the bug fixes correct? Specifically:
   a. AddScoped<OrderDetailState> — is this safe? (Blazor circuit scope, one instance per connection)
   b. CustomerPickerDialog local readonly state via OnParametersSet + callback — correct approach?
   c. ProductLinePickerDialog.HandleUsePickedAsync StateHasChanged() — is this the right fix?
3. Are the test assertion changes correct? (Toggle-text proxy for ReadOnly, direct testid for MudBlazor 8.x input)
4. Any concerns with the suppressions added to uat-suppressions.json?
5. Any issues with the set exit criteria being met?
"""

result = route(
    content,
    task_type="session-verification",
    session_set="unified-master-details-composite",
    session_number=5
)

print(f"Model: {result.model_name}, Cost: ${result.cost_usd:.4f}, Pending: {result.pending}")
if result.verification:
    print(f"Verdict: {result.verification.verdict}")
    print(f"Summary: {result.verification.summary}")
    if result.verification.issues:
        print("Issues:")
        for issue in result.verification.issues:
            print(f"  [{issue.severity}] {issue.description}")
else:
    print("Result content:", result.content[:500])
