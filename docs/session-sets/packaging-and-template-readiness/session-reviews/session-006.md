# Verification Round 1

ISSUES_FOUND

Coverage note: A1-A18 and G1-G23 are all represented in the ledger tables; I do not see a silent drop from the Session 5 assigned list. The issues are internal consistency / closure-bar issues.

- **Correctness / Major** — **§10 criterion #10 contradicts §2.2 on G21/G22**
  - §2.2 marks:
    - **G21** = `applied-this-session`
    - **G22** = `applied-this-session`
    - **G18** = `accept-as-limitation`
  - But §10 criterion #10 says:
    - "`accept-as-limitation` items (**G18, G21, G22**) documented per §7."
  - That is internally wrong. G21/G22 are not limitations in the ledger; they are documented fixes already applied in Session 6.
  - This matters because Session 11 closeout would be verifying against the wrong disposition set.

- **Completeness / Major** — **UAT closure bar is under-specified for `requiresUAT: true`**
  - Spec config says:
    - `requiresUAT: true`
    - `uatScope: per-set`
    - rationale: automation cannot fully judge the consumer-facing install/scaffold experience.
  - §10 criterion #8 currently requires:
    - "Human UAT checklist **authored** ... with W2 runner green."
  - That is weaker than "human UAT checklist **executed and passed/signed off**."
  - For a per-set UAT requirement, the acceptance criteria should explicitly require human execution/completion of the checklist, not just authorship plus automated W2 status.

- **Correctness / Major** — **Stub-mode “intentional opt-in” conflicts with the template contract as written**
  - Session goal #3 was to reconfirm stub mode as intentional opt-in, not accidental default.
  - §4 freezes that contract strongly:
    - no auto-discovery
    - no env-var shortcut
    - no production fallback
    - explicit registration only
  - But §5.1 says `dabbler-app` has **“Stubs wired? Yes (meta-package)”**.
  - As written, that makes the standard consumer template appear to ship with stubs already wired by default, which is at least ambiguous and possibly contrary to the “not an accidental default” rule.
  - This needs clarification:
    - either stubs are **test-only** in the template,
    - or they are present but **commented/disabled by default**,
    - or the generated app explicitly documents itself as a stub-backed demo variant.
  - Without that clarification, §§4 and 5 are not fully aligned.

- **Correctness / Minor** — **Package-count wording is inconsistent**
  - §1 summary says: **“Nine NuGet libraries + one template package + one CLI tool package.”**
  - §3.1/§10 operate on **12 library/sub-package packages + template + tool = 14 total**.
  - The mismatch is explainable by the Stubs split being called out separately, but the summary wording is still inconsistent with the frozen packable surface and could confuse downstream sessions.

- **Correctness / Minor** — **Change log filename is inconsistent**
  - §6.5 says pre-push checklist requires a `CHANGELOG.md` entry.
  - §10 says the set closes with `change-log.md`.
  - One canonical filename should be chosen.

- **False Positive / Minor** — **Skipping Playwright in Session 6 is not a completion defect**
  - Given the session changed docs, csproj metadata, and a placeholder project only, not running Playwright here is reasonable.
  - The required E2E obligations are still captured later in:
    - §6.1 smoke
    - §10 criterion #7
  - I would **not** treat the Session 6 Playwright omission as a verification failure.

Overall: the ledger is broadly sound and complete, but the three major issues above should be corrected before treating it as the clean binding contract for Sessions 7-11.

---

# Verification Round 2

ISSUES_FOUND

1. **Category: Correctness — Severity: Major**  
   **Details:** §6.3’s hermetic-pack contract is over-constrained to the point of being unimplementable from a clean machine. It says `tools/pack.ps1` must isolate `NUGET_PACKAGES` **and** set `RestoreSources` / `RestoreAdditionalProjectSources` to the **explicit local feed only**. That blocks restore of third-party packages (MudBlazor, bUnit, Playwright, SourceLink, etc.) unless Session 7 also mirrors every external dependency into that local feed, which the ledger never requires.  
   This needs to be revised to: **Dabbler.\*** packages must resolve only from the local pack feed, while approved external feeds remain available for non-Dabbler dependencies (typically via source mapping and/or resolved-source validation). As written, Session 7/9 can fail for reasons unrelated to Dabbler contamination.

2. **Category: Correctness — Severity: Major**  
   **Details:** §3.4’s Compat sunset guidance still conflicts with §3.6’s preview-only versioning policy. §3.4 says `Dabbler.Components.Compat` publishes through `0.4.0-preview.N`, then consumers needing compat “stay pinned to `0.3.x`.” But §3.6 says **every** pre-1.0 public release is `0.x.y-preview.N`; a plain `0.3.x` line does not exist under this contract.  
   The ledger needs one consistent pin target: e.g. `0.3.x-preview.N`, or “the last published Compat preview before sunset.”

3. **Category: Correctness — Severity: Minor**  
   **Details:** The stub test-wiring API name is inconsistent between §4.4 and §5.1.  
   - §4.4 makes the canonical bUnit helper `ctx.AddDabblerStubs(definition)`.  
   - §5.1 says the scaffolded bUnit `TestContext` calls `AddDabblerPlatformStubs(...)`.  
   Those read like different APIs (bUnit extension vs service-registration helper). The production contract is now correct, but the test-project contract should name one canonical helper or explicitly describe how they relate.

4. **Category: Correctness — Severity: Minor**  
   **Details:** There are still a couple of bad internal cross-references:  
   - §10 criterion #8 cites “ledger §11 Reading-guide”; the Reading guide is §0, not §11.  
   - §2.1 / A4 points to “§3.4 + §3.5 + §9.1”; §3.5 is about Security csproj metadata, not Compat.  
   These are small, but this document is supposed to be the frozen downstream contract.

**Round 1 fix check:** the five Round 1 fixes themselves look resolved cleanly. In particular, the §5.1 rewrite does **not** reopen production stub activation; the remaining stub issue is only the test-helper naming mismatch above.

**Overall:** not yet ready to stand as the binding contract for Sessions 7–11 until the two Major issues are corrected.

---

# Verification Round 3

ISSUES_FOUND

1. **Correctness — Major — §6.3 hermetic scope is still internally over-broad**
   - §6.3 now correctly says external dependencies may restore from approved feeds, and it explicitly names `Dabbler.Uat.Dsl.*` from a sibling-repo local feed as an example.
   - But the next two bullets require:
     - source mapping that pins **every `Dabbler.*` package ID** to the local pack-output feed, and
     - a pre-pack check that fails if **any `Dabbler.*` PackageReference** resolves from a non-local source.
   - That conflicts with the allowed `Dabbler.Uat.Dsl.*` case, because those IDs also match `Dabbler.*` but are not produced by this repo.
   - Result: the contract still over-constrains the implementation. The mapping/check must target **only the package IDs produced by this repo** (or an explicit allowlist/prefix set for in-repo packages), not blanket `Dabbler.*`.

2. **Correctness — Minor — Round 2 stub-API naming fix is not fully propagated into §5.1**
   - §4.4 correctly distinguishes:
     - `services.AddDabblerPlatformStubs(definition)` for `IServiceCollection`
     - `ctx.AddDabblerStubs(definition)` for bUnit `TestContext`
   - But §5.1 still says the scaffolded bUnit test project has a **`TestContext` calling `AddDabblerPlatformStubs(...)`**, and the following paragraph again says test fixtures opt in via explicit `AddDabblerPlatformStubs(...)` calls.
   - As written, that is inconsistent with §4.4 and the literal `TestContext` call is not the API shape §4.4 defines.
   - This was one of the Round 2 issues; it is only partially fixed.

3. **Correctness — Minor — §2.1 A1 “Where implemented” cross-reference is stale**
   - A1 currently points to **“§6.2 acceptance criteria”**.
   - The hermetic-pack contract is in **§6.3**, not §6.2.
   - This is a small cross-reference error, but it remains incorrect in the decision index.

**Summary:** Round 2 fixes for **Compat sunset wording** and the previously cited **bad cross-references** look clean. The **stub API fix is still incomplete**, and the **hermetic-pack fix still has a blocking correctness problem**, so this session should not close yet.

---

# Verification Round 4

VERIFIED

- Verification / Resolved — **§6.3 hermeticity fix is clean.** The prior over-broad `Dabbler.*` glob is replaced with an explicit 14-package in-repo allowlist, and the text now explicitly excludes sibling-repo packages such as `Dabbler.Uat.Dsl.*` from hermetic pinning.
- Verification / Resolved — **§5.1 stub API propagation is clean.** The template/bUnit text now consistently uses `ctx.AddDabblerStubs(...)` and correctly cross-references §4.4.
- Verification / Resolved — **§2.1 A1 cross-ref is clean.** The A1 row now points to §6.3 instead of the stale §6.2 reference.

- Documentation / Minor (deferred) — **§2 status legend omission.** The legend defines `frozen-here`, `applied-this-session`, `applied-earlier`, and `defer-out-of-set`, but `accept-as-limitation` is also used later (§2.2, totals, §10) and is not listed in the legend. Non-blocking; next-touch cleanup.
- Bookkeeping / Minor (deferred) — **§2 aggregate count table uses a mixed counting basis.** The “Count” values are not strictly row-counts because they blend adjustment rows with per-file/per-README touches. The explanatory note makes this survivable, so it does not block verification.

This supports the operator’s preferred Round 4 disposition: **VERIFIED with deferred Minors only**.
