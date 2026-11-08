# Temporal Tables Spec

> **Purpose:** Add SQL Server temporal-table support (`SysStart`, `SysEnd`,
> `SYSTEM_VERSIONING`) to every transactional entity in the demo app.
> This is the narrowed rewrite of the original `transactional-system-columns`
> stub — see **Scope Rationale** below for why the full spec was split.
> **Created:** 2026-04-19 (original stub); **narrowed:** 2026-05-14.
> **Session Set:** `docs/session-sets/transactional-system-columns/`
> **Roadmap:** `C:\Users\denmi\.claude\plans\ok-i-did-some-abstract-blossom.md`
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Session Set Configuration

```yaml
requiresUAT: false
requiresE2E: false
uatScope: none
```

Reasoning: Temporal tables are a pure database-infrastructure feature. No
user-visible behavior changes — SQL Server manages the history columns
transparently on every write. Coverage is provided by SQL Server integration
tests (not Playwright). UAT and browser E2E are not applicable.

---

## Scope Rationale

The original stub bundled two independent concerns into 3–4 sessions:

**A — Multi-tenancy system columns** (`OrganizationId`, `RoleId`, SaveChanges
interceptor, global EF query filter, replacing `CustomerService.ApplyOrganizationScope()`).

**B — Temporal tracking** (`SysStart`, `SysEnd`, `.IsTemporal()`).

Both GPT-5.4 and Gemini Pro independently reviewed the spec on 2026-05-14
and returned the same verdict: **ship B now, defer A separately.** Key reasons:

- A and B are technically independent — temporal tables work without org columns.
- B is low-risk (1 session, DB-only, no application logic change).
- A is far larger than it looks — interceptor semantics, global filter bypass
  rules for admin roles, backfill strategy, regression risk across all reads.
- `RoleId` on every transactional row needs design rethink (GPT-5.4): a role
  is a session attribute, not stable row ownership. If actor auditing is the
  goal, `CreatedByUserId`/`ModifiedByUserId` is the right model. Do not assume
  `RoleId` on the business row without a dedicated spec to justify it.

The two deferred pieces become their own follow-on specs:

- **`org-scoping-global-filter`** — `OrganizationId` on entities, SaveChanges
  stamping, global EF query filter, replacing the manual
  `CustomerService.ApplyOrganizationScope()` deferral. Prioritize within 1–2
  sets of this one if the app will be genuinely multi-tenant.
- **`actor-audit-columns`** — `CreatedByUserId`, `ModifiedByUserId` stamping
  if actor-level audit attribution is needed. Design independently; do not
  bundle with org scoping.

---

## Goals

1. Call `.IsTemporal()` in the EF Fluent API for each transactional entity:
   `Product`, `Order`, `OrderItem`, `Customer`, `OrderNote`.
2. Configure explicit period-column names (`SysStart`, `SysEnd`) and explicit
   history-table names (`ProductHistory`, `OrderHistory`, `OrderItemHistory`,
   `CustomerHistory`, `OrderNoteHistory`) for predictability in migrations and
   raw queries.
3. Keep `SysStart` / `SysEnd` as **shadow properties** — do not add them to
   the C# entity classes. App code does not stamp them; SQL Server manages
   them on every write automatically.
4. Generate and apply the EF migration. Verify it produces clean
   `WITH (SYSTEM_VERSIONING = ON, HISTORY_TABLE = ...)` DDL against the
   Docker SQL Server instance.
5. Write SQL Server integration tests proving that each temporal entity:
   - creates a history row on update,
   - creates a history row on delete,
   - supports an `AsOf` or `TemporalAll` query via EF Core.
6. Brief doc note on how to query history in tests/exploratory use
   (`TemporalAsOf`, `TemporalAll`, `TemporalFromTo`).

## Non-goals

- `OrganizationId`, `RoleId` columns — deferred to `org-scoping-global-filter`.
- SaveChanges interceptor — deferred.
- Global EF query filter — deferred.
- Replacing `CustomerService.ApplyOrganizationScope()` — deferred; its deferral
  comment already says "until Set C adds Customer.OrganizationId."
- Actor audit columns (`CreatedByUserId`, `ModifiedByUserId`) — deferred to
  `actor-audit-columns`.
- Retention / cleanup policy — operational concern, out of scope.
- History UI / API — no user-facing surfaces for historical data in this set.
- Marker interface or base class for transactional entities — not needed here
  because temporal support is entirely in the EF configuration (shadow
  properties); C# entity classes are unchanged.

---

## Risk notes

**Temporal + org columns ordering gotcha (GPT-5.4).**
If `OrganizationId` is added after history has accumulated, older history rows
will not carry tenant identity. Tenant-scoped historical queries
("show org X's past Order states") would be incomplete for that pre-column
period. Mitigation: ship `org-scoping-global-filter` promptly after this set
if org-scoped historical queries are a real requirement. When adding
`OrganizationId` to a live temporal table, add it nullable first, backfill
current rows, optionally backfill history rows if attribution matters, then
tighten to non-null.

**Schema changes on temporal tables require extra migration steps.**
EF Core handles this by emitting `ALTER TABLE ... SET (SYSTEM_VERSIONING = OFF)`
before schema changes and re-enabling afterward. Review the generated
migration SQL before applying — do not apply blindly.

**Period columns must be DB-managed, not app-stamped.**
Do not write application code that assigns `SysStart` / `SysEnd`. If they
accidentally appear in entity classes rather than as shadow properties,
EF will try to map them as writable columns and the migration DDL may be wrong.
Keep them shadow-only.

**Temporal queries are provider-specific.**
In-memory and SQLite providers do not support temporal queries. All temporal
integration tests must run against the Docker SQL Server instance. Do not
add temporal tests to a test project that uses an in-memory provider.

**Global filters + temporal queries need explicit testing later.**
When the `org-scoping-global-filter` set adds `HasQueryFilter(...)`, verify
that it works correctly with `TemporalAsOf`, `TemporalAll`, and
`IgnoreQueryFilters()` paths. Do not assume global filters behave identically
across temporal and non-temporal query patterns without integration tests.

---

## Session Plan

### Session 1 of 1: Temporal configuration, migration, and integration tests

**Steps:**

1. Read planning docs (`project-guidance.md`, `lessons-learned.md`,
   `session-set-authoring-guide.md`).
2. **Configure `.IsTemporal()`** in the Fluent API for all five entities.
   In each entity's `IEntityTypeConfiguration<T>.Configure()` (files are
   already in `src/Apps/AcmeCoffeeOnline.Api/Data/Configuration/`):
   - Call `builder.ToTable(t => t.IsTemporal(tt => { tt.HasPeriodStart("SysStart"); tt.HasPeriodEnd("SysEnd"); tt.UseHistoryTable("<Entity>History"); }))`.
   - Do not add `SysStart`/`SysEnd` to the C# model classes.
3. **Generate the EF migration.** Run
   `dotnet ef migrations add AddTemporalTables --project ...Api --startup-project ...Api`.
   Review the generated migration SQL: confirm each table gets
   `PERIOD FOR SYSTEM_TIME`, `WITH (SYSTEM_VERSIONING = ON)`, and the
   explicit history table name.
4. **Apply the migration** against the running Docker SQL Server
   (`docker compose -f docker/docker-compose.infra.yml up -d` then
   `dotnet ef database update`). Verify the history tables appear.
5. **Write integration tests** in `tests/AcmeCoffeeOnline.Api.Tests/`.
   Create a `TemporalTablesTests.cs` that seeds a row, updates it,
   and then queries the history table via EF Core temporal API to
   confirm a version row exists. Cover at least `Product`, `Order`,
   and `Customer` (representative sample; not all five required
   individually if architecture is uniform).
6. Route test-generation if the tests are complex:
   `route(task_type="test-generation")`.
7. **Build and test** (`dotnet test Dabbler.Platform.sln`). The temporal
   integration tests must run against Docker SQL Server, not in-memory.
8. **Cross-provider verification** via `route(task_type="session-verification")`.
9. **Change-log, cost report, commit, push, notify.**
10. **Step 9 reorganization review.** Propose: update the
    `project-guidance.md` Architecture Principle about `SysStart`/`SysEnd`
    from "planned" to "implemented"; confirm the `RoleId`-on-every-row
    principle is explicitly deferred pending design of `org-scoping-global-filter`
    and `actor-audit-columns`.

**Creates:** EF entity configs updated (5 files), migration file,
`TemporalTablesTests.cs`.
**Touches:** `ProductConfiguration.cs`, `OrderConfiguration.cs`,
`OrderItemConfiguration.cs`, `CustomerConfiguration.cs`,
`OrderNoteConfiguration.cs`.
**Does NOT touch:** Any entity C# class, any service, any API controller,
any Blazor component, `project-guidance.md` (Step 10 is a separate commit).

**Ends with:** All five transactional tables are system-versioned in Docker
SQL Server. Every write creates a history row. EF Core temporal queries work
in integration tests. No application behavior changes.

**Progress keys:** `session-1/read-planning`, `session-1/temporal-config`,
`session-1/migration-generate`, `session-1/migration-apply`,
`session-1/integration-tests`, `session-1/build-test`,
`session-1/verification`, `session-1/change-log-commit-notify`,
`session-1/reorganization`.

---

## Exit criteria

- Build green on `Dabbler.Platform.sln`.
- Docker SQL Server has history tables for all five entities.
- Integration tests pass against real SQL Server, proving update and delete
  create history rows, and `TemporalAsOf` / `TemporalAll` queries return
  expected results.
- No C# entity class changes. No service, controller, or UI changes.
- `org-scoping-global-filter` spec stub created (or existing stub updated
  to reflect the RoleId rethink) as the acknowledged follow-on.
