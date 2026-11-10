# Change Log - `uat-dsl-verify-input-value`

## 2026-05-11

*   **Feature:** Added `VERIFY INPUT with label <label> has value matching <regex>` to assert an input's value matches a regex using Playwright's `ToHaveValueAsync(Regex)`. Validated against `elementRole=Input` in the catalog.
*   **Feature:** Added `VERIFY INPUT with label <label> has no value` to assert an input's value is empty using `ToHaveValueAsync("")`.
*   **Fix:** Resolved `OPEN DROPDOWN` race condition with a 3-attempt retry mechanism, incorporating a `WaitForAsync(Attached)` guard and `PlaywrightException` catch.
*   **Tests:** Added 18 new unit tests, increasing the total from 199 to 217.
*   **Documentation:** Updated `docs/language-reference.md` to include new verbs, guidance on `VERIFY TEXT` vs `VERIFY INPUT`, and new bare keywords.
*   **Update:** Modified masking checklist step `UAT-3/step-14` from `VERIFY ELEMENT visible` to `VERIFY INPUT has value matching`.

**Motivation:** The `sensitive-field-masking-audit-uat-remediation` identified that `VERIFY ELEMENT visible` was insufficient for detecting empty vs. populated input fields, failing to meet the E2E automation principle.