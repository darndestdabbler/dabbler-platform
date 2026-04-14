# Flake Resistance Guidance

## Purpose

This document captures the durable rules from Session 3 of
`platform-post-cutover-hardening`. Use it when adding or updating startup
helpers, Playwright infrastructure, and smoke coverage so local and CI-like
runs stay fast, deterministic, and diagnosable.

## Startup And Fixture Rules

- Check build outputs before self-starting the Web or API hosts. Fail fast with
  a clear message instead of waiting for a generic startup timeout.
- Probe `GET /health` before starting a host so externally managed processes can
  be reused safely.
- Resolve the repository root by searching parent directories for
  `Dabbler.Platform.sln`. Do not depend on a fixed `..` chain from test output.
- If a self-started process exits early or never becomes healthy, include
  recent process output in the failure so the next session can triage quickly.
- Track browser-side failures per page and fail the owning test during cleanup
  if console errors, page errors, or unexpected request failures occurred.

## Helper Endpoint Polling Rules

- Use `HttpClient` plus `X-Test-Auth-Secret` for helper endpoints that do not
  depend on browser cookies, such as magic-link inbox polling and inbox
  clearing.
- Reserve temporary Playwright helper pages for session-dependent endpoints
  such as `/test-auth/session-info`.
- When a helper page is required, navigate with `WaitUntilState.DOMContentLoaded`
  and a short explicit timeout. Fetching JSON should not wait for
  `NetworkIdle`.
- Keep polling loops cheap and predictable. Do not create a new browser page for
  every poll attempt when direct HTTP is sufficient.

## Smoke-Test Determinism Rules

- Restore shared business state in `finally` blocks when a test mutates it.
  Repeat runs must start from the same baseline.
- Prefer pure C# file and content assertions for workflow smoke checks. Do not
  shell out to PowerShell or Python for simple repo asset validation.
- Add narrow assertions for the exact contract being exercised. For example,
  assert the captured magic-link email type instead of only checking that a
  link exists.
- Keep startup and workflow smoke checks focused on durable contracts that
  future sessions are expected to preserve.

## Avoid Reintroducing

- Hardcoded relative paths to the repository root.
- Startup helpers that timeout without surfacing recent process diagnostics.
- Polling helpers that create disposable Playwright pages for session-free HTTP
  checks.
- Smoke tests that leave shared product or auth state changed after completion.
- Workflow asset checks that depend on a specific local shell or interpreter.
