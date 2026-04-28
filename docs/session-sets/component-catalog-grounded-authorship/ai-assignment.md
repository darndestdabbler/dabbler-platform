# AI Assignment — component-catalog-grounded-authorship

> **Source:** Recommendations below were produced via
> `route(task_type="analysis")` per `docs/ai-led-session-workflow.md`
> Step 3.5. The orchestrator does not self-opine on which model could
> have been cheaper. Engine names and model identifiers below are
> normalized to the entries in `ai-router/router-config.yaml`.

---

## Session 1: Crawler scaffold, catalog schema, first manifest

### Recommended orchestrator
claude-code (anthropic) `claude-sonnet-4-6` @ effort=medium

### Rationale
Mostly mechanical .NET console-app authoring with one auto-verified
code-review on the accessibility-tree scraper and a mandatory
cross-provider session verification. A mid-tier orchestrator is
sufficient; the routed code-review and session-verification calls
carry the deeper-reasoning load and are independent of the
orchestrator model.

### Estimated routed cost
moderate

| Step | Action | Routing Decision |
|------|--------|------------------|
| 0 | Author this `ai-assignment.md` | ROUTE `task_type=analysis` |
| 1 | Read planning docs | direct |
| 2 | Verify W0 prerequisite (build sibling repo, inspect schema) | direct |
| 3 | Scaffold `tools/ComponentCatalogCrawler/` csproj + add to sln | direct |
| 4 | Implement `RouteEnumerator.cs` (scan `@page` directives, resolve params via API) | direct (mechanical, ~80 lines) |
| 5 | Implement `SeededSessionAuth.cs` (port `PlaywrightFixture.SeededSignInAsync`) | direct (~40 lines) |
| 6 | Implement `AccessibilityTreeScraper.cs` (snapshot recursion + ARIA→elementRole mapping) | direct write, then ROUTE `task_type=code-review` (auto-verified cross-provider) |
| 7 | Implement `CrawlerOrchestrator.cs` (per-role × per-route loop, write per-role JSON + merged index.json) | direct |
| 8 | Boot compose stack and run crawler | direct |
| 9 | Validate manifest by loading via `JsonCatalogAdapter` (CLI fallback if no `validate-catalog` subcommand) | direct |
| 10 | Spot-check: `/customers` (BackofficeUser) shows "Add Customer"; `/admin/security` (SystemAdmin) shows tabs; `User` is a restricted set | direct |
| 11 | End-of-session verification | MUST ROUTE `task_type=session-verification` (always cross-provider) |
| 12 | Cost report, mark complete, commit, push, notify | direct |

### Actuals (filled after the session)
- Orchestrator used: claude-code anthropic `claude-opus-4-7` (1M context) @ effort=high
- Total routed cost: $0.1744 across 3 calls
  - `gemini-pro` analysis (this ai-assignment.md authoring): $0.0086
  - `gpt-5-4` session-verification round 1: $0.0660 (ISSUES_FOUND, 2 Major)
  - `gpt-5-4` session-verification round 2: $0.0998 (VERIFIED)
- Deviations from recommendation:
  - Orchestrator was opus rather than the recommended sonnet — human
    invoked default model (opus) at session start. Routed reasoning
    steps still went to the cheapest capable model via `route()`, so
    the deviation affects orchestrator-side reasoning cost only, not
    routed-call cost.
  - **Session-verification routed to OpenAI `gpt-5.4` instead of the
    default Anthropic `opus`.** The Anthropic API account hit a zero
    credit balance mid-session (HTTP 400 "credit balance is too low"
    on every Anthropic call). Per the workflow's verifier-failure
    escalation ladder, the verification was routed to the
    cross-provider tier-3 model (gpt-5.4) directly, preserving the
    cross-provider session-verification intent. The router's normal
    `task_type_overrides` plus tier mapping picks opus by default;
    forcing `gpt-5.4` required a `call_model` invocation rather than
    `route()`, so the metric record was written manually with
    `note: "Anthropic credit balance exhausted; routed to OpenAI
    GPT-5.4 directly to preserve cross-provider session-verification
    intent."`. Once credits are restored, normal routing resumes
    automatically.
- Notes for next-session calibration:
  - The Playwright `IAccessibility.SnapshotAsync` API is marked obsolete
    in 1.53+ and additionally trips the stdio transport's
    `System.Text.Json` 64-deep parse limit on the `/admin/security`
    MudBlazor tree. The crawler uses an in-page `EvaluateAsync` DOM
    walk instead. If a future session reaches for `Accessibility.*`
    APIs in this repo, prefer the DOM-walk pattern.
  - Round-1 verifier (gpt-5.4) found two Major issues that round-2
    confirmed fixed: (1) `RouteEnumerator` template paths now strip
    .NET constraint suffixes; (2) `AccessibilityTreeScraper` now
    emits named landmark roles as `Element` per the W0 schema's
    catchall.

**Next-session orchestrator recommendation (Session 2):**

| Anthropic credits state | Recommended orchestrator | Rationale |
|---|---|---|
| Restored | claude-code (anthropic) `claude-sonnet-4-6` @ effort=medium | Session 2's heavy lifting (prompt authoring, checklist re-authoring, change-log generation) all goes through `route()`; the orchestrator only dispatches + iterates, which sonnet handles cheaply. |
| Still out | codex (openai) `gpt-5.4` @ effort=medium **or** gemini-cli `gemini-2.5-pro` @ effort=medium | Either gives a non-Anthropic orchestrator while session-verification routes cross-provider to whichever Anthropic model is available; if Anthropic stays out the verification fallback to gpt-5.4 (with gemini-pro backup) continues. |

**Per-step routing for Session 2 (identical for both states):**

| Step | Action | Routing Decision |
|------|--------|------------------|
| 1 | Read planning | direct |
| 2 | Edit `ai-router/router-config.yaml` (add task type) | direct |
| 3 | Author `ai-router/prompts/uat_script_author.md` | direct |
| 4 | Re-author remediation checklist | ROUTE `task_type=uat-script-author` |
| 5 | Validate via `dabbler-uat-dsl validate`; iterate | direct (CLI) + re-route on failure |
| 6 | Manual compare against original | direct |
| 7 | Save final checklist + backfill suppressions | direct |
| 8 | Update `docs/planning/project-guidance.md` + `docs/ai-led-session-workflow.md` | direct |
| 9 | Generate `change-log.md` | ROUTE `task_type=summarization` (`max_tier=1` if Gemini Pro 400s) |
| 10 | `dotnet build` + `dotnet test` | direct |
| 11 | End-of-session verification | MUST ROUTE `task_type=session-verification` (cross-provider) |
| 12 | Commit, push, mark complete, notify | direct |
| 13 | Step 9 reorg review (last session) | ROUTE `task_type=analysis` for candidate identification |

---

## Session 2: ai-router task type, re-author remediation checklist, change log, Step 9 reorg

### Recommended orchestrator
claude-code (anthropic) `claude-sonnet-4-6` @ effort=medium

### Rationale
Session 2 is dominated by routed reasoning (prompt template authoring,
checklist re-authoring via the new `uat-script-author` task type,
change-log summarization, Step 9 reorganization analysis) and a
validate-iterate loop against the W0 CLI. The orchestrator's role is
dispatching, parsing CLI errors, and shepherding the iteration —
mid-tier is sufficient.

### Estimated routed cost
moderate-to-high (driven by the Sonnet-backed `uat-script-author`
calls and the iterate-on-validation-failure loop)

| Step | Action | Routing Decision |
|------|--------|------------------|
| 1 | Read planning | direct |
| 2 | Add `uat-script-author` task type to `ai-router/router-config.yaml` | direct |
| 3 | Author `ai-router/prompts/uat_script_author.md` | direct (mechanical prompt-template authoring; the runtime use is what gets routed) |
| 4 | Re-author remediation checklist | ROUTE `task_type=uat-script-author` |
| 5 | Validate via `dabbler-uat-dsl validate`; iterate (re-route Step 4 on failure) | direct (CLI invoke + parse), routed re-author on failure |
| 6 | Manual compare against original checklist | direct (mechanical diff review) |
| 7 | Save final checklist | direct |
| 8 | Backfill `tools/uat-suppressions.json` for grandfathered closed-set checklists | direct |
| 9 | Update `docs/planning/project-guidance.md` (catalog-grounded authoring convention + workflow note) | direct |
| 10 | Update `docs/ai-led-session-workflow.md` Step 5 (UAT generation conditional) | direct |
| 11 | Generate `change-log.md` | ROUTE `task_type=summarization` (per project-guidance: prefer summarization over documentation for prose) |
| 12 | `dotnet build` + `dotnet test` | direct |
| 13 | End-of-session verification | MUST ROUTE `task_type=session-verification` (always cross-provider) |
| 14 | Commit, push, mark complete, notify | direct |
| 15 | Step 9 reorg review (last session of set, post-notify) | ROUTE `task_type=analysis` for candidate identification; direct for human-loop adjudication |

### Actuals (filled after the session)
- Orchestrator used: TBD
- Total routed cost: TBD
- Deviations from recommendation: TBD
- Notes for next-session calibration: TBD

**Next-session-set recommendation (post-Session 2):**
TBD — author at end of Session 2 via routed analysis per workflow Step 8 (last-session branch). Per the W1→W2→W3 pipeline memo, the planned next set is `naive-uat-runner-with-feedback` (W2). Confirm or surface a deviation at Session 2 close.
