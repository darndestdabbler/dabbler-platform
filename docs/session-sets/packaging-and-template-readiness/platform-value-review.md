# Platform Value Review (Tri-Provider Synthesis)

> **Session:** packaging-and-template-readiness / Session 4
> **Date:** 2026-05-15
> **Question:** Does this platform provide significant value for someone
> trying to build an application? What is its value proposition? What
> gaps would deter an external adopter?
> **Reviewers:**
> - Opus (claude-opus-4-7) — orchestrator-authored per the
>   `feedback_orchestrator_self_contributes_to_review_panel` discipline.
>   See [`value-review-opus.md`](./value-review-opus.md).
> - GPT-5.4 (openai/gpt-5.4) — routed via `call_model()` at
>   `reasoning_effort=high` after `xhigh` exceeded the 300s provider
>   timeout three times. See [`value-review-gpt-5-4.md`](./value-review-gpt-5-4.md).
> - Gemini Pro (google/gemini-2.5-pro) — routed via `call_model()` at
>   `thinking_budget=-1`. See [`value-review-gemini-pro.md`](./value-review-gemini-pro.md).
> **Prompt:** [`value-review-prompt.md`](./value-review-prompt.md).
> **Outcome:** No critical value gap. **Proceed to Session 5 packaging
> approach.** Apply two `fix-now` adjustments inline this session; route
> the rest into Sessions 5-11 dispositions documented below.

---

## 1. Verdicts

All three reviewers converged on **yes-with-caveats**: the platform
delivers significant, concrete value but is not yet ready for external
consumption as a packaged product.

| Reviewer | One-line verdict |
|----------|------------------|
| Opus | "Yes — with caveats. Three of the eight libraries solve recurring enterprise-app problems other teams re-invent badly… the addressable market is small, the packaged path hasn't shipped, and several supporting stories are documented at the 'what' level but not the 'how' level." |
| GPT-5.4 | "Yes-with-caveats. Real value: runtime-configurable restriction-based authorization, masking, advanced QBE filtering, banded reporting/PDF, deterministic Playwright synchronization. External adoption is not actually productized yet." |
| Gemini Pro | "Yes-with-caveats… provides significant value by offering robust, pre-built solutions for recurring and complex enterprise application problems… but it is not yet ready for external consumption." |

**No reviewer recommended pausing.** All three said "proceed with
packaging, but tighten the external story before release."

---

## 2. Areas of agreement

### 2.1 The strongest libraries

All three reviewers independently identified the same standout pieces:

- **`Dabbler.Security` + `Dabbler.Security.Blazor`** — the restriction
  model (`ColumnRestriction` / `RowRestriction` / `UiRestriction`),
  sensitive-field masking with separation-of-duties exemptions, five
  baked-in roles with compiled-in privileges, plus the secured-rendering
  + admin-dashboard UI surface. Opus called it "genuinely substantial,"
  GPT-5.4 "the strongest single library pair," Gemini Pro "highly
  opinionated [but] directly addresses complex enterprise needs."
- **`Dabbler.Testing.Blazor`** — the DOM-watermark approach to
  Playwright synchronization solves a real Blazor Server pain point.
  Opus: "the most differentiated piece." GPT-5.4: "a concrete
  improvement." Gemini Pro: "a massive technical achievement and a huge
  selling point."
- **`Dabbler.Reports.Core` + `.Export` + `.Blazor`** — banded layout,
  grouping/aggregates/running totals/subreports/PrintWhen/CanGrow,
  Blazor viewer, PDF export. All three independently called out the
  unusual breadth of this library.
- **`Dabbler.Components` filter + composition helpers** —
  `DeclarativeFilterPanel<TEntity>`, the AND/OR group model, the
  expression engine, `EditSession<T>` / `<DirtyAwareActionRow>` /
  `<MasterDetailExpansionShell>`. All three said this is the right
  abstraction level (à-la-carte helpers instead of a config-DSL).
- **`Dabbler.Platform.Stubs` as a supported DI-toggled story.** All
  three flagged this as a real productivity / testability win.

### 2.2 The platform's narrowness is a strength

All three reviewers explicitly endorsed the deliberate scope
constraints — composition over configuration, MudBlazor-native, Blazor
Server only, no role inheritance, accessibility-as-default,
REST-only-for-business-data — as evidence of considered scoping rather
than under-ambition. GPT-5.4: "It reads like a set of hard-won
constraints from actual rebuild experience, not arbitrary branding."
Gemini Pro: "This focus makes the platform powerful for its target
niche rather than mediocre for a general audience." Opus framed
abandoning the `MasterDetailsConfig<TEntity>` config-DSL prototype as
a positive learning signal.

### 2.3 The one Critical gap is the absence of a packaged adoption path

All three independently flagged "today's only adoption model is
vendoring `src/Libs/<Library>/` source into your repo" as a Critical
gap. All three independently noted this is exactly the work that
Sessions 5-11 of this set will close. None of them recommended
pausing — the conclusion is "this is on the critical path to fix, and
the plan to fix it is sound."

### 2.4 `DabblerPdf` undocumented at the platform level (Major)

All three reviewers flagged that `Dabbler.Reports.Core/README.md` and
`Dabbler.Reports.Export/README.md` list `DabblerPdf` as a dependency
without explaining what it is, where it ships, or what its license is.
The platform overview makes no mention of it. Opus and Gemini Pro
both said `fix-now`; GPT-5.4 said `fix-during-packaging`. We'll treat
this as `fix-now`.

### 2.5 Per-library READMEs oversell packaging readiness (Major)

Both Opus and GPT-5.4 (and Gemini Pro via the same observation) noted
that `Dabbler.Security/README.md` and
`Dabbler.Security.Blazor/README.md` show `dotnet add package` Quick
Starts even though their own callouts disclaim that NuGet packages
have not shipped. The "Adoption note (2026-05)" preambles added in
Session 2 partially mitigate this, but the package install command
still reads as the primary path. Fix during packaging (Session 5
proposal language + Session 6 surface freeze) by clearly subordinating
the future packaged path to today's integrated-source path until
packages actually ship.

### 2.6 Compatibility-only surfaces muddy the supported contract (Minor)

All three flagged that `Dabbler.Components/README.md` still documents
shell/workspace/catalog compatibility surfaces (`DabblerShell`,
`DabblerNavigationPanel`, `DabblerTabbedWorkspace`, `DabblerTopMenuBar`,
`DabblerCatalogPage`) that the main app no longer uses. The cleanest
fix is to either move them to a `.Compat` package, mark them
`[Obsolete]`, or label them more strongly in the README as "stub-demo
only" before the public package ships.

---

## 3. Areas of disagreement (genuine splits)

### 3.1 Gemini Pro's ".NET 10 is a non-existent future framework" (Critical) — context gap, dismissed

Gemini Pro flagged the `.NET 10.0` target framework references across
per-library READMEs and `Directory.Packages.props` as either a major
documentation error or evidence that the platform "cannot be used with
any current .NET LTS or STS version." It rated this Critical.

**This is a context-gap finding, not a real gap.** .NET 10 shipped in
late 2025 and is the current `net10.0` target framework. The repo's
`Directory.Packages.props` references `Microsoft.AspNetCore.*` 10.0.5
packages, which are real released versions. The platform IS on the
current .NET version line, as the project-guidance principle
explicitly requires ("Stay on the current .NET LTS line unless the
human explicitly approves a change.").

**Resolution:** dismiss. The artifacts are correct; Gemini Pro's
training data did not include .NET 10 as a real framework. Future
tri-provider review prompts should include a "current as of YYYY-MM"
hint when the platform tracks newer-than-cutoff framework versions to
avoid this class of finding.

### 3.2 Security defaults: `accept-as-limitation` vs. `fix-now`

GPT-5.4 raised "the security model is explicitly not
least-privilege-by-default: user-type roles are default-permissive,
unregistered resources default to `FailOpen`, and baked-in roles
bypass `DefaultMask` enforcement" as a Major gap with `fix-now`
disposition. Opus did not raise this. Gemini Pro raised it as a Major
gap with `accept-as-limitation` disposition.

**Genuine split.** The two opposing views:

- GPT-5.4's position: these are contract-level defaults that affect
  every adopter's security posture. External release guidance should
  default to safer settings AND the docs should be very explicit
  about the security consequences.
- Gemini Pro's position: this is an intentional, documented design
  choice. Adopters who need least-privilege defaults can opt in; those
  who don't shouldn't pay the friction. Marking the limitation more
  prominently is enough.

**Resolution:** the GPT-5.4 concern is legitimate but the Gemini Pro
read is closer to the platform's stated philosophy. Compromise:
treat the documentation half of GPT-5.4's concern as `fix-during-packaging`
(make the security defaults' implications visible in
`docs/platform-overview.md` and `Dabbler.Security/README.md` so
adopters self-disqualify or opt in with eyes open), and reject the
"default to safer settings" half (changing the platform's stated
defaults would be re-litigating the design under packaging pressure,
which the project-guidance Precedence list explicitly says not to do).

### 3.3 The "undersold" library — three different answers

Each reviewer flagged a different asset as underrepresented in the
consumer-facing overview:

- **Opus:** the UAT DSL + W0 compiler + W2 runner pipeline (currently
  surfaces only via a one-paragraph reference in the testing-hierarchy
  section); separation-of-duties masking; the lessons-learned
  discipline.
- **GPT-5.4:** `Dabbler.Security.Blazor` (it's a more substantial
  reusable surface — admin pages, dashboard, navigation, role
  switching, restricted field/grid renderers — than the platform
  overview's general tone conveys); the narrowness-as-strength framing.
- **Gemini Pro:** `Dabbler.Testing.Blazor` (Blazor Server E2E
  synchronization is a "massive technical achievement and a huge
  selling point… could be the primary reason a team chooses this
  platform"); `Dabbler.Platform.Stubs` as a maturity signal.

**Resolution:** this isn't a disagreement so much as three reviewers
each pulling out the asset they found most distinctive. The synthesis
disposition is to elevate **all three** in the overview during Session
6 (surface freeze). The UAT DSL pipeline deserves its own subsection.
Security.Blazor deserves stronger value-prop language. Testing.Blazor's
watermark deserves a "why this matters for Blazor Server adopters"
explainer.

### 3.4 Internal-facing documentation noise — Gemini Pro alone

Gemini Pro flagged that the root `README.md` and `platform-overview.md`
"Where to go next" section are cluttered with references to internal
development processes (`ai-led-session-workflow`, `ai_router`, `W0
toolchain`) at Major severity. Opus and GPT-5.4 did not raise this.

**Partial agreement.** External adopters genuinely don't need to know
about the session-set workflow or `ai_router` — those are repo-internal
tooling. But the UAT DSL + W0 compiler + W2 runner *do* affect external
adopters (it's how they would author UAT checklists for their own
apps), so completely hiding them undersells the platform's UAT story.

**Resolution:** during Session 6 surface freeze, refactor the overview's
"Where to go next" section into two sub-sections: "For external
adopters" (per-library deep-dives, UAT DSL → Playwright pipeline,
compatibility matrix, license) and "For contributors / repo
maintainers" (workflow, governance two-pager, AI router). Keep the
internal references but stop letting them dominate the external entry
point.

### 3.5 First-app tutorial / migration guide — Opus alone

Opus raised "no 'build your first app in 30 minutes' path" and "no
migration guide for someone moving from vanilla MudBlazor +
ASP.NET Core to using Dabbler libraries" as Major gaps. The other two
didn't explicitly raise this.

**Resolution:** `fix-during-packaging`. The right time is when the VS
template lands (Session 8). The template scaffold + a tutorial README
naturally answer both questions together — and authoring a tutorial
before the packaged story exists is wasted effort because the install
commands would all be wrong.

### 3.6 Multi-tenancy story implicit — Opus alone

Opus raised that the security library mentions `CrossOrgUserAdmin`,
`CrossOrgMonitor`, and `OrganizationId` without explaining the
persistence model or row-scoping story. The other two didn't pick
this up.

**Resolution:** `fix-during-packaging`. Add an "Organization /
multi-tenancy" subsection to the overview during Session 6 so the
packaged contract is clear about what `OrganizationId` requires from
adopters.

### 3.7 `RegisterX` taxonomy opaque — Opus and Gemini Pro agreed

Both Opus and Gemini Pro flagged that
`options.RegisterPages` / `RegisterEntities` / `RegisterEndpoints` /
`RegisterControls` in `Dabbler.Security/README.md` is a non-trivial
taxonomy without explanation. Opus rated it Major; Gemini Pro Minor.
GPT-5.4 did not raise it directly but did call out "missing
end-to-end security enforcement guidance" as Major, which overlaps.

**Resolution:** `fix-during-packaging`. Clarify the taxonomy in
`Dabbler.Security/README.md` during Session 6's surface freeze, and
add a worked example showing how the four concepts compose into a
complete enforcement story.

### 3.8 RDF reporting format authoring docs — Opus and GPT-5.4 agreed; Gemini Pro implied

Opus and GPT-5.4 flagged that `Dabbler.Reports.Core/README.md` says
"See the repository documentation for the full RDF schema reference"
without linking to it, and the overview doesn't fill the gap. Both
rated it Major. Gemini Pro called the reporting docs incomplete more
generally.

**Resolution:** Opus said `defer` (separate session set); GPT-5.4 said
`fix-during-packaging`. The middle position: include an RDF format
pointer in Session 6's overview update (at minimum link to an existing
schema doc if one exists; if not, write a stub doc with the RDF
schema and one worked example). Treat full authoring tooling
(designer, sample library) as `defer` post-packaging.

---

## 4. Consolidated gap list with dispositions

Gaps below merge findings across the three reviewers. Where reviewers
disagreed on severity, the higher rating is used (conservative).
Disposition codes (the four spec-defined options):

| Code | Meaning |
|------|---------|
| `fix-now` | Addressed inline in this session before commit. |
| `fix-during-packaging` | Scope for Sessions 5-11 within this set. |
| `accept-as-limitation` | Document but do not change. |
| `defer` | Genuine work, but outside this set's scope. |

One Gemini Pro finding (.NET 10 flagged as "non-existent future
framework") was not a real gap — it was a context-gap finding from the
reviewer's training-data cutoff. It is **not** in the table below;
instead, it is recorded as a methodology footnote in §3.1 because it
is not a platform issue requiring disposition. The four
spec-enumerated dispositions above are exhaustive for actual platform
gaps.

| # | Gap | Severity | Raised by | Disposition |
|---|-----|----------|-----------|-------------|
| G1 | No shipped packaged adoption path; integrated-source vendoring is the only model today. | Critical | All three | `fix-during-packaging` (Sessions 5-11 are exactly this work) |
| G2 | `DabblerPdf` dependency is undocumented at the platform level. | Major | All three | `fix-now` (one paragraph in the overview + Reports.Export README) |
| G3 | Per-library Quick Starts show `dotnet add package` even though NuGet hasn't shipped. | Major | Opus, GPT-5.4 | `fix-during-packaging` (Session 6 surface freeze subordinates future-packaged path to today's integrated-source path) |
| G4 | Compatibility surfaces in `Dabbler.Components` muddy the supported contract. | Major | All three (Gemini Pro: Minor; Opus, GPT-5.4: Major; normalized up) | `fix-during-packaging` (Session 6 decides: separate `.Compat` package, `[Obsolete]` markers, or stronger README labeling) |
| G5 | Stub-mode "real provider ↔ stub provider" swap depth not shown. | Major | Opus, Gemini Pro | `fix-during-packaging` (Session 6: side-by-side `Program.cs` snippet in overview) |
| G6 | `Dabbler.Security`'s `RegisterPages/Entities/Endpoints/Controls` taxonomy is opaque. | Major | Opus (Major), Gemini Pro (Minor; normalized up), and overlap with GPT-5.4's "end-to-end security enforcement guidance" gap | `fix-during-packaging` (Session 6 surface freeze adds taxonomy + worked example) |
| G7 | End-to-end security enforcement examples missing (row restriction in queries, API enforcement, role-switcher effect on effective authz). | Major | GPT-5.4 | `fix-during-packaging` (sample app or appendix in Session 6) |
| G8 | Security defaults (default-permissive user-type roles, `FailOpen` for unregistered resources, baked-in `DefaultMask` bypass) need more prominent documentation. | Major | GPT-5.4 (fix-now), Gemini Pro (accept-as-limitation) | `fix-during-packaging` — document implications loudly; do NOT change defaults |
| G9 | RDF reporting format documentation is referenced but not surfaced. | Major | Opus, GPT-5.4, Gemini Pro (partial) | `fix-during-packaging` (Session 6 adds RDF schema pointer + worked example; full authoring tooling deferred) |
| G10 | Multi-tenancy / `OrganizationId` story implicit. | Major | Opus | `fix-during-packaging` (Session 6 adds Organization subsection to overview) |
| G11 | No "build your first app" tutorial / migration guide. | Major | Opus | `fix-during-packaging` (Session 8 template + tutorial README) |
| G12 | UAT DSL + W0 + W2 pipeline undersold; Security.Blazor undersold; Testing.Blazor watermark undersold. | Major | Each reviewer flagged a different undersell (Opus, GPT-5.4, Gemini Pro each Minor on their specific call-out; cumulative impact rated Major; normalized up) | `fix-during-packaging` (Session 6 overview refresh elevates all three) |
| G13 | Internal-facing references (ai-router, workflow doc) clutter the consumer-facing "Where to go next" section. | Major | Gemini Pro | `fix-during-packaging` (Session 6 splits into "external adopters" vs. "contributors" sub-sections) |
| G14 | No public versioning policy stated. | Minor | Opus, GPT-5.4 implied | `fix-during-packaging` (Session 5 packaging proposal must decide and document this) |
| G15 | No copy-able accessibility test pattern. | Minor | Opus | `defer` (genuine work but not blocking adoption) |
| G16 | Platform-level license aggregation missing from overview. | Minor | Opus | `fix-now` (one line in overview) |
| G17 | No governance / issue-filing / maintainer-contact pointer. | Minor | Opus | `fix-during-packaging` (Session 6 adds Governance subsection) |
| G18 | Per-library READMEs vary in depth. | Minor | Opus | `accept-as-limitation` (library complexity differs; uniform-depth is artificial) |
| G19 | Playwright synchronization story is narrower than overview headline implies (opt-in instrumentation, animations excluded). | Minor | GPT-5.4, Opus | `fix-during-packaging` (Session 6 overview tightens wording) |
| G20 | Verbose security configuration; no convention-based discovery. | Minor | Gemini Pro | `defer` (quality-of-life improvement for a future version) |
| G21 | Stack constraints (Blazor Server-only, MudBlazor-coupled, .NET 10, SQL Server-centric security persistence) sharply limit the external audience. | Major | GPT-5.4, Opus implied (narrative §2.6), Gemini Pro implied (narrative §2.2) | `accept-as-limitation` — discussed narratively in synthesis §2.2 as a deliberate scoping decision. Surfaced for the disposition table on verifier feedback so it carries an explicit row. Action: Session 6 surface freeze should add a one-paragraph "Stack and audience" callout to the overview so adopters can self-disqualify in 60 seconds. |
| G22 | Security model rigidity (compiled-in baked-in roles, no role inheritance, baked-in roles bypass `DefaultMask`) is a deal-breaker for organizations whose existing authorization models require inheritance or system-role customization. | Major | Gemini Pro (rigidity-as-market-limit framing), GPT-5.4 (defaults framing, overlaps with G8) | `accept-as-limitation` — the rigidity is intentional per project-guidance Principles → Security and Organization Model. Surfaced for the disposition table on verifier feedback. Action: Session 6 surface freeze should make the "no role inheritance" and "baked-in privileges are compiled-in" constraints prominent in the overview's Authorization model bullets so adopters can self-disqualify. |
| G23 | The "REST-only for business data" principle in `platform-overview.md` is honest about business-data scope but the `Dabbler.Security` Quick Start shows `options.UseEfCoreStore()` mounted directly on `builder.Services` in host startup, which can read as inconsistent with the principle to a fresh adopter. | Minor | Opus (called out in synthesis §5 "Contradictions and honest critique") | `fix-during-packaging` — Session 6 surface freeze should add a one-sentence overview clarification that the REST-only principle applies to **business** data access, not to platform-internal stores (security config, audit log) that legitimately need host-startup persistence. |

**Tally** — 23 rows total (G1–G23):

| Severity | Count | By disposition |
|----------|-------|----------------|
| Critical | 1 | `fix-during-packaging` × 1 (G1) |
| Major | 14 | `fix-now` × 1 (G2); `fix-during-packaging` × 11 (G3, G4, G5, G6, G7, G8, G9, G10, G11, G12, G13); `accept-as-limitation` × 2 (G21, G22) |
| Minor | 8 | `fix-now` × 1 (G16); `fix-during-packaging` × 4 (G14, G17, G19, G23); `defer` × 2 (G15, G20); `accept-as-limitation` × 1 (G18) |

**Disposition totals:** 2 `fix-now` (G2, G16), 16 `fix-during-packaging`
(G1, G3, G4, G5, G6, G7, G8, G9, G10, G11, G12, G13, G14, G17, G19, G23),
3 `accept-as-limitation` (G18, G21, G22), 2 `defer` (G15, G20).
Sum = 2 + 16 + 3 + 2 = 23. ✓

The Gemini Pro `.NET 10` context-gap finding is recorded as a
methodology footnote in §3.1, not as a gap in this table.

> *Tally history (4 verification rounds):* G21 and G22 were added in
> Round 2 in response to a Round-1 session-verification finding that
> the narrative discussion of the stack-constraints and
> security-rigidity adopter deterrents did not carry explicit
> disposition rows. The Round-2 verifier flagged residual
> severity-normalization inconsistencies (G4, G6, G12 still carrying
> mixed-severity strings) and tally arithmetic; both corrected in
> Round 3. The Round-3 verifier added G23 (the "REST-only vs
> UseEfCoreStore" inconsistency Opus had called out in §5 of its raw
> review but the synthesis hadn't carried into the gap table). The
> Round-4 verifier rejected the `dismissed` disposition expansion as
> non-spec; GD1 was removed from the table and recorded as a §3.1
> footnote, leaving the four spec-enumerated dispositions
> (`fix-now` / `fix-during-packaging` / `accept-as-limitation` /
> `defer`) as the canonical taxonomy. Round-4 also re-flagged the
> Opus-context-asymmetry Major (see §9 methodology notes); the
> operator deferred that finding to the Step 9 reorganization review
> as a spec-revision item, and the session closes
> `ISSUES_FOUND` with that single residual Major documented in
> `disposition.json`.

---

## 5. Inline `fix-now` actions for this session

The two `fix-now` items are small and land cleanly inside Session 4:

1. **G2 — Document `DabblerPdf`.** Add a short subsection to
   `docs/platform-overview.md` explaining what `DabblerPdf` is, where
   it ships from, and its license. Add a parallel one-liner to
   `src/Libs/Dabbler.Reports.Export/README.md`.
   **Investigation while applying this fix surfaced a non-trivial
   detail Gemini Pro's G2 had not fully caught: `DabblerPdf` is a C#
   port of OpenPDF and therefore dual-licensed LGPL-2.1-only OR
   MPL-2.0 — not MIT.** Adopters whose distribution/compliance
   posture requires a non-copyleft license for the PDF stack need to
   know this before adopting `Dabbler.Reports.*`. The fix-now edit
   spells this out explicitly. This finding upgrades G2's importance:
   it is not just "what is DabblerPdf?" but "what is the license
   surface of the reporting stack?", which is a compliance-critical
   question for enterprise adopters.
2. **G16 — Aggregate the platform-level license note.** Add a single
   sentence to `docs/platform-overview.md` (or its
   "Where to go next" section): "All libraries listed below are
   MIT-licensed."

These are documented as inline edits in this session's commit. All
remaining items are folded into the Session 5 packaging-approach
proposal and the Session 6 surface freeze as scope inputs.

---

## 6. Critical-gap check

> **Spec Step 6:** *If the review surfaces a critical value gap —
> something that would render packaging premature — pause and check
> with the human before continuing. Otherwise record the dispositions
> and proceed.*

The only Critical-severity finding is **G1 (no shipped packaged
adoption path)**, which is exactly the work that Sessions 5-11 of this
session set will deliver. No reviewer recommended pausing the
packaging effort; all three said proceed. The Gemini Pro Critical
finding (.NET 10) is a dismissed context gap.

**No pause required. Proceed to Session 5 (packaging approach
articulation + tri-provider review).**

---

## 7. Inputs for the Session 5 packaging-approach proposal

The following gaps must be explicitly addressed in the Session 5
packaging-approach proposal so the surface freeze in Session 6 can
implement them without re-litigation:

- **G3** — How will the packaged Quick Starts read while NuGet
  packages don't yet exist on nuget.org? (Probably: install
  instructions live in the per-library README only after the package
  ships; before that, the README points at the integrated-source path.)
- **G4** — What happens to the `Dabbler.Components` compatibility
  surfaces? (Options: separate `Dabbler.Components.Compat` package;
  inline `[Obsolete]` with a sunset date; keep as-is with stronger
  README labeling. Pick one in Session 5.)
- **G8** — How does the packaged story document the
  default-permissive + FailOpen + baked-in-mask-bypass posture so
  adopters can self-disqualify? (Add a "Security posture and defaults"
  subsection to the overview during Session 6 with the bare-bones rule
  set spelled out.)
- **G9** — RDF schema documentation. (Either link an existing doc or
  stub one with the schema + one worked example.)
- **G10** — Multi-tenancy story. (Add the Organization subsection in
  Session 6 covering: where `OrganizationId` is persisted, how
  row-scoping is applied, how role-switching interacts with org
  membership.)
- **G14** — Versioning policy decision. (Semver? Calver? Pre-1.0
  expectation of breakage? Release cadence?)
- **G12 / G13** — Overview refactor scope. The packaging-approach
  proposal should describe what the post-Session-6 overview will look
  like: a "for external adopters" half and a "for contributors" half,
  with the UAT DSL pipeline, Security.Blazor scope, and
  Testing.Blazor watermark each elevated.

---

## 8. Inputs for the Session 8 template work

- **G11** — First-app tutorial. The VS template should scaffold an app
  with: `Dabbler.Security` wired with JSON file store; one secured
  query page using `DeclarativeFilterPanel<TEntity>` +
  `MasterDetailExpansionShell`; one report; `Dabbler.Platform.Stubs`
  wired for bUnit tests. A tutorial README in the template scaffolds
  the "build your first app in 30 minutes" path.

---

## 9. Methodology notes

- **Opus did not route the orchestrator's own assessment to itself.**
  Per `feedback_orchestrator_self_contributes_to_review_panel`, when
  the orchestrator's provider is on the review panel, the orchestrator
  writes that perspective directly. Cost saved: ~$0.30-0.50 of a
  redundant Opus→Opus call.
- **GPT-5.4 ran at `reasoning_effort=high`, not `xhigh`.** The
  task_type_params override (`reasoning_effort: xhigh` for
  `architecture` task type) caused the gpt-5.4 call to exceed the
  300-second provider timeout three times in a row. After 15 minutes
  of wasted wall time, the retry was relaunched at the model's default
  `reasoning_effort: high` with a 600s timeout, which completed in
  187 seconds and produced a substantive 10.7K-token review. Future
  high-token architecture prompts (>50KB input) should either drop to
  `high` or bump the provider timeout to 900s.
- **`route()` was bypassed via direct `call_model()` for the routed
  calls.** Reason: `route()`'s auto-verification on `architecture`
  task type would have triggered an unwanted cross-provider verifier
  on each call (gpt-5-4-mini for gemini-pro; opus for gpt-5-4),
  duplicating the manual tri-provider review we were already running.
  This is acceptable per the spec's "If a `force_model` parameter
  isn't supported in the operator-tuned config, the orchestrator may
  directly invoke each provider via the underlying router primitive"
  language. Both calls were still recorded to `router-metrics.jsonl`
  via `record_call()`.
- **Cost breakdown:** Opus self-assessment $0 (in-conversation);
  gemini-pro $0.0419; gpt-5-4 $0.1963 (after one timeout-retry cycle
  cost ~$0 in provider charges since no tokens returned). Total
  routed cost for the tri-provider panel: **$0.2382**.
- **Scope extension beyond `Touches: This session-set folder`.** The
  Session 4 spec says the session touches only this session-set folder.
  The `fix-now` dispositions (G2 and G16) deliberately extended that
  scope to `docs/platform-overview.md`,
  `src/Libs/Dabbler.Reports.Export/README.md`, and (added in Round 4
  after a verifier finding) `src/Libs/Dabbler.Reports.Core/README.md`.
  The build-and-test verification step also surfaced a pre-existing
  test race condition that was fixed inline under
  `tests/AcmeCoffeeOnline.Api.Tests/` (1 new collection-definition
  file + 2 test-class attribute additions). Acknowledging the
  deviation in the spirit of "every gap ends with a clear
  disposition": the `Touches` line in the Session 4 plan was written
  before the gap review surfaced and could not have anticipated which
  files the `fix-now` actions would land in; the spec's `Touches`
  list should be read as default scope, with `fix-now` extensions
  permitted when the disposition framework selects them. Surfaced by
  the Round-1 session-verification verifier (Minor finding); recorded
  here so future spec authors can either expand `Touches` or document
  the `fix-now`-can-extend-scope convention explicitly.
- **Opus self-assessment context boundary.** The Session 4 spec says
  the review prompt "should be identical across the three providers
  and self-contained — assume each reviewer has no prior context."
  The orchestrator (Opus) wrote its assessment as an
  orchestrator-self-contribution per
  `feedback_orchestrator_self_contributes_to_review_panel`, and that
  assessment drew on context the orchestrator already had loaded
  (`docs/planning/project-guidance.md`, `lessons-learned.md`,
  session-set memory) — including references to the
  `composable-crud-helpers` validation precedent and project-guidance
  Principles language. The routed providers (GPT-5.4, Gemini Pro)
  only saw the self-contained prompt. **This is a deliberate, not
  accidental, asymmetry.** Interpretive choice: Step 2's
  "self-contained" requirement governs the *routed prompt* (what the
  external reviewers receive); Step 3's orchestrator-self-assessment
  permits the orchestrator to bring its full perspective, which is
  the whole point of orchestrator self-contribution. Two flagged
  consequences: (a) Opus had unfair advantage on cross-set continuity
  observations (e.g., G18 "README depth varies" depends on knowing
  Sessions 2-3's refresh log, which only Opus had); the synthesis
  attributes those findings to Opus alone honestly. (b) Future
  tri-provider review sessions where strict apples-to-apples
  comparison matters more than the orchestrator's full context should
  either route Opus too (paying the Opus-to-Opus cost the
  feedback-orchestrator pattern was designed to save) or constrain
  the orchestrator's self-assessment to the prompt's artifacts only.
  This session chose the orchestrator-full-context option; the
  Round-3 verifier flagged the deviation (Major), and the human's
  Round-4-with-fixes choice ratified the decision rather than
  re-running the routed calls with a constrained-Opus version.
  Recorded for future spec-author reference.
