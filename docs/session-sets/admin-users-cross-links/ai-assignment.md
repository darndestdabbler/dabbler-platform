```markdown
## Session 1: Implement all 5 components + UAT checklist

### Recommended orchestrator
claude-3-opus-20240229 @ effort=medium

### Rationale
The task involves multi-file changes to Blazor components, requiring careful C# logic for authorization gating and parameterization. A high-capability model is recommended to correctly implement the component interactions and authorization logic in a single pass.

### Estimated routed cost
moderate

| Step | Action | Routing Decision |
|------|--------|------------------|
| 1    | Flush out spec (done) | Direct |
| 2    | Read guidance (done) | Direct |
| 3    | Route `ai-assignment.md` analysis | route(task_type="analysis") |
| 4    | **Component 1:** Edit `UserManagement.razor` (rename) | route(task_type="code-gen") |
| 5    | **Component 2:** Edit `UserRoleAssignmentDetails.razor` (param, filter) | route(task_type="code-gen") |
| 6    | **Component 3:** Add cross-link to `UserRoleAssignmentDetails.razor` | [Bundled with step 5] |
| 7    | **Component 4:** Add banner to `UserManagement.razor` | [Bundled with step 4] |
| 8    | **Component 5:** Apply gating (inline) | [Bundled with steps 4-7] |
| 9    | Route code review | route(task_type="code-review") |
| 10   | Author UAT checklist | route(task_type="uat-script-author") |
| 11   | Compile UAT checklist (W0) | Direct |
| 12   | Run UAT (W2) | Direct |
| 13   | Run `dotnet build && dotnet test` | Direct |
| 14   | Cross-provider session verification | route(task_type="session-verification") |
| 15   | Handle verification result | Direct |
| 16   | Generate `change-log.md` | route(task_type="summarization") |
| 17   | Cost report, commit, push, notify | Direct |
| 18   | Step 9 reorganization review | Direct |

### Actuals (filled after the session)
- Orchestrator used:
- Total routed cost:
- Deviations from recommendation:
- Notes for next-session calibration:

**Next-session orchestrator recommendation:** N/A (last session)
```