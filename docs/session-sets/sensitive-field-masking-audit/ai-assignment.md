```markdown
---
title: AI Assignment Plan
project: sensitive-field-masking-audit
---

### Session Plan

This document outlines the planned AI orchestration for the "sensitive-field-masking-audit" feature set.

```yaml
sessions:
  - id: 1
    title: "Field mask rendering service"
    description: "Adds IFieldMaskService, FieldMaskService, Customer.SocialSecurityNumber, unit tests, bUnit tests, wires RestrictedFormField.razor (1 inject + markup change + 1 new param)."
    effort: high
    actuals:
      orchestrator: claude-sonnet-4-6
      reasoning: "Initial session used a high-capability model to establish patterns. Analysis shows this work was voluminous but not complex; a cheaper model would have been sufficient."

  - id: 2
    title: "AppSensitiveAccessLog schema + RoleSwitch and exemption-lifecycle audit"
    description: "New EF entity, temporal table, ISensitiveAccessAuditor, AuditingRoleSwitcher decorator, exemption hooks, integration tests."
    effort: high
    assignment:
      orchestrator: claude-3-sonnet-20240229
      reasoning: "High complexity. Requires understanding of the Decorator pattern, cross-cutting concerns, and specific EF Core features (temporal tables). A mid-tier model is necessary to correctly implement the architecture and challenging integration tests."

  - id: 3
    title: "FieldUnmaskedRead interception with per-render CorrelationId batching"
    description: "New scoped service, binder extension, component wiring, integration tests."
    effort: high
    assignment:
      orchestrator: claude-3-opus-20240229
      reasoning: "Highest complexity. Requires deep framework knowledge to implement interception and extend the model binder. State management for per-render batching is a non-trivial problem. A top-tier model is required to ensure a correct and performant solution."

  - id: 4
    title: "ComplianceReview viewer + CSV export"
    description: "MudDataGrid viewer, FilterPanel, ICsvExportService, row-expander, bUnit + Playwright."
    effort: medium
    assignment:
      orchestrator: claude-3-haiku-20240307
      reasoning: "Medium complexity, primarily standard UI and API work. Tasks are well-defined (build a grid, create a CSV export). A cost-effective model is sufficient for generating boilerplate UI code, component tests, and procedural E2E tests against a known API."

  - id: 5
    title: "UAT checklist, Playwright coverage, change log"
    description: "UAT checklist + 1:1 Playwright coverage + change log + Step 9 reorg."
    effort: low
    assignment:
      orchestrator: claude-3-haiku-20240307
      reasoning: "Low complexity. Tasks involve documentation, summarization, and repetitive test generation based on established patterns. The cheapest capable model is the most efficient choice."
```

---

## Session 1 Actuals

- Orchestrator used: claude-code claude-sonnet-4-6 @ effort=high
- ai-assignment.md routed cost: $0.0075 (gemini-pro)
- Session verification cost: $0.5027 (opus / VERIFIED 1 Minor 2 Nitpicks)
- Next-session rec cost: $0.0041 (gemini-pro)
- Deviations: No EF migration created (AcmeCoffeeDbContext uses EnsureCreated); used DatabaseInitializer.EnsureAcmeCoffeeSchemaAsync instead

**Next-session recommendation:**

### Recommendation

*   **Orchestrator:** `claude-3-opus-20240229`
*   **Effort:** `critical`

---

### Reasoning

1.  **Increased Complexity & Architectural Scope:** Session 2 introduces multiple interconnected components across different layers of the application (database schema, entity framework, DI container, service logic, testing). This is a significant step up from the "mostly mechanical single-file edits" of Session 1. Tasks like configuring EF temporal tables, implementing a DI decorator, and writing stateful integration tests require a deep understanding of system architecture, which is a core strength of top-tier models like Opus.

2.  **High Cost of Error:** A mistake in the EF migration, temporal table configuration, or auditing logic could have severe consequences for data integrity and auditability. The `critical` effort rating is justified by the high impact of potential failures. Using a more powerful orchestrator is a risk mitigation strategy, as it is more likely to generate a correct, holistic solution that accounts for the interactions between the new components.

3.  **Cross-Cutting Concerns:** The work involves a cross-cutting concern (auditing) implemented via a decorator pattern. This requires precise code generation for not only the decorator itself but also the DI registration. A less capable model might fail to connect these pieces correctly, leading to a non-functional implementation that is difficult to debug.

4.  **Integration Testing Nuance:** Writing effective integration tests for this scenario is non-trivial. It involves setting up the database, triggering multiple distinct business events (role switch, exemption grant, exemption expiry), and asserting against the state of a temporal table over time. This complex sequence of operations benefits from the advanced reasoning and planning capabilities of a model like Opus.

---

## Session 2 Actuals

- Orchestrator used: claude-code claude-opus-4-7 @ effort=high
- Total routed cost (this session): $1.0420
  - session-verification: $1.0377 (opus, VERIFIED, 2 Minor + 1 Nitpick — 2 Minors fixed inline, 1 Nitpick deferred)
  - next-session analysis: $0.0043 (gemini-pro)
- Deviations from recommendation: None — Opus 4.7 at high effort matched the prior session's "critical effort" recommendation
- Notable: EF tooling required temporal period columns to be shadow properties; initial entity declared them as real properties and the migration generation failed clearly. Removed and re-ran successfully. Migration shape matches spec (history schema + indexes correct).

**Next-session recommendation (Session 3):**

### Recommendation

```yaml
orchestrator:
  engine: claude-code
  model: claude-opus-4-7
  effort: high
```

### Reasoning

-   **Blazor Lifecycle Nuance:** The core task involves a Blazor scoped service (`IRenderCorrelationScope`) tied to the circuit start and navigation events, a state management pattern that benefits from a model with a deep understanding of the framework's lifecycle.
-   **Complex Integration Testing:** Session 3 requires non-trivial, state-dependent integration tests to verify the batching logic across renders and navigations, a task where this model proved highly effective in the prior session.
-   **Cross-Cutting Implementation:** The work spans and modifies multiple existing layers (components, service interfaces, DI), requiring the holistic context retention and reasoning capabilities of a top-tier model to ensure consistency.
-   **Proven Track Record:** The chosen orchestrator delivered high-quality, nearly-correct output for the comparable complexity of Session 2, indicating it is the lowest-risk, highest-probability-of-success option for this session.

### Estimated Routed Cost

**Moderate.** The session involves complex logic and stateful integration tests which typically incur higher token usage for both generation and verification compared to simpler CRUD or model-only tasks.

---

## Session 3: FieldUnmaskedRead interception with per-render `CorrelationId` batching

### Recommended orchestrator
claude-code claude-opus-4-7 @ effort=high

### Rationale
This session implements a new Blazor scoped service and integrates it into the existing security and component layers. This complex task, involving Blazor's DI lifecycle, event handling, and multi-layer modifications, benefits from a model with strong C# and Blazor expertise.

### Estimated routed cost
moderate

| Step | Action | Routing Decision |
| :--- | :--- | :--- |
| 1 | Read planning docs. | orchestrator (direct) |
| 2 | Implement `IRenderCorrelationScope` and `RenderCorrelationScope` as a Blazor scoped service, generating a new `Guid` on circuit start and regenerating on `NavigationManager.LocationChanged`. | orchestrator (direct) |
| 3 | Extend the binder to emit `FieldUnmaskedRead` rows with per-correlation batching, including a new `entityId` parameter on `ResolveAsync`. | orchestrator (direct) |
| 4 | Wire `RestrictedFormField` and `RestrictedGridColumn` to pass `EntityId` through to the binder. | orchestrator (direct) |
| 5 | Unit tests for `RenderCorrelationScope` (new Guid per scope, regeneration on navigation/reset). | orchestrator (direct) |
| 6 | Integration tests for FieldUnmaskedRead batching (dedupe per correlation, fresh row after scope regen, 2N rows on a grid render). | orchestrator (direct) |
| 7 | Build + test (`dotnet build` + `dotnet test`). | orchestrator (direct) |
| 8 | Cross-provider verification. | route(task_type="session-verification") |
| 9 | Commit + push + notify. | orchestrator (direct) |

### Actuals (filled after the session)
- Orchestrator used: claude-code claude-opus-4-7 @ effort=high
- Total routed cost: ~$1.31
  - session-verification round 1: $1.1126 (opus, ISSUES_FOUND, 1 moderate + 2 low)
  - session-verification round 2: $0.1920 (opus, VERIFIED)
  - analysis (Session 3 ai-assignment): $0.0063 (gemini-pro)
  - analysis (Session 4 next-session rec): $0.0043 (gemini-pro)
- Deviations from recommendation: None — Opus 4.7 at high effort matched the recommendation routed at the end of Session 2.
- Notable: Initial AcmeCoffeeOnline.Web.Tests had 5 failures because `RenderCorrelationScope`'s constructor unconditionally subscribed to `NavigationManager.LocationChanged`, which throws `InvalidOperationException` when the manager is resolved inside a non-Blazor HTTP request scope (test harnesses, plain MVC endpoints, prerender-disabled flows). Fixed by guarding the subscribe in a `try/catch InvalidOperationException` and falling back to a non-navigation-aware scope; the API path's `ResetForRequest()` still drives request-boundary regeneration. Round-1 verifier flagged S3-01 (DI registration) as moderate but missed that the canonical `services.AddScoped<IRenderCorrelationScope, RenderCorrelationScope>()` was already in `Dabbler.Security/Extensions/ServiceCollectionExtensions.cs` (Session 2 artifact). Added a defensive `services.TryAddScoped<...>()` in `AddDabblerSecurityBlazor()` for Blazor-only hosts. S3-02 (move impl to Blazor project) deferred with rationale captured in the file's `<remarks>`. S3-03 (split into two files) fixed.

**Next-session recommendation (Session 4):**

### Recommendation

```yaml
orchestrator:
  engine: claude-code
  model: claude-opus-4-7
  effort: high
```

(Routed analysis suggested `claude-3-opus-20240229`; the current Anthropic-published model id is `claude-opus-4-7`, applied here verbatim per the model-id discipline in `CLAUDE.md`.)

### Reasoning

- The task's complexity is high, spanning a sophisticated Blazor UI (`MudDataGrid` + `ServerData` callback, cascading `FilterPanel<T>`, row-expander correlation cluster), a generic RFC 4180 CSV export service, JS interop for file downloads, and bUnit + Playwright reachability coverage.
- Comparable in complexity to Session 3, which Opus 4.7 + high effort handled cleanly across two verification rounds.
- The `ServerData` callback must translate filter state into performant EF `IQueryable` expressions across nullable composite columns (TargetRoleName, GranterUserId, EntityKey/ColumnKey, EventKind), which benefits from holistic context retention.
- bUnit + Playwright authoring against the existing dashboard tab gating, drawer, and `IJSRuntime` mocks is established pattern work — a top-tier model still earns its cost on the full-set authoring without needing iterative correction.

### Estimated Routed Cost

**Moderate.** UI-heavy session with bUnit + Playwright authoring; expect a comparable verifier round to Session 3 (~$1–1.5 verification + a low-cost summarization for the change log if rolled in).


## Session 4: Compliance Review viewer (6th dashboard tab) + CSV export

### Recommended orchestrator
claude-code claude-opus-4-7 @ effort=high

### Rationale
Session 4 builds a net-new feature spanning UI composition (`ComplianceReviewTab` + grid), `ServerData`-callback EF query against `AppSensitiveAccessLog`, generic RFC 4180 CSV export, JS-assisted download via the existing `dabbler-download.js` module, plus bUnit + Playwright coverage. Comparable cross-cutting complexity to Session 3, which Opus 4.7 + high effort handled cleanly. Routed analysis (gemini-pro, $0.0098) confirmed Opus + high; the routed analysis incorrectly said Step 9 verification could be direct — applied per the workflow's hard rule that `session-verification` is always cross-provider.

### Estimated routed cost
moderate

| Step | Action | Routing Decision |
| :--- | :--- | :--- |
| 1 | Read planning docs. | orchestrator (direct) |
| 2 | Add `ComplianceReviewTab.razor` to `DabblerSecurityDashboard.AllTabs` as 6th entry (`SystemAdmin`, `CrossOrgUserAdmin`, `CrossOrgMonitor`). | orchestrator (direct) |
| 3 | Build viewer: `FilterPanel<AppSensitiveAccessLog>` + `MudDataGrid<>` with `ServerData` paging + composite filters; row expander shows correlation cluster. | orchestrator (direct) |
| 4 | `ICsvExportService` + impl in `Dabbler.Components/Csv/`, RFC 4180 quoting, CRLF. | orchestrator (direct) |
| 5 | Export button calls existing `_content/Dabbler.Components/dabbler-download.js` `downloadFile` helper (reuse instead of duplicating per spec — see deviation note in Step 9 verification). | orchestrator (direct) |
| 6 | bUnit tests: filter apply, server-data path, expander cluster, CSV button via `JSRuntime.Loose` capture. | orchestrator (direct) |
| 7 | Playwright smoke: SystemAdmin reaches Compliance Review tab via drawer. | orchestrator (direct) |
| 8 | Build + test (`dotnet build` + `dotnet test`). | orchestrator (direct) |
| 9 | Cross-provider verification. | route(task_type="session-verification") |
| 10 | Commit + push + notify. | orchestrator (direct) |

### Actuals (filled after the session)
- Orchestrator used: claude-code claude-opus-4-7 @ effort=high
- Total routed cost: ~$1.36
  - analysis (Session 4 ai-assignment): $0.0098 (gemini-pro)
  - session-verification round 1: $0.8532 (opus, CONDITIONAL_PASS — 2 medium + 2 low)
  - session-verification round 2: $0.4963 (opus, VERIFIED — Round 1 issues all resolved or accepted-deferred)
  - analysis (Session 5 next-session rec): $0.0051 (gemini-pro)
- Deviations from recommendation: None — Opus 4.7 @ high matched the recommendation routed at the end of Session 3.
- Notable: Reused the existing `_content/Dabbler.Components/dabbler-download.js` module instead of authoring a new `downloadFile.js` (spec phrasing) — disclosed as a deviation. Round-1 verifier returned the non-standard verdict `CONDITIONAL_PASS` (schema only allows VERIFIED / ISSUES_FOUND), but flagged actionable MEDIUM and LOW issues; orchestrator treated them as ISSUES_FOUND, applied fixes (anchor-excluded correlation cluster + page-scope disclosure, two new bUnit tests for filter + expander, explicit `SortBy` on TemplateColumns), and re-ran verification to VERIFIED. The non-standard verdict label is a candidate for the Session 5 Step 9 reorganization review.

**Next-session recommendation (Session 5):**

### Recommendation

```yaml
orchestrator:
  engine: claude-code
  model: claude-sonnet-4-6
  effort: medium
```

(Routed analysis suggested `Sonnet 3.5 @ medium`; the current Anthropic-published Sonnet model id is `claude-sonnet-4-6`, applied here verbatim per the model-id discipline in `CLAUDE.md`.)

### Reasoning

- Session 5 is a closeout: most reasoning happens inside routed calls (`uat-plan-generation` → Opus; `uat-coverage-review` → Sonnet via mechanical-gate calibration; `summarization` → Gemini Flash via `max_tier=1`). The orchestrator's only novel synthesis is translating the generated checklist's structured Steps into matching Playwright test bodies — well-defined, spec-driven code generation that Sonnet handles cleanly.
- Aligns with the "Mechanical-Gate Task Types Calibrate To Sonnet-Cost For Adequate Quality" lesson — running an Opus orchestrator over what is mostly closeout finalization pays for capability that is not exercised.
- The high-leverage cognitive cost lives in the routed `uat-plan-generation` call regardless of orchestrator choice; downgrading the orchestrator does not affect that.
- Risk: the Step 9 reorganization review at end-of-set is the only step that benefits from architectural reasoning, but the candidate reorganizations are mechanical (Lesson → Convention promotion, etc.) and follow the workflow's well-defined procedure.

### Estimated Routed Cost

**Moderate.** The session's biggest costs are routed regardless of orchestrator: `uat-plan-generation` (~$1–2 Opus) and Docker Playwright run (no API cost but build/test time). Verification round expected to be one round (VERIFIED) given closeout scope.

