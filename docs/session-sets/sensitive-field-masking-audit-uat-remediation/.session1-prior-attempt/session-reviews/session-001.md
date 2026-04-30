# Verification Round 1

## Session 1 Verification Assessment

### 1. Code Changes — Correctness and Consistency

**user-roles.json**

| Concern | Severity |
|---|---|
| User ID `"2"` present in API file (`CrossOrgUserAdmin`) but absent from Web file | Low — likely intentional (API may serve a different consumer), but undocumented. Should be explicitly noted or reconciled. |
| User ID `"1"` gets `SystemAdmin`, `BackofficeUser`, `CrossOrgMonitor` in both files | ✅ Matches spec's one-user, multi-role persona commitment |

**security-config.json**

- `RoleSwitching.AllowedRoles` correctly lists all three roles. ✅
- `DefaultMasks` has the `Customer.SocialSecurityNumber → XXX-XX-{last4}` entry. ✅
- `MaskExemptions` is empty (clean slate for test isolation). ✅
- No stale or conflicting entries in `UserTypeRoles` that would gate the persona out of the masking flow. ✅

**UserContextBar.razor.cs — The core fix**

```csharp
_allowedSwitchRoles = configuredRoles.Where(rolesSet.Contains).ToList();
```

This is correct. The `HashSet<string>` is constructed with `StringComparer.OrdinalIgnoreCase`, so the `Contains` predicate inherits case-insensitive matching. The prior behavior (implied: `_allowedSwitchRoles = configuredRoles.ToList()`) would have shown roles the user doesn't hold in the switch menu — the fix intersects with the user's actual roles.

One subtlety: `RolePrecedence` omits `CrossOrgMonitor`. This is harmless because `ResolvePrimaryRole()` only governs the *initial* display label, and `_activeRoleDisplay` checks `_switchedRoleOverride` first after any switch. When the user hasn't switched yet and holds `SystemAdmin`, the display reads "System Admin" — correct. After switching to `CrossOrgMonitor`, `FormatRoleName("CrossOrgMonitor")` yields "Cross Org Monitor" — correct.

**No issue with `_canSwitchRole` semantics**: it's `configuredRoles.Any(rolesSet.Contains)`, which is `true` if the user holds *any* allowed role. This preserves the original gate while the menu only shows held roles. ✅

---

### 2. Test Rewrite — Faithfulness to Design

**Real-login commitment**: Every `[Fact]` method contains steps 001–008 performing magic-link login through `/login` → `/dev/email-inbox` → follow href. No `SeededSignInAsync(roles=...)` in any step block. ✅

**Role switching via UserContextBar**: Steps 009–011 use `GetByRole(AriaRole.Button, "System Admin, switch role")` → `GetByRole(AriaRole.Menuitem, "Backoffice User")` → assert snackbar. This is a visible-surface-only interaction. ✅

**Pre-test setup discipline**: Setup helpers (`EnsureNoBackofficeUserExemptionAsync`, `EnsureBackofficeUserExemptionExistsAsync`, `EnsureComplianceAuditEventsExistAsync`) use `SeededSignInAsync` for sign-in (allowed — not a human-equivalent step) and drive the actual config mutation through the Mask Exemptions sub-tab UI (clicking tabs, selecting dropdowns, pressing Add/Remove). They use `GetByTestId` for locators within setup, which is acceptable per the spec:

> *"Test setup that pre-creates state outside the UAT's scope is allowed, but every human-equivalent step in the UAT must be driven through user-visible surfaces only."*

Setup helpers are not human-equivalent steps. ✅

**Remaining locator issues in step blocks** (flagged by coverage reviewer):

| Location | Issue | Impact |
|---|---|---|
| Step 006 (all 6 tests) | `Locator("tbody tr.mud-table-row")`, `Locator("td[data-label='Link'] a")` | Raw CSS selectors — violates DSL visible-label-only rule |
| Step 013 (UAT-1, UAT-3) | `Locator("tbody tr.mud-table-row").Filter(HasText)` | Should be `GetByRole(AriaRole.Row, ...)` |
| Step 011 (UAT-2) | Same row pattern for "Backoffice User" | Same |
| Steps 013–014 (UAT-2) | `.mud-popover-open` framework class | Should use `GetByRole(AriaRole.Option, ...)` |
| Step 015 (UAT-5) | `.mud-table-body tr.mud-table-row` | Raw CSS |
| Steps 016–017 (UAT-5) | `[data-testid*='compliance-review-row-']` | GetByTestId-equivalent; mitigated by `IsJudgmentItem=true` but spec doesn't explicitly exempt judgment items from locator rules |
| Step 016 (UAT-6) | `.mud-snackbar` | Framework class; should use `GetByRole(AriaRole.Alert)` or `GetByText` |
| Steps 012 (multiple tests) | `NavigateViaDrawerAsync`, `NavigateViaDrawerGroupAsync` | Opaque helpers — can't verify locator compliance without source |

These are real DSL violations. The coverage review correctly flagged them. They don't affect *functional* correctness of the tests — the locators will find the right elements — but they violate the convention that step blocks use visible-label locators only.

---

### 3. UAT Checklist Alignment

**Step counts**: All six items match (16, 16, 16, 17, 17, 16). ✅

**E2ETestReference values**: Each Review item's reference resolves to the correct `[Fact]` method. ✅

**Step-by-step semantic alignment** (spot-checked all 98 steps):

| Checklist Step | Code Step | Aligned? |
|---|---|---|
| UAT-1/009: `CLICK BUTTON with aria-label "System Admin, switch role"` | `GetByRole(AriaRole.Button, Name = "System Admin, switch role")` | ✅ |
| UAT-2/013: `OPEN DROPDOWN with label "Table" and select "Customer"` | `GetByLabel("Table").ClickAsync()` → `.mud-popover-open.GetByText("Customer")` | ⚠️ Functional match, locator non-conformant |
| UAT-4/015: `CLICK INPUT with label "Entity" and enter "Customer"` | `GetByLabel("Entity").First.FillAsync(EntityKey)` | ✅ |
| UAT-5/015: `EXPAND ROW in Compliance Review grid` | `Locator(".mud-table-body tr").First → td.First.Click` | ⚠️ Functional match, no visible-label locator for expand action |
| UAT-6/016: `VERIFY snackbar containing "Exported"` | `.mud-snackbar.Filter(HasText = "Exported")` | ⚠️ Functional match, framework-class locator |

Every step performs the correct logical action. The mismatches are all locator-strategy issues, not semantic misalignment. ✅ on alignment, ⚠️ on locator conformance.

---

### 4. Security and Correctness Concerns

**No new security concerns beyond what's noted.** Specific checks:

| Check | Result |
|---|---|
| `SeededSignInAsync(roles=...)` not used in any human-equivalent step block | ✅ Only in pre-test setup |
| Role switch is enforced UI-side; backend `RoleSwitcher.SwitchToRole()` is the real gate | ✅ UI fix is defense-in-depth |
| `MaskExemptions` array is empty in committed config — no accidental exemption leakage | ✅ |
| `EnsureComplianceAuditEventsExistAsync` uses `SeededSignInAsync(..., roles: BackofficeUserRole)` to generate a `FieldUnmaskedRead` event | Acceptable — this is data-seeding for the audit trail, not a UAT step |
| SSN seed data: tests use relaxed `XXX-XX-` prefix assertion, not exact `XXX-XX-6789` | ✅ Explicitly handles the "existing DB without reseeding" risk per spec's Risk Notes |

**One concern not previously noted:**

`EnsureComplianceAuditEventsExistAsync` calls `GetSsnCustomerIdAsync()` which uses `_fixture.GetCustomersAsync(adminId, pageSize: 1)` — this assumes the first customer returned is the SSN-bearing one (`james.smith@example.com`). If the API returns customers in a non-deterministic order, this could return the wrong customer ID. The test then navigates to that customer's detail page and asserts a `\d{3}-\d{2}-\d{4}` regex — if it's the wrong customer without an SSN, the setup fails. This is fragile but not a security issue.

---

### 5. Blocking Issues Summary

| # | Issue | Blocks Handoff? |
|---|---|---|
| 1 | **Playwright suite red (0/6)** — Add-exemption flow fails in `EnsureBackofficeUserExemptionExistsAsync`, cascading to UAT-2/3/4/5/6 | **Yes** — spec requires green suite run twice for idempotency |
| 2 | **Coverage review ISSUES_FOUND** — 16 step blocks use raw CSS / framework-class selectors, 5 use opaque helpers | **Yes** — spec says "ISSUES_FOUND on any human-unreachable step blocks the handoff"; reviewer passed reachability (Protocol 5) but failed locator conformance (Protocol 4) |
| 3 | user-roles.json API/Web discrepancy (user ID "2") | No — cosmetic / documentation gap |

### Verdict

The structural design is sound: multi-role persona, UserContextBar filter fix, real-login test flow, checklist alignment. The session delivers the correct *shape* of the remediation.

Two blockers remain open:

1. **Root-cause the Add-exemption failure.** Likely candidates: (a) the `Add` button's `GetByRole` match is ambiguous if multiple "Add" buttons are in DOM, (b) `WaitForLoadStateAsync(LoadState.NetworkIdle)` returns before the Blazor re-render completes — needs a `WaitForSelectorAsync` or `Expect(...).ToBeVisibleAsync` on the new row instead, (c) the exemption save endpoint returns success but the grid component doesn't re-fetch. Debugging this single flow unblocks 5 of 6 tests.

2. **Replace raw CSS selectors in step blocks** with `GetByRole`/`GetByLabel`/`GetByText` equivalents, and either inline the opaque drawer-navigation helpers or provide their source for review. This is a mechanical pass, not a design change.

Neither blocker invalidates the session's architecture. The session is **not verifiable as green** but the design and code changes are **correct in intent and structure**, pending the two mechanical fixes above.

---

# Verification Round 2

## VERIFIED

All four verification questions are satisfactorily resolved. Detailed analysis below.

---

### 1. Granter-precedence fix — correct and non-regressing

The static precedence in `ResolveGranterAsync` deterministically resolves the non-determinism that caused the original blocker. When `EffectiveRoles` returned `[SystemAdmin, BackofficeUser, CrossOrgMonitor]` in arbitrary order, the old first-match loop could land on `CrossOrgMonitor`, which `MaskExemptionAuthority.Evaluate` correctly denies for user-type-role targets. The fix always picks `SystemAdmin` first.

**Escalation concern raised in the prompt:** The precedence always picks the highest-privilege baked-in role the user holds, regardless of current switch-context. This is **not an escalation** — the user genuinely holds `SystemAdmin`. The `MaskExemptionAuthority` gate is role-possession-based ("does the granter hold a role with sufficient authority?"), not switch-context-based ("is the granter currently viewing-as that role?"). The pre-fix code could randomly resolve to `SystemAdmin` from a `CrossOrgMonitor` view anyway; the fix just made it deterministic.

**Minor observation (non-blocking):** `CrossOrgMonitor` sits above `UserAdmin` in the precedence. If a user holds `CrossOrgMonitor` + `UserAdmin` but not `SystemAdmin`/`CrossOrgUserAdmin`, and `CrossOrgMonitor` has no grant authority while `UserAdmin` does, the resolution would pick `CrossOrgMonitor` and be denied. This configuration doesn't exist in the current test persona or seed data, so it can't manifest today. Worth revisiting if the role matrix expands.

**Regression:** The fallback loop for host-registered baked-in roles outside the platform list is preserved. 4 bUnit tests + 666 non-Playwright tests green. No regression.

---

### 2. Test-file fidelity to design

**Step-number alignment:** All six [Fact] methods have `// begin STEP NNN` markers with identical numbering and ordering to their corresponding `Steps[].StepNumber` in the checklist. Verified:

| UAT Item | Checklist Steps | Code Steps | Match |
|----------|----------------|------------|-------|
| UAT-1 | 1–16 | 001–016 | ✓ |
| UAT-2 | 1–16 | 001–016 | ✓ |
| UAT-3 | 1–16 | 001–016 | ✓ |
| UAT-4 | 1–17 | 001–017 | ✓ |
| UAT-5 | 1–17 | 001–017 | ✓ |
| UAT-6 | 1–16 | 001–016 | ✓ |

**E2ETestReference alignment:** All six checklist entries name the correct `Class.Method`. ✓

**Locator exceptions — justified:**

| Locator | Usage | Justification |
|---------|-------|---------------|
| `GetByTestId("user-context-role")` | Step 9 in UAT-1,3,4,5,6 | MudMenu activator div + inner MudLink both emit `role='button'` with same aria-label; `GetByRole` is ambiguous in strict mode |
| `.mud-popover-open` | MudSelect/MudMenu item scoping | MudBlazor 8.x doesn't set `AriaRole.Menuitem` on `MudMenuItem`/`MudSelectItem` |
| `tbody tr.mud-table-row` | Grid row clicks | MudTable body rows have no aria role |
| `td[data-label='Link'] a` | Email inbox link | MudTable cell data-label is the most reliable selector for the link column |

All are framework-imposed limitations, not lazy locator choices. Round 1 flagged 16 testid/CSS uses; these are the justified residual set.

**Setup helpers vs. human-equivalent steps:** Setup helpers (`EnsureNo…`, `EnsureBackoffice…`, `EnsureCompliance…`) use `SeededSignInAsync` for sign-in but use the **UI flow** for config mutations (Table dropdown → Column dropdown → Add button). This satisfies the spec requirement that pre-test cleanup "must use the same UI flow the UAT exemption-grant step uses." The DSL rule applies to `// begin STEP` blocks, not to setup helpers.

**Minor observation (non-blocking):** UAT-1/UAT-3 checklist step 13 says `CLICK ROW… where Account Email = "james.smith@example.com"` but the code clicks `Locator("tbody tr.mud-table-row").First`. The human instruction is still executable (james.smith is seeded customer i=0 and will be present in the grid). The code is more permissive for pagination stability. The verification steps (masked/unmasked assertions) work for any customer. Not a functional divergence.

---

### 3. Suite greenness and idempotency

6/6 Playwright × 2 consecutive runs. 672 total tests green. The four root causes form a coherent causal chain:

1. Non-deterministic granter → authority denial → silent form return → timeout (fixed by precedence)
2. Aria-label collision → strict-mode GetByRole failure (fixed by testid targeting)
3. Pagination/sort instability → row not on page 1 (fixed by first-visible-row click)
4. MudTextField value-vs-text mismatch → regex wait timeout (fixed by label-visibility wait)

The `Task.Delay` calls (300ms–1500ms) are hardcoded waits rather than condition-based waits, which is a minor robustness concern for slow CI environments, but they've proven stable across two consecutive runs and are supplemented by `WaitForLoadStateAsync` / `Assertions.Expect` timeouts.

---

### 4. Multi-role persona security

`admin@acmecoffee.com` holding `[SystemAdmin, BackofficeUser, CrossOrgMonitor]` is the **explicit design commitment** from the spec: "One-user, multi-role persona… assigned all three roles needed for the masking UAT." The role switcher surfaces all three, and the human exercises each via `UserContextBar` without re-authenticating.

No privilege escalation exists: the user legitimately holds all three roles. The precedence fix ensures the *highest authority the user actually possesses* is used for grant resolution, which is the correct behavior for a user who holds that authority. Production environments would assign these roles to separate users per separation-of-duties policy; the dev persona conflates them intentionally for testing coverage.
