# Auth And Workflow Observability

## Purpose

Use these Development/Test diagnostics to trace the auth helper path from dev
magic-link capture through seeded-session sign-in and into AI-router session
verification. They are the fastest signals for local runs, CI-like test runs,
and future AI-led sessions when auth or workflow automation starts failing.

## Signals

### Dev Magic-Link Capture

- Emitter: `Dabbler.Security.Services.DevEmailSender`
- Event ID: `7100`
- Fields: `EmailType`, `MaskedEmail`, `HasCode`, `HasLink`
- Meaning: a dev/test email was captured for inspection instead of being sent
  through external mail infrastructure

Example:

```text
Captured MagicLink dev email for c***r@acme.test hasCode=True hasLink=True
```

### Seeded-Session And Inbox Diagnostics

- Emitter: `AcmeCoffeeOnline.AuthObservability`
- Event ID `4101`: seeded session issued
- Fields: `MaskedEmail`, `ResolvedUserId`, `Roles`, `MatchedExistingUser`
- Event ID `4100`: magic-link diagnostics access
- Fields: `EmailFilter`, `ClearInbox`, `CapturedCount`

Examples:

```text
Seeded session issued for s***r@acme.test userId=seeded-user roles=BackofficeUser matchedExistingUser=False
Magic link diagnostics requested for c***r@acme.test clearInbox=True capturedCount=1
```

### AI Router Session Verification

- Emitter: `ai-router/__init__.py`
- Logger: `ai_router.session_verification`
- Events: `session_verification_started`, `session_verification_completed`
- Format: JSON payload emitted through Python logging output

Example:

```json
session_verification {"complexity_score": 70, "event": "session_verification_started", "max_tier": 3, "model": "opus"}
session_verification {"elapsed_seconds": 18.342, "escalated": false, "event": "session_verification_completed", "model": "opus", "total_cost_usd": 0.214583}
```

## Inspecting Runs

- Local app logs: run `dotnet run --project src/Apps/AcmeCoffeeOnline.Web` and
  watch the console for event IDs `7100`, `4101`, and `4100` while exercising
  `/auth/send-magic-link` or `/test-auth/*` helpers.
- Local verification runs: when a session ends, watch the
  `.venv/Scripts/python.exe` command output for the
  `ai_router.session_verification` JSON lines before and after the verifier call.
- CI-like logs: search the combined job output for
  `Captured MagicLink dev email`, `Seeded session issued`,
  `Magic link diagnostics requested`, or `session_verification`.
- Fast repo-wide smoke check: run
  `dotnet test tests/AcmeCoffeeOnline.Web.Tests/AcmeCoffeeOnline.Web.Tests.csproj`
  to confirm the structured auth/workflow diagnostics still exist.

## Troubleshooting

- No auth-helper logs usually means the app is not running in `Development` or
  `Test`, or the guarded `/test-auth/*` endpoints were not invoked from a
  loopback/test request with `X-Test-Auth-Secret`.
- Missing `4101` with a failing seeded-session flow usually means the request
  was blocked before sign-in completed; check the secret header, allowed roles,
  and environment gating first.
- Missing `4100` after checking `/test-auth/magic-links` usually means the
  diagnostics endpoint was never hit, even if `7100` shows the inbox captured a
  magic link.
- Missing router verification logs usually means the session did not reach the
  mandatory `route(task_type="session-verification")` step or the Python
  command output was not collected.
