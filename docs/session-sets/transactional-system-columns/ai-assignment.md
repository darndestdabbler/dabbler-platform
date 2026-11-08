# ai-assignment.md

## Session 1: Temporal configuration, schema upgrade, and integration tests

### Recommended orchestrator
GPT-4o

### Rationale
This session involves well-defined, provider-specific tasks: modifying EF Core Fluent API configurations, writing T-SQL DDL, and creating xUnit integration tests using EF Core's SQL Server-specific temporal query APIs. GPT-4o has strong proficiency in all three areas. The task is self-contained with no UI or complex business logic changes, making it a low-risk, single-shot implementation suitable for a capable orchestrator.

### Estimated routed cost
$0.15 - $0.30

### Plan

| Step | Action | Routing Decision |
| :--- | :--- | :--- |
| 1 | Configure `.IsTemporal()` with explicit period/history table names in the five `IEntityTypeConfiguration` files. | Orchestrator (mechanical edits) |
| 2 | Author idempotent T-SQL DDL and integrate it into `DatabaseInitializer.EnsureAcmeCoffeeSchemaAsync` to add period columns and enable `SYSTEM_VERSIONING` on the five transactional tables. | route(task_type="code-generation", complexity_hint="medium — requires generating five similar ALTER TABLE...SET SYSTEM_VERSIONING=ON blocks and integrating into the existing idempotent C# schema initializer.") |
| 3 | Create `TemporalTablesTests.cs` to test update/delete history creation and `TemporalAsOf` / `TemporalAll` queries for `Product`, `Order`, and `Customer`. | route(task_type="test-generation", complexity_hint="medium — requires seeding data, performing updates/deletes, and using EF Core's temporal query APIs against a live SQL Server DB.") |
| 4 | Build the solution and run all tests, ensuring the new integration tests pass against the Docker SQL Server instance. | Orchestrator (mechanical edits) |
| 5 | Perform final verification of the implemented code and test results. | route(task_type="session-verification", complexity_hint="low — verify DDL, EF configs, and test correctness.") |
| 6 | Write the change-log, commit, and push. | Orchestrator (mechanical edits) |

### Actuals (filled after the session)

- Orchestrator used: claude-code Claude Opus 4.7 @ effort=high
- Total routed cost: $0.115 (analysis $0.0089 + session-verification $0.1014 + summarization $0.0003 + analysis $0.0049)
- Deviations from recommendation:
  - Routed recommendation suggested GPT-4o; actual orchestrator was Claude Opus 4.7 (human's choice; not a maxout override).
  - Step 2 (raw SQL DDL) was authored directly by the orchestrator instead of routed via `code-generation` — judged mechanical enough (a single 5-row pattern in one method) to fit the Delegation Discipline "single-file / mechanical / <50 lines" criterion. Routed cost saved: small but legitimate.
  - Step 3 (test authoring) was authored directly. The orchestrator had full context on the entity schema, existing test harness pattern (`WebApplicationFactory<Program>`), and EF Core temporal API. Routed cost saved: small.
- Notes for next-session calibration: None — this is the final session of the set.

### Next-session-set recommendation (routed via task_type=analysis on gemini-pro)

- **Recommended:** `org-scoping-global-filter`
- **Why:** Direct, high-priority follow-on per the cross-provider review that narrowed Set C; multi-tenancy foundation for the role-admin rearchitecture roadmap.
- **Prerequisite:** spec authoring (folder does not yet exist).
- **Sized:** ~3 sessions (entity changes + SaveChanges interceptor + global query filter with bypass logic + data backfill).

> This is the final session of the set; no next-session orchestrator recommendation.
