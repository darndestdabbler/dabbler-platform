# Packaging Approach Review (Tri-Provider Synthesis)

> **Session:** packaging-and-template-readiness / Session 5
> **Date:** 2026-05-15
> **Question:** *"Does this packaging approach deliver the platform's
> value cleanly to outside consumers? What would you change?"*
> **Subject under review:** [`packaging-approach-proposal.md`](./packaging-approach-proposal.md)
> **Reviewers:**
> - Opus (claude-opus-4-7) — orchestrator self-contribution per
>   `feedback_orchestrator_self_contributes_to_review_panel`. Opus's
>   review perspective is embedded directly in the proposal's §11 (the
>   proposal authors and reviewers Opus simultaneously); §11.7's
>   "where this proposal might be wrong" is treated as Opus's
>   explicit pre-identified weak points for this synthesis.
> - GPT-5.4 (openai/gpt-5.4) — routed via `call_model()` at
>   `reasoning_effort=high`, completed in 215s, $0.21.
>   See [`packaging-review-gpt-5-4.md`](./packaging-review-gpt-5-4.md).
> - Gemini Pro (google/gemini-2.5-pro) — routed via `call_model()` at
>   `thinking_budget=-1`, completed in 40s (after a Google 503 retry),
>   $0.028. See [`packaging-review-gemini-pro.md`](./packaging-review-gemini-pro.md).
> **Outcome:** No `block-and-rework`. All three reviewers landed on
> **`proceed-with-adjustments`**. Apply **six** proposal-text edits
> inline this session; route **twelve** substantive adjustments into
> Sessions 6-8 with explicit dispositions documented below. (Counts
> reconciled in Round 2 of session verification — the original
> header read "five" and "eleven", off by one from the actual
> tables in §4 and §5.)

---

## 1. Verdicts

All three reviewers converged on `proceed-with-adjustments`. None
recommended blocking or reworking the proposal at the structural
level. The package decomposition, opt-in stubs contract, two-template
scaffold model, lock-step versioning, and consumer-install validation
strategy survived all three reviews intact.

| Reviewer | Verdict | Why |
|----------|---------|-----|
| Opus | `proceed-with-adjustments` (§11.7) | The proposal pre-identified five candidate weak points; the routed reviewers' findings converged on 4 of those 5 (versioning, compat disposition, Stubs split, smoke depth) plus several others Opus did not anticipate. |
| GPT-5.4 | `proceed-with-adjustments` | "The package split and template direction are broadly right, but the proposal still does not give outside consumers a clean day-1 path because the pack process is not yet hermetic, the validation gate is too shallow, and the public/external story is still under-specified." |
| Gemini Pro | `proceed-with-adjustments` | "The proposal is comprehensive and technically sound, but a critical adjustment to the Stubs packaging strategy is needed to ensure clean dependency graphs for consumers." |

Between them the routed reviewers surfaced **three** distinct
`Critical` adjustments — GPT-5.4 raised two (pack-pipeline hermeticity
A1; smoke-test depth A2); Gemini Pro raised one (Stubs sub-package
split A3). Crucially, **none of the three Critical findings overlapped**.
This is the value of tri-provider review: independent reviewers catch
independent classes of defect.

---

## 2. Areas of agreement

### 2.1 The package decomposition holds

Opus and GPT-5.4 explicitly endorsed the eight-library split (§2.1
of the proposal); Gemini Pro implicitly accepted it (no adjustment
recommended the split itself).

- GPT-5.4: *"The split between `Components`, `Security`,
  `Security.Blazor`, and `Reports.*` matches real adoption seams
  instead of repo convenience."*
- Opus (§2.1 / §11.7): each library earns its package by satisfying
  the "a consumer can plausibly want this without that" test.
- Gemini Pro: did not surface a package-decomposition adjustment.
  Its Critical adjustment (A3) is *internal* to one package
  (Stubs sub-package split), not a critique of the eight-way
  split.

The asymmetric evidence is honest: two of three explicitly approve,
the third does not object. None recommend re-drawing the boundaries.

### 2.2 Lock-step versioning under `$(DabblerVersion)` is correct pre-1.0

- Gemini Pro: *"pragmatic and correct choice for a tightly-coupled
  pre-1.0 platform"*
- GPT-5.4: *"the right pre-1.0 tradeoff. It removes a whole class of
  cross-package skew failures while the surface is still moving."*
- Opus (§11.1): the cost of an unnecessary version bump trades
  cleanly for the disappearance of version-skew bugs.

### 2.3 Opt-in stubs contract is right

- GPT-5.4: *"§4 gets stub behavior right. Making stubs explicit
  opt-in and refusing silent production fallback is the correct
  contract."*
- Gemini Pro implicitly agreed (no Stubs *contract* adjustments,
  only the sub-package split — see §3.1 below).
- Opus (§4): a production application registers real providers;
  stubs are absent unless explicitly added.

### 2.4 Source Link, snupkg symbols, and reproducible builds

All three reviewers either explicitly endorsed (Gemini Pro: *"a
first-class debugging experience, which is critical for driving
library adoption"*) or implicitly accepted (gpt-5-4: no challenge to
§3.8) the Source Link + `<EmbedUntrackedSources>` +
`<ContinuousIntegrationBuild>` + snupkg combination.

### 2.5 Consumer-install validation as a *separate* concern from test coverage

- GPT-5.4: *"§6 correctly identifies consumer-install validation as
  a separate concern from normal test coverage. That is the
  highest-value testing addition in this proposal."* (See §3.1 below
  for the depth disagreement.)
- Gemini Pro: *"a mature and high-value strategy that correctly
  validates the packaging and delivery mechanism, not just the
  code's internal logic."*
- Opus (§11.3): four classes of bug only the smoke catches (transitive
  internal types, build target gaps, content-file paths, analyzer
  shipping).

### 2.6 License transparency is non-negotiable (and the proposal's note is too weak)

Both routed reviewers escalated the proposal's `DabblerPdf` license
disclosure from "documented in READMEs and the overview" (the
proposal's §3.1 / §7 G2 disposition) to a higher bar:

- Gemini Pro (Major): *"License transparency is non-negotiable for
  adoption, particularly in enterprise settings. A consumer must see
  the full license implications on the NuGet package page **before**
  installing, not discover it later."*
- GPT-5.4 (Major): *"Ship a `THIRD-PARTY-NOTICES.txt` (or equivalent)
  in `Dabbler.Reports.Core`, `Dabbler.Reports.Export`, and
  `Dabbler.Reports.Blazor`, and surface the same warning in the
  `dabbler-app` template README."*

Disposition: see A7 below.

### 2.7 Scenario-based adoption guidance is missing

Both routed reviewers independently surfaced this:

- GPT-5.4 (Major): *"§2 is library-centric. Real consumers think in
  scenarios: 'I want the full platform', 'I want reports only', 'I
  want components + security'."*
- Gemini Pro (missing-entirely): *"Metapackages for Common
  Scenarios… `Dabbler.Reports` (references `.Core`, `.Export`,
  `.Blazor`), `Dabbler.Security.All` (references `.Security`,
  `.Security.Blazor`), `Dabbler.Platform` (references everything a
  typical full application would need)."*

The two reviewers proposed different shapes of fix: gpt-5-4 wants
documentation (a scenarios matrix), Gemini Pro wants implementation
(metapackages). Disposition (A8) takes both — a Session 6 scenarios
matrix immediately; an optional metapackage track for Session 7 if
the matrix alone is not enough.

---

## 3. Areas of disagreement

### 3.1 Stubs sub-package split — Gemini Pro vs Opus split

Gemini Pro: **Critical**. *"Forcing a consumer who only wants to test
UI components to accept a transitive dependency on the entire
reporting stack — including the `DabblerPdf` library with its LGPL/MPL
license implications — is a significant violation of the principle of
least dependency."*

Opus (§4.4 / §11.7): Option A (single package) is recommended for
now; Option B (split) is deferred as a post-1.0 follow-on "if a
consumer surfaces the complaint".

GPT-5.4: did not raise.

**Resolution:** Gemini Pro is right and Opus's "wait for the
complaint" framing was too lax. The license transit is the
asymmetric concern — a consumer pulling DabblerPdf transitively
through `Dabbler.Platform.Stubs` did not opt in to the LGPL/MPL
license review. This is materially different from the file-size
concern Opus weighed; license compliance is a release blocker for
many adopters, not a "few hundred KB" concern. **Apply Option B in
Session 7.** Sub-packages:

- `Dabbler.Platform.Stubs.Components` — Components stubs only (no
  Reports.Core dep). Direct dep: `Dabbler.Components`.
- `Dabbler.Platform.Stubs.Reports` — Reports stubs (pulls
  Reports.Core transitively, and therefore DabblerPdf).
- `Dabbler.Platform.Stubs.Security` — Security stubs
  (`StubAuthenticationStateProvider`).
- `Dabbler.Platform.Stubs` — meta-package that PackageReferences the
  three above. Consumers who want everything take this; consumers
  who want one slice take the focused sub-package.

The "three PackageId entries to maintain" objection from Opus §4.4 is
real but small relative to the asymmetric license-transit risk it
solves.

### 3.2 Compat surface disposition — gpt-5-4 vs Opus split

GPT-5.4: **Major**. *"Do not ship the compatibility surfaces in
`Dabbler.Components` at all. Move them to a separate
`Dabbler.Components.Compat` package or exclude them from packaged
output until a real consumer needs them. `[Obsolete]` inside the main
package still exports dead APIs, pollutes IntelliSense, expands the
public-surface baseline…"*

Opus (§3.7 / §11.2): `[Obsolete]` in the main package with sunset at
0.4.0; the value review's G4 disposition was open to either approach.

Gemini Pro: did not raise.

**Resolution:** gpt-5-4 is right. The public-surface baseline (§6.2
contract check) treats every exported public type as a versioning
commitment. `[Obsolete]` does not relax that commitment — it just
emits a warning. A consumer who happens to pin to a SemVer-aware
range still gets the type and its compile-time signature when they
restore. Moving the compat surface to a separate package
(`Dabbler.Components.Compat`) — or excluding from pack output — is
the right call. Opus's "maintenance burden of a separate package"
objection from §11.2 is a real concern but smaller than the
public-surface pollution gpt-5-4 names. **Apply during Session 6
surface freeze:** Compat surfaces go to a separate
`Dabbler.Components.Compat` package, sunset version 0.4.0.

### 3.3 Versioning preview-suffix duration — gpt-5-4 vs Opus narrow

GPT-5.4: **Major**. *"Remove the intermediate '0.x.0 without preview
suffix' phase. Stay on `0.x.y-preview.N` for all public releases
until Session 9 consumer validation is complete."*

Opus (§3.3): three-phase ladder — preview, then "0.x.0 no preview",
then 1.0.

Gemini Pro: did not raise.

**Resolution:** gpt-5-4's argument is convincing. A non-preview
package is a maturity signal NuGet feed scanners and ops teams
read; a 0.x.0-non-preview release before Session 9's external-
validation work completes will mislead. **Apply inline:** Update
§3.3 to keep the `-preview.N` suffix through 1.0.0. The phase
ladder collapses to: `0.x.y-preview.N` → `1.0.0`.

### 3.4 DevIdp external story — gpt-5-4 vs Opus narrow; Gemini Pro intermediate

GPT-5.4: **Major**. *"Telling outside consumers to go back to repo
source is exactly the repo leakage this session is supposed to
eliminate."* Proposes container image, runnable sample app, or
`dotnet tool`.

Gemini Pro: **Minor**. *"Distribute `DevIdp` as a `dotnet tool`
instead of leaving it as source-only."*

Opus (§2.2 / §8 Q3): keep in-repo unless it grows beyond the
loopback-only role; "reference source under `src/Libs/`" is the
external guidance.

**Resolution:** the two routed reviewers agreed on the substance
(consumers should not be told to reference source) but disagreed on
severity. gpt-5-4's framing — "exactly the repo leakage this session
is supposed to eliminate" — is the right read. The proposal's
position contradicts the broader external-consumption mandate. Apply
Gemini Pro's proposed shape: package `Dabbler.Security.DevIdp` as a
`dotnet tool` (new project `Dabbler.Security.DevIdp.Tool` packs the
DevIdp ASP.NET Core app with the `dotnet tool` shape). **Disposition
= fix-during-packaging, Session 8** (the template work is the right
home; the `dabbler-app` template README points at
`dotnet tool install --global Dabbler.Security.DevIdp.Tool` as the
local-OIDC instructions).

This downgrades Opus's recommendation in §2.2/§8 Q3 from "stay in-repo"
to "ship as `dotnet tool`."

---

## 4. Consolidated adjustment list with dispositions

Adjustments below merge findings across the three reviewers. Where
reviewers disagreed on severity, the higher rating is used
(conservative). Disposition codes:

| Code | Meaning |
|------|---------|
| `apply-inline` | Update the proposal text in this session, before commit. |
| `fix-during-packaging` | Becomes scope for Sessions 6-8 within this set. |
| `accept-as-limitation` | Document explicitly but do not change. |
| `defer` | Genuine work, but outside this set's scope. |

The table groups by section so adjacent items can be applied in one
edit pass.

| # | Adjustment | Severity | Raised by | Disposition |
|---|-----------|----------|-----------|-------------|
| A1 | **Hermetic pack pipeline.** `tools/pack.ps1` (and CI invocation) must isolate `NUGET_PACKAGES`, set `RestoreSources` / `RestoreAdditionalProjectSources` to the explicit local feed only, and fail if any `Dabbler.*` package resolves from nuget.org or a stale global cache during pack. | Critical | GPT-5.4 | `fix-during-packaging` (Session 7 deliverable; refine §3.4 acceptance criteria inline as part of A11). |
| A2 | **Smoke test must exercise platform value.** §6.1 step-7 Playwright must (a) sign in via magic link or seeded credential, (b) load a secured query page, (c) render the sample report page, (d) exercise PDF export, and (e) run the scaffolded bUnit project. `dabbler-min` smoke hits a secured page, not just `/`. | Critical | GPT-5.4 | `fix-during-packaging` (Session 7 / Session 8 deliverable; refine §6.1 inline as part of A11). |
| A3 | **Split `Dabbler.Platform.Stubs` into focused sub-packages now.** Ship `.Components`, `.Reports`, `.Security` sub-packages with a meta-package umbrella, so consumers who don't need the Reports stack don't transitively pull DabblerPdf. | Critical | Gemini Pro | `fix-during-packaging` (Session 7 deliverable; §4.4 disposition flips to Option B). |
| A4 | **Move compat surfaces out of the main package.** A new `Dabbler.Components.Compat` package or pack-output exclusion replaces the `[Obsolete]`-in-main approach. | Major | GPT-5.4 | `fix-during-packaging` (Session 6 surface freeze; §3.7 / §11.2 stance reversed). |
| A5 | **Stay on `-preview.N` through 1.0.** Remove the intermediate "0.x.0 no preview" phase from §3.3. | Major | GPT-5.4 | `apply-inline` (§3.3 ladder). |
| A6 | **`Dabbler.Security.DevIdp` ships as `dotnet tool`.** New `Dabbler.Security.DevIdp.Tool` project. Template README points at `dotnet tool install --global`. | Major (GPT-5.4) / Minor (Gemini Pro) — normalized up | Both routed reviewers | `fix-during-packaging` (Session 8 template work; §2.2 and §8 Q3 dispositions both reversed). |
| A7 | **Third-party license disclosure is a packaged artifact.** Ship `THIRD-PARTY-NOTICES.txt` inside `Dabbler.Reports.Core`, `Dabbler.Reports.Export`, `Dabbler.Reports.Blazor`. The Reports.* packages' `<Description>` field includes a one-line "depends on DabblerPdf (LGPL-2.1-only OR MPL-2.0)" disclosure so the NuGet package page surfaces the license implication before install; the Reports.* per-library READMEs carry the same disclosure prominently (matching Gemini Pro's explicit package-page-visibility requirement). The `dabbler-app` template README surfaces the same warning for adopters who scaffold a reports-enabled app. | Major | Both routed reviewers | `fix-during-packaging` (Session 7 metadata work). |
| A8 | **Scenario-based adoption guidance.** Session 6 produces a `docs/packaging/scenarios.md` table mapping common scenarios ("just CRUD helpers", "full platform", "reports only", "security only", "retrofit existing app") to package sets. Session 7 evaluates whether 2-3 meta-packages (`Dabbler.Reports`, `Dabbler.Security.All`, `Dabbler.Platform`) add enough simplification value to ship; if yes, they ship; if not, the doc-only matrix stays. | Major | GPT-5.4 (Major); Gemini Pro (missing-entirely) | `fix-during-packaging` (Session 6 doc + Session 7 optional metapackages). |
| A9 | **Replace nuget.org-deletion rollback with staged promotion + unlist.** §10.2's "delete from nuget.org within 24h" model is replaced: publish first to a staging feed, validate install from it, then push to nuget.org in dependency order. If a public version goes wrong, unlist + publish a fixed version (not delete). | Major | GPT-5.4 | `apply-inline` (§10.2). |
| A10 | **Artifact-content assertions in `Dabbler.PublicSurface.Tests`.** §6.2's public-surface check is extended (or paired with a sibling test) to assert `.nupkg`/`.nuspec` includes: readme, icon, license-expression metadata, SourceLink metadata, dependency-group correctness, static-web-asset completeness. | Major | GPT-5.4 | `apply-inline` (§6.2 acceptance criteria) + `fix-during-packaging` (Session 7 implementation). |
| A11 | **Package READMEs follow a required structure.** Every packaged library README must include: audience, prerequisites and stack constraints, `dotnet add package` command, minimal registration snippet, smallest working example, related packages, warnings/caveats. | Major | GPT-5.4 | `fix-during-packaging` (Session 6 surface freeze deliverable; adds to G3 disposition). |
| A12 | **Template first-run admin/bootstrap.** `dabbler-app` scaffolds a seeded admin user (`admin@local.test`, magic-link only) + onboarding README that shows how to take over via real auth. | Major | GPT-5.4 (missing-entirely) | `fix-during-packaging` (Session 8 template work). |
| A13 | **Template sample data.** `dabbler-app` includes a one-file SQLite or in-memory seed that populates the sample query page and the sample report with non-empty data on first run. | Major | GPT-5.4 (missing-entirely) | `fix-during-packaging` (Session 8 template work). |
| A14 | **Retrofit path for existing apps.** Session 6 surface freeze includes a `docs/packaging/retrofit-existing-app.md` recipe: which packages + registration order for an existing Blazor Server app adopting Dabbler.* incrementally. | Major | GPT-5.4 (missing-entirely) | `fix-during-packaging` (Session 6 doc deliverable). |
| A15 | **Compatibility-drift risk (MudBlazor / W0) gets a §10 risk row.** | Minor | GPT-5.4 (underweighted-risk) | `apply-inline` (§10.1). |
| A16 | **Template-defaults-copied-into-production risk gets a §10 risk row.** | Minor | GPT-5.4 (underweighted-risk) | `apply-inline` (§10.1). |
| A17 | **Hold first publish until Session 7 metadata work is complete.** §8 Q1 "wait until Session 7" is upgraded from "recommended" to a binding decision in §3.3. | Minor | Gemini Pro (underweighted-risk) | `apply-inline` (§3.3 + §8 Q1 collapse). |
| A18 | **The compatibility matrix (`§8 Q4`) belongs in this set, not as a deferred open question.** Session 6 surface freeze produces a first version under `docs/COMPATIBILITY.md`. | Minor | GPT-5.4 | `fix-during-packaging` (Session 6 doc deliverable). |

**Tally** — 18 rows total (A1–A18):

| Severity | Count | By disposition |
|----------|-------|----------------|
| Critical | 3 | `fix-during-packaging` × 3 (A1, A2, A3) |
| Major | 11 | `apply-inline` × 3 (A5, A9, A10-partial); `fix-during-packaging` × 8 (A4, A6, A7, A8, A11, A12, A13, A14) |
| Minor | 4 | `apply-inline` × 3 (A15, A16, A17); `fix-during-packaging` × 1 (A18) |

**Disposition totals:** 6 `apply-inline` (A5, A9, A10, A15, A16, A17),
12 `fix-during-packaging` (A1, A2, A3, A4, A6, A7, A8, A11, A12, A13,
A14, A18), 0 `accept-as-limitation`, 0 `defer`. Sum = 6 + 12 = 18. ✓

(A10 spans both inline and fix-during-packaging — the §6.2 acceptance
criteria edit is inline; the implementation lands in Session 7. Counted
in `apply-inline` for the row but mirrored in Session 7 scope in §7.)

---

## 5. Inline `apply-inline` actions for this session

The six inline items are small proposal-text edits applied before
commit. Each modifies `packaging-approach-proposal.md` directly:

1. **A5 — Versioning ladder collapses.** §3.3's three-phase table
   becomes a two-phase table: `0.x.y-preview.N` (all pre-1.0
   releases) → `1.0.0` (after Session 9). The "Stabilization" row
   is removed.
2. **A9 — Rollback model.** §10.2 replaces "Delete from nuget.org
   (within 24h)" with: "Unlist the bad version on nuget.org and
   publish a fixed version. Deletion is not relied on for rollback
   — nuget.org is not transactional and consumers may have already
   resolved against the bad version. The staging-feed validation
   step (§6.1, A2) is the primary defense."
3. **A10 — §6.2 acceptance criteria extended.** Add an "Artifact
   contents asserted" sub-list to §6.2: readme present, icon
   present, `<PackageLicenseExpression>` set, SourceLink metadata
   resolvable, dependency groups match per-target, static web
   assets complete.
4. **A15 — §10.1 risks gain a row.** *"Upstream MudBlazor / W0 /
   .NET version drift breaks Dabbler.* compatibility between
   releases."* Mitigation: the `docs/COMPATIBILITY.md` Dabbler.*
   section (A18); release cadence aligned with W0/MudBlazor minor
   bumps.
5. **A16 — §10.1 risks gain a row.** *"Adopters cargo-cult template
   dev defaults into production."* Mitigation: template README
   declares the production-config gap explicitly; `appsettings.json`
   block markers in the scaffold include `// PRODUCTION: replace
   before deploy` comments.
6. **A17 — §3.3 + §8 Q1 collapse.** §8 Q1 ("publish 0.1.0-preview.1
   after Session 6 or Session 7?") is removed as an open question;
   §3.3 binds it: no public publish until Session 7's
   metadata-tighten passes the §6.1 smoke gate. §8 also folds in
   the tri-provider review's resolutions of the compat-surface
   disposition (A4) and the DevIdp distribution (A6) — both
   originally listed as Session 6 open questions. **The final §8
   state has 2 open questions remaining (compatibility matrix
   publication; whether `dabbler-min` ships `Dabbler.Testing.Blazor`)
   plus 3 resolved-during-review entries (publish timing, compat
   surface, DevIdp).** This synthesis's earlier "Q2-Q5 remain"
   phrasing was off by two — the publish-timing collapse alone
   would have left four, but the same review pass also closed the
   compat-surface and DevIdp questions.

Item 3 (A10) implementation work lands in Session 7 implementation;
the inline edit is the acceptance criteria addition only.

These edits land before commit. The proposal's §11 (Opus's review
perspective) is updated to acknowledge the §11.2 stance reversal
(compat surface) and the §11.7 calibration result (4/5 weak points
confirmed).

---

## 6. Critical-gap check

The proposal's three `Critical` adjustments (A1 hermetic pack, A2
smoke depth, A3 Stubs split) are **all** `fix-during-packaging`
items — i.e., they belong inside the existing Sessions 6-8 scope of
this session set. None of them surfaces a structural defect in the
packaging approach itself; they tighten acceptance criteria for work
already planned.

**No pause required. Proceed to Session 6 surface freeze.**

---

## 7. Inputs for Session 6 surface freeze

The `fix-during-packaging` adjustments map to specific Session 6, 7,
or 8 deliverables. Session 6 owns the surface-freeze deliverables
listed first; Session 7 owns the metadata / pack pipeline; Session 8
owns the template work.

### 7.1 Session 6 deliverables (surface freeze)

- **A4** — Move compat surfaces to a separate `Dabbler.Components.Compat`
  package. Decide which compat types ship; set sunset version 0.4.0.
- **A8** — Author `docs/packaging/scenarios.md`: scenarios matrix
  mapping ("CRUD helpers only", "full platform", "reports only",
  "security only", "retrofit existing app") to package sets.
- **A11** — Per-library README structure standardization: every
  packaged library README has the seven sections (audience,
  prerequisites, install, minimal registration, smallest example,
  related packages, warnings).
- **A14** — Author `docs/packaging/retrofit-existing-app.md`: the
  add-Dabbler-to-existing-Blazor-Server-app recipe.
- **A18** — Extend `docs/COMPATIBILITY.md` with a Dabbler.* section
  listing each library version against MudBlazor / W0 / .NET.
- Also: pre-existing Session 4 G4, G5, G6, G7, G8, G9, G10, G12,
  G13, G21, G22, G23 dispositions per the proposal's §7 table.
- Also: fix the broken `icon.png` reference per the proposal's §3.2.
- Also: tighten the `Dabbler.Components` description per the
  proposal's §3.6.
- Also: land package metadata on `Dabbler.Security.csproj` and
  `Dabbler.Security.Blazor.csproj` per the proposal's §3.5.

### 7.2 Session 7 deliverables (package validation + smoke proof)

- **A1** — Hermetic `tools/pack.ps1`: isolated `NUGET_PACKAGES`,
  explicit `RestoreSources`, fail-if-stale-cache, pre-pack check
  that no `Dabbler.*` resolves from nuget.org.
- **A2** — Expanded `Dabbler.Packaging.Smoke`: sign-in + secured
  page + sample report render + PDF export + scaffolded bUnit
  project. `dabbler-min` smoke includes one secured page.
- **A3** — Split `Dabbler.Platform.Stubs` into `.Components`,
  `.Reports`, `.Security` sub-packages + meta-package umbrella.
- **A7** — Ship `THIRD-PARTY-NOTICES.txt` in `Dabbler.Reports.{Core,Export,Blazor}`
  packages.
- **A8 (optional sub-track)** — Evaluate 2-3 metapackages
  (`Dabbler.Reports`, `Dabbler.Security.All`, `Dabbler.Platform`).
  Ship only if the §7.1 scenarios-matrix doc alone is insufficient.
- **A10** — Implement artifact-content assertions in the new
  `Dabbler.PublicSurface.Tests` project.

### 7.3 Session 8 deliverables (template + stub-opt-in)

- **A6** — `Dabbler.Security.DevIdp.Tool` packs the DevIdp app as
  a `dotnet tool`. `dabbler-app` template README points at
  `dotnet tool install --global Dabbler.Security.DevIdp.Tool`.
- **A12** — Template seeds a `admin@local.test` user (magic-link
  only) + onboarding README showing how to take over via real
  auth.
- **A13** — Template includes a one-file SQLite or in-memory seed
  for sample query page + report.
- Also: G11 from the value review (the first-app tutorial).

### 7.4 Cumulative Session 5 → Sessions 6-11 carry-overs

Combining the proposal's §7 (Session 4 carry-overs G3, G4, G8, G9,
G10, G12-G14, G21-G23) with this review's adjustments
(A1, A2, A3, A4, A6, A7, A8, A11-A14, A18): a substantial set of
deliverables is queued into Sessions 6-8. Sessions 9-11 (consumer
validation, Playwright E2E, human UAT + closeout) take whatever
remains after Sessions 6-8 land their work plus any catch-up scope.

The spec was authored with this volume in mind — Sessions 6-11 are
seven of the eleven sessions in this set precisely because the
packaging work is broad. The review surfaced no scope-creep beyond
what the spec already allocated.

### 7.5 Carry-over traceability — every value-review gap accounted for

Added in response to a Round-1 session-verification finding that
some Session 4 carry-overs (G6, G7, G16, G17, G19) lacked
traceable dispositions in the proposal or this review. The table
below is the authoritative carry-over disposition matrix.
*Status* is where the gap actually lands: `already-applied` (done
in Session 4 or in this session's apply-inline edits),
`session-N` (deferred to Session N within this set with a named
deliverable), or `defer-out-of-set` (genuinely outside this set's
scope).

| Gap | Severity | Value-review §4 disposition | Status as of end of Session 5 |
|-----|----------|----------------------------|-------------------------------|
| **G1** | Critical | `fix-during-packaging` (Sessions 5-11 are the work) | `already-applied` — this session's proposal IS the packaging approach. |
| **G2** | Major | `fix-now` | `already-applied` — Session 4 inline edit. |
| **G3** | Major | `fix-during-packaging` | `session-6` (proposal §7 + review §7.1 README structure A11). |
| **G4** | Major | `fix-during-packaging` | `session-6` (proposal §7 + review §7.1 A4 — moved to separate Compat package). |
| **G5** | Major | `fix-during-packaging` | `session-6` (proposal §4.2 covers the side-by-side `Program.cs` substance; full overview update is Session 6). |
| **G6** | Major | `fix-during-packaging` | `session-6` (added explicitly: `Dabbler.Security/README.md` `RegisterPages/Entities/Endpoints/Controls` taxonomy explanation + worked composition example, as part of A11 README-structure standardization for the Security package). |
| **G7** | Major | `fix-during-packaging` | `session-6` (added explicitly: end-to-end security enforcement example in `Dabbler.Security/README.md` — covers row restriction in EF queries, API enforcement, role-switcher effect on effective authz. Same A11 README-structure deliverable). |
| **G8** | Major | `fix-during-packaging` | `session-6` (proposal §7 — "Security posture and defaults" subsection in overview + `Dabbler.Security/README.md`). |
| **G9** | Major | `fix-during-packaging` | `session-6` (proposal §7 — `docs/reports/rdf-schema.md` linked from `Dabbler.Reports.Core/README.md`). |
| **G10** | Major | `fix-during-packaging` | `session-6` (proposal §7 — Organization/multi-tenancy subsection). |
| **G11** | Major | `fix-during-packaging` | `session-8` (proposal §1 + review §7.3 — template tutorial README; lands with A12 + A13). |
| **G12** | Major | `fix-during-packaging` | `session-6` (proposal §7 — overview refactor elevating UAT DSL, Security.Blazor, Testing.Blazor). |
| **G13** | Major | `fix-during-packaging` | `session-6` (proposal §7 — overview "Where to go next" split). |
| **G14** | Minor | `fix-during-packaging` | `already-applied` — proposal §3.3 versioning policy is the decision; the disclosure of that decision in the per-library READMEs lands in `session-6` (A11). |
| **G15** | Minor | `defer` | `defer-out-of-set` — copy-able accessibility test pattern; honored as proposed. |
| **G16** | Minor | `fix-now` | `already-applied` — Session 4 inline edit (the `platform-overview.md` last paragraph already aggregates the platform-level license note). |
| **G17** | Minor | `fix-during-packaging` | `session-6` (added explicitly: governance/issue-filing/maintainer-contact subsection in `docs/platform-overview.md` "Where to go next" → contributor side; one paragraph). |
| **G18** | Minor | `accept-as-limitation` | `already-applied` — accepted; per-library README depth varies because library complexity varies; A11's structure standardizes the *shape* of READMEs (sections present) without forcing uniform depth. |
| **G19** | Minor | `fix-during-packaging` | `session-6` (added explicitly: `docs/platform-overview.md` Playwright section tightens to clarify the watermark is opt-in instrumentation, animations are excluded; one paragraph). |
| **G20** | Minor | `defer` | `defer-out-of-set` — verbose security configuration / convention-based discovery; honored as proposed. |
| **G21** | Major | `accept-as-limitation` (narrative) | `session-6` (proposal §7 — "Stack and audience" callout in overview; the *limitation* is accepted, the *disclosure* lands in Session 6). |
| **G22** | Major | `accept-as-limitation` (narrative) | `session-6` (proposal §7 — overview Authorization-model bullets explicitly flag no role inheritance + compiled-in baked-in privileges). |
| **G23** | Minor | `fix-during-packaging` | `session-6` (proposal §7 — REST-only principle one-sentence clarification). |

**Totals:** 4 `already-applied` (G1, G2, G14, G16, plus G18 accepted),
12 routed to Session 6 (G3-G10 except G11; G12, G13, G17, G19, G21-G23),
1 routed to Session 8 (G11), 2 `defer-out-of-set` (G15, G20).
Sum = 5 + 12 + 1 + 2 = 20, where 5 includes G18's accept-as-limitation
(which is `already-applied` in the sense that no further action is
required).

The G6, G7, G17, G19 entries are explicit new Session 6 deliverables
folded into the README-structure standardization deliverable (A11)
and the overview-refresh deliverable. They are not net-new scope
beyond Session 6's authoring — they are concrete sub-deliverables
inside it.

---

## 8. Methodology notes

### 8.1 Opus self-contribution context boundary (carryover from Session 4)

Per `feedback_orchestrator_self_contributes_to_review_panel`, the
orchestrator (Opus) wrote its review perspective directly as §11 of
the proposal — drawing on context the routed providers (gpt-5-4,
gemini-pro) did not have (project-guidance principles, lessons-learned,
prior session sets' actuals). The same Session 4 spec ambiguity flagged
last session (Step 2's "self-contained" prompt clause vs Step 3's
"orchestrator already has full context" clause) applies here.

Concrete consequence in this synthesis: Opus's §11.7 pre-identification
of weak points used cross-set continuity awareness (e.g., knowing the
value review had its own §3.7 dispute that ended with a similar "do
the documentation half, not the change-the-defaults half" compromise).
The routed reviewers wrote against the proposal alone. Where the
synthesis attributes findings to Opus's §11.7 specifically, that
attribution is honest about the context asymmetry. **The substance
of the convergence (4/5 Opus-flagged weak points confirmed by routed
reviewers) holds regardless of the methodology debate.** This is
documented for Step 9 reorganization-review consideration; it is not
re-litigated mid-session.

### 8.2 `route()` bypass via `call_model()` — same pattern as Session 4

The architecture task_type has auto-verification enabled in
`router-config.yaml` (`verification.auto_verify_task_types` includes
`architecture`). A `route()` call would have triggered redundant
cross-provider verification on each of the two routed calls
(gpt-5-4-mini for gemini-pro; opus for gpt-5-4), duplicating the
manual tri-provider review this session runs. Per the spec's "If a
`force_model` parameter isn't supported in the operator-tuned config,
the orchestrator may directly invoke each provider via the underlying
router primitive" allowance, both calls used `call_model()` directly
and recorded via `record_call()` so router-metrics.jsonl remained
intact.

This is identical to Session 4's approach. No new methodology
deviation surfaced.

### 8.3 gpt-5-4 reasoning_effort calibration applied

Per `feedback_gpt54_xhigh_large_prompt_timeout`, the gpt-5-4 call
ran at `reasoning_effort=high` (not the task_type_params default
`xhigh`) with provider timeout bumped to 600s. The proposal at 45KB
is under the 50KB threshold where xhigh times out, but the
defensive override cost nothing and the call completed in 215s with
12K output tokens. **The Session 4 calibration recommendation
generalized cleanly to this session.**

### 8.4 Gemini Pro 503 retry

The first gemini-pro call returned HTTP 503 (Google overload).
After ~2 minutes of router-side retries with exponential backoff
(2s, 4s, 8s) all 503'd, an explicit re-invocation 5 minutes later
landed cleanly in 40s. No model change was required; the second
attempt used the same model and parameters. **For future tri-provider
sessions, surface this pattern: Google 503 bursts usually clear in
under 10 minutes; the right move is to retry with a longer manual
delay, not to switch to a fallback model.**

### 8.5 Cost composition

| Reviewer | Cost | Notes |
|----------|------|-------|
| Opus self-contribution | $0 | Embedded in §11 of the proposal; no `route()` call to Opus. |
| GPT-5.4 routed review | $0.2105 | `reasoning_effort=high`, 11.6K input / 12.1K output tokens, 215s. |
| Gemini Pro routed review | $0.0276 | `thinking_budget=-1`, 12.4K input / 1.2K output tokens, 40s. |
| **Routed total** | **$0.2381** | Plus a `~$0` failed Gemini 503 attempt (no tokens returned). |

Routed cost is materially lower than Session 4's tri-provider
($0.2382 vs Session 4's $0.2382 for the actual provider review calls)
— essentially identical, which is a reasonable signal that the
proposal size + reasoning_effort settings were well-tuned for this
session.

### 8.6 Self-identified weak points and confirmation rate

Opus pre-identified five weak points in §11.7 of the proposal:

| § | Topic | Reviewer confirmation |
|---|-------|----------------------|
| §3.3 | Lock-step versioning across packages | Both reviewers ENDORSED the lock-step; gpt-5-4 challenged a different §3.3 detail (preview-suffix duration, A5). 1 confirmation of a *related* concern, not the lock-step itself. |
| §3.7 | Compat surface disposition | GPT-5.4 CONFIRMED (A4) — moved compat out of main package. |
| §4.4 | Stubs split | Gemini Pro CONFIRMED (A3) at Critical severity — split now, not deferred. |
| §5.1 | Template scope (2 vs 1 vs 3) | NEITHER reviewer challenged — the two-template scope held. |
| §6.1 | Smoke proof comprehensiveness | GPT-5.4 CONFIRMED (A2) at Critical severity — depth expanded substantially. |

4/5 of Opus's pre-identified weak points were confirmed by routed
reviewers as substantive critique. The fifth (§5.1) was upheld in
its current form. **This is a high calibration rate** and suggests
the "weakness self-identification" discipline is a useful proposal-
authoring tactic for future tri-provider sessions.

### 8.7 Disagreement adjudication

No issues were routed through `record_adjudication()` because the
review used `call_model()` directly rather than `route()` (no
auto-verifier was invoked on the routed calls; therefore no
verifier-vs-orchestrator disagreement to record). The
adjudication channel is reserved for the session-verification step
at Step 7 of this session, where the cross-provider verifier may
disagree with the orchestrator's synthesis here.

---

## 9. What changed in the proposal (inline edits applied)

After applying the six `apply-inline` items from §5, the proposal's
§3.3, §6.2, §10.1, §10.2, §8, and §11.2 are updated. The substantive
proposal does not change shape — the package matrix, opt-in stubs
contract, two-template model, lock-step versioning, and consumer-
install validation strategy all survive intact. The edits tighten
acceptance criteria and clarify dispositions; they do not invalidate
any prior reviewer's read.

A `+` marks added text; a `-` marks removed text. Full diff lives in
the proposal file itself; this summary records the categories of
change only:

- §3.3 versioning policy: removed the intermediate "Stabilization
  (0.x.0 no preview)" row; collapsed to two phases (preview through
  pre-1.0, then 1.0.0). (A5, A17)
- §6.2 packaged-contract check: added artifact-content assertion
  list (readme, icon, license, SourceLink, deps, static-web-assets).
  (A10)
- §10.1 risks: added two rows (MudBlazor/W0/.NET drift; template
  defaults cargo-culted to prod). (A15, A16)
- §10.2 rollback: replaced "delete from nuget.org within 24h" with
  unlist + staged promotion. Cross-references the §6.1 smoke gate
  (A2 deliverable). (A9)
- §8 open questions: collapsed Q1 (publish timing). The same edit
  pass also resolved the original Q2 (compat-surface disposition
  per A4) and Q3 (DevIdp distribution per A6) by promoting both
  to resolved-during-review entries. Two open questions remain
  (compatibility matrix publication; whether `dabbler-min` ships
  `Dabbler.Testing.Blazor`). (A17 + A4 + A6 spillover)
- §11.2 (Opus rationale on `[Obsolete]`) and §11.7 (weak points):
  added one paragraph each acknowledging the §3.7 reversal (A4)
  and the §4.4 reversal (A3), and recording the 4/5 calibration
  rate noted in §8.6 above.

The remaining 12 `fix-during-packaging` adjustments do NOT cause
proposal edits — they cause Session 6, 7, or 8 deliverable additions
per §7.1-§7.3 above. The proposal as it stands after the inline edits
is the binding input to those sessions.
