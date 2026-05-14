import json, pathlib, sys
sys.path.insert(0, '.')

import os
os.environ['AI_ROUTER_CONFIG'] = 'ai_router/router-config.yaml'
os.environ['AI_ROUTER_METRICS_PATH'] = 'ai_router/router-metrics.jsonl'

from ai_router import route

example = pathlib.Path("docs/session-sets/admin-users-cross-links/admin-users-cross-links-uat-checklist.json").read_text()
example_data = json.loads(example)
first_two = {"ProjectName": example_data["ProjectName"], "Review": example_data["Review"][:2]}
example_snippet = json.dumps(first_two, indent=2)

pw_dir = pathlib.Path("tests/AcmeCoffeeOnline.Playwright")
pw_texts = {}
for fname in ["CustomerBatchEntryWorkflowTests.cs", "OrderCustomerPickerWorkflowTests.cs",
              "OrderProductLinePickerWorkflowTests.cs", "OrderItemTunerPageWorkflowTests.cs",
              "CustomerPickerInspectToDecideTests.cs"]:
    pw_texts[fname] = (pw_dir / fname).read_text()

content = r"""Generate a UAT checklist JSON file for the `unified-master-details-composite` session set.

## EXACT SCHEMA REQUIRED

Root object fields:
  "ProjectName": string
  "ReleaseLabel": string
  "DocumentDate": string
  "RequiresAppStateReset": false
  "Review": array of items

Each Review item:
  "FunctionalArea": string   (top-level group)
  "Subarea": string          (second-level group)
  "ItemLabel": string        (short item name)
  "Steps": array of steps
  "E2ETestReference": string (ClassName.MethodName or empty string)
  "IsJudgmentItem": bool
  "Passes": false
  "Feedback": ""

Each Step:
  "StepNumber": integer
  "Action": string           (ONLY use DSL verbs below)
  "Verifications": array of { "Text": string, "VerificationMethod": "ui" }

## Action DSL verbs (ONLY these)
- NAVIGATE TO "url"
- CLICK BUTTON with caption "text"
- CLICK LINK with text "text"
- CLICK INPUT with label "label" and enter "value"
- CLICK TAB with text "text"
- VERIFY TEXT contains "text"
- VERIFY URL matches "/path"

## Standard sign-in steps (reuse for every item that requires sign-in)
Step 1: NAVIGATE TO "http://localhost:5010/login"
Step 2: CLICK INPUT with label "Email address" and enter "admin@acmecoffee.com"
Step 3: CLICK BUTTON with caption "Send sign-in link"
Step 4: NAVIGATE TO "http://localhost:5010/dev/email-inbox"
Step 5: CLICK LINK with text "Open"
Step 6: CLICK BUTTON with caption "Admin"

## Reference example (first two items showing correct schema)
"""

content += example_snippet

content += r"""

## Items to generate

FunctionalArea groupings and E2ETestReference values:

### FunctionalArea: "Scenario 1: Add-Only"
Items:
  1a - Subarea: "Page Layout"
    ItemLabel: "Add-Only page renders full-width with no expansion panel chrome"
    E2ETestReference: "CustomerBatchEntryWorkflowTests.BatchEntry_PageLoads_NoExpansionChromeAsync"
    Steps: sign in -> NAVIGATE TO "http://localhost:5010/customers/batch-entry"
      -> VERIFY TEXT contains "Add customer" (or the page title)
    Verifications on last step: "Page shows full-width form; no collapse/expand panel header or body is visible"

  1b - Subarea: "Save Behavior"
    ItemLabel: "Save clears form and increments session counter"
    E2ETestReference: "CustomerBatchEntryWorkflowTests.BatchEntry_Save_IncrementsCounterAndClearsFormAsync"
    Steps: sign in -> navigate to page -> fill first name "Batch" -> fill last name "Test"
      -> fill email "batch-1b@test.local" -> CLICK BUTTON with caption "Save"
    Verifications: "Snackbar shows 'Customer added.'" and "Session counter shows 'Records added this session: 1'" and "Email field is empty"

  1c - Subarea: "Database Persistence"
    ItemLabel: "Two successive saves produce two persisted customer records"
    E2ETestReference: "CustomerBatchEntryWorkflowTests.BatchEntry_TwoRecords_BothPersistedInDbAsync"
    Steps: save first customer -> verify counter=1 -> save second customer -> verify counter=2
    Verifications: "Both customer records retrievable from the database"

### FunctionalArea: "Scenario 2: Role Master-Details Tab Regression"
(All IsJudgmentItem=true; E2ETestReference="")
  2a - Subarea: "Expansion Panel Behavior"
    ItemLabel: "Selecting a role row collapses the expansion panel"
    Steps: sign in -> CLICK LINK with text "Security Dashboard" -> CLICK TAB with text "ROLES"
      -> click first role row -> verify panel collapsed
    Verifications: "Expansion panel is collapsed; collapsed summary header visible"

  2b - Subarea: "Expansion Panel Behavior"
    ItemLabel: "Clicking Add expands the expansion panel"
    Steps: (from 2a with panel collapsed) -> CLICK BUTTON with caption "Add"
    Verifications: "Expansion panel expands showing empty detail form"

  2c - Subarea: "Save Behavior"
    ItemLabel: "Editing and saving a role persists the change"
    Steps: sign in -> navigate to Roles tab -> select a role -> edit name -> CLICK BUTTON with caption "Save"
    Verifications: "Snackbar confirms save" and "After page refresh edited value appears in grid"

### FunctionalArea: "Scenario 3: Pick-or-Add Customer"
  3a - Subarea: "Row-wise Use"
    ItemLabel: "Row-wise Use links existing customer and populates parent form"
    E2ETestReference: "OrderCustomerPickerWorkflowTests.Picker_PickExisting_PopulatesParentFieldAsync"
    Steps: sign in -> NAVIGATE TO "http://localhost:5010/orders/new"
      -> CLICK BUTTON with caption "Pick customer" -> wait for dialog
      -> CLICK BUTTON with caption "Use" in first row
    Verifications: "Dialog closes; parent order form customer field shows selected customer's name"

  3b - Subarea: "Add New Customer"
    ItemLabel: "Add new customer via dialog creates DB record and sets parent FK"
    E2ETestReference: "OrderCustomerPickerWorkflowTests.Picker_AddNew_SavesAndPopulatesParentFieldAsync"
    Steps: sign in -> navigate to /orders/new -> CLICK BUTTON with caption "Pick customer"
      -> fill customer fields -> CLICK BUTTON with caption "Save and Use"
    Verifications: "Dialog closes; parent form shows new customer name; DB record exists"

  3c - Subarea: "Dirty State Label"
    ItemLabel: "Selecting existing row then editing flips primary button from 'Use' to 'Save and Use'"
    E2ETestReference: "OrderCustomerPickerWorkflowTests.Picker_DirtyState_FlipsPrimaryButtonLabelAsync"
    Steps: sign in -> navigate to /orders/new -> CLICK BUTTON with caption "Pick customer"
      -> click row to select -> VERIFY TEXT contains "Use" -> edit first name field
    Verifications: "Primary button text changes to 'Save and Use'"

### FunctionalArea: "Scenario 3: Inspect-to-Decide"
  3d - Subarea: "Initial Read-Only State"
    ItemLabel: "Inspect dialog shows read-only target fields and primary button labeled 'Use'"
    E2ETestReference: "CustomerPickerInspectToDecideTests.InspectToDecide_ToggleFlow_MakesFieldsEditableAsync"
    Steps: sign in -> navigate to /orders/new -> pick customer via row Use
      -> CLICK BUTTON with caption "Inspect" -> select customer row in dialog
    Verifications: "Target fields are read-only; 'Edit record' toggle visible; primary button shows 'Use'"

  3e - Subarea: "Toggle Without Edit"
    ItemLabel: "Clicking 'Edit record' toggle makes fields editable but label stays 'Use'"
    E2ETestReference: "CustomerPickerInspectToDecideTests.InspectToDecide_ToggleFlow_MakesFieldsEditableAsync"
    Steps: (from 3d with dialog open) -> CLICK BUTTON with caption "Edit record"
    Verifications: "Fields become editable; primary button still shows 'Use'"

  3f - Subarea: "Dirty State Label"
    ItemLabel: "Editing a field after enabling inspect-edit mode flips label to 'Save and Use'"
    E2ETestReference: "CustomerPickerInspectToDecideTests.InspectToDecide_ToggleFlow_MakesFieldsEditableAsync"
    Steps: (from 3e with editing enabled) -> fill first name field with new value
    Verifications: "Primary button text changes to 'Save and Use'"

### FunctionalArea: "Scenario 4a: Both Cards Editable"
  4a-layout - Subarea: "Page Layout"
    ItemLabel: "Order Item Tuner page shows both target and link data cards with no expansion chrome"
    E2ETestReference: "OrderItemTunerPageWorkflowTests.TunerPage_Loads_CorrectLayoutAsync"
    Steps: sign in -> NAVIGATE TO "http://localhost:5010/admin/order-item-tuner"
      -> VERIFY TEXT contains "Order item tuner"
    Verifications: "Both target card and link data card visible; Save and Cancel buttons visible; no collapse/expand panel chrome"

  4a-link-only - Subarea: "Dirty State Save"
    ItemLabel: "Edit Quantity (link data) only -> save shows 'Saved line details (demo mode).'"
    E2ETestReference: "OrderItemTunerPageWorkflowTests.TunerPage_DirtyState_FlipsSaveMessageAsync"
    Steps: sign in -> navigate to tuner page -> fill quantity field -> CLICK BUTTON with caption "Save"
    Verifications: "Snackbar contains 'Saved line details (demo mode).'"

  4a-target-only - Subarea: "Dirty State Save"
    ItemLabel: "Edit Product Name (target) only -> save shows 'Saved product (demo mode).'"
    E2ETestReference: "OrderItemTunerPageWorkflowTests.TunerPage_DirtyState_FlipsSaveMessageAsync"
    Steps: sign in -> navigate to tuner page -> fill product name field -> CLICK BUTTON with caption "Save"
    Verifications: "Snackbar contains 'Saved product (demo mode).'"

  4a-both - Subarea: "Dirty State Save"
    ItemLabel: "Edit both cards -> save shows 'Saved product + line details (demo mode).'"
    E2ETestReference: "OrderItemTunerPageWorkflowTests.TunerPage_DirtyState_FlipsSaveMessageAsync"
    Steps: sign in -> navigate to tuner page -> fill product name and discount fields -> CLICK BUTTON with caption "Save"
    Verifications: "Snackbar contains 'Saved product + line details (demo mode).'"

### FunctionalArea: "Scenario 4b: Locked Target"
  4b-locked-layout - Subarea: "Dialog Layout"
    ItemLabel: "Product line picker shows disabled target fields and no edit-toggle button"
    E2ETestReference: "OrderProductLinePickerWorkflowTests.AddProduct_PickFillAndSave_PersistsOrderItemAsync"
    Steps: sign in -> navigate to /orders/new -> pick customer -> save order
      -> CLICK BUTTON with caption "Add Product" -> select first product via Use button
    Verifications: "Target product fields are disabled; no 'Edit record' toggle button visible"

  4b-locked-save - Subarea: "Save Link Data"
    ItemLabel: "Fill Quantity=2 and Discount=15.50, Save and Use closes dialog and adds row to items grid"
    E2ETestReference: "OrderProductLinePickerWorkflowTests.AddProduct_PickFillAndSave_PersistsOrderItemAsync"
    Steps: (from 4b-locked-layout with product selected) -> fill quantity "2" -> fill discount "15.50"
      -> CLICK BUTTON with caption "Save and Use"
    Verifications: "Dialog closes; order items grid shows new row with product name and quantity 2"

  4b-locked-db - Subarea: "Database Verification"
    ItemLabel: "DB verification: join row has correct Quantity/Discount; product record unchanged"
    E2ETestReference: "OrderProductLinePickerWorkflowTests.AddProduct_PickFillAndSave_PersistsOrderItemAsync"
    Steps: (API verification after 4b-locked-save)
    Verifications (VerificationMethod="api"): "OrderItem.Quantity=2 and OrderItem.Discount=15.50"
                                              and "Product entity itself unchanged"

### FunctionalArea: "Scenario 4b: Toggle Variant" (all IsJudgmentItem=true, E2ETestReference="")
  4b-toggle-layout - Subarea: "Dialog Layout"
    ItemLabel: "Inspect Line dialog shows read-only target fields and 'Edit record' toggle"
    Steps: open Inspect Line dialog for existing order item
    Verifications: "Target product fields read-only; 'Edit record' toggle visible"

  4b-toggle-click - Subarea: "Toggle Behavior"
    ItemLabel: "Clicking 'Edit record' toggle makes target fields editable"
    Steps: click toggle
    Verifications: "Target fields become editable"

  4b-toggle-label - Subarea: "Dirty State Label"
    ItemLabel: "Editing target field flips primary button to 'Save and Use'"
    Steps: edit a target field
    Verifications: "Primary button text changes to 'Save and Use'"

  4b-toggle-save - Subarea: "Database Persistence"
    ItemLabel: "Save and Use persists both target entity and join row in DB"
    Steps: click Save and Use
    Verifications (VerificationMethod="api"): "Both target product and join row updated in database"

## CRITICAL OUTPUT RULES
1. Output ONLY the JSON. Start your response with { and end with }. Absolutely nothing else.
2. Use DocumentDate: "2026-05-12"
3. RequiresAppStateReset: false
4. Passes: false for ALL items
5. Feedback: "" for ALL items
6. ONLY use the DSL verbs listed above in Action strings
7. For VerificationMethod omit the field entirely when it is "ui" (default)
8. For api/sql verifications include "VerificationMethod": "api" or "sql"
"""

result = route(
    content,
    task_type="uat-plan-generation",
    session_set="unified-master-details-composite",
    session_number=5
)

print(f"Model: {result.model_name}, Cost: ${result.cost_usd:.4f}")

text = result.content.strip()
if text.startswith("```"):
    text = text[text.find("\n")+1:]
    end_fence = text.rfind("```")
    if end_fence != -1:
        text = text[:end_fence]
text = text.strip()

decoder = json.JSONDecoder()
parsed, end_idx = decoder.raw_decode(text)

assert "Review" in parsed, f"Missing 'Review'. Keys: {list(parsed.keys())}"
assert "ProjectName" in parsed, "Missing 'ProjectName'"
print(f"JSON OK — {len(parsed['Review'])} items")

out_path = pathlib.Path("docs/session-sets/unified-master-details-composite/unified-master-details-composite-uat-checklist.json")
out_path.write_text(json.dumps(parsed, indent=2), encoding="utf-8")
print(f"Written to {out_path}")
