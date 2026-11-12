## UAT Step 0 — Ambiguous Locator

**Step text:**
> AcmeCoffeeOnline.Playwright.Generated.SensitiveFieldMaskingAuditUatRemediationUatTests.RunChecklistAsync

**Triage classification:** `AMBIGUOUS_LOCATOR` (confidence: 100.00%)

**Triage evidence:**
> Playwright strict-mode-violation pattern: locator resolved to 2 elements (>1). A data-testid discriminator is needed.

**Playwright error:**
```
Microsoft.Playwright.PlaywrightException : Error: strict mode violation: GetByRole(AriaRole.Alert) resolved to 2 elements:
    1) <div role="alert" aria-live="polite" class="mud-snackbar mud-alert-filled-warning ">…</div> aka GetByRole(AriaRole.Alert).Filter(new() { HasText = "Now viewing as 'Cross Org" })
    2) <div role="alert" aria-live="polite" class="mud-snackbar mud-alert-filled-success ">…</div> aka GetByRole(AriaRole.Alert).Filter(new() { HasText = "Exported 382 rows to" })

Call log:
  - Expect "ToContainTextAsync" with timeout 5000ms
  - waiting for GetByRole(AriaRole.Alert)
    3 × locator resolved to <div role="alert" aria-live="polite" class="mud-snackbar mud-alert-filled-warning ">…</div>
      - unexpected value "Now viewing as 'Cross Org Monitor'."
```

**Status:** Locator is ambiguous; requires a `data-testid` discriminator.

**Remediation approach:**
  1. Review the DOM snapshot to see which element should be targeted.
  2. Add a unique `data-testid` attribute to that element in the .razor component.
  3. Update the catalog entry to use the new `data-testid` in the locator.
  4. Re-run the UAT checklist to confirm the locator now matches unambiguously.

**Example data-testid:**
```html
<!-- Before (ambiguous) -->
<button>Approve</button>
<button>Approve</button>

<!-- After (unambiguous) -->
<button data-testid="approve-pending-request">Approve</button>
<button data-testid="approve-completed-request">Approve</button>
```