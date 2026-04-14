# Development And Test Auth Modes

## Supported Modes

### Real Magic Link

- use for browser coverage that should exercise the same login path as customers
- in Development/Test, magic links are captured by the dev inbox so local and CI-like runs can retrieve the link safely
- prefer this mode for at least one representative end-to-end sign-in journey

### Seeded Session

- use for fast deterministic automation when the test needs a known role immediately
- keep it Development/Test-only, loopback-only, and guarded by `X-Test-Auth-Secret`
- prefer real seeded users by email so the Web-to-API handoff uses actual app user IDs and permissions

### Organization Login

- use only when a session explicitly needs OIDC behavior
- keep it optional for local developer productivity; it is not the baseline path for smoke coverage

## Operational Rules

- never enable seeded-session helpers in production
- never expose dev inbox or session-info helpers beyond loopback/test usage
- use the real auth flow for confidence, and seeded sessions for speed
- if a new auth path is added, document the intended tests and guardrails at the same time
