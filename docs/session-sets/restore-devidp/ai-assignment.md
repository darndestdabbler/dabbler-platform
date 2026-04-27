```markdown
# AI Assignment: `restore-devidp`

This assignment document outlines the plan for an AI-led session set.

- **Session Set:** `restore-devidp`
- **Purpose:** Restore the `Dabbler.Security.DevIdp` project to enable a local OIDC sign-in flow for the "Sign in with your organization account" button.
- **Spec:** `docs/session-sets/restore-devidp/spec.md`
- **Branch:** `u/ai/restore-devidp`

## Session 1: Restore project and verify it builds and runs

- **Recommended orchestrator:** claude-code (Opus 4.7 @ effort=high)
- **Rationale:** Human-selected. Appropriate for the code archaeology and refactoring tasks involved in recovering a project from Git history and adapting it to a new location and modern dependencies.
- **Estimated routed cost:** <$0.25

| Step | Action | Routing Decision |
| :--- | :--- | :--- |
| 1 | Read and internalize guidance documents. | `human` |
| 2 | Recover all files from the `Dabbler.Security.DevIdp` project directory using `git show 8730107^:<path>`. | `none` |
| 3 | Move recovered files to `src/Libs/Dabbler.Security.DevIdp/` and update `RootNamespace` and any file paths in the `.csproj` file. | `none` (route as `task_type=analysis, complexity=low` if non-trivial issues arise) |
| 4 | Run `dotnet sln add src/Libs/Dabbler.Security.DevIdp/Dabbler.Security.DevIdp.csproj`. | `none` |
| 5 | Run `dotnet build Dabbler.Platform.sln` and resolve any build errors. | `none` (route as `task_type=code_gen, complexity=medium` if build errors require code changes) |
| 6 | Launch the `DevIdp` project and use `curl` to verify the OIDC metadata endpoint `https://localhost:5100/.well-known/openid-configuration` is responsive and returns a valid document. | `task_type=code_gen, complexity=low` |

### Actuals

| Metric | Value |
| :--- | :--- |
| **Orchestrator** | claude-code (Opus 4.7) @ effort=high |
| **Final routed cost** | $0.7079 |
| **Session duration** | ~25 minutes |
| **Session rating** | VERIFIED on round 1 (no issues) |
| **Routed calls** | `analysis` (ai-assignment) → gemini-pro $0.0156; `session-verification` → opus $0.6923 |
| **Deviations** | None. Work was almost entirely mechanical (git restore, file copy, sln add, launch-profile rename). RootNamespace override not needed — SDK default already matched the recovered namespaces. |
| **Notes for next-session calibration** | The `analysis` step (ai-assignment authoring) is small but cheap on Gemini Pro; keep routing it. Session-verification dominated cost as expected; complexity_hint=70 was correct given file count. Two advisory notes from the verifier are forwarded to Session 2: (a) confirm Web app HTTPS port matches the seeded `acmecoffee-dev` redirect URI `https://localhost:7002/signin-EntraId` and the OIDC scheme's `CallbackPath` is `/signin-EntraId`; (b) `scp:openid` omitted from client permissions is benign per OIDC spec — no action. |
| **Progress keys** | `session-1/restore-source`, `session-1/launch-profile-rename`, `session-1/solution-wireup`, `session-1/build-and-run`, `session-1/test`, `session-1/verification` |

**Next-session orchestrator recommendation (Session 2):** claude-code Opus 4.7 @ effort=high.
Rationale (routed via `analysis`, gemini-pro): Session 2 is substantially more
complex than Session 1 — Playwright test authoring, multi-service OIDC debugging,
and Docker Compose configuration justify a high-capability model to land it in a
single pass.

---

## Session 2: Wire up SSO, enable in Dev/Test, add Playwright coverage

- **Recommended orchestrator:** claude-code (Opus 4.7 @ effort=high)
- **Rationale:** Consistency with Session 1. Strong performance on generating E2E tests (Playwright) and handling the mix of configuration changes, targeted code removal, and Docker file modifications required by this session.
- **Estimated routed cost:** $0.25-$0.50

| Step | Action | Routing Decision |
| :--- | :--- | :--- |
| 1 | Read and internalize guidance documents. | `human` |
| 2 | Manually perform an end-to-end OIDC sign-in flow locally to confirm the Web app and DevIdp integrate correctly. | `none` |
| 3 | Set `Authentication:ShowSsoButton` to `true` in `src/Apps/AcmeCoffeeOnline.Web/appsettings.Development.json` (and `Test` config if present). | `none` |
| 4 | Remove the try/catch wrapper from the `/auth/challenge/{scheme}` handler in `Web/Program.cs` and the `sso-unavailable` error message from `Web/Components/Pages/Auth/Login.razor`. | `task_type=code_gen, complexity=low` |
| 5 | Create a new Playwright test file that automates the SSO login happy path, from login page to dashboard. | `task_type=code_gen, complexity=high` |
| 6 | Add a `devidp` service definition to `docker/docker-compose.app.yml`. | `task_type=code_gen, complexity=low` |
| 7 | Run `dotnet test Dabbler.Platform.sln` and the Playwright test suite to confirm all tests pass, including the new SSO test. | `task_type=code_gen, complexity=low` |

### Actuals

| Metric | Value |
| :--- | :--- |
| **Orchestrator** | |
| **Final routed cost** | |
| **Session duration** | |
| **Session rating** | |
| **Progress keys** | `session-2/oidc-roundtrip`, `session-2/enable-sso`, `session-2/retire-safe-fail`, `session-2/playwright-coverage`, `session-2/docker` |

---

### Next-session-set recommendation

> Orchestrator: Route this section as `task_type=analysis, complexity=low` at the end of Session 2. Provide a recommendation for the next logical session set to undertake, based on the context of this work.
```