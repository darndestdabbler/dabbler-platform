# AI Assignment & Cost Ledger: `crud-scaffolding`

This document is a per-session ledger of the cheapest capable AI for each step plus a forward-looking recommendation for the next session.

- **Set Name**: `crud-scaffolding`
- **Total Sessions**: 5

---

## Session 1: Contract And QBE Helper Foundation

-   **Recommended Orchestrator**: `Anthropic Sonnet 4.6 @ effort=high`
-   **Rationale**: The primary authoring task is a moderately complex C# extension method using expression trees, which is well within Sonnet's capabilities. Since the most complex reasoning (contract unification, transport decisions) is routed via `analysis`, using a premium orchestrator like Opus would be cost-ineffective for a session focused on dispatch and a single file implementation.
-   **Estimated Routed Cost**: Moderate

| Step | Action                                                                | Routing Decision                                                              |
| :--- | :-------------------------------------------------------------------- | :---------------------------------------------------------------------------- |
| 1    | Read project guidance and prior art.                                  | `direct`                                                                      |
| 2    | Analyze `GridDataRequest`/`PagedRequest` for contract unification.    | `route(task_type="analysis", complexity_hint=55)`                             |
| 3    | Author `IQueryable<T>.ApplyQbeFilter` extension method and unit test. | `direct`                                                                      |
| 4    | Decide HTTP transport for QBE filter graph (GET vs POST).             | `route(task_type="analysis", complexity_hint=20)`                             |
| 5    | Write `request-contract-decision.md` and `qbe-helper-api.md`.         | `direct`                                                                      |
| 6    | Build, test, and run cross-provider verification.                     | `direct` (build/test), `route(task_type="session-verification")` (verification) |

#### Actuals (filled after the session)

- **Orchestrator used:** Claude Code / claude-opus-4-7 (started Opus, switched to Sonnet mid-session per ai-assignment recommendation, then back to Opus for closeout)
- **Total routed cost:** $0.3455 (Gemini Pro $0.0547 for 3 analysis/refactoring calls + GPT-5.4 $0.2908 for 3 verification rounds)
- **Deviations from recommendation:** Started as Opus 4.7 (recommended Sonnet 4.6). Human switched orchestrator mid-session; no impact on output quality. The QBE extension method was implemented via a routed refactoring call (not direct authoring) due to >50 line threshold.
- **Notes:** 3 verification rounds (recommended 1-2). Rounds 1-2 found real bugs: `object?` → `JsonElement` deserialization gap in `GridQueryFilter.Value` and `Expression.Constant` failure for nullable value types. Both required code fixes + new tests. Round 3 VERIFIED clean. Total 13 unit tests authored.

---
**Next-session orchestrator recommendation (Session 2)**: `Anthropic claude-opus-4-7 @ effort=low`
**Rationale (routed analysis):** Session 1 actuals show that subtle expression-tree correctness bugs required 3 verification rounds; a stronger model with careful first-pass authoring of the C# service/controller templates and smoke test reduces round-trip cost even at effort=low.

---

## Session 2: `dabbler-crud-service` Template

-   **Recommended Orchestrator**: `Anthropic Sonnet 4.6 @ effort=high`
-   **Rationale**: This session consists of authoring structured C# template files (interface, implementation, controller) and a corresponding test project. Sonnet offers the best price-to-performance ratio for this type of mechanical, yet non-trivial, code generation, avoiding unnecessary costs associated with upward delegation to Opus.
-   **Estimated Routed Cost**: Low

| Step | Action                                                                              | Routing Decision                                                              |
| :--- | :---------------------------------------------------------------------------------- | :---------------------------------------------------------------------------- |
| 1    | Author service interface template (`IDabblerCrudService.cs.in`).                    | `direct`                                                                      |
| 2    | Author service implementation template (`DabblerCrudService.cs.in`).                | `direct`                                                                      |
| 3    | Author controller template (`DabblerCrudController.cs.in`).                         | `direct`                                                                      |
| 4    | Author consumer smoke test project under `tests/Dabbler.Templates.Tests/`.          | `direct`                                                                      |
| 5    | Build, test (including smoke test execution), and run cross-provider verification. | `direct` (build/test), `route(task_type="session-verification")` (verification) |

#### Actuals (filled after the session)

- **Orchestrator used:** Claude Code / claude-opus-4-7 / effort=high (recommended: Sonnet 4.6 @ high per spec, OR Opus 4.7 @ low per Session 1 actuals revision; the human chose to keep the active Opus 4.7 @ high before mutating state).
- **Total routed cost:** ~$0.48 across 3 verification rounds via GPT-5.4 (R1 $0.142 / R2 $0.191 / R3 $0.145) + $0.003 Gemini Pro for Session 3 recommendation.
- **Deviations from recommendation:** Substantial scope expansion to promote `GridQueryRequest` + `QueryableExtensions` (and a new `GridQueryResponse<T>`) out of `AcmeCoffeeOnline.*` into a new packable library `Dabbler.Api.Querying` (per user direction after the orchestrator surfaced the Session 1/Session 2 placement gap). Also updated `tools/pack.ps1` and `Dabbler.Packaging.Smoke` `requiredIds` to include the new package.
- **Notes:**
  - 3 verification rounds is now the empirical baseline for this set (Session 1 also took 3). Round 1: 1 Critical mapping stub + 2 Major spec/doc + 1 Minor test gap. Round 2: 2 Major correctness (non-int PK route constraint, PK-injection via reflection copy) + 1 Major spec (camelCase CLI) + 1 Minor doc. Round 3: 2 Minor docs only — applied inline.
  - Template-engine learnings landed for next sessions: (a) `replaces` only does content substitution; `fileRename` is required for filename renames; (b) non-overlapping tokens (e.g., `T1Entity` vs `T2EntityPlural`) avoid substring-replace conflicts; (c) `dotnetcli.host.json` with `symbolInfo.longName` provides kebab-case CLI aliases the spec implies but `template.json` alone cannot deliver; (d) the `coalesce` generator pattern (sourceVariableName + fallbackVariableName) replaces a parameter's value with a derived one when omitted; (e) the `switch` generator drives derived values like a route constraint from a parameter (`pkType` → `:int`/`:long`/`:guid`).
  - Default reflection-based mapping with a `NonCopyablePropertiesFromDto` seam is the safe shape for scaffolded MapToDto / MapToEntity / CopyTo — PK exclusion prevents create-time key injection and update-time key-mismatch confusion.

---
**Next-session orchestrator recommendation (Session 3)**: `Anthropic claude-opus-4-7 @ effort=max`
**Rationale (routed analysis, Gemini Pro):** Session 3's core challenge is schema-driven property iteration, a complex task not natively supported by `dotnet new`. Given that Opus @ high still required three verification rounds for Session 2, the most capable model with maximum effort is appropriate to generate the novel templating logic, aiming to reduce verification cycles.

---

## Session 3: `dabbler-crud-page` Template With Bundled ApiClient + GridDataProvider

-   **Recommended Orchestrator**: `Anthropic claude-opus-4-7 @ effort=max` (revised at Session 2 close via routed analysis; supersedes the original Session 1 recommendation of Sonnet 4.6 @ effort=high).
-   **Rationale**: Session 3's core challenge is schema-driven property iteration paired with a Razor + C# templating mix; given Session 2 needed three verification rounds at Opus @ high, Opus @ max is selected to drive first-pass correctness down to one or two verification rounds. The original Sonnet @ high recommendation underweighted both the schema-driven post-process design work and the multi-file blast radius of the page template.
-   **Estimated Routed Cost**: Moderate

| Step | Action                                                                              | Routing Decision                                                              |
| :--- | :---------------------------------------------------------------------------------- | :---------------------------------------------------------------------------- |
| 1    | Author page template (`CrudPage.razor.in`)                                          | `direct`                                                                      |
| 2    | Author bundled `{Entity}ApiClient.cs.in`.                                           | `direct`                                                                      |
| 3    | Author bundled `{Entity}GridDataProvider.cs.in`.                                    | `direct`                                                                      |
| 4    | Author end-to-end smoke test to verify DI resolution of both templates.             | `direct`                                                                      |
| 5    | Build, test (including smoke test execution), and run cross-provider verification. | `direct` (build/test), `route(task_type="session-verification")` (verification) |

#### Actuals (filled after the session)

- **Orchestrator used:** Claude Code / claude-opus-4-7 / effort=max (the human upgraded from Opus@high mid-question after the initial AskUserQuestion; matches the Session 2 close routed revision).
- **Total routed cost (this session's calls only):** ~$0.56 across 3 verification rounds via GPT-5.4 (R1 $0.2618 / R2 $0.2335 / R3 $0.0605) + ~$0.0012 Gemini Pro for the Session 4 next-orc recommendation. The session-tagged bucket in `router-metrics.jsonl` shows ~$1.21 total but that includes Session 2 close's forward-prep analysis traffic tagged with `session_number=3`.
- **Deviations from recommendation:** None substantive — actually ran at the recommendation (Opus 4.7 @ max). One mid-session orchestrator-effort upgrade from `high` to `max` via the human's reply to the start-of-session AskUserQuestion.
- **Notes:**
  - 3 verification rounds — the empirical baseline for this set holds (Sessions 1+2+3 all needed 3 rounds). Round 1 found 1 Critical + 3 Major + 2 Minor; Round 2 closed 5 of 6 (remaining: DateTime emission compile-safety); Round 3 VERIFIED.
  - Confirmed the verifier-spiral pattern from `feedback_verifier_spiral_doc_sessions`: each round surfaced previously-uncaught nuance even when substance was sound. Round 3 was the natural settling point at "operator-decision boundary" per the round-3 verifier output itself.
  - Key design lesson: `dotnet new` cannot iterate over a JSON array of properties. The "schema-file-driven post-process" allowance from spike review gate B5 lets a companion PowerShell script (`Apply-{Entity}Schema.ps1`) carry the per-property iteration. The script is idempotent and uses BEGIN/END placeholder markers in the `.razor` files. This pattern is reusable for any future template that needs schema-driven iteration over a list.
  - Compile-safety took precedence over UX perfection for the `DateTime` field: `v ?? default` emission compiles for both `DateTime` and `DateTime?` DTO properties; the trade-off (cleared nullable dates become MinValue, not null) is documented inline in the script and the generated code.
  - Smoke test now exercises `AddSmokeScaffolding(builder.Configuration)` from `Program.cs` so the `dotnet build` is a real DI-resolution proof, not just a "scaffold emitted" proof. Promoted from the Round-1 verifier's recommendation.

---
**Next-session orchestrator recommendation (Session 4)**: `Anthropic Haiku 4.5 @ effort=max` (confirmed via routed Gemini Pro analysis, $0.0012).
**Rationale (routed analysis):** "The Session 3 actuals are irrelevant as that was a complex coding task, whereas Session 4 is a straightforward documentation task with a built-in review process. A fast, cost-effective model is ideal for generating the initial markdown drafts for human and cross-provider review." This matches the original Session 1 recommendation; no revision needed.

---

## Session 4: Documentation, Examples, Roadmap Markers

-   **Recommended Orchestrator**: `Anthropic Haiku 4.5 @ effort=max`
-   **Rationale**: The task is to draft four documentation files based on the completed implementation, which is a low-complexity summarization and formatting exercise. Quality is ensured by a mandatory `documentation` review route, making the cheap and fast Haiku the optimal choice for the initial draft generation.
-   **Estimated Routed Cost**: Low

| Step | Action                                                               | Routing Decision                                                              |
| :--- | :------------------------------------------------------------------- | :---------------------------------------------------------------------------- |
| 1    | Author `docs/scaffolding-templates/README.md`.                       | `direct`                                                                      |
| 2    | Author `docs/scaffolding-templates/crud-service-template.md`.        | `direct`                                                                      |
| 3    | Author `docs/scaffolding-templates/crud-page-template.md`.           | `direct`                                                                      |
| 4    | Author `docs/scaffolding-templates/non-mvp-roadmap.md`.              | `direct`                                                                      |
| 5    | Route all four documents for a single quality and clarity review.    | `route(task_type="documentation")`                                            |
| 6    | Build (no-op), test (no-op), and run cross-provider verification.    | `route(task_type="session-verification")`                                     |

#### Actuals (filled after the session)

*(empty)*

---
**Next-session orchestrator recommendation (Session 5)**: `Anthropic Opus 4.7 @ effort=high`

---

## Session 5: End-To-End Reference Scaffold + Closeout

-   **Recommended Orchestrator**: `Anthropic Opus 4.7 @ effort=high`
-   **Rationale**: This final session requires high-fidelity synthesis to integrate the new templates into the main application, which involves precise, context-aware code modifications. Opus is justified for this high-stakes integration task and for authoring the forward-looking strategic recommendation for the next session set.
-   **Estimated Routed Cost**: Moderate

| Step | Action                                                                 | Routing Decision                                                                     |
| :--- | :--------------------------------------------------------------------- | :----------------------------------------------------------------------------------- |
| 1    | Pre-stage `Supplier` entity, DTO, and schema file in `AcmeCoffeeOnline`. | `direct`                                                                             |
| 2    | Run templates to scaffold `Supplier` service and page.                 | `direct`                                                                             |
| 3    | Hand-edit the four documented integration points.                      | `direct`                                                                             |
| 4    | Run full build, unit tests, and Playwright E2E test suite.             | `direct`                                                                             |
| 5    | Author per-session `change-log.md`.                                    | `route(task_type="summarization", max_tier=1)`                                       |
| 6    | Author next-session-set recommendation.                                | `route(task_type="analysis", complexity_hint=60)`                                    |
| 7    | Run final cross-provider verification.                                 | `route(task_type="session-verification")`                                            |
| 8    | Commit, push, mark complete.                                           | `direct`                                                                             |

#### Actuals (filled after the session)

*(empty)*

---

## Delegation Discipline Summary

The orchestrator selection strategy for this set balances cost and capability against the specific work mix of each session.

-   **Session 1** uses Sonnet as a cost-effective orchestrator, delegating complex analysis while directly handling moderately difficult C# implementation.
-   **Sessions 2 & 3** leverage Sonnet for its strong price/performance on direct authoring of structured C# and Razor template code, minimizing delegation.
-   **Session 4** opts for the cheapest model, Haiku, for mechanical document drafting, relying on a routed specialist review to ensure quality.
-   **Session 5** justifies the use of the premium model, Opus, for the high-stakes synthesis required for final application integration and strategic forward-planning.