# Sensitive-Field Masking And Audit Spec

> **Purpose:** Ship the masking *rendering* path that the restriction
> enforcement seam already anticipates (Set B left the hook), wire a
> durable audit log for sensitive-field interactions, and deliver a
> compliance-review viewer the reviewer role can use to inspect
> activity. Closes the last major gap in the role-rearchitecture
> roadmap before Set C (transactional-system-columns).
> **Created:** 2026-04-19 (stub); **fleshed out:** 2026-04-21.
> **Session Set:** `docs/session-sets/sensitive-field-masking-audit/`
> **Roadmap:** `C:\Users\denmi\.claude\plans\ok-i-did-some-abstract-blossom.md`
> **Architecture doc:** [../rearchitecting-role-administration/rearchitecting-role-administration.md](../rearchitecting-role-administration/rearchitecting-role-administration.md)
> **Prerequisite:** Sets A, B, UAT-remediation, and followup-fixes all
> complete (master contains `09c57a1` or later). The restriction
> enforcement seam ([`IRestrictionBinder`](../../../src/Libs/Dabbler.Security.Blazor/Components/Restriction/IRestrictionBinder.cs))
> and the `data-masked="true"` placeholder in
> [`RestrictedFormField.razor`](../../../src/Libs/Dabbler.Security.Blazor/Components/Restriction/RestrictedFormField.razor)
> must be in place — they are.
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Context

Set B landed the `DefaultMaskRule` and `MaskExemption` configuration
surface, the Default Masks dashboard tab, the Mask Exemptions sub-tab
(with full separation-of-duties authority in
[`MaskExemptionAuthority.cs`](../../../src/Libs/Dabbler.Security/Services/MaskExemptionAuthority.cs)),
and the `IsMasked` + `MaskFormat` fields on `RestrictionResolution`.
What Set B explicitly did **not** ship was:

- Anything that renders the mask format into an actual visible value
  (today the placeholder is a hardcoded `<span data-masked="true">********</span>`
  at [`RestrictedFormField.razor:19-25`](../../../src/Libs/Dabbler.Security.Blazor/Components/Restriction/RestrictedFormField.razor#L19)).
- Any audit trail of role switches, exemption grants, exemption
  expirations, or unmasked reads of sensitive columns.
- Any viewer for the audit trail.
- A real `Customer.SocialSecurityNumber` field on the Customer entity
  (the metadata-registry key exists, but the model field does not —
  Set B seeded a default-mask rule against a key that does not yet
  have a column behind it).

Set D closes those four gaps in five sessions.

## Design Commitments (Decided Up Front So Session 1 Is Not Blocked)

These decisions are load-bearing for the per-session plans below. If
new information during the set overturns any of them, capture the
deviation in the end-of-session verification prompt per the existing
workflow rules.

- **Temporal table for `AppSensitiveAccessLog`.** SQL Server native
  temporal tables. Matches the Architecture principle in
  `project-guidance.md`: *"Prefer temporal tables for durable
  auditing. When practical, keep the history tables in a separate
  `history` schema while preserving the base table name."*
  Write-once semantics at the application level (we only INSERT),
  but the temporal infrastructure is still the right platform fit
  because a future session-set may need to correct mis-emitted rows
  without losing the raw signal.
- **Mask-format token set.** `{last4}`, `{firstChar}`, and literal
  passthrough of any other characters. Unknown tokens render
  literally (`{unknown}` → `{unknown}`) rather than throwing, so a
  typo in config does not crash rendering. Null stored-values
  render the format with `{last4}` → `****` and `{firstChar}` → `*`
  so absence of the underlying value never leaks through an empty
  unmasked control — consistent with the masking principle in
  `project-guidance.md` → Security And Organization Model.
- **Compliance Review placement.** Sixth in-dashboard tab
  ("Compliance Review") rather than a standalone
  `/admin/sensitive-access-log` page. Three reasons: (a) the tab
  gating infrastructure already supports per-role visibility
  ([`DabblerSecurityDashboard.ResolveVisibleTabs`](../../../src/Libs/Dabbler.Security.Blazor/Components/Admin/DabblerSecurityDashboard.razor)),
  (b) the reviewer personas (SystemAdmin, CrossOrgMonitor) already
  land on the dashboard, and (c) a standalone page duplicates shell,
  drawer, and navigation infra for a read-only viewer. Revisit this
  decision only if a concrete reviewer workflow surfaces that
  benefits from being outside the dashboard.
- **Correlation ID scope.** One `CorrelationId` (Guid) per Blazor
  circuit invocation / HTTP request. Regenerated on navigation.
  All audit rows emitted during the same user-facing action share
  the same `CorrelationId`, so a reviewer can reconstruct "this
  unmasked read happened as part of this role switch" without
  guessing from timestamps.
- **FieldUnmaskedRead batching.** De-dupe per
  `(CorrelationId, EntityKey, EntityId, ColumnKey)` so that
  rendering a 50-row grid with three sensitive columns emits 150
  rows at most per render, not 150 × N-rerender. Reach rather
  than volume is the signal the reviewer wants.
- **Separation-of-duties rules.** Unchanged from Set B.
  `MaskExemptionAuthority.Evaluate()` remains the single source of
  truth. Set D audits the *grants* but does not rewrite the rules.
- **Customer.SocialSecurityNumber becomes real.** Added as a
  nullable `string?` on [`Customer.cs`](../../../src/Apps/AcmeCoffeeOnline.Shared/Models/Customer.cs)
  with a migration and seed-data update so at least one customer
  has a non-null SSN. Without this, end-to-end tests cannot
  exercise a real masked → exempted → unmasked path against the
  column the UAT checklist already references.

## Architecture Context (Grounded In The Existing Code)

### What already exists and is reused unchanged

- [`DefaultMaskRule`](../../../src/Libs/Dabbler.Security/Configuration/DefaultMaskRule.cs) —
  `(EntityKey, ColumnKey, MaskFormat)`. Persisted via
  `SecurityConfigurationManager` + `security-config.json`.
- [`MaskExemption`](../../../src/Libs/Dabbler.Security/Configuration/MaskExemption.cs) —
  includes `TargetRoleName`, `GrantedBy`, `GrantedAt`, `ExpiresAt?`.
- [`MaskExemptionAuthority`](../../../src/Libs/Dabbler.Security/Services/MaskExemptionAuthority.cs) —
  the full separation-of-duties evaluator. Three outcomes:
  `Allowed`, `Denied`, `ExpiryRequired`.
- [`IRestrictionBinder`](../../../src/Libs/Dabbler.Security.Blazor/Components/Restriction/IRestrictionBinder.cs) —
  already returns `IsMasked` and `MaskFormat` in its
  `RestrictionResolution`. Set D reads these as-is.
- [`DefaultMasksGrid`](../../../src/Libs/Dabbler.Security.Blazor/Components/Admin/Masks/DefaultMasksGrid.razor) —
  the add/edit/delete grid in the Default Masks tab.
- [`MaskExemptionsSubTab`](../../../src/Libs/Dabbler.Security.Blazor/Components/Admin/Role/MaskExemptionsSubTab.razor) —
  the exemption grant/renew/delete UI inside Role Master Details.
- [`IRoleSwitcher`](../../../src/Libs/Dabbler.Security/Services/IRoleSwitcher.cs)
  and [`SessionRoleSwitcher`](../../../src/Libs/Dabbler.Security/Services/SessionRoleSwitcher.cs) —
  the hook point for the `RoleSwitch` audit event.
- [`FilterPanel<T>`](../../../src/Libs/Dabbler.Components/FilterPanel/FilterPanel.razor.cs) —
  used by `DefaultMasksGrid`; pattern reused in the viewer.
- [`RestrictedFormField.razor`](../../../src/Libs/Dabbler.Security.Blazor/Components/Restriction/RestrictedFormField.razor)
  and `RestrictedGridColumn.razor` — both already accept a
  `MaskedTemplate` `RenderFragment` parameter. The hardcoded
  placeholder is only the fallback when `MaskedTemplate` is null.
  Set D fills in a default template that calls `IFieldMaskService`.

### What Set D creates

| New asset | Where | Shape |
|---|---|---|
| `IFieldMaskService` | `src/Libs/Dabbler.Security/Services/IFieldMaskService.cs` | `string RenderMasked(DefaultMaskRule rule, string? storedValue)` — pure, token substitution + null handling |
| `FieldMaskService` | `src/Libs/Dabbler.Security/Services/FieldMaskService.cs` | DI-scoped concrete; stateless |
| `AppSensitiveAccessLog` entity | `src/Libs/Dabbler.Security/Models/AppSensitiveAccessLog.cs` | See §Data model below |
| `SensitiveAccessEventKind` enum | Same file | `RoleSwitch | FieldUnmaskedRead | ExemptionGranted | ExemptionExpired` |
| EF configuration | `src/Libs/Dabbler.Security/Configuration/AppSensitiveAccessLogConfiguration.cs` | Temporal table, history in `history` schema |
| Migration | `src/Libs/Dabbler.Security/Migrations/…` | Creates `dbo.AppSensitiveAccessLog` + history |
| `ISensitiveAccessAuditor` | `src/Libs/Dabbler.Security/Services/ISensitiveAccessAuditor.cs` | Writes audit rows; fanout for RoleSwitch / ExemptionGranted / ExemptionExpired / FieldUnmaskedRead |
| `IRenderCorrelationScope` | `src/Libs/Dabbler.Security/Services/IRenderCorrelationScope.cs` | Holds `CorrelationId` per circuit / request; regenerates on navigation |
| `ComplianceReviewTab.razor` | `src/Libs/Dabbler.Security.Blazor/Components/Admin/ComplianceReview/ComplianceReviewTab.razor` | 6th tab; filter + grid + CSV export |
| `ICsvExportService` + default impl | `src/Libs/Dabbler.Components/Csv/ICsvExportService.cs` | Generic `ExportAsync<T>(rows, columns)` → `byte[]` |
| `Customer.SocialSecurityNumber` field | `src/Apps/AcmeCoffeeOnline.Shared/Models/Customer.cs` | `string?` nullable; migration; seed update |
| Playwright coverage | `tests/AcmeCoffeeOnline.Playwright/SensitiveFieldMaskingUatCoverageTests.cs` | 1:1 with UAT checklist |
| UAT checklist | `docs/session-sets/sensitive-field-masking-audit/sensitive-field-masking-audit-uat-checklist.json` | Per-session-set convention |

### Data model — `AppSensitiveAccessLog`

Fixed shape per the four-event design. Nullable on columns that do not
apply to every event kind (a RoleSwitch event has no EntityKey /
ColumnKey; a FieldUnmaskedRead has no TargetRoleName or ExpiresAt).

| Column | Type | Notes |
|---|---|---|
| `Id` | `long` PK identity | |
| `EventKind` | `SensitiveAccessEventKind` (int) | Discriminator |
| `Timestamp` | `DateTime` UTC | The logical event time |
| `CorrelationId` | `Guid` | Shared across rows emitted in the same render/action |
| `UserId` | `int` | The user whose active role triggered the event |
| `RoleName` | `string` | Canonical `Name`, not `DisplayAlias` |
| `OrganizationId` | `int?` | When the event is org-scoped |
| `EntityKey` | `string?` | e.g., `"Customer"` |
| `EntityId` | `string?` | Stringified to avoid per-entity FKs |
| `ColumnKey` | `string?` | e.g., `"SocialSecurityNumber"` |
| `TargetRoleName` | `string?` | For `ExemptionGranted` / `ExemptionExpired` |
| `GranterUserId` | `int?` | For `ExemptionGranted` (matches `MaskExemption.GrantedBy`) |
| `ExpiresAt` | `DateTimeOffset?` | For `ExemptionGranted` |
| `Details` | `string?` | Free-form; bounded at 1024 chars; used sparingly |
| `SysStart` | `datetime2` | Temporal period start |
| `SysEnd` | `datetime2` | Temporal period end |

Indexes: `(UserId, Timestamp desc)`, `(EntityKey, ColumnKey, Timestamp desc)`,
`(CorrelationId)`.

---

## Session Set Configuration

```yaml
requiresUAT: false
requiresE2E: false
```

## Sessions
### Session 1 of 5: Field mask rendering service, Customer.SSN field, placement decision locked in

**Purpose:** Replace the hardcoded `<span data-masked="true">********</span>`
fallback with a service-driven render that honors `MaskFormat`, and
make `Customer.SocialSecurityNumber` a real column so every downstream
session can exercise the actual masking path end-to-end.

**Steps:**

1. Read `docs/planning/project-guidance.md` and
   `docs/planning/lessons-learned.md`.
2. Add `SocialSecurityNumber` (`string?`, max length 11) to
   [`Customer.cs`](../../../src/Apps/AcmeCoffeeOnline.Shared/Models/Customer.cs).
   EF migration. Update seed data so at least one customer has a
   non-null, realistic SSN (`"123-45-6789"` is fine — it is a known
   test SSN).
3. Create `IFieldMaskService` + `FieldMaskService` per the shape in
   the Architecture Context table. Rules:
   - `{last4}` → last 4 characters of the stored value, or `****` if
     the value is null or has fewer than 4 characters.
   - `{firstChar}` → first character, or `*` if null/empty.
   - Any other `{…}` token renders literally (no throw).
   - If `storedValue` is null, the entire render still produces a
     non-empty masked string so presence-vs-absence does not leak.
4. Unit tests in `tests/Dabbler.Security.Tests/` covering: SSN
   format (`"XXX-XX-{last4}"`), null input, empty input, unknown
   token, multi-token format.
5. Wire the service into
   [`RestrictedFormField.razor`](../../../src/Libs/Dabbler.Security.Blazor/Components/Restriction/RestrictedFormField.razor):
   when `IsMasked` and no explicit `MaskedTemplate` is supplied,
   render `@FieldMaskService.RenderMasked(resolution.MaskFormat, Value)`
   inside the `data-masked="true"` span. Same wiring in
   `RestrictedGridColumn.razor`.
6. bUnit coverage in `tests/Dabbler.Security.Blazor.Tests/` that
   asserts a `Customer.SocialSecurityNumber = "123-45-6789"` +
   default mask `"XXX-XX-{last4}"` renders as `XXX-XX-6789`.
7. Build + test:
   ```bash
   dotnet build Dabbler.Platform.sln
   dotnet test Dabbler.Platform.sln
   ```
8. Cross-provider verification via
   `route(task_type="session-verification")`. Include the new
   service, the modified Razor components, and the test files.
9. Log every step. Commit + push. No Step 9 (not the last session).

**Creates:** `IFieldMaskService.cs`, `FieldMaskService.cs`, Customer
SSN migration file, new unit + bUnit tests.
**Touches:** `Customer.cs`, `RestrictedFormField.razor`,
`RestrictedGridColumn.razor`, seed data files, DbContext (if any
registration is needed — the `IFieldMaskService` is DI-registered in
`ServiceCollectionExtensions`).
**Ends with:** A masked Customer.SSN field renders as
`XXX-XX-6789` through the existing enforcement seam, with bUnit +
unit coverage.
**Progress keys:** `session-1/read-planning`, `session-1/customer-ssn-field`,
`session-1/mask-service`, `session-1/mask-service-tests`,
`session-1/wire-components`, `session-1/bunit-tests`,
`session-1/build-and-test`, `session-1/verification`,
`session-1/commit-push-notify`.

---

### Session 2 of 5: `AppSensitiveAccessLog` schema + RoleSwitch and exemption-lifecycle audit

**Purpose:** Create the audit table with its four event kinds and
emit three of them: `RoleSwitch`, `ExemptionGranted`,
`ExemptionExpired`. Leave `FieldUnmaskedRead` for Session 3.

**Steps:**

1. Read planning docs.
2. Create `SensitiveAccessEventKind` enum and `AppSensitiveAccessLog`
   entity per the data-model table above.
3. EF Core configuration with SQL Server temporal-table syntax.
   History in `history` schema, base table `dbo.AppSensitiveAccessLog`.
   Indexes as listed. Generate migration.
4. Create `ISensitiveAccessAuditor` with one method per event kind
   plus a generic `AuditAsync(AppSensitiveAccessLog row)`. The
   service reads current `UserId`, `RoleName` (canonical), and
   `CorrelationId` (from `IRenderCorrelationScope` — placeholder for
   Session 1's stub; full impl in Session 3).
5. Decorator pattern for `IRoleSwitcher`: wrap
   [`SessionRoleSwitcher`](../../../src/Libs/Dabbler.Security/Services/SessionRoleSwitcher.cs)
   with `AuditingRoleSwitcher` that emits a `RoleSwitch` row
   post-switch (on both `SwitchToRole` and `ClearSwitch` — the
   latter reverting to the user's base role also counts as a
   switch event). DI registration updates accordingly.
6. Hook `ExemptionGranted`: in
   [`MaskExemptionsSubTab.AddAsync()`](../../../src/Libs/Dabbler.Security.Blazor/Components/Admin/Role/MaskExemptionsSubTab.razor),
   after the authority check passes and the exemption persists,
   emit an audit row. Record `GranterUserId` from the current user,
   `TargetRoleName` from the exemption, and `ExpiresAt` from the
   exemption. Same hook in `RenewAsync()`.
7. `ExemptionExpired` detection is query-time, not scheduler-based
   (no background service). When `IRestrictionBinder.Resolve`
   finds a matching exemption with `ExpiresAt < now`, emit one
   `ExemptionExpired` row **once per exemption** (use an
   in-memory set of already-emitted exemption IDs keyed per circuit
   to avoid duplicate rows on every subsequent resolve). Row carries
   the original `TargetRoleName`, `EntityKey`, `ColumnKey`, and
   `ExpiresAt`.
8. Integration tests under `tests/Dabbler.Security.IntegrationTests/`:
   - Role switch emits a RoleSwitch row with correct fields.
   - Exemption grant emits an ExemptionGranted row with correct
     granter / target / expiry.
   - Exemption resolution past expiry emits exactly one
     ExemptionExpired row, then no further rows on repeated access.
9. Build + test + cross-provider verification. Commit + push.

**Creates:** `AppSensitiveAccessLog.cs`, `SensitiveAccessEventKind.cs`,
`AppSensitiveAccessLogConfiguration.cs`, migration, `ISensitiveAccessAuditor`,
`AuditingRoleSwitcher`, integration tests.
**Touches:** `SessionRoleSwitcher.cs` DI wiring,
`MaskExemptionsSubTab.razor(.cs)`, `IRestrictionBinder` impl (the
expired-exemption detection path).
**Ends with:** RoleSwitch, ExemptionGranted, and ExemptionExpired
events land in the temporal audit table, verified by integration
tests.
**Progress keys:** `session-2/read-planning`, `session-2/audit-entity`,
`session-2/ef-config-migration`, `session-2/auditor-service`,
`session-2/role-switch-decorator`, `session-2/exemption-granted-hook`,
`session-2/exemption-expired-detection`, `session-2/integration-tests`,
`session-2/build-and-test`, `session-2/verification`,
`session-2/commit-push-notify`.

---

### Session 3 of 5: FieldUnmaskedRead interception with per-render `CorrelationId` batching

**Purpose:** Emit `FieldUnmaskedRead` rows when a user with an active
exemption actually reads a sensitive field, batched per render so a
50-row grid with 3 sensitive columns does not emit 150+ rows per
repaint.

**Steps:**

1. Read planning docs.
2. Implement `IRenderCorrelationScope` as a Blazor scoped service:
   - Generates a new `Guid` on circuit start.
   - Regenerates on `NavigationManager.LocationChanged`.
   - Exposes `Current { get; }` for the active `CorrelationId`.
   - Also exposes `ResetForRequest()` for non-circuit API contexts
     (the API layer consumes the same auditor indirectly via any
     handler that resolves mask restrictions).
3. In the restriction-resolution path
   ([`IRestrictionBinder.ResolveAsync`](../../../src/Libs/Dabbler.Security.Blazor/Components/Restriction/IRestrictionBinder.cs)),
   when the verdict is "exempted → unmasked":
   - Compute the batch key `(CorrelationId, EntityKey, EntityId, ColumnKey)`.
     Session 1 does not carry `EntityId` through `RestrictionResolution` —
     extend the resolution type or pass it as a separate parameter on
     `ResolveAsync`.
   - Maintain an in-memory set per `IRenderCorrelationScope` (so the
     set auto-resets when the scope regenerates). If the key is
     already in the set, no-op. Otherwise add and call
     `ISensitiveAccessAuditor.AuditFieldUnmaskedReadAsync`.
4. Wire `RestrictedFormField` and `RestrictedGridColumn` to pass the
   `EntityId` through to the binder (they already know the entity
   instance via `ChildContext` / `Value`).
5. Unit tests for `RenderCorrelationScope` (new Guid per circuit,
   regeneration on nav).
6. Integration tests:
   - Rendering the same customer row with SSN twice in a single
     correlation scope emits only one FieldUnmaskedRead row.
   - Navigating to a new page regenerates the scope and the next
     read emits another row.
   - A grid with N customers and 2 sensitive columns emits exactly
     2N rows in one render, not 2N × rerender count.
7. Build + test + cross-provider verification. Commit + push.

**Creates:** `IRenderCorrelationScope.cs`, `RenderCorrelationScope.cs`,
integration tests for the batching.
**Touches:** `IRestrictionBinder` + impl, `RestrictionResolution` (if
`EntityId` needs adding), `RestrictedFormField.razor(.cs)`,
`RestrictedGridColumn.razor(.cs)`.
**Ends with:** `FieldUnmaskedRead` rows are correctly emitted once
per `(correlation, entity, id, column)` per user action.
**Progress keys:** `session-3/read-planning`, `session-3/correlation-scope`,
`session-3/binder-emit`, `session-3/component-wiring`,
`session-3/unit-tests`, `session-3/integration-tests`,
`session-3/build-and-test`, `session-3/verification`,
`session-3/commit-push-notify`.

---

### Session 4 of 5: Compliance Review viewer (6th dashboard tab) + CSV export

**Purpose:** Expose the audit trail through a read-only, filterable,
exportable viewer. Sixth dashboard tab, per the decision locked in
above.

**Steps:**

1. Read planning docs.
2. Add `ComplianceReviewTab.razor` to
   [`DabblerSecurityDashboard`](../../../src/Libs/Dabbler.Security.Blazor/Components/Admin/DabblerSecurityDashboard.razor)'s
   `AllTabs` list as the 6th entry. `AllowedBakedInRoles`:
   SystemAdmin, CrossOrgMonitor, CrossOrgUserAdmin. Insert
   alphabetically in the tab order? No — explicit order matters
   in the dashboard; insert at the end so the existing UAT test
   expectations for tab positions 1–5 do not shift.
3. Build the viewer:
   - `FilterPanel<AppSensitiveAccessLog>` with columns: User
     (text), Date range, Organization (cascading select from
     `OrganizationRegistry`), Entity (select from
     `MetadataRegistry.Tables`), Column (cascading select),
     Event kind (enum multi-select).
   - `MudDataGrid<AppSensitiveAccessLog>` with `ServerData`
     callback that applies the composite filter + paging against
     the EF context. `Bordered="true"` per the MudBlazor
     Usage convention; right-align Timestamp. Columns:
     Timestamp, EventKind (chip), User, Role, Entity, Column,
     TargetRole, GranterUser, ExpiresAt.
   - CorrelationId is **not** a filterable column (internal
     concept) but is available on a row-expander that shows
     "Related events in this correlation" — a secondary grid
     keyed on `CorrelationId`. Useful for the
     role-switch-then-read reconstruction.
4. `ICsvExportService` in `src/Libs/Dabbler.Components/Csv/` +
   default impl that takes `IEnumerable<T>` and an ordered list of
   `(header, Func<T, object?>)` column definitions. Returns a
   `byte[]` with CRLF line endings and RFC 4180 quoting.
5. Export button on the viewer: calls `ICsvExportService`, triggers
   a JS-assisted download via a new `downloadFile.js` (one small
   invoker). Export respects the current filter, not the whole
   table.
6. bUnit coverage: filters apply correctly, grid refreshes,
   correlation-row expansion shows related events, CSV button
   fires (assert on `IJSRuntime` mock).
7. Playwright sequence-reachability test in
   `tests/AcmeCoffeeOnline.Playwright/` (SmokeTests addition):
   sign in as SystemAdmin, navigate to Compliance Review tab via
   the drawer, confirm grid renders.
8. Build + test + cross-provider verification. Commit + push.

**Creates:** `ComplianceReviewTab.razor(.cs)`,
`ICsvExportService.cs`, `CsvExportService.cs`, `downloadFile.js`,
bUnit tests, one smoke test.
**Touches:** `DabblerSecurityDashboard.razor(.cs)` (AllTabs list),
DI registration for the CSV service.
**Ends with:** SystemAdmin / CrossOrgMonitor / CrossOrgUserAdmin can
open Compliance Review, filter by user/org/entity/column/event kind
+ date range, expand a row to see the correlation cluster, and
export the filtered view as CSV.
**Progress keys:** `session-4/read-planning`, `session-4/tab-wiring`,
`session-4/viewer-ui`, `session-4/csv-service`,
`session-4/export-button`, `session-4/bunit-tests`,
`session-4/smoke-test`, `session-4/build-and-test`,
`session-4/verification`, `session-4/commit-push-notify`.

---

### Session 5 of 5: UAT checklist + 1:1 Playwright coverage + change log + Step 9 reorg

**Purpose:** Close the set. Per-set UAT checklist with full Playwright
parity, generated change log, and the end-of-set reorganization
review.

**Steps:**

1. Read planning docs.
2. Generate
   `docs/session-sets/sensitive-field-masking-audit/sensitive-field-masking-audit-uat-checklist.json`
   via `route(task_type="uat-plan-generation")`. The checklist
   exercises the canonical path:
   - Sign in as a Customer persona; open a customer detail
     page; confirm SSN renders as `XXX-XX-6789`.
   - Sign in as CrossOrgUserAdmin; open the Role Master Details
     tab → the matching user-type role → Mask Exemptions sub-tab;
     grant a 7-day exemption on `Customer.SocialSecurityNumber`
     for the Customer role.
   - Switch role to the Customer persona; re-open the detail
     page; confirm SSN renders unmasked as `123-45-6789`.
   - Sign in as CrossOrgMonitor; open the Compliance Review
     tab; filter by the Customer user; confirm at least one
     `RoleSwitch`, one `ExemptionGranted`, and one
     `FieldUnmaskedRead` row appear in the grid.
   - Expand the `FieldUnmaskedRead` row; confirm the related
     `RoleSwitch` row is present in the correlation cluster.
   - Export CSV; confirm the downloaded file has those rows with
     the same fields.
3. Author
   `tests/AcmeCoffeeOnline.Playwright/SensitiveFieldMaskingUatCoverageTests.cs`
   with the structured-`Steps` schema — step numbers, actions,
   parameters, and verifications **identical** to the checklist.
   Wait until the file is green before committing the checklist,
   per the Human UAT Philosophy in `project-guidance.md`.
4. `route(task_type="uat-coverage-review")` to confirm 1:1 match.
   Iterate until VERIFIED — align wording early to avoid
   round-escalation per the "Align Checklist Wording To The
   Test's Literal Assertions Early" lesson.
5. `route(task_type="summarization")` to generate
   `docs/session-sets/sensitive-field-masking-audit/change-log.md`.
   Cover: five sessions' scope, the new `AppSensitiveAccessLog`
   table and temporal-history schema, the new Compliance Review
   tab, the CSV export service, the new `Customer.SocialSecurityNumber`
   field, and the silent behavioural change that the placeholder
   `<span data-masked>********</span>` now renders real
   token-substituted values.
6. Full `dotnet build Dabbler.Platform.sln` + `dotnet test Dabbler.Platform.sln`
   + Docker Playwright run. Zero failures, or a documented
   disposition per the "Isolate Flaky-Under-Parallel Tests" lesson.
7. Cross-provider verification. Commit the code, checklist, and
   change log in one push (per Step 8 rule — change log lands in
   the final session's commit).
8. Send session-complete notification via
   `send_session_complete_notification` **before** Step 9.
9. Step 9 reorganization review:
   - Candidate principles from this set:
     - The masking-auditor pattern (write-once temporal rows,
       per-correlation batching) may be abstractable as a
       platform convention for future auditable features.
     - The CSV export service, if reused in future admin pages,
       is a Convention candidate.
   - Candidate lessons from this set (scan the verification
     rounds and UAT coverage-review iterations for reusable
     tactics).
   - Propose each candidate to the human individually; accept /
     reject / relocate per the workflow rules.
   - Apply accepted proposals in a **separate follow-up commit**
     with the standard `Step 9 reorganization` message.

**Creates:** `sensitive-field-masking-audit-uat-checklist.json`,
`SensitiveFieldMaskingUatCoverageTests.cs`, `change-log.md`.
**Touches:** Potentially `project-guidance.md` and
`lessons-learned.md` if Step 9 accepts proposals.
**Ends with:** Set complete. Compliance Review live. UAT checklist
waiting on the human behind 1:1 Playwright parity. Change log
committed. Step 9 review applied or deferred per human decision.
**Progress keys:** `session-5/read-planning`,
`session-5/generate-uat-checklist`,
`session-5/playwright-parity-tests`,
`session-5/uat-coverage-review`, `session-5/generate-change-log`,
`session-5/full-test-run`, `session-5/verification`,
`session-5/commit-push-notify`, `session-5/step-9-reorg`.

---

## What This Set Preserves

| Existing Asset | Status |
|---|---|
| `DefaultMaskRule` / `MaskExemption` config model | Unchanged; reused |
| `MaskExemptionAuthority` separation-of-duties rules | Unchanged; reused |
| Default Masks tab + sub-tab UIs | Unchanged; audit hooks added but no UX regression |
| `IRestrictionBinder.ResolveAsync` contract | Extended (adds `EntityId` parameter) but backward-compatible with an overload |
| Existing 5 Security Dashboard tabs + their role gating | Unchanged order; new tab appended as 6th |
| All pre-existing Playwright and unit tests | Must continue passing |

## Risk Notes

- **Temporal-table restore on fresh dev DBs.** SQL Server temporal
  tables require a history table and correctly-configured periods.
  First developer to run Session 2's migration against an existing
  local database may see a surprising create-history-schema step.
  Verify with a `docker-compose down -v` + fresh seed in Session 2's
  closeout so the migration is known-good end-to-end.
- **Per-render correlation on API calls.** Session 3's scope service
  is designed for Blazor circuits. The API side (handlers that also
  resolve restrictions) needs the same scope, created per HTTP
  request. Easy to forget; flag it in Session 3's verification
  prompt.
- **`FieldUnmaskedRead` volume.** Even with batching, a CrossOrgMonitor
  browsing 50 customers × 3 sensitive columns per page emits 150
  rows per navigation. Over a workday this is thousands of rows.
  Acceptable for now, but the Compliance Review grid must page
  server-side from Day 1 — a client-side paging fallback would
  OOM in front of any real volume.
- **Customer.SSN is PII.** Even in seed data, prefer a
  well-known-test-value format (`"123-45-6789"`). Do not copy
  real SSNs from any source into seeds or tests.
- **Audit trail is append-only at the app layer.** There is no delete
  or update path for `AppSensitiveAccessLog`. If a bad row gets
  emitted (bug), the fix is a new migration or manual DB edit
  outside the app — never an in-app delete.
- **UI contract of the placeholder `<span data-masked="true">********</span>`.**
  Any existing tests that assert on the literal `********` will
  fail after Session 1 replaces it with `XXX-XX-6789` and similar.
  Session 1 must either update those assertions or disclose the
  list in the verification prompt so the verifier does not
  hallucinate a regression.
- **CrossOrgMonitor's access to Compliance Review.** CrossOrgMonitor
  can see audit rows for every organization. This is by design — the
  role exists for cross-org compliance oversight — but the tab
  gating must make that intentional. Called out in Session 4.
