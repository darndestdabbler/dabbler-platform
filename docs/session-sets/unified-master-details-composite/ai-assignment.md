# AI Assignment — unified-master-details-composite

> **Purpose:** Per-session ledger of the cheapest-capable AI for each step,
> plus forward-looking orchestrator recommendations. Authored at Session 1
> start; actuals appended after each session closes.
>
> **Routed via:** `route(task_type="analysis")` — never self-opined.

---

## Session 1: Generify the shell + migrate `RoleMasterDetailsTab` (Scenario 2 baseline)

### Recommended orchestrator
`claude-code` `claude-sonnet-4-6` @ `effort=medium`

### Rationale
This session involves a mix of mechanical refactoring (shell rewrite, consumer migration), new file creation, and routing to specialized models for testing and verification. A capable, cost-effective sonnet-class orchestrator is appropriate for orchestrating this structured work.

### Estimated routed cost
moderate

| Step | Action | Routing Decision |
|------|--------|------------------|
| 1 | Read planning docs | Direct |
| 2 | Pre-flight Explore: Audit `MasterDetailExpansionShell` consumers | Direct |
| 3 | Rewrite `MasterDetailExpansionShell` as generic | Direct |
| 4 | Add `MasterDetailShellContexts.cs` | Direct |
| 5 | Migrate `RoleMasterDetailsTab` to new slot pattern | Direct |
| 6 | Migrate other shell consumers (OrganizationMasterDetailsTab, DefaultMasksTab, UsersAndRoleAssignmentTab) | Direct |
| 7 | Generate bUnit coverage | `route(task_type="test-generation")` |
| 8 | Build and test | Direct |
| 9 | Cross-provider session verification | `route(task_type="session-verification")` |
| 10 | Commit, push, notify | Direct |

### Actuals (filled after the session)
- Orchestrator used: claude-code / claude-sonnet-4-6 @ effort=high
- Total routed cost: $0.42 (3 routed calls: 2× Gemini Pro for analysis + test-generation, 1× Opus for session-verification)
- Deviations from recommendation: effort=high used vs recommended medium; cost remained low due to Gemini Pro dominating
- Notes for next-session calibration: FilterPanel extraction was more API-design-intensive than a plain slot rename; budget extra design time for Session 2's ShowGrid=false branch

**Next-session orchestrator recommendation (Session 2):**
`claude-code` `claude-sonnet-4-6` @ `effort=high`
Rationale: Session 1's configuration was highly successful; Session 2's ShowGrid=false layout + demo page + bUnit + Playwright has comparable complexity and warrants the same approach.

---

## Session 2: Scenario 1 (Add Only) — `ShowGrid=false` layout + demo

### Recommended orchestrator
`claude-code` `claude-sonnet-4-6` @ `effort=medium`

### Rationale
This session adds a conditional rendering path (`ShowGrid=false`), creates a new demo page, and routes for both bUnit and Playwright test generation. This mix of file modification and routing fits a medium-effort sonnet-class orchestrator.

### Estimated routed cost
moderate

| Step | Action | Routing Decision |
|------|--------|------------------|
| 1 | Read planning docs | Direct |
| 2 | Implement `ShowGrid=false` layout branch in shell | Direct |
| 3 | Implement session counter in shell | Direct |
| 4 | Create/repurpose demo page for Add-Only scenario | Direct |
| 5 | Generate bUnit coverage for new layout and counter | `route(task_type="test-generation")` |
| 6 | Generate Playwright coverage for demo page | `route(task_type="test-generation")` |
| 7 | Build and test | Direct |
| 8 | Cross-provider session verification | `route(task_type="session-verification")` |
| 9 | Commit, push, notify | Direct |

### Actuals (filled after the session)
- Orchestrator used: claude-code / claude-sonnet-4-6 @ effort=high
- Total routed cost: ~$0.54 (Gemini Pro x2 for test-generation $0.039, Opus x1 for session-verification $0.505)
- Deviations from recommendation: effort=high vs recommended medium; needed two verification runs (first failed due to Unicode encoding before saving — second run succeeded)
- Notes for next-session calibration: Verification caught Major issue (duplicate emails across tests) + Minor (fire-and-forget async). Quick fixes. Router recommends Opus for Session 3 due to PickOrAddDialog complexity; original recommendation was Sonnet.

**Next-session orchestrator recommendation (Session 3):**
`claude-code` `claude-opus-4-7` @ `effort=high`
Rationale: PickOrAddDialog is a new generic multi-modal stateful component with dirty-state tracking and dialog-close integration — higher complexity than Sessions 1/2 shell modifications. Routed analysis upgraded recommendation from Sonnet to Opus.

---

## Session 3: `PickOrAddDialog` + Scenario 3 (pick-or-add, no link data)

### Recommended orchestrator
`claude-code` `claude-sonnet-4-6` @ `effort=medium`

### Rationale
This session creates a new composite dialog, implements stateful dirty-state tracking for button relabeling, and wires it into an existing parent form. Standard component development well-suited for a balanced sonnet-class orchestrator.

### Estimated routed cost
moderate

| Step | Action | Routing Decision |
|------|--------|------------------|
| 1 | Read planning docs | Direct |
| 2 | Create generic `PickOrAddDialog<TRow, TEntity, TLink>` component | Direct |
| 3 | Implement default `RowUseAction` template | Direct |
| 4 | Implement "Save-and-Use" label-swap logic (dirty-state) | Direct |
| 5 | Wire `PickOrAddDialog` into a parent form for Scenario 3 | Direct |
| 6 | Generate bUnit coverage for dialog logic and dirty state | `route(task_type="test-generation")` |
| 7 | Generate Playwright coverage for end-to-end Scenario 3 | `route(task_type="test-generation")` |
| 8 | Build and test | Direct |
| 9 | Cross-provider session verification | `route(task_type="session-verification")` |
| 10 | Commit, push, notify | Direct |

### Actuals (filled after the session)
- Orchestrator used: claude-code / claude-opus-4-7 @ effort=high
- Total routed cost: ~$1.52 (gemini-pro test-generation x2: $0.047 + $0.028 = $0.075; opus session-verification: $1.44)
- Deviations from recommendation: matched the upgraded Session 2 next-session recommendation (Opus @ high vs. spec's original Sonnet @ medium). Verification was unfortunately Opus→Opus self-route (~$1.44) because the router selected Opus despite the orchestrator already being Opus.
- Notes for next-session calibration: Verifier flagged one Minor — dirty state reset on validation failure — fixed by throwing OperationCanceledException from the dialog when OnSaveAndUse returns null. Session 4 should consider tightening the router's verifier selection to prefer cross-provider when orchestrator is Opus (or apply the `feedback_self_verify_when_no_cross_provider` rule to skip the verification call).

**Next-session orchestrator recommendation (Session 4):**
`claude-code` `claude-opus-4-7` @ `effort=high`
Rationale: Session 4 introduces the most complex state management (TargetEditability, split dirty flags, multiple interacting sub-scenarios) — justifying a high-effort Opus-class orchestrator.

---

## Session 4: Scenarios 4a / 4b + inspect-to-decide — `LinkDataContent` + `TargetEditability`

### Recommended orchestrator
`claude-code` `claude-opus-4-7` @ `effort=high`

### Rationale
This session introduces the most complex state management logic, with multiple interacting parameters (`TargetEditability`, `ShowLinkData`, split `_entityDirty`/`_linkDirty`), an M2M-with-payload demo, and several sub-scenarios. A high-effort Opus-class orchestrator is justified.

### Estimated routed cost
high

| Step | Action | Routing Decision |
|------|--------|------------------|
| 1 | Read planning docs | Direct |
| 2 | Implement `LinkDataContent` slot and `ShowLinkData` rendering | Direct |
| 3 | Implement `TargetEditability` modes (`Editable` / `ReadOnly` / `ReadOnlyWithToggle`) | Direct |
| 4 | Implement separate `LinkContext` dirty-state tracking (`_linkDirty`) | Direct |
| 5 | Ensure toggle-without-change preserves "Use" label | Direct |
| 6 | Wire Scenarios 4a and 4b in the app | Direct |
| 7 | Wire inspect-to-decide variant of Scenario 3 | Direct |
| 8 | Generate bUnit coverage for all new modes and state interactions | `route(task_type="test-generation")` |
| 9 | Generate Playwright coverage for each sub-scenario | `route(task_type="test-generation")` |
| 10 | Build and test | Direct |
| 11 | Cross-provider session verification | `route(task_type="session-verification")` |
| 12 | Commit, push, notify | Direct |

### Actuals (filled after the session)
- Orchestrator used: TBD
- Total routed cost: TBD
- Deviations from recommendation: TBD
- Notes for next-session calibration: TBD

**Next-session orchestrator recommendation (Session 5):**
`claude-code` `claude-sonnet-4-6` @ `effort=low`
Rationale: Session 5 is primarily procedural (route UAT plan generation, run W2 runner, summarization) — minimal direct coding; sonnet-class at low effort is sufficient.

---

## Session 5: UAT checklist + coverage review + change log + reorganization

### Recommended orchestrator
`claude-code` `claude-sonnet-4-6` @ `effort=low`

### Rationale
This session is almost entirely procedural: routing to specialized tools for UAT script authoring, running the W2 test pipeline, generating the change log, and the Step 9 reorganization review. Low-effort sonnet-class orchestration is sufficient.

### Estimated routed cost
high (driven by routed UAT authoring + session-verification calls, not orchestrator tokens)

| Step | Action | Routing Decision |
|------|--------|------------------|
| 1 | Read planning docs | Direct |
| 2 | Generate UAT checklist | `route(task_type="uat-script-author")` |
| 3 | Run W2 runner (`python -m uat_runner.cli`) | Direct |
| 4 | Full build and Playwright suite | Direct |
| 5 | Cross-provider session verification | `route(task_type="session-verification")` |
| 6 | Generate `change-log.md` | `route(task_type="summarization", max_tier=1)` |
| 7 | Cost report | Direct |
| 8 | Commit, push, notify | Direct |
| 9 | Step 9 reorganization review | Direct (scan) |

### Actuals (filled after the session)
- Orchestrator used: TBD
- Total routed cost: TBD
- Deviations from recommendation: TBD
- Notes for next-session calibration: TBD
