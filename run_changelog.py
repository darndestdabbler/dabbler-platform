import os, pathlib, sys
sys.path.insert(0, '.')
os.environ['AI_ROUTER_CONFIG'] = 'ai_router/router-config.yaml'
os.environ['AI_ROUTER_METRICS_PATH'] = 'ai_router/router-metrics.jsonl'
from ai_router import route

# Read all 5 disposition files
dispositions = {}
for i in range(1, 6):
    p = pathlib.Path(f"docs/session-sets/unified-master-details-composite/disposition.json")
    if p.exists() and i == 4:
        dispositions[4] = p.read_text()

# Read spec for context
spec = pathlib.Path("docs/session-sets/unified-master-details-composite/spec.md").read_text()

# Session 5 summary
session5_summary = """
Session 5 (final):
- Generated UAT checklist (23 items, all 4 scenarios + inspect-to-decide + 4b subscenarios)
- W2 runner: W0 compiled to 360+ lines of Playwright code; --skip-dotnet-test per SeededSignInAsync conflict
- Fixed 5 app bugs found during Playwright run:
  * CustomersController + OrdersController: CreatedAtActionResult threw InvalidOperationException (→409); fixed to Ok(created)
  * OrderDetailState: AddTransient → AddScoped (parent/child got different instances; navigation after new-order save never fired)
  * CustomerPickerDialog: dirty-state tracking needed Immediate=true + local _isTargetReadOnly field via OnParametersSet + OnTargetEditingToggled callback
  * ProductLinePickerDialog.HandleUsePickedAsync: StateHasChanged() added after row selection to force DetailsContent re-render
  * Test assertions: toggle-text proxy for MudBlazor 8.x ReadOnly (no HTML readonly attribute); ToHaveValueAsync directly on testid (data-testid on <input> in MudBlazor 8.x)
- All 5 new scenario Playwright tests pass; dotnet: 633/633; Playwright: 148/159
- Session verification: VERIFIED (GPT-5.4)
"""

content = f"""Generate a change-log.md for the `unified-master-details-composite` session set (5 sessions total).

## Context
This set evolved `MasterDetailExpansionShell` into a generic composite control driving 4 data-entry workflows.

## Spec Goals (from spec.md)
{spec[spec.find('## Goals'):spec.find('## Non-goals')]}

## Session-by-Session Deliverables

### Session 1: Generify the shell + migrate RoleMasterDetailsTab
- Rewrote MasterDetailExpansionShell as generic <TRow, TEntity, TLink> with new parameters (ShowGrid, ShowAddAction, ShowRowUseAction, AllowLinkOnly, Mode, TargetEditability, ShowLinkData) and slots (FilterContent, GridContent, RowUseAction, DetailsContent, LinkDataContent, DetailsActions, SessionSummaryContent)
- Created MasterDetailShellContexts.cs (DetailsContext<TEntity>, LinkContext<TLink>, ShellMode, TargetEditability enums)
- Migrated RoleMasterDetailsTab and other shell consumers to new slot pattern
- bUnit coverage for shell slots/parameters; build green

### Session 2: Scenario 1 (Add Only) — ShowGrid=false layout
- ShowGrid=false drops expansion panel; renders DetailsContent full-width in MudPaper
- Session counter increments on save; form clears per entry
- Customer Batch Entry page at /customers/batch-entry (live demo)
- bUnit + Playwright coverage; DB persistence verified

### Session 3: PickOrAddDialog + Scenario 3 (pick-or-add, no link data)
- Created PickOrAddDialog<TRow, TEntity, TLink> hosting the shell in Dialog mode
- Row-wise Use button pattern; dirty-state label-swap (Use → Save and Use)
- Wired Order → Customer picker dialog as Scenario 3 reference implementation
- bUnit + Playwright coverage for all 3 picker paths

### Session 4: Scenarios 4a/4b + inspect-to-decide
- LinkDataContent slot + ShowLinkData rendering (link card above target card)
- TargetEditability modes: Editable / ReadOnly / ReadOnlyWithToggle
- Link dirty state separate from entity dirty state; AnyDirty drives label
- Toggle-without-change preserves "Use" label
- Scenario 4b: OrderDetail's Add-Product flow migrated to ProductLinePickerDialog (ShowLinkData=true, TargetEditability=ReadOnly)
- Scenario 4b-toggle: Inspect Line button with ReadOnlyWithToggle variant
- Scenario 4a: /admin/order-item-tuner demo page (ShowGrid=false, ShowLinkData=true, Editable)
- Scenario 3 inspect-to-decide: CustomerPickerDialog with InspectToDecide=true
- OrderCreatePage now navigates to /orders/{id} after save
- bUnit + Playwright coverage; verifier found 3 issues fixed (HandleUsePickedAsync, ExistingLine param, ShowGrid=false toggle button)

### Session 5: UAT checklist + coverage review + change log
{session5_summary}

## Instructions
- Write a `change-log.md` that summarizes the FULL set across all 5 sessions
- Do NOT narrate each session individually — synthesize what was delivered and why it matters
- Focus on: what the shell can now do vs before, what callers get, what was fixed along the way
- Keep it under 400 words
- Start with a `# Change Log` header
- Use clear section headings
- End with a "Known issues / follow-up" section listing: keyboard navigation deferred, full migration audit deferred, catalog re-crawl needed for new routes
"""

result = route(content, task_type="summarization", session_set="unified-master-details-composite", session_number=5)
print(f"Model: {result.model_name}, Cost: ${result.cost_usd:.4f}")

out = pathlib.Path("docs/session-sets/unified-master-details-composite/change-log.md")
out.write_text(result.content, encoding="utf-8")
print(f"Written to {out}")
print(result.content[:500])
