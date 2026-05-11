```markdown
# ai-assignment.md — admin-user-creation-flow

> **Session set:** `docs/session-sets/admin-user-creation-flow/`
> **Created:** 2026-05-11 (Session 1)
> **Spec sizing:** opus-class, 3 sessions
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Session 1 of 3 — Design + spec fleshing-out

| Field | Value |
|---|---|
| **Recommended orchestrator** | Claude Opus 4.7 (1 M context) |
| **Effort** | `high` |
| **Qualitative cost** | **Moderate** |

**Rationale:** Session 1 is pure design: reading the prerequisite close-out state, resolving seven interlocking design questions (form layout, API surface, email collision, magic-link sync, audit event kind, library scope, self-service parity), and authoring the full spec with per-session step lists, API contracts, and risk notes. Opus-class reasoning at high effort is required to hold the entire authorization model, the existing `RoleAssignmentAuthority` contract, and the magic-link race condition in context simultaneously and produce a coherent, internally-consistent spec.

### Step-routing table

| # | Step | Route | Target | Notes |
|---|---|---|---|---|
| 1 | Read project-guidance, lessons-learned, session-set-authoring-guide | `orchestrator` | — | Context loading; no delegation needed. |
| 2 | Register session start | `orchestrator` | — | Write `activity-log.json` entry. |
| 3 | Read stub spec + prerequisite close-out state | `orchestrator` | — | `admin-users-cross-links` disposition + UAT results. |
| 4 | Walk human through design questions | `orchestrator` | — | Interactive; 7 decisions per the design-decisions table. |
| 5 | Write fleshed-out spec (`spec.md`) | `orchestrator` | — | Includes per-session steps, API contract, form sketch, audit plan, risk notes. |
| 6 | Route `ai-assignment.md` analysis | `route(task_type="analysis")` | AI Router → cross-provider | Orchestrator never self-opines on its own assignment doc. |
| 7 | Route cross-provider verification of design | `route(task_type="cross-provider-verification")` | AI Router → verification provider | Verify spec coherence: transaction boundaries, `IsProvisioned` race guard, `RoleAssignmentAuthority` dual-check pattern, audit payload shape. |
| 8 | Author `disposition.json` | `orchestrator` | — | Status, progress keys, blockers. |
| 9 | Commit, push, mark complete, notify | `orchestrator` | — | End-of-session mechanics. |

### Actuals

| Field | Value |
|---|---|
| Orchestrator used | *(fill after session)* |
| Effort used | *(fill after session)* |
| Wall-clock duration | *(fill after session)* |
| Token consumption | *(fill after session)* |
| Deviation from plan | *(fill after session)* |

---

## Session 2 of 3 — API + service layer + unit tests

| Field | Value |
|---|---|
| **Recommended orchestrator** | Claude Sonnet 4.6 (200 K context) |
| **Effort** | `high` |
| **Qualitative cost** | **Low** |

**Rationale:** Session 2 is implementation-heavy: a schema migration, a new service class with transactional EF logic, an API controller, an API client method, and five unit-test cases. The work is well-specified by Session 1's output and follows established codebase patterns (`EnsureAcmeCoffeeSchemaAsync`, `RoleAssignmentAuthority.Evaluate`, `AdminUserApiClient`). Sonnet at high effort handles pattern-following code generation and unit-test scaffolding efficiently without the cost premium of Opus; the design decisions are already locked.

### Step-routing table

| # | Step | Route | Target | Notes |
|---|---|---|---|---|
| 1 | Add `IsProvisioned` column + backfill | `route(task_type="code-gen")` | Sonnet 4.6 | Schema pattern: `ALTER TABLE ADD` + `UPDATE … SET IsProvisioned=true WHERE Email IN (…)` in `EnsureAcmeCoffeeSchemaAsync`. |
| 2 | Add `SensitiveAccessEventKind.UserCreated` | `route(task_type="code-gen")` | Sonnet 4.6 | Single enum addition; trivial but must match existing naming convention. |
| 3 | Implement `BackofficeUserCreationService` | `route(task_type="code-gen")` | Sonnet 4.6 | Core service: validation → transaction (AppUser + membership + role + audit + IsProvisioned flip) → return projection. Heaviest step. |
| 4 | Update `IMagicLinkService.IssueAsync` | `route(task_type="code-gen")` | Sonnet 4.6 | Add `IsProvisioned` guard with domain-specific exception. |
| 5 | Add `POST /api/admin/users` endpoint | `route(task_type="code-gen")` | Sonnet 4.6 | `AdminUsersController` with `[Authorize]` + policy gate; delegates to service. |
| 6 | Add `CreateBackofficeUserAsync` to `AdminUserApiClient` | `route(task_type="code-gen")` | Sonnet 4.6 | HTTP POST + deserialization; follows existing client methods. |
| 7 | Unit tests for `BackofficeUserCreationService` | `route(task_type="test-gen")` | Sonnet 4.6 | 5 cases: happy path, UserAdmin cross-org rejection, CrossOrgMonitor denial, duplicate email, transaction rollback on authority failure. |
| 8 | Build + test | `orchestrator` | — | `dotnet build` + `dotnet test` on affected projects. |
| 9 | Route cross-provider verification | `route(task_type="cross-provider-verification")` | AI Router → verification provider | Verify: transaction boundary correctness, `IsProvisioned` flag semantics, audit payload completeness, unit-test coverage of all rejection paths. |
| 10 | Commit, push, mark complete, notify | `orchestrator` | — | End-of-session mechanics. |

### Actuals

| Field | Value |
|---|---|
| Orchestrator used | *(fill after session)* |
| Effort used | *(fill after session)* |
| Wall-clock duration | *(fill after session)* |
| Token consumption | *(fill after session)* |
| Deviation from plan | *(fill after session)* |

---

## Session 3 of 3 — Razor surface + UAT checklist + W2 + close

| Field | Value |
|---|---|
| **Recommended orchestrator** | Claude Sonnet 4.6 (200 K context) |
| **Effort** | `high` |
| **Qualitative cost** | **Low** |

**Rationale:** Session 3 is UI assembly and test authoring: a `MudDialog`-based Razor component, bUnit tests, hand-rolled Playwright `[Fact]` tests, and a UAT checklist JSON — all following established patterns from `admin-users-cross-links` and `sensitive-field-masking`. Sonnet at high effort is well-suited to Blazor component generation with `AuthenticationStateProvider` injection, Playwright test scaffolding matching existing `AdminUsersCrossLinksUatTests.cs` conventions, and structured UAT-checklist JSON authoring. No novel design decisions remain.

### Step-routing table

| # | Step | Route | Target | Notes |
|---|---|---|---|---|
| 1 | Create `CreateBackofficeUserDialog.razor` | `route(task_type="code-gen")` | Sonnet 4.6 | `MudDialog` + `MudForm`; fields: DisplayName, Email, InitialRoleName (dropdown filtered by `RoleAssignmentAuthority`), InitialOrganizationId (dropdown filtered by granter context). Injects `AuthenticationStateProvider`. |
| 2 | Add "Add User" button to `UsersAndRoleAssignmentTab.razor` | `route(task_type="code-gen")` | Sonnet 4.6 | `MudButton` at top of tab; opens dialog via `IDialogService.ShowAsync`; on `OnCreated` callback refreshes grid. |
| 3 | Wire `AuthenticationStateProvider` flow through dialog | `route(task_type="code-gen")` | Sonnet 4.6 | Mirrors `UserRoleAssignmentDetails` actor-context resolution pattern. |
| 4 | bUnit tests for `CreateBackofficeUserDialog` | `route(task_type="test-gen")` | Sonnet 4.6 | 4 cases: empty render, submit-disabled-while-invalid, auth-filter-excludes-unassignable-roles, successful-submit-calls-client-and-raises-OnCreated. |
| 5 | Author UAT checklist JSON | `route(task_type="uat-script-author")` | AI Router → UAT author | 4 journeys: happy path, UserAdmin cross-org rejection, duplicate email, audit emission. Each starts from `/login`; none is VERIFY-only. |
| 6 | Author Playwright `[Fact]` tests | `route(task_type="test-gen")` | Sonnet 4.6 | 4 tests mirroring UAT-1 through UAT-4. Hand-rolled per `feedback_e2e_verify_before_uat.md`. |
| 7 | Run W0 compile | `orchestrator` | — | `dotnet build` full solution. Note known W0 runner conflict with DSL-driven login (document blocker if hit). |
| 8 | Run W2 runner | `orchestrator` | — | Best-effort; deterministic pass target. Document any blocker per the upstream W0 conflict. |
| 9 | Build + test | `orchestrator` | — | `dotnet build` + `dotnet test` on all affected projects including Playwright. |
| 10 | Route cross-provider verification | `route(task_type="cross-provider-verification")` | AI Router → verification provider | Verify: dialog authorization filtering matches service-side gate, bUnit test coverage, Playwright test isolation, UAT checklist completeness against spec's 4 journeys. |
| 11 | Author `change-log.md` | `orchestrator` | — | Full session-set change log. |
| 12 | Author final `disposition.json` | `orchestrator` | — | Status=complete, all progress keys resolved. |
| 13 | Commit, push, mark complete, notify | `orchestrator` | — | End-of-session-set mechanics. |

### Actuals

| Field | Value |
|---|---|
| Orchestrator used | *(fill after session)* |
| Effort used | *(fill after session)* |
| Wall-clock duration | *(fill after session)* |
| Token consumption | *(fill after session)* |
| Deviation from plan | *(fill after session)* |

---

## Cost summary

| Session | Orchestrator | Effort | Qualitative cost |
|---|---|---|---|
| 1 — Design + spec | Opus 4.7 | high | Moderate |
| 2 — API + service + unit tests | Sonnet 4.6 | high | Low |
| 3 — Razor + UAT + W2 + close | Sonnet 4.6 | high | Low |
| **Set total** | | | **Low–Moderate** |

---

## Routing constraints

- **Orchestrator never self-opines.** All analysis of this document and all cross-provider verification steps are routed via `route(task_type=…)` to the AI Router for delegation.
- **`outsourceMode: first`** per session-set config. Each routed task goes to the first available provider that meets the task-type capability requirement.
- **Verification provider** for Sessions 2 and 3 must confirm transaction-boundary correctness, authorization dual-check parity (client-side advisory + server-side authoritative), and test-coverage completeness against the spec's rejection paths.
```