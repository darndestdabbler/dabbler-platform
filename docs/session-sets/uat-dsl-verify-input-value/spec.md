# UAT DSL — VERIFY INPUT Value Match + OPEN DROPDOWN Timing (Proposal)

> **Status:** Draft proposal authored 2026-05-01 by the
> `sensitive-field-masking-audit-uat-remediation` Session 1 closeout. Not
> yet triggered. Trigger phrase will be one of the standard sequential or
> parallel-worktree forms once the human is ready to schedule the work.
> **Created:** 2026-05-01
> **Session Set:** `docs/session-sets/uat-dsl-verify-input-value/`
> **Working repository:** `../dabbler-uat-dsl/` (sibling — grammar,
> AST, validator, emitter changes happen there) plus one platform commit
> to refresh `docs/language-reference.md` snippets if they exist on the
> platform side.
> **Prerequisite:** None. The DSL ships in 199 tests today; this set
> adds one verb without disturbing existing ones.
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Session Set Configuration

```yaml
requiresUAT: false
requiresE2E: false
uatScope: none
```

> Rationale: Library-only change in a sibling repo. No Blazor UI / API /
> user-facing behavior change. The quality bar is: grammar accepts every
> positive example below and rejects every negative example; validator
> probes the catalog correctly; emitter produces compilable Playwright
> C# that exercises the new verb; the existing 199 unit tests continue
> to pass.

---

## Why this verb is needed

The session set
`sensitive-field-masking-audit-uat-remediation` (closing 2026-05-01)
ran into a structural blind spot in the W2 pipeline:

- **UAT-3** asks the human to verify that a BackofficeUser with an
  active mask exemption sees the **real, unmasked** SSN value in the
  customer detail input. The human reads the input and judges yes/no.
- **The DSL approximation today** is
  `VERIFY ELEMENT with label "Social Security Number" is visible`,
  which only checks structural presence of the input. An input element
  with an empty value is "visible," so the W2-emitted Playwright test
  passes structurally even when the human-observable behavior is wrong.

This violates the project's foundational E2E principle (from
`project-guidance.md`):

> The E2E pipeline must catch what UAT catches.

When the human's UAT step is "read the SSN value and confirm it looks
like a real number," the emitted Playwright test must do the equivalent
read-and-assert. The existing `VERIFY TEXT contains` family operates
on `textContent` — it cannot read input value semantics, because for
`<input>` elements `textContent` is empty regardless of `value`.

A direct emit-side hack ("if label resolves to an input element, read
value instead of textContent") is rejected: the DSL is the source of
truth, the emitter is downstream of the AST, and the human reading a
checklist must be able to tell from the verb whether the assertion is
"look at" (text) vs. "read the value" (form-control).

---

## Proposed grammar addition

```antlr
// In DabblerUatDsl.g4
verifyStep
    : VERIFY TEXT CONTAINS QUOTED_STRING                                    #verifyTextContains
    | VERIFY URL MATCHES QUOTED_STRING                                      #verifyUrlMatches
    | VERIFY ELEMENT WITH LABEL QUOTED_STRING IS (VISIBLE | NOT VISIBLE)    #verifyElementVisible
    | VERIFY SNACKBAR SHOWS QUOTED_STRING                                   #verifySnackbarShows
    | VERIFY ROW IN QUOTED_STRING WHERE rowKvList EXISTS                    #verifyRowExists
    // NEW:
    | VERIFY INPUT WITH LABEL QUOTED_STRING HAS VALUE MATCHING QUOTED_STRING #verifyInputValueMatching
    | VERIFY INPUT WITH LABEL QUOTED_STRING HAS NO VALUE                    #verifyInputNoValue
    ;
```

Two complementary forms:

1. **`VERIFY INPUT with label "Social Security Number" has value matching "[0-9]{3}-[0-9]{2}-[0-9]{4}"`**
   — assert the input's `value` matches the regex.
2. **`VERIFY INPUT with label "Social Security Number" has no value`**
   — assert the input's `value` is empty / null. Useful for edge cases
   where the human's expectation is explicitly "no data here."

Both forms are deliberately constrained to **inputs labeled by the
catalog** — the validator probes
`(elementRole="Input", userVisibleLabel=<label>)` against the catalog,
exactly the same path `CLICK INPUT … and enter` already uses. No
free-form CSS or XPath. This preserves the "human can locate this by
the same label they read on screen" contract.

### AST nodes

```csharp
public sealed record VerifyInputValueMatchingStep(
    string Label,
    string Pattern) : IStep;

public sealed record VerifyInputNoValueStep(string Label) : IStep;
```

### Validator probes

For both forms, probe catalog as `("Input", Label)` — same as
`ClickInputAndEnterStep`. Failure mode: `CATALOG_STALE`, identical to
existing input-targeting verbs.

### Emitter pattern

```csharp
// VERIFY INPUT with label "Social Security Number" has value matching "[0-9]{3}-..."
await Assertions.Expect(
    currentPage.GetByLabel("Social Security Number", new() { Exact = true }).First)
    .ToHaveValueAsync(new System.Text.RegularExpressions.Regex(
        "[0-9]{3}-[0-9]{2}-[0-9]{4}"));

// VERIFY INPUT with label "Social Security Number" has no value
await Assertions.Expect(
    currentPage.GetByLabel("Social Security Number", new() { Exact = true }).First)
    .ToHaveValueAsync("");
```

`ToHaveValueAsync` is Playwright's first-class API for input value
assertions. It auto-retries until the timeout, mirroring how
`ToBeVisibleAsync` handles the no-quiet-watermark case.

---

## Also in scope: OPEN DROPDOWN timing tightening

While running the masking-audit W2 pipeline during 2026-05-01, an
intermittent flake appeared on UAT-2 step "OPEN DROPDOWN with label
'Table'" — `Locator('.mud-popover-open').Last.WaitForAsync()` would
occasionally time out at the 30-second default. Pass rate ~2/3 even
from a clean container.

The current emit pattern is:

```csharp
// OPEN DROPDOWN — current emit
await currentPage.GetByLabel("Table", new() { Exact = true }).First.ClickAsync();
await currentPage.Locator(".mud-popover-open").Last.WaitForAsync();
await currentPage.Locator(".mud-popover-open").Last
    .GetByText("Customer", new() { Exact = true })
    .First.ClickAsync();
```

The race: MudSelect attaches its click handler asynchronously after the
component first paints. Playwright's auto-wait on the trigger element
is "visible + enabled," which both succeed before the click handler is
wired. The click fires, but the popover's open class never gets
applied because the handler wasn't attached yet.

Proposed fix (sibling-repo emit-pattern change, no grammar change):

```csharp
// OPEN DROPDOWN — proposed emit
var trigger = currentPage.GetByLabel("Table", new() { Exact = true }).First;
await trigger.WaitForAsync(new() { State = WaitForSelectorState.Attached });
// Bounded retry: click + wait-for-popover up to 3 attempts. MudSelect's
// click-handler-attach race occasionally swallows the first click; the
// subsequent attempts succeed because the popover's existence (or its
// non-existence) is observable within ~500ms of click.
for (var attempt = 0; attempt < 3; attempt++)
{
    await trigger.ClickAsync();
    try
    {
        await currentPage.Locator(".mud-popover-open").Last
            .WaitForAsync(new() { Timeout = 2000 });
        break;
    }
    catch (TimeoutException)
    {
        if (attempt == 2) throw;
    }
}
await currentPage.Locator(".mud-popover-open").Last
    .GetByText("Customer", new() { Exact = true })
    .First.ClickAsync();
```

This keeps the user-faithful contract (click is a real click on the
real trigger; selector targets the real popover) while giving the
emit pattern a tolerance for the framework's first-click race. The
3-attempt bound is conservative; if it doesn't open by then the popover
truly isn't going to open and the test should fail.

CLICK MENU ITEM has the same shape (popover after a button click) and
would benefit from the same retry loop.

## Out of scope for this set

- Reading **`<select>` selected option label** — that's a different
  surface (`option:checked` text). If the masking work calls for it,
  that's its own follow-on.
- Reading **`textarea`** values — same pattern as input but the
  catalog classifier may not currently distinguish them. If needed,
  proposed verb is `VERIFY TEXTAREA …`. Keep this set tightly scoped to
  `<input>` so the grammar review doesn't bikeshed.
- Reading **MudDataGrid cell values** — that's a row-scoped read; the
  existing `VERIFY ROW … WHERE … EXISTS` form covers the most common
  case. A `VERIFY CELL` primitive belongs in its own set with its own
  rationale.

---

## Sessions

### Session 1 of 1: ship the verb

Single session. Add grammar, AST, validator, emitter; backfill unit
tests in `src/Dabbler.Uat.Dsl.Core` (positive + negative parse,
validator catalog hit/miss) and
`src/Dabbler.Uat.Dsl.Emitters.Playwright.CSharp` (emit-shape snapshot).
Update `docs/language-reference.md` with the new verbs. Backfill the
`sensitive-field-masking-audit-uat-remediation-uat-checklist.json` UAT-3
step 14 to use the new verb (one-line edit, demonstrates end-to-end
plumbing).

Deliverables:

- New grammar tokens + rules; regenerate parser.
- Two new AST node types.
- Two new validator probes.
- Two new emitter shapes.
- Unit tests bringing the count from 199 → ~205.
- Updated `docs/language-reference.md`.
- One-line edit to the platform's masking checklist (use the verb).
- W2 pipeline runs end-to-end against the updated checklist; UAT-3
  now structurally catches an empty-input regression.

### Bar for closure

- All existing unit tests still pass (no regressions in the 199).
- New unit tests pass.
- W2 run of the masking checklist passes with the new verb in UAT-3.
- Cross-provider session-verification verdict: APPROVED.

---

## Risk notes

- **Grammar-token collision.** `HAS`, `VALUE`, `MATCHING`, `NO`
  introduce new keywords. ANTLR will need them lexed before the
  identifier rule. Mitigation: review the existing token order in
  `DabblerUatDsl.g4` and add at the same precedence band as
  `WITH`, `CONTAINS`, etc.
- **Catalog classifier.** The crawler already records inputs as
  `elementRole="Input"`. This set does not change the crawler — but
  any inputs misclassified today (e.g., as Button or unclassified)
  will fail the validator. Audit pass: regenerate the masking
  catalog and grep for `"elementRole":"Input"` rows for "Social
  Security Number" before declaring W2 green.
- **Anti-pattern guard.** Do not let this verb become a backdoor for
  reading hidden values. The verb only operates on inputs the human
  can see (catalog-confirmed visible label). If a future request asks
  for "verify the database has X," that's an integration-test
  surface, not a UAT verb — push back.
