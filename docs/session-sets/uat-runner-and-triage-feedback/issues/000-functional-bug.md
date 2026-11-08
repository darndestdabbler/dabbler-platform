## UAT Step 0 — Functional Bug

**Step text:**
> AcmeCoffeeOnline.Playwright.Generated.SensitiveFieldMaskingAuditUatRemediationUatTests.RunChecklistAsync

**Triage classification:** `APP_FUNCTIONAL_BUG` (confidence: 55.00%)

**Triage evidence:**
> The Playwright runtime throws a SyntaxError before any element lookup completes: the generated locator embeds a PCRE-style inline flag `(?i)` inside a JavaScript RegExp literal (`/(?i)^Send\ sign-in\ link$/`), which is invalid in JS. None of the three self-healing classes (CATALOG_STALE, SEED_DATA_GAP, AMBIGUOUS_LOCATOR) match; per decision rule 5 the default is APP_FUNCTIONAL_BUG.

**Playwright error:**
```
Microsoft.Playwright.PlaywrightException : SyntaxError: Invalid regular expression: /(?i)^Send\ sign-in\ link$/: Invalid group
    at new RegExp (<anonymous>)
    at createTextMatcher (<anonymous>:7637:16)
    at Object.queryAll (<anonymous>:6591:29)
    at InjectedScript._queryEngineAll (<anonymous>:6536:49)
    at InjectedScript.querySelectorAll (<anonymous>:6523:30)
    at eval (eval at evaluate (:291:30), <anonymous>:2:35)
    at UtilityScript.evaluate (<anonymous>:293:16)
    at UtilityScript.<anonymous> (<anonymous>:1:44)
Call log:
  - waiting for internal:role=button >> internal:has-text=/(?i)^Send\ sign-in\ link$/ >> nth=0
```

**Status:** Requires human investigation and code fix.

**Next steps:**
  1. Review the evidence and Playwright artifacts above.
  2. Identify the root cause in the application code.
  3. Implement a code fix.
  4. Re-run the UAT checklist to confirm the fix.