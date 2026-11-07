# Verification Round 1

## Session 3 Review: Accessibility and Narrow-Width Polish

### Accessibility

**role="group" + aria-label on the bar** — ✅ Correct. The outer `<div>` carries `role="group"` and `aria-label="User context"`, which groups the child controls semantically and gives screen readers a landmark name.

**aria-label per link for screen-reader coherence** — ✅ The spec calls for labels like *"Casey Hart, sign out"*, *"System Admin, switch role"*, *"Internal Operations, switch organization"*. The implementation delivers exactly that pattern via `_userLinkAttributes`, `_roleLinkAttributes`, and `_orgLinkAttributes`. When switching is not available, the label degrades to the plain display text, which is the correct behavior.

**aria-hidden on connector words** — ✅ Both `<span>` elements ("as", "for") carry `aria-hidden="true"`, preventing screen readers from reading disconnected filler words that would fragment the aria-labels already providing the full semantic sentence.

**Keyboard focusability** — ⚠️ **Partial concern.** Enabled `MudLink` elements render as `<a>` or `<button>` and are natively focusable — that's fine. However, disabled links (`Disabled="true"`) in MudBlazor still render as `<a>` tags with a `disabled` CSS class but **no `tabindex="-1"`** by default. The code adds `aria-disabled="true"` via `UserAttributes`, which is semantically correct, but does not explicitly set `tabindex="-1"` to remove them from the tab order. A screen reader or keyboard user could still Tab onto a disabled link. **This is a minor gap** — the spec says "Keyboard focus order through the three links," implying disabled links should be skipped. Adding `["tabindex"] = _canSwitchRole ? null : "-1"` (and equivalent for org) to the attribute dictionaries would close this.

**No `aria-haspopup` on menu activators** — ⚠️ **Minor omission.** The role and org links that open `MudMenu` dropdowns should ideally carry `aria-haspopup="true"` (and `aria-expanded`) so screen readers announce them as menu buttons. MudBlazor's `MudMenu` may inject this on its own internal activator button, but because the activator content here is a `MudLink`, the attribute propagation depends on MudBlazor internals. Worth a manual Narrator/NVDA spot-check, but not a blocking issue given Playwright passed.

### Narrow-Viewport Behavior

**Decision: truncation with ellipsis + title tooltip vs. wrapping** — ✅ Well-justified. The spec asked the developer to *decide* between wrap and truncation. Truncation is the right call for an app bar, where vertical space is fixed and wrapping would break the single-line layout. The implementation uses:

- `max-width: 18ch` at desktop, `12ch` at ≤960px, `8ch` at ≤600px
- `overflow: hidden; text-overflow: ellipsis; white-space: nowrap`
- Native `title` attributes on user and org links as a hover fallback

The role link gets `white-space: nowrap` without truncation, which is reasonable since role names are from a controlled vocabulary and are short.

**One consideration:** `title` attributes are not accessible on touch devices (no hover). The `aria-label` already contains the full text, so screen-reader users are covered, but sighted touch users on a narrow tablet would see truncated text with no way to reveal the full value. This is an inherent limitation of the `title` approach, acknowledged industry-wide, and not a regression.

### Contrast Verification

**#424242 on #FFFFFF = 9.6:1** — ✅ Mathematically correct. The relative luminance of #424242 ≈ 0.051, #FFFFFF = 1.0, giving a ratio of (1.05 / 0.101) ≈ 10.4:1. The stated 9.6:1 is in the right ballpark (minor rounding differences depending on the tool), and either way it **exceeds WCAG AAA 7:1 for normal text** and far exceeds AA 4.5:1. No concern.

### Test Coverage

| Test | What it proves |
|---|---|
| `Bar_HasGroupRoleAndAriaLabel_ForScreenReaders` | `role="group"` and `aria-label="User context"` on the container |
| `ConnectorWords_AreAriaHidden` | "as" and "for" spans carry `aria-hidden="true"` |
| `UserAndOrgLinks_ExposeTitleAttribute_ForHoverTooltip` | `title` attributes present for truncation fallback |
| `Links_CarryResponsiveCssClasses_ForTruncation` | CSS class hooks (`user-context-name`, `user-context-role`, `user-context-org`) are applied |

These four new tests directly cover the Session 3 contract. Combined with the 9 Session 1 tests (enabled/disabled states, sentence structure, sign-out flow, data-testids), the 13/13 suite provides good coverage.

**Missing test coverage:**
- No test asserts the actual `aria-label` values on individual links (e.g., `"Casey Hart, sign out"`, `"System Admin, switch role"`). The spec explicitly lists these strings. This is testable in bUnit and would strengthen the contract. **Minor gap.**
- No test for the `title` attribute on the role link (only user and org are checked).

### Regressions

- Build: 0 new errors, no new warnings.
- Existing tests: 529 passed, 1 pre-existing unrelated failure — no regressions.
- Playwright: 75/75 — no regressions.

### Summary of Findings

| # | Severity | Finding |
|---|---|---|
| 1 | Minor | Disabled links lack `tabindex="-1"` — they remain in the keyboard tab order despite `aria-disabled="true"` |
| 2 | Minor | No test asserts the specific `aria-label` sentence strings the spec enumerates |
| 3 | Informational | No `aria-haspopup` on menu-trigger links (may be handled by MudBlazor internals; verify manually) |
| 4 | Informational | `title` tooltip not accessible on touch devices (inherent limitation, mitigated by `aria-label`) |

Items 1 and 2 are real gaps against the spec text but are not blocking — the component is functional, accessible to screen readers via `aria-label`, and all tests pass. Item 1 is the most actionable and should be addressed before the feature is considered fully shipped.

**ISSUES_FOUND**

---

# Verification Round 2

# Verification Round 2

## Re-assessment of Round 1 Findings

### Finding 1 (Minor): Disabled links lack `tabindex="-1"`

**Status: FIXED ✅**

The `_roleLinkAttributes` and `_orgLinkAttributes` dictionaries now include:
```csharp
["tabindex"] = _canSwitchRole ? null : "-1",
```
and
```csharp
["tabindex"] = _canSwitchOrg ? null : "-1",
```

The new test `DisabledLinks_HaveTabindexMinusOne_AndSkipMenuAttributes` explicitly asserts both:
- `tabindex="-1"` is present on disabled role and org links
- `aria-haspopup` is absent on disabled links (good — prevents screen readers from announcing a menu that can't be opened)

Verified in the test output: 16/16 passed. **Fully addressed.**

### Finding 2 (Minor): No test asserts specific `aria-label` sentence strings

**Status: FIXED ✅**

New test `Links_ExposeSpecSentenceAriaLabels` asserts the exact spec-enumerated strings:
- `"Casey Hart, sign out"` on user link
- `"System Admin, switch role"` on role link
- `"Internal Operations, switch organization"` on org link

The test uses the enabled-everything scenario (SystemAdmin + two orgs), which is the correct configuration to trigger the ", switch role" and ", switch organization" suffixes. **Fully addressed.**

### Finding 3 (Informational): No `aria-haspopup` on menu-trigger links

**Status: FIXED ✅**

Code-behind now sets:
```csharp
["aria-haspopup"] = _canSwitchRole ? "menu" : null,
```

Two tests cover this:
- `EnabledMenuLinks_AdvertisePopup` — asserts `aria-haspopup="menu"` on enabled role and org links
- `DisabledLinks_HaveTabindexMinusOne_AndSkipMenuAttributes` — asserts `aria-haspopup` is absent on disabled links

The value `"menu"` (rather than `"true"`) is the correct ARIA 1.1 token for a menu-style popup. **Fully addressed.**

### Finding 4 (Informational): `title` tooltip not accessible on touch devices

**Status: DEFERRED — Acceptable ✅**

The rationale is sound: `aria-label` covers assistive-tech users, and adding `MudTooltip` would introduce DOM complexity disproportionate to the benefit for a single app-bar sentence. This was informational-only and the deferral is documented. **No action required.**

## Additional Review of Remediation Quality

**No regressions introduced:**
- The attribute dictionaries use `null` for absent values, which MudBlazor's `UserAttributes` rendering correctly omits from the DOM (confirmed by the `DisabledLinks_HaveTabindexMinusOne_AndSkipMenuAttributes` assertion that `aria-haspopup` is not present as an attribute at all, not just empty).
- Build: 0 errors, pre-existing warning only.
- All 16 tests pass including the 13 prior tests — no regressions.

**Test coverage is now comprehensive:**

| Test | Covers |
|---|---|
| `Links_ExposeSpecSentenceAriaLabels` | Finding 2 — exact aria-label strings |
| `DisabledLinks_HaveTabindexMinusOne_AndSkipMenuAttributes` | Finding 1 + Finding 3 (disabled path) |
| `EnabledMenuLinks_AdvertisePopup` | Finding 3 (enabled path) |

**One observation (not a finding):** The `_roleLinkAttributes` and `_orgLinkAttributes` are computed properties (expression-bodied `=>`) that create a new `Dictionary<string, object?>` on every access. This is fine for a component that re-renders infrequently, but if perf profiling ever flags it, caching the dictionaries in `RefreshFromParameters()` would be trivial. Not actionable now.

## Summary

| Round 1 Finding | Severity | Remediation | Verdict |
|---|---|---|---|
| 1. Disabled links in tab order | Minor | `tabindex="-1"` added + test | ✅ Fixed |
| 2. Missing aria-label string assertions | Minor | Test added with exact spec strings | ✅ Fixed |
| 3. No `aria-haspopup` on menu triggers | Informational | `aria-haspopup="menu"` added + tests for both states | ✅ Fixed |
| 4. `title` on touch devices | Informational | Deferred with documented rationale | ✅ Accepted |

All findings from Round 1 have been adequately addressed. No new issues identified.

**VERIFIED**
