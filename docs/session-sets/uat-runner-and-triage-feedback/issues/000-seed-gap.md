## UAT Step 0 — Seed Data Gap

**Step text:**
> AcmeCoffeeOnline.Playwright.Generated.SensitiveFieldMaskingAuditUatRemediationUatTests.RunChecklistAsync

**Triage classification:** `SEED_DATA_GAP` (confidence: 78.00%)

**Triage evidence:**
> The Playwright locator timed out waiting for a table row containing the specific email address 'james.smith@example.com', which indicates the record does not exist in the current database rather than a structural UI change.

**Playwright error:**
```
System.TimeoutException : Timeout 30000ms exceeded.
Call log:
  - waiting for GetByRole(AriaRole.Row).Filter(new() { HasText = "james.smith@example.com" }).First
```

**Status:** Required test data is not present in the seeded database.

**Next steps:**
  1. Review the Playwright error above to identify which entity/field is missing.
  2. Determine which EF seed class should provide the data (e.g., `CustomerSeeder`).
  3. Add or update a seed entry in that class with the required data.
  4. Rebuild the test database and re-run the UAT checklist.