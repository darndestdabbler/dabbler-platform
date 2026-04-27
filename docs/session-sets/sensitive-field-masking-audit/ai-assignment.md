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