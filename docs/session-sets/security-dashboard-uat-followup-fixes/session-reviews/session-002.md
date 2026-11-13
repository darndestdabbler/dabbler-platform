# Verification Round 1

## Review Against Criteria

### 1. Determinism of juser at Id=11

**Verdict: Sound, with a documented fragility.**

juser is appended to the `users` list at a fixed index (11th element, 0-based index 10) — after the 10 hard-coded internal personas and before the `foreach (var customer in customers)` loop. Because EF Core's `AddRange` inserts in list order and SQL Server assigns `IDENTITY` values sequentially, the Id is deterministic on a fresh seed regardless of the customer count.

**Latent risk:** The Id=11 assumption is duplicated as a magic number in both `user-roles.json` files. If a future commit inserts a new user before juser, the Id silently shifts and the role mapping breaks with no compile-time or test-time signal until the Playwright suite runs. Consider adding a seed-time assertion or a comment in both `user-roles.json` files cross-referencing the insertion site.

---

### 2. Real-login test fidelity and `tbody` scoping

**Verdict: Correct.**

The `tbody tr.mud-table-row` selector is necessary and accurate. MudBlazor renders *both* `<thead><tr class="mud-table-row">` and `<tbody><tr class="mud-table-row">` elements. Without the `tbody` scope, `.First` would match the header row, which contains no `<a>` in a `td[data-label='Link']` cell — the test would fail with a misleading locator timeout rather than a clear assertion failure.

The six-step sequence (clear inbox → login → send link → open inbox → follow link → assert tabs) is a faithful reproduction of the human flow described in the Session 1 "Other" feedback.

---

### 3. Magic-link href regex

```
^https?://.+/login\?token=
```

**Verdict: Appropriately balanced.**

| Scenario | Matches? | Correct? |
|---|---|---|
| `http://localhost:5010/login?token=abc` | ✅ | ✅ |
| `https://app.example.com/login?token=abc` | ✅ | ✅ |
| `http://web:8080/login?token=abc` (Docker internal) | ✅ | ✅ |
| `http://localhost:5010/login` (no token) | ❌ | ✅ — catches broken link |
| `http://localhost:5010/other?token=abc` (wrong path) | ❌ | ✅ — catches wrong endpoint |

One minor rigidity: `\?token=` requires `token` to be the *first* query parameter. If the app ever emits `/login?redirect=%2F&token=abc`, the regex fails. This is unlikely for a magic-link URL where the token is the primary payload, but worth noting.

---

### 4. Separate Playwright page vs. state leak

**Verdict: Correct — no state leak.**

`CreatePageAsync()` creates a new page within the **same `BrowserContext`**, which shares cookies and `localStorage`. This is the exact analogue of a human opening a second browser tab:

```
Page 1 (login tab):   /login → fill email → submit → confirmation screen
Page 2 (inbox tab):   /dev/email-inbox → read href → [closed in finally]
Page 1 (login tab):   navigate to magic-link href → authenticated shell
```

The timing is safe: page 2's DOM read (`GetAttributeAsync("href")`) completes and the href string is captured *before* page 1 navigates to the magic link. The `finally` block closes page 2 after all reads are done. Auth cookies set by the magic-link navigation on page 1 cannot retroactively affect page 2's already-completed operations.

---

### Summary

| Criterion | Status |
|---|---|
| 1 — Seed determinism | ✅ Deterministic. Fragile to future insertions (magic-number Id in JSON). |
| 2 — Test fidelity / `tbody` scoping | ✅ Correct and necessary. |
| 3 — Regex strictness | ✅ Balanced. Minor note on query-param ordering. |
| 4 — Separate page / state leak | ✅ No leak. Correctly mirrors second-tab workflow. |
