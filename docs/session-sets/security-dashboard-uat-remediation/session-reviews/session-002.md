# Verification Round 1

**Verdict:** `ISSUES_FOUND`

---

## Issue 1 — ConcurrentCallsDoNotThrow unit test is likely a placebo

**Severity:** Minor

**Description:** The `ConcurrentCallsDoNotThrow_SecondOperationOnContext` test uses `Task.WhenAll` with three calls against an InMemory-backed `IDbContextFactory`. The EF Core InMemory provider completes `ToListAsync` synchronously (returning already-completed tasks). When all three tasks resolve synchronously on the same thread, they execute serially — they never genuinely overlap. EF Core's concurrency detector (`ConcurrencyDetector.EnterCriticalSection`) only throws `InvalidOperationException` when a second operation starts while the first is still in-flight. Under InMemory, no operations are ever in-flight across an `await` boundary, so even the **old** scoped-`DbContext` code would likely pass this test.

The architectural fix is correct and is properly validated by the three Playwright tests (especially `UsersTab_RapidRemountDoesNotCrashAsync`, which exercises the real browser race). The unit test is additive — it doesn't mask anything — but it provides false confidence about the specific failure mode it claims to cover.

**Suggested remediation:** Either:
- Rename and reframe the test as a structural assertion (e.g., verify that the SUT constructor accepts `IDbContextFactory<>` and that each public method calls `CreateDbContextAsync` at least once — confirming the factory wiring is in place).
- Or inject a custom `IDbContextFactory` wrapper that introduces a `Task.Yield()` or `Task.Delay` inside `CreateDbContextAsync` to force genuine async interleaving, then test against a real shared context to confirm it would throw.

---

## Issue 2 — `UserRoleAssignmentDetails.razor` neither shown nor disclaimed

**Severity:** Minor

**Description:** The spec's "Touches" list explicitly includes `src/Libs/Dabbler.Security.Blazor/Components/Admin/Users/UserRoleAssignmentDetails.razor`. The Playwright tests assert `page.GetByTestId("user-role-assignment-details")`, implying this `data-testid` attribute exists on the component. The file was neither included in the submission nor mentioned in the out-of-scope disclosures.

Per the lessons-learned entry: *"Send Related Files To The Cross-Provider Verifier Or Disclaim What Is Out Of Scope."*

Two possibilities, both requiring disclosure:
- The `data-testid` was **added** in Session 2 → the diff should have been included.
- The `data-testid` **already existed** → a one-line disclosure ("file listed in spec Touches but required no changes; `data-testid` was pre-existing from Session 1") would satisfy the lesson.

**Suggested remediation:** Amend the submission to include the component diff or add an explicit out-of-scope disclosure confirming the file was unchanged and why.

---

## Issue 3 — Full Playwright regression suite not run

**Severity:** Minor

**Description:** Spec Step 5 states: *"Regression: run the full Playwright suite."* The submission reports: *"Full Playwright suite not re-run; regression surface for the DbContextFactory change is the dashboard, which is green."* This is a spec deviation. The rationale is defensible (the change surface is bounded to dashboard services and DI registration; 14 dashboard Playwright tests passed; 668 non-Playwright tests passed), but the spec is explicit.

The `AddDbContextFactory` registration and the `ServiceLifetime.Scoped` parameter touch the DI container globally. While unlikely, a regression in non-dashboard code that also resolves `DabblerSecurityDbContext` would only be caught by the full suite.

**Suggested remediation:** Run the full Playwright suite before closing the session. If it passes, update the test-results section. If infrastructure constraints prevented running it, document that constraint explicitly rather than rationalizing partial coverage as sufficient.

---

## Issue 4 — Duplicate DbContext configuration lambda

**Severity:** Minor

**Description:** The DI registration now contains two independent configuration lambdas that must stay in sync:

```csharp
services.AddDbContext<DabblerSecurityDbContext>((sp, opts) =>
{
    var cfg = sp.GetRequiredService<IOptions<DabblerSecurityOptions>>().Value;
    opts.UseSqlServer(cfg.ConnectionString);
});

services.AddDbContextFactory<DabblerSecurityDbContext>((sp, opts) =>
{
    var cfg = sp.GetRequiredService<IOptions<DabblerSecurityOptions>>().Value;
    opts.UseSqlServer(cfg.ConnectionString);
}, ServiceLifetime.Scoped);
```

If a future session adds `.EnableSensitiveDataLogging()`, a query filter, or an interceptor to one registration but not the other, the factory-created contexts and the scoped context will silently diverge.

**Suggested remediation:** Extract the configuration into a shared local function or `Action<IServiceProvider, DbContextOptionsBuilder>` variable:

```csharp
static void ConfigureDb(IServiceProvider sp, DbContextOptionsBuilder opts)
{
    var cfg = sp.GetRequiredService<IOptions<DabblerSecurityOptions>>().Value;
    opts.UseSqlServer(cfg.ConnectionString);
}

services.AddDbContext<DabblerSecurityDbContext>(ConfigureDb);
services.AddDbContextFactory<DabblerSecurityDbContext>(ConfigureDb, ServiceLifetime.Scoped);
```

---

## Non-blocking observations (no action required for Session 2)

| Topic | Note |
|---|---|
| **Scoped factory lifetime** | `ServiceLifetime.Scoped` for the factory itself is uncommon but valid. The more typical pattern is `Singleton` (the factory is stateless; contexts it creates are caller-owned). No functional difference here since `IOptions<T>` is effectively singleton, but if a future session switches to `IOptionsSnapshot<T>` for hot-reload, the Scoped lifetime will be required. Current choice is fine. |
| **Deferral of Organization components to Session 4** | Defensible. The audit correctly identified that `OrganizationMasterGrid` and `OrganizationDetailsForm` don't trigger concurrent initial awaits. The residual risk (rapid user selection causing overlapping queries on the details form's scoped context) is real but low-probability and properly scoped to Session 4. |
| **DbContextFactory fix is architecturally correct** | The core refactor — `IDbContextFactory` injection, `await using var db = await _dbFactory.CreateDbContextAsync(ct)` per public method, shared private helper accepting an explicit `db` parameter — is the canonical Blazor Server pattern per Microsoft's guidance. No concerns with the fix itself. |

---

# Verification Round 2

# Verification Round 2

**Verdict:** `VERIFIED`

---

## Re-assessment of Round-1 Issues

### Issue 1 — Placebo concurrent-calls unit test → **Resolved**

The replacement `EachPublicMethodResolvesItsOwnDbContext` test with `CountingDbContextFactory` is a sound structural assertion. It proves the factory is invoked per public method without relying on EF InMemory scheduling semantics. Key details confirmed:

- `IDbContextFactory<T>` defines only `CreateDbContext()` (sync). The `CreateDbContextAsync` the SUT calls is an EF Core extension method that delegates to the sync overload, so the counting wrapper correctly intercepts all factory resolutions.
- Only the two DB-touching public methods (`GetUsersAsync`, `GetActorAssignedOrganizationIdsAsync`) are asserted. The remaining public methods (`GetActorActiveAdminRole`, `GetActorUserId`) read only from `IHttpContextAccessor` and correctly have no factory interaction to assert.
- The inline comment explains the placebo rationale clearly — future maintainers will understand why a behavioral concurrency test was not used.

### Issue 2 — `UserRoleAssignmentDetails.razor` not shown/disclaimed → **Resolved**

The disclosure is explicit and complete: file was unmodified, `data-testid` attributes originate from Set B Session 7, the spec Touches list was pessimistic, and the full changed-file manifest is provided. Satisfies the lessons-learned requirement.

### Issue 3 — Full Playwright regression suite not run → **Resolved**

Suite was run: **90 passed / 5 failed / 0 skipped**. All 14 security dashboard tests pass. The 5 failures are individually attributed to unrelated areas (access-denied rendering → Session 5 scope; test-data pollution; intermittent flake) with change-surface reasoning confirming Session 2's two runtime files (`AdminUserQueryService.cs`, `ServiceCollectionExtensions.cs`) do not touch orders, products, customers, or access-denied flows. Spec Step 5 is now satisfied.

### Issue 4 — Duplicate DbContext configuration lambda → **Resolved**

Extracted to `static void ConfigureSecurityDbContext(...)` and passed to both `AddDbContext` and `AddDbContextFactory`. Single source of truth; future interceptor/filter additions cannot silently diverge. The `UseEfCoreStore` isolation rationale is sound — it operates in a different extension method with deliberately independent lifecycle.

---

## New Concerns Check

No new issues introduced by the fixes. Specifically:

| Area checked | Result |
|---|---|
| `CountingDbContextFactory` interface conformance | Clean — single-method interface correctly intercepted |
| Shared config function scope/capture | `static` local function, no closure risk |
| Playwright test resource cleanup | All three tests use `try/finally` with `ClosePageAsync` |
| `Force = true` in rapid-remount test | Documented rationale; test intent is DbContext regression, not tab-click UX |
| DI registration ordering (`AddDbContext` before `AddDbContextFactory`) | Correct — factory picks up the same `DbContextOptions<T>` registration |
