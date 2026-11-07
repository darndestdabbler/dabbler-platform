# transactional-system-columns — change log

This session introduced SQL Server temporal table support for all five transactional entities (Product, Order, OrderItem, Customer, OrderNote) in the demo application. This enables automatic tracking of row-level history for these entities. The session set was a 1-of-1, completed 2026-05-15, with cross-provider verification (GPT-5.4) returning VERIFIED.

**Delivered**

- SQL Server temporal table support for Product, Order, OrderItem, Customer, and OrderNote entities.
- Each entity is configured with `.IsTemporal()` using explicit `SysStart`/`SysEnd` period column names (shadow properties — not on the C# model classes) and explicit history-table names (`ProductHistory`, `OrderHistory`, `OrderItemHistory`, `CustomerHistory`, `OrderNoteHistory`).

**Approach notes**

- `AcmeCoffeeDbContext` continues to use `EnsureCreatedAsync()` (it does not use EF migrations). The spec's wording about "EF migrations" was generalized; the human-chosen path for this set was to honor the existing pattern.
- Idempotent raw SQL DDL was added to `DatabaseInitializer.EnsureAcmeCoffeeSchemaAsync` to convert existing tables to `SYSTEM_VERSIONED` with the named history tables, guarded by `sys.tables.temporal_type <> 2`. Fresh databases get the same shape via EF's `.IsTemporal()` on `EnsureCreatedAsync`.

**Tests added**

New integration tests in `tests/AcmeCoffeeOnline.Api.Tests/TemporalTablesTests.cs` verify:

- All five tables report `temporal_type = 2` (SYSTEM_VERSIONED).
- All five named history tables exist.
- Update creates a history row (probe pattern on seeded data, restored in `finally`).
- Delete preserves the row in history (only test that creates a new row — Products do not feed the security seed).
- `TemporalAsOf` returns the pre-update value at a captured timestamp.
- `TemporalAll` returns current + historical versions.

All 6 new tests pass; 38/38 in `AcmeCoffeeOnline.Api.Tests`.

**Out of scope (follow-on specs)**

- `org-scoping-global-filter` — `OrganizationId` on entities, SaveChanges stamping, global EF query filter.
- `actor-audit-columns` — `CreatedByUserId` / `ModifiedByUserId` stamping if actor-level audit attribution is needed.

**Cost**

Routed spend this session: $0.11 (analysis $0.009 + session-verification $0.10).
