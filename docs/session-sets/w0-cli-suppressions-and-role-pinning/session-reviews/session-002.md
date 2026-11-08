# Verification Round 1

## Session 2 Verification Report

### Acceptance Criteria Status

| # | Criterion | Status | Evidence |
|---|-----------|--------|----------|
| 1 | `--active-role <name>` accepted by both `validate` and `compile` | ✅ | Both `ValidateCommand` and `CompileCommand` in `Program.cs` parse `--active-role`/`-r` and thread `activeRole` into `validator.Validate(steps, catalogAdapter, activeRole)` |
| 2 | Persona inference from first sign-in step works | ✅ | `personas.py::infer_active_role` + `_EMAIL_STEP_RE` 5-step lookahead; `test_happy_path_admin_signin_resolves_to_systemadmin` passes |
| 3 | Absence of role context falls back to AnyRole `*` | ✅ | `_resolve_active_role` returns `(None, "")` when no sign-in step found → `_invoke_w0_compile` omits `--active-role` flag → validator defaults to `*`; `test_no_signin_step_returns_no_role_and_no_error` covers this |
| 4 | Email present but missing from `personas.json` → clear error | ✅ | `personas.py` returns `error=` message naming the email + file; `run_checklist` early-returns `compile_ok=False` with `runner_message`; `test_email_not_in_personas_returns_explicit_error` covers this |
| 5 | `--active-role GhostRole` not in catalog → `UnknownRoute` with role name | ✅ | `Validate_ActiveRoleNotInCatalog_ProducesUnknownRouteWithRoleInMessage` asserts `Kind == UnknownRoute` and `Contains("GhostRole")` |
| 6 | CLI version bumped to 1.2.1 | ✅ | `Dabbler.Uat.Dsl.Cli.csproj` 1.2.0→1.2.1; `pack-output/Dabbler.Uat.Dsl.Cli.1.2.1.nupkg` present |
| 7 | Build clean, all tests pass | ✅ | 187/187 .NET, 5/5 Python |
| 8 | End contract: admin@acmecoffee.com validates as `SystemAdmin` without manual `--active-role` | ✅ | `_resolve_active_role` → `infer_active_role` → personas.json lookup → `--active-role SystemAdmin` threaded to W0 CLI automatically |

---

### Issues Found

#### Issue 1 — Duplicate `_invoke_w0_compile` definition (dead code)

**Location:** `ai-router/uat_runner/runner.py`

**Problem:** `_invoke_w0_compile` is defined twice with identical bodies — once before `_resolve_active_role` and once after it. Python silently shadows the first definition with the second. The bodies are identical so this is not a runtime bug, but it is dead code and a merge/paste artifact that will confuse future readers.

**Fix:** Delete the first definition (the one appearing before `_resolve_active_role`). Keep the second, which sits in the natural call-order position after the helper it logically follows.

```python
# Remove lines: the first _invoke_w0_compile block (approx. lines before _resolve_active_role)
# Keep: the second _invoke_w0_compile block (after _resolve_active_role)
```

**Severity:** Low — no behavioral impact, but should be cleaned up before commit.

---

#### Issue 2 — `auth_email` / persona-inference divergence potential

**Location:** `ai-router/uat_runner/runner.py` → `run_checklist` signature

**Problem:** `run_checklist` accepts both `auth_email` (pre-existing, defaults to `admin@acmecoffee.com`, used for triage metadata) and infers the active role independently by scanning the checklist's DSL steps. A caller could pass `auth_email="jdoe@acmecoffee.com"` while the checklist's sign-in step says `admin@acmecoffee.com`, creating a silent mismatch between the role the validator enforces and the persona the triage record claims.

**Fix (optional, minor):** Add a diagnostic warning when `auth_email` is set and the inferred email differs:

```python
resolved_role, role_message = _resolve_active_role(checklist, active_role)
# After inference, optionally warn on mismatch:
if (
    resolved_role
    and auth_email
    and hasattr(_resolve_active_role, '_last_email')  # or thread email through
    # simpler: log a note in role_message
):
    ...
```

Alternatively, document the contract: `auth_email` is for the Playwright infrastructure / triage record; the checklist's embedded email is the authoritative persona signal. This is design debt, not a bug.

**Severity:** Informational — no incorrect behavior today since both values are consistent in all current checklists.

---

### Verification of key logic paths

**`_resolve_active_role` return-value matrix:**

| Scenario | `resolved_role` | `role_message` | `run_checklist` behavior |
|---|---|---|---|
| Explicit `--active-role Admin` | `"Admin"` | `"Active role pinned via override: Admin"` | Compiles with `--active-role Admin` ✅ |
| Inference success (admin@→SystemAdmin) | `"SystemAdmin"` | `"Active role inferred from sign-in step (admin@… → SystemAdmin)."` | Compiles with `--active-role SystemAdmin` ✅ |
| Inference error (email not in map) | `None` | `"Persona inference error: Persona 'ghost@…' has no entry…"` | Early-returns `compile_ok=False` ✅ |
| No sign-in step found | `None` | `""` | Compiles without `--active-role` (AnyRole fallback) ✅ |

The gate `if role_message and not resolved_role` correctly distinguishes case 3 (abort) from case 4 (proceed without role).

**Regex correctness (`_EMAIL_STEP_RE`):** VERBOSE mode ignores literal whitespace; `(?:Email|Email\s+address)` backtracks correctly in Python's NFA engine to match both short and long label forms; `re.DOTALL` + `.*?` handles multi-token step text without over-matching. Case-insensitive flag covers author casing variation.

**Lookahead boundary:** `_LOOKAHEAD_STEPS = 5` checks indices 0–4. `test_signin_lookahead_window_skips_late_email_step` places the email step at index 5 (the 6th action) and confirms it's outside the window.

---

### Verdict

**Pass with one cleanup.** All eight acceptance criteria are satisfied. The duplicate `_invoke_w0_compile` definition should be removed before final commit. The `auth_email` divergence potential is informational and does not block.
