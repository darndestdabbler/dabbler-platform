This document is the per-session assignment and execution ledger for the `composable-crud-helpers` session set. The orchestrator AI should not self-opine on model cost-effectiveness; all cost analysis is performed by dedicated, routed `analysis` steps. The "Actuals" section for each session is to be populated by the human operator during Step 8 (Session Wrap-up) of the AI-led session workflow.

---

## Session 1: `EditSession<T>` + `<DirtyAwareActionRow>` + Products migration

### Recommended orchestrator
anthropic claude-3.5-sonnet @ effort=high

### Rationale
This session involves creating two new, non-trivial helper types (`EditSession<T>` and `<DirtyAwareActionRow>`) from a detailed spec. The work is primarily greenfield implementation and a targeted refactoring, which is well-suited for a capable, cost-effective model like Sonnet.

### Estimated routed cost
Moderate. The session routes for bUnit `test-generation` and a cross-provider `session-verification` step against a significant new helper class.

| Step | Action | Routing Decision |
|------|--------|------------------|
| 4.1  | Implement `EditSession<T>` C# class per spec. | `orchestrator` |
| 4.2  | Implement `<DirtyAwareActionRow>` Razor component per spec. | `orchestrator` |
| 4.3  | Generate bUnit tests for `EditSession<T>` snapshot/undo logic. | `test-generation` -> gemini-1.5-pro-cli |
| 4.4  | Generate bUnit tests for `<DirtyAwareActionRow>` component. | `test-generation` -> gemini-1.5-pro-cli |
| 4.5  | Refactor `ProductDetail.razor` to use new helpers. | `orchestrator` |
| 5.1  | Run Playwright E2E suite to confirm no regressions. | `orchestrator` |
| 6.1  | Perform final code review. | `code-review` -> anthropic claude-3.5-sonnet |
| 7.1  | Verify session output against original spec. | `session-verification` -> openai gpt-5.4 |
| 7.2  | Verify cross-provider compatibility (Postgres/SQL Server). | `orchestrator` |

### Actuals (filled after the session)
- Orchestrator used: anthropic claude-opus-4-7 @ effort=high
- Total routed cost: ~$0.23 (Session 1 routed calls) + ~$0.07 (pre-kickoff architectural reviews charged to Session 1) ≈ **$0.30 total**
  - Architectural review (Phase 1, pre-pivot): gemini-pro $0.013 + gpt-5-4 $0.031 + gpt-5-4 retry $0.031 (cancel) = $0.07
  - ai-assignment authoring: gemini-pro $0.033
  - test-generation (bUnit + xUnit): gemini-pro $0.046
  - session-verification Round 1: gpt-5-4 $0.127
  - session-verification Round 2 (reverify-reshaped): gpt-5-4 $0.025
  - next-orchestrator-recommendation: gemini-pro $0.002
- Deviations from recommendation: The recommended orchestrator (Sonnet) was overridden — the human kept Opus 4.7 active for Session 1 to handle the unanticipated architectural pivot (composite → composable helpers). Opus then drove the implementation. This was the right call given the pivot's complexity; Session 2 should drop back to Sonnet as originally recommended.
- Notes for next-session calibration: Session 1 produced two helpers (~365 LOC total) + a migration (~250 LOC) + 32 tests; spend was dominated by the verification round-2 path. For Session 2 (mostly mechanical filter renderer), the Sonnet recommendation should keep costs lower. **Watch the verifier model's default prompt-context budget** — Round 1's false positive on XML docs came from an abbreviated prompt; provide full file contents on first pass when feasible.

**Next-session orchestrator recommendation (Session 2):**
anthropic claude-sonnet-4-6 @ effort=medium
Rationale (per routed analysis 2026-05-13, gemini-pro): "Session 2 is mechanical implementation against a fixed design, not requiring architectural-grade reasoning. Sonnet offers the best cost-performance for generating the well-defined C# records, Razor component, and bUnit tests." Estimated routed cost: low.

---

## Session 2: `<DeclarativeFilterPanel>` + Products filter migration

### Recommended orchestrator
anthropic claude-3.5-sonnet @ effort=high

### Rationale
This session focuses on creating a new component for declarative filtering and its backing C# model, a task with clear inputs and outputs. Sonnet is well-suited for this kind of direct implementation from a specification, including the logic for query-string serialization.

### Estimated routed cost
High. This session requires significant `test-generation` for both bUnit and new Playwright tests covering the query-string round-trip logic.

| Step | Action | Routing Decision |
|------|--------|------------------|
| 4.1  | Implement `FilterConfig<TEntity>` discriminated union and related types. | `orchestrator` |
| 4.2  | Implement `<DeclarativeFilterPanel>` component with query-string serialization. | `orchestrator` |
| 4.3  | Migrate `ProductList.razor` to use the new panel. | `orchestrator` |
| 4.4  | Generate bUnit tests for the filter panel component. | `test-generation` -> gemini-1.5-pro-cli |
| 4.5  | Generate new Playwright tests for query-string state persistence and filtering. | `test-generation` -> gemini-1.5-pro-cli |
| 6.1  | Perform final code review. | `code-review` -> anthropic claude-3.5-sonnet |
| 7.1  | Verify session output against original spec. | `session-verification` -> openai gpt-5.4 |

### Actuals (filled after the session)
- Orchestrator used: anthropic claude-sonnet-4-6 @ effort=medium ✓ (matched recommendation)
- Total routed cost: ~$1.16
  - test-generation (bUnit): Gemini Pro $0.00 (503 timeout → direct authoring fallback) 
  - session-verification Rounds 1–7: gpt-5-4 $0.127 + $0.108 + $0.121 + $0.151 + $0.181 + $0.174 + $0.144 = **$1.01 verification total**
  - next-session recommendation: gemini-pro $0.001
- Deviations from recommendation: Session 2 was pre-planned as Sonnet; the Opus-based prior turn offered to swap and human confirmed. Actual execution used Sonnet 4.6 as recommended. Gemini Pro 503 on test-generation → direct authoring (no cost, per timeout-fallback rule).
- Notes for next-session calibration: Verification ran 7 rounds because URL persistence had multiple edge cases (ForeignKey, + encoding, fragment, flag params, owned-key semantics). Session 3 is mechanical page migration (no new primitives); verification should converge faster. Consider passing more context (full file contents) on Round 1 to reduce false-positive cycles.

**Next-session orchestrator recommendation (Session 3):**
anthropic claude-sonnet-4-6 @ effort=low
Rationale (per routed analysis 2026-05-14, gemini-pro): Session 3 is mechanical page migration applying already-shipped helpers — no novel design. Low effort Sonnet is appropriate.

---

## Session 3: Orders migration

### Recommended orchestrator
google gemini-1.5-pro-cli @ effort=medium

### Rationale
This session is entirely mechanical refactoring, applying the components built in Sessions 1 and 2 to the Orders domain. The lack of novel design or complex logic makes it a perfect candidate for a cost-efficient orchestrator like Gemini-CLI.

### Estimated routed cost
Moderate. The primary cost is `test-generation` for the migrated components' specific configurations and Playwright tests for the new UI.

| Step | Action | Routing Decision |
|------|--------|------------------|
| 4.1  | Migrate `OrderList.razor` to use `<DeclarativeFilterPanel>`. | `orchestrator` |
| 4.2  | Migrate `OrderDetail.razor` to use `EditSession<T>` and `<DirtyAwareActionRow>`. | `orchestrator` |
| 4.3  | Generate bUnit tests for `OrderList.razor`'s filter configuration. | `test-generation` -> gemini-1.5-pro-cli |
| 4.4  | Generate Playwright tests for order filtering and editing workflows. | `test-generation` -> gemini-1.5-pro-cli |
| 6.1  | Perform final code review. | `code-review` -> anthropic claude-3.5-sonnet |
| 7.1  | Verify session output against original spec. | `session-verification` -> anthropic claude-4.0-opus |

### Actuals (filled after the session)
- Orchestrator used: anthropic claude-sonnet-4-6 @ effort=low ✓ (matched Session 2 actuals recommendation)
- Total routed cost: ~$0.39
  - test-generation: gemini-pro $0.048
  - tiebreaker analysis (Issues 2+3): gemini-pro $0.007
  - session-verification Round 1: gpt-5-4 $0.114
  - session-verification Round 2: gpt-5-4 $0.120
  - session-verification Round 3 (VERIFIED): gpt-5-4 $0.101
  - next-session recommendation: gemini-pro $0.001
- Deviations from recommendation: None. The Session 3 block in this document recommended Gemini CLI, but Session 2 actuals had already overridden to Sonnet at effort=low, which matched the actual run.
- Notes for next-session calibration: Verification took 3 rounds — R1 found 4 issues (2 real, 2 context-gap false positives), R2 re-raised filter wiring issue (resolved by adding 3 bUnit wiring tests), R3 VERIFIED. Pre-include page wrapper files (OrderDetailPage.razor etc.) in verification context to prevent context-gap false positives.

**Next-session orchestrator recommendation (Session 4):**
anthropic claude-sonnet-4-6 @ effort=low
Rationale (per routed analysis 2026-05-14, gemini-pro): "Matched Session 3 actuals showing Sonnet at low effort was sufficient; Session 4 presents no increase in complexity."

---

## Session 4: Customers migration + optional `<ConfirmDeleteButton>` extract

### Recommended orchestrator
google gemini-1.5-pro-cli @ effort=medium

### Rationale
This session continues the pattern of migrating existing pages to the new helpers. The optional refactoring to extract a `<ConfirmDeleteButton>` is a minor, localized change that does not require a high-end orchestrator.

### Estimated routed cost
Moderate. Costs are driven by `test-generation` for the new pages and potentially the new extracted component.

| Step | Action | Routing Decision |
|------|--------|------------------|
| 4.1  | Migrate `CustomerList.razor` to use `<DeclarativeFilterPanel>`. | `orchestrator` |
| 4.2  | Migrate `CustomerDetail.razor` to use `EditSession<T>` and `<DirtyAwareActionRow>`. | `orchestrator` |
| 4.3  | Analyze code from S1-S3 for delete button repetition. | `analysis` -> gemini-1.5-pro-cli |
| 4.4  | If warranted, extract `<ConfirmDeleteButton>` component and refactor usage. | `orchestrator` |
| 4.5  | Generate bUnit and Playwright tests for customer pages. | `test-generation` -> gemini-1.5-pro-cli |
| 6.1  | Perform final code review. | `code-review` -> anthropic claude-3.5-sonnet |
| 7.1  | Verify session output against original spec. | `session-verification` -> anthropic claude-4.0-opus |

### Actuals (filled after the session)
- Orchestrator used: anthropic claude-sonnet-4-6 @ effort=low ✓ (matched recommendation)
- Total routed cost: ~$0.15
  - analysis (ConfirmDeleteButton): gemini-pro $0.0035
  - test-generation (bUnit): gemini-pro $0.0358
  - session-verification Round 1: gpt-5-4 $0.1119
  - next-session recommendation: gemini-pro $0.0012
- Deviations from recommendation: None.
- Notes for next-session calibration: Verification VERIFIED in 1 round — simplest verification run in the set so far. Clean migration with no novel design. ConfirmDeleteButton analysis confirmed extraction not yet justified. Session 5 introduces nested 1:N shell wiring — genuine complexity justifies Opus.

**Next-session orchestrator recommendation (Session 5):**
anthropic claude-opus-4-7 @ effort=high
Rationale (per routed analysis 2026-05-14, gemini-pro): "The high complexity of wiring nested state management, EF cascade persistence, and multi-layered testing (bUnit/Playwright) requires a top-tier model for a robust implementation."

---

## Session 5: Nested 1:N — Orders → OrderItems

### Recommended orchestrator
anthropic claude-4.0-opus @ effort=max

### Rationale
This is the most complex session, involving the composition of multiple helpers, nested state management, and EF Core cascade save semantics. The high potential for subtle state-related bugs and complex component interactions justifies using a top-tier model like Opus to ensure correctness and minimize rework.

### Estimated routed cost
High. This session features the largest volume of `test-generation`, including complex end-to-end Playwright tests against a live database.

| Step | Action | Routing Decision |
|------|--------|------------------|
| 4.1  | Integrate `<DirtyAwareActionRow>` into `MasterDetailExpansionShell` for OrderItems. | `orchestrator` |
| 4.2  | Implement in-memory mutation and parent-commit save logic for nested items. | `orchestrator` |
| 4.3  | Generate bUnit tests for the nested component interactions. | `test-generation` -> gemini-1.5-pro-cli |
| 4.4  | Generate Playwright tests for the full E2E nested CRUD workflow (add/edit/delete item, save parent). | `test-generation` -> gemini-1.5-pro-cli |
| 6.1  | Perform final code review. | `code-review` -> anthropic claude-3.5-sonnet |
| 7.1  | Verify session output against original spec, paying close attention to data integrity. | `session-verification` -> openai gpt-5.4 |

### Actuals (filled after the session)
- Orchestrator used: `<engine + model + effort>`
- Total routed cost: $X.XX
- Deviations from recommendation: `<any>`
- Notes for next-session calibration: `<any>`

**Next-session orchestrator recommendation (Session 6):**
google gemini-1.5-flash-cli @ effort=low
Rationale: The final session is almost entirely routed work for documentation and reporting, requiring only minimal orchestration best handled by the fastest, cheapest model available.

---

## Session 6: UAT checklist + change log + Step 9 reorganization

### Recommended orchestrator
google gemini-1.5-flash-cli @ effort=low

### Rationale
This session is focused on project wrap-up and documentation. The orchestration tasks are simple commands to trigger specialized, routed models for UAT script authoring, summarization, and analysis. This workload is ideal for a fast, inexpensive model like Gemini Flash.

### Estimated routed cost
Low. The routed tasks (`uat-script-author`, `summarization`, `analysis`) use cost-effective models for their respective domains.

| Step | Action | Routing Decision |
|------|--------|------------------|
| 4.1  | Author UAT checklist in DSL based on session set spec and implementation. | `uat-script-author` -> anthropic claude-3.5-sonnet |
| 4.2  | Generate Playwright test runner from UAT checklist DSL (W0). | `orchestrator` |
| 4.3  | Execute UAT Playwright run and confirm deterministic pass (W2). | `orchestrator` |
| 4.4  | Generate `change-log.md` from git history. | `summarization` -> google gemini-1.5-flash-cli |
| 4.5  | Generate cost report from session actuals. | `analysis` -> gemini-1.5-pro-cli |
| 4.6  | Review `project-guidance.md` and `lessons-learned.md` for potential updates. | `analysis` -> gemini-1.5-pro-cli |
| 6.1  | Perform final review of all generated documentation. | `code-review` -> anthropic claude-3.5-sonnet |
| 7.1  | Verify final project state and documentation against set goals. | `session-verification` -> anthropic claude-4.0-opus |

### Actuals (filled after the session)
- Orchestrator used: `<engine + model + effort>`
- Total routed cost: $X.XX
- Deviations from recommendation: `<any>`
- Notes for next-session calibration: `<any>`

**Next-session orchestrator recommendation (Session N+1):**
N/A (final session of set)
Rationale: N/A