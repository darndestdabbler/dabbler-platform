# Session 6 Verification — composable-crud-helpers (Round 1)

**Model:** GPT-5.4  
**Date:** 2026-05-14  
**Verdict:** ISSUES_FOUND (4 nits)

---

Short version: **mostly yes**. I do not see a functional blocker, but I'd close out **4 nits** before changelog/Step 9:

1. tighten the dev-email inbox step so it's unambiguous,
2. replace the `RunId` implementation to remove the rare `int.MinValue` edge,
3. verify `FlakyPlaywrightCollection` actually disables parallelization,
4. scope the `*` suppressions more narrowly if your suppression system allows it.

## 1. UAT checklist: structurally sound and self-contained?

### Verdict
**Yes, structurally sound.**  
The checklist has the right shape for runnable UAT journeys:

- explicit `FunctionalArea` / `Subarea` / `ItemLabel`
- ordered `Steps`
- per-step `Verifications`
- `E2ETestReference`
- `IsJudgmentItem`
- operator guidance in `Feedback`

The design choices you called out are correct:

- full sign-in preamble on each item
- detail pages accessed via explicit URLs for W0 grounding
- judgment flags only on genuinely dynamic/non-catalog cases

### What is good
- **Item-level independence** is mostly there because each item signs in and navigates directly.
- **Catalog grounding** is stronger because you deep-link to concrete routes instead of relying on menu traversal.
- **Traceability** is good because every UAT item maps to a concrete Playwright test.

### Caveats
#### A. The dev inbox step is currently ambiguous

Current pattern:
- navigate to `/dev/email-inbox`
- click link `"Open"`

If multiple emails exist, `"Open"` is not self-identifying.

### Recommended fix
Make the step say something like:
- "In `/dev/email-inbox`, locate the **most recent** email sent to `admin@acmecoffee.com` with the sign-in subject, then click `Open`."

Without that, the items are "mostly runnable" but not fully deterministic in a reused environment.

---

#### B. Some items are self-contained but not idempotent
You already noted this for Product #10 rename. That same principle applies to any persistent state mutation:

- Product availability toggle
- Order status transition to SHIPPED
- Nested OrderItems add+save
- Any saved entity rename/edit

That is fine for UAT, but it means:
- items are **self-contained**,
- the **whole checklist is not endlessly re-runnable on the same DB** unless reset/cleanup occurs.

### Recommendation
For every mutating item, add one of:
- a cleanup note in `Feedback`, or
- a "Run against fresh seeded DB" precondition.

You already did this well for UAT-1. Make sure that pattern is consistent across all mutating items.

---

#### C. Encoding issue
Your pasted JSON showed mojibake:

- `Products — DirtyAwareActionRow`
- `persistent DB change — run against...`

If that is in the actual JSON file, fix the file encoding before closeout. Use UTF-8 and real em dash `—` or plain hyphen `-`.

### Answer to Q1
**Yes, with minor caveats.**  
The checklist is structurally sound and the items are mostly self-contained runnable journeys. The only real improvement I'd make before finalization is clarifying the dev-inbox "Open" step and ensuring all DB-mutating items explicitly note reset/cleanup expectations.

---

## 2. RunId fix: does it correctly address `[Phone]` validation?

### Verdict
**Conceptually yes. Implementation: almost, but not quite bulletproof.**

You correctly identified the root cause:
- old value used GUID hex characters `a-f`
- phone validation rejected letters
- digits-only suffix fixes that

So the direction is correct.

### The problem with the current implementation
```csharp
private static readonly string RunId =
    Math.Abs(Guid.NewGuid().GetHashCode()).ToString("D7")[..7];
```

This has a rare edge case:

- `Math.Abs(int.MinValue)` is still negative because `int.MinValue` cannot be represented as a positive `int`.

In that rare case, you could still end up with a `-` in the string, which reintroduces invalid phone content.

### Better implementation
Use an actual non-negative bounded integer source:

```csharp
private static readonly string RunId =
    Random.Shared.Next(0, 10_000_000).ToString("D7");
```

That gives you exactly 7 digits, always.

### Answer to Q2
**Yes, it addresses the hex-vs-digits validation problem.**  
But I would still change the implementation before closeout, because `Math.Abs(...GetHashCode())` has a rare negative edge case.

---

## 3. Is `FlakyPlaywrightCollection` assignment correct for the compiled UAT test?

### Verdict
**Yes, that is the right classification** for a test that mutates shared seeded state.

Your rationale is sound:
- it renames Product #10
- it activates Product #17
- re-running in-suite changes the expected initial UI state
- therefore it should not run in the normal parallel mutable-state lane

### One important condition
This is only fully correct **if** `FlakyPlaywrightCollection` is actually configured to serialize execution.

### One additional point
`FlakyPlaywrightCollection` solves the **state mutation / parallelism** problem.  
It does **not** solve the separate known issue:
- compiled UAT runner still conflicts with the checklist's login steps because of `SeededSignInAsync`

If the test is still expected to fail in default CI because of the W2 limitation, consider:
- marking it with a trait/category like `GeneratedUat` or `Quarantined`
- excluding it from the default green lane until the W2 enhancement lands

### Answer to Q3
**Yes, correct for mutation/isolation purposes.**  
Verify the collection really disables parallelization, and consider quarantining/excluding the test from the default green lane until the W2 login conflict is fixed.

---

## 4. Any issues with the W2 runner suppressions?

### Verdict
**No blocker, but the wildcard suppressions are broader than I'd like.**

### Fine as-is
- `UnknownRoute` for `/login`
- `UnknownRoute` for `/dev/email-inbox`

### Risky
- `UnknownRoute` for `*`
- `UnknownLabel` for `*`
- `ElementTypeMismatch` for `*`

These are broad and can mask real regressions. They are acceptable as temporary session-closeout measures; better if scoped down before or during Step 9 reorg.

### Answer to Q4
**No immediate issue, but the `*` suppressions are too broad for long-term comfort.**  
Acceptable as a temporary session-closeout measure.

---

## 5. Anything missing before changelog and Step 9 reorg?

### My closeout list

1. **Harden `RunId`** — replace with `Random.Shared.Next(0, 10_000_000).ToString("D7")`
2. **Fix dev-inbox step wording** — make magic-link steps deterministic (most recent email)
3. **Audit all mutating UAT items for cleanup/precondition notes**
4. **Fix encoding** — if the file contains em dash `—`, convert to ASCII
5. **Decide how the generated compiled UAT test is represented in CI**
6. **Document the suppressions as temporary runner debt**

---

## Bottom line

1. **UAT checklist sound?** **Yes.** Structurally good and mostly self-contained.
2. **RunId fix correct?** **Yes in principle**, but replace implementation to avoid `int.MinValue` bug.
3. **FlakyPlaywrightCollection correct?** **Yes**, `DisableParallelization = true` is confirmed.
4. **Any suppression issues?** **No blocker**, wildcard suppressions are broad but scoped per checklist.
5. **Anything missing?** Just the closeout nits above. No major functional gap.
