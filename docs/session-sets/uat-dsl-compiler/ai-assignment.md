# ai-assignment.md — uat-dsl-compiler

> Per the workflow's mandatory rule, the orchestrator never self-opines on
> model choice; this file is authored via route(task_type="analysis") on
> Session 1 and appended on subsequent sessions with actuals + the next
> recommendation.

## Session 1: Repo bootstrap, ANTLR grammar v1, grammar tests

### Recommended orchestrator
Claude Code @ effort=high

### Rationale
The ANTLR grammar is a core architectural artifact requiring frontier reasoning for its design. The subsequent large test suite generation also benefits from a highly capable model to ensure comprehensive coverage.

### Estimated routed cost
High

| Step | Action | Routing Decision |
|------|--------|------------------|
| 1 | Read planning docs | Direct (mechanical reads) |
| 2 | Repo init + skeleton files | Direct (mechanical) |
| 3 | dotnet new solution + projects | Direct (CLI commands) |
| 4 | Add ANTLR NuGet refs | Direct (csproj edits) |
| 5 | Author DabblerUatDsl.g4 grammar | route(task_type="architecture") |
| 6 | Configure Antlr4BuildTasks | Direct (csproj) |
| 7 | Build verification | Direct (dotnet build) |
| 8 | Author ~60-80 grammar tests | route(task_type="test-generation") |
| 9 | dotnet test | Direct |
| 10 | Cross-provider verification | route(task_type="session-verification") |
| 11 | Commits | Direct |

### Actuals (filled after the session)
- Orchestrator used: claude-code (Claude Opus 4.7 1M, effort=high) — matched recommendation
- Total routed cost: $2.717 (architecture grammar $1.535 + session-verification $1.093 + test-generation $0.069 + analysis $0.020)
- Deviations from recommendation: none on engine. Mid-session router-config fix required: `architecture` task had `effort: xhigh` for Opus, which the Anthropic API rejects (valid: `low|medium|high|max`). Corrected in `ai-router/router-config.yaml` to `effort: max` so all future architecture-routed calls work.
- Notes for next-session calibration:
  - Architecture-routed grammar authoring used Opus at `max` effort, returning ~20K output tokens for $1.54 — appropriate for a 240-line design artifact, but the single biggest cost line. Reusable header/verb-list content could be cached if subsequent architecture calls revisit the same scope.
  - Test-generation routed to Gemini Pro at $0.069 for 85 passing tests — strong cost/quality on this kind of structured generation.
  - Verification routed to Opus (Anthropic) after GPT-5.4 (the preferred OpenAI verifier) returned 429. Same-provider as the Claude orchestrator; the router does not currently filter verifier candidates by orchestrator provider for `session-verification` itself. Worth a follow-up router-config change so the verifier excludes the orchestrator's provider.
  - Verification prompt was hand-rolled and did not include the structured JSON response schema from `ai-router/prompt-templates/verification.md`; the verifier returned markdown rather than the JSON block. Future verification calls should reuse the template (or include the JSON schema verbatim) so the verdict can be parsed mechanically.

**Next-session orchestrator recommendation (Session 2):**
codex GPT-5.4 @ effort=medium
Rationale: Session 2's task is primarily mechanical C# implementation (~22 visitor methods, ~22 record types, JSON facade), making the more cost-effective Codex orchestrator a better fit than the design-focused Opus used in Session 1.

---
## Session 2: AST node types, visitor, parser facade

### Recommended orchestrator
Codex / GPT-5.4 @ effort=medium

### Rationale
This session involves structured, multi-file C# implementation following a standard Visitor pattern. A cost-effective orchestrator strong in mechanical code generation is sufficient.

### Estimated routed cost
Moderate

| Step | Action | Routing Decision |
|------|--------|------------------|
| 1 | Author AST record types | Direct (repetitive C#) |
| 2 | Implement UatDslVisitor | Direct (pattern-based implementation) |
| 3 | Implement UatChecklistParser facade | Direct (facade pattern) |
| 4 | Author AST round-trip tests | route(task_type="test-generation") |
| 5 | Cross-provider verification | route(task_type="session-verification") |
| 6 | Commits | Direct |

### Actuals (filled after the session)
- Orchestrator used: Codex / GPT-5 (OpenAI), effort=medium — matched recommendation
- Total routed cost: $1.285553 (test-generation $0.063488 + session-verification $1.222065)
- Deviations from recommendation: none
- Notes for next-session calibration:
  - The implementation was mechanical enough for Codex, confirming the recommendation. Verification remained the dominant cost line.

**Next-session orchestrator recommendation (Session 3):** Claude Code @ effort=high
Rationale: The semantic validator requires complex reasoning, state tracking while walking an AST, and implementing fuzzy-matching logic. This warrants the most capable orchestrator for correctness and quality.

---
## Session 3: Catalog adapter, semantic validation, schema

### Recommended orchestrator
Claude Code @ effort=high

### Rationale
The semantic validator requires complex reasoning, state tracking while walking an AST, and implementing fuzzy-matching logic. This warrants the most capable orchestrator for correctness and quality.

### Estimated routed cost
High

| Step | Action | Routing Decision |
|------|--------|------------------|
| 1 | Author component-catalog.json schema | route(task_type="architecture") |
| 2 | Implement ICatalogAdapter & JsonCatalogAdapter | Direct (IO/deserialization) |
| 3 | Implement SemanticValidator | Direct (complex implementation) |
| 4 | Author validator tests with mock catalog | route(task_type="test-generation") |
| 5 | Cross-provider verification | route(task_type="session-verification") |
| 6 | Commits | Direct |

### Actuals (filled after the session)
- Orchestrator used: claude-code (Claude Opus 4.7 1M, effort=high) — matched recommendation
- Total routed cost: $1.5630 (architecture $0.2386 + test-generation $0.0358 + session-verification round 1 $0.7434 + session-verification round 2 $0.5452)
- Deviations from recommendation: none on engine. Verification needed two rounds: round 1 returned ISSUES_FOUND with two Minor items (dead `labelIndex` in `JsonCatalogAdapter.BuildIndex`; `SemanticValidator` referenced concrete `JsonCatalogAdapter.AnyRole`). Both fixed mechanically — labelIndex removed, `CatalogRoles.AnyRole` const introduced in `ICatalogAdapter.cs` and referenced from both adapter and validator. Round 2 returned VERIFIED with no new issues.
- Notes for next-session calibration:
  - Session-verification routed to Opus (Anthropic) on both rounds, even though the orchestrator was also Anthropic (Claude). The lesson "Cross-Provider Verification Must Exclude The Orchestrator's Provider, Not Just The Generator's" (lessons-learned.md ~L1513) is not yet enforced when GPT-5.4 (the cross-provider preference) is unavailable. Carry forward as a router-config follow-up: when OpenAI is the only different-provider option and is rate-limited, the router should escalate-and-retry rather than fall back to the orchestrator's own provider, OR document an accepted exception. For Session 4 with Codex (OpenAI) as orchestrator, the verifier should land on Anthropic naturally and avoid this issue.
  - Test-generation output came back wrapped in ```csharp ... ``` fences and used a non-existent `.Steps` accessor on `UatChecklistParser.Parse`. Both fixed mechanically before build. The "no markdown fences" wording in the test-generation prompt template is worth tightening, and the prompt should pass the parser facade signature rather than relying on the generator's prior knowledge.
  - Architecture-routed schema generation returned a minified single-line JSON document; pretty-printed in-place after writing. Future architecture calls that produce JSON should ask for indented output explicitly.
  - The two Minor verification fixes were code-level mechanical edits; both rounds of session-verification together cost more than the entire implementation, which is the expected shape for a one-or-two-Minor-issues pass.

**Next-session orchestrator recommendation (Session 4):**
codex GPT-5 @ effort=medium
Rationale: The session's most complex work (PlaywrightCSharpEmitter implementation) is a direct, mechanical mapping from AST nodes to Playwright C# call shapes — well-suited to Codex's C# strength. Routing the verification to a non-OpenAI provider (e.g. Anthropic Opus) keeps the cross-provider check honest. Routed analysis (gemini-pro, $0.0023) returned the same engine + effort recommendation; model-id substituted to GPT-5 to match this repo's Codex orchestrator target.

---
## Session 4: Playwright C# emitter

### Recommended orchestrator
Codex GPT-5 @ effort=medium  *(superseded the Session-1 placeholder of "Claude Code @ effort=high"; updated end of Session 3 via routed analysis)*

### Rationale
Per Session-3-end routed analysis: the emitter implementation is a mechanical AST-to-Playwright-call mapping that fits Codex's C# strength; the routed `architecture` call for the fixture interface and the routed `test-generation` call for emitter tests both use cheaper tier-2 models, so a frontier orchestrator is overkill. Cross-provider verification will land on an Anthropic model naturally, avoiding the verifier-on-same-provider snag observed in Session 3.

### Estimated routed cost
High

| Step | Action | Routing Decision |
|------|--------|------------------|
| 1 | Create Playwright.CSharp emitter project | Direct (CLI + csproj edits) |
| 2 | Define emitter/fixture interfaces | route(task_type="architecture") |
| 3 | Implement PlaywrightCSharpEmitter | Direct (complex code generation) |
| 4 | Author emitter tests (incl. Roslyn gate) | route(task_type="test-generation") |
| 5 | Cross-provider verification | route(task_type="session-verification") |
| 6 | Commits | Direct |

### Actuals (filled after the session)
- Orchestrator used: <pending>
- Total routed cost: <pending>
- Deviations from recommendation: <pending>
- Notes for next-session calibration: <pending>

**Next-session orchestrator recommendation (Session 5): authored at end of Session 4.**

---
## Session 5: CLI, packaging, dabbler-platform integration, change log, Step 9 reorg

### Recommended orchestrator
Gemini CLI @ effort=low

### Rationale
This session is primarily mechanical tasks like setting up CLI boilerplate, editing package metadata, and updating documentation. The cheapest orchestrator is sufficient for these read-heavy and command-oriented steps.

### Estimated routed cost
Low

| Step | Action | Routing Decision |
|------|--------|------------------|
| 1 | Create CLI project, add System.CommandLine | Direct (boilerplate) |
| 2 | Add NuGet packaging metadata to all projects | Direct (mechanical csproj edits) |
| 3 | Smoke test CLI end-to-end | Direct (running commands) |
| 4 | Update dabbler-platform docs | route(task_type="documentation") |
| 5 | Generate change-log.md | route(task_type="summarization") |
| 6 | Cross-provider verification (both repos) | route(task_type="session-verification") |
| 7 | Commit both repos | Direct |
| 8 | Final notifications and reorg review | Direct (workflow steps) |

### Actuals (filled after the session)
- Orchestrator used: <pending>
- Total routed cost: <pending>
- Deviations from recommendation: <pending>
- Notes for next-session calibration: <pending>
