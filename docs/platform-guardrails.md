# Platform Guardrails

## App Foundations

- wire Dabbler static assets through a single supported entrypoint
- fail fast in Development/Test when required Dabbler assets are missing
- keep auth pages on an auth-specific layout
- make routed content visibility testable so missing layout-body regressions are caught quickly

## Auth And Permissions

- keep seeded-session and mailbox helpers Development/Test-only, loopback-only, and secret-guarded
- resolve seeded sessions to real app users when possible so browser tests exercise the real permission model
- preserve customer scoping by carrying enough identity to derive `CustomerId` for backend row filtering
- treat permission tests as contract tests against checked-in security configuration

## E2E Expectations

- fail the suite on browser console errors, Blazor runtime errors, page exceptions, and relevant failed requests
- keep at least one real magic-link flow covered
- keep at least one seeded-session flow for fast deterministic role coverage
- confirm at least one mutating business action against backend state, not only UI feedback

## Workflow Assets

- keep `ai-router/` at repo root
- keep root instruction files at repo root
- save verifier output raw and never edit it
- log every session step in the active session set

## Template Discipline

- keep `template-seed/` workflow-focused
- prefer placeholders and small bootstrap scripts over product-specific scaffolding
- update the template whenever the repo learns a durable workflow lesson worth repeating elsewhere
