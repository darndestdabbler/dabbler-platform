# Full-App UAT Reviewer Guide

This guide supports Session 2 of `docs/session-sets/full-app-uat/`.

Use it with
`docs/session-sets/full-app-uat/full-app-uat-checklist.json` in the checklist
editor at `https://darndestdabbler.github.io/uat-checklist-editor/`.

## Purpose

This review is intentionally small.

It is not a second copy of the full automated suite.

Its job is to confirm that the representative workflows still feel clear,
coherent, and trustworthy to a human reviewer after the automation baseline is
already green.

## Quick Start

For local full-app UAT, the web `http` launch profile does not start the API.
You need both processes available unless you are using Docker.

Recommended local startup:

```powershell
dotnet run --project src/Apps/AcmeCoffeeOnline.Api --launch-profile http
dotnet run --project src/Apps/AcmeCoffeeOnline.Web --launch-profile http
```

The expected local URLs are:

- web: `http://localhost:5010`
- api: `http://localhost:5011`

The web app's Development configuration points at the API on
`http://localhost:5011`, so the UI will not behave correctly if only the web
process is running.

Optional Docker-based startup for a more CI-like stack:

```powershell
docker compose -f docker/docker-compose.yml up --build sqlserver mailhog api web
```

Current practice for local E2E and UAT is still direct app startup rather than
Docker or .NET Aspire orchestration. Docker remains available as an optional
stack, but it is not the default reviewer path documented here.

Optional automation guardrail refresh before the live pass:

```powershell
dotnet test tests/AcmeCoffeeOnline.Playwright/AcmeCoffeeOnline.Playwright.csproj --filter "(FullyQualifiedName~E2E_WF_04)|(FullyQualifiedName~E2E_WF_05)|(FullyQualifiedName~E2E_WF_02)|(FullyQualifiedName~E2E_WF_01)|(FullyQualifiedName~E2E_WF_03)"
```

Open the editor in Chrome or Edge:

`https://darndestdabbler.github.io/uat-checklist-editor/`

Then open:

`docs/session-sets/full-app-uat/full-app-uat-checklist.json`

Seeded-session and dev-inbox helpers remain guarded by the existing
Development or Test loopback endpoints described in
`docs/workflow-auth-observability.md`.

## Seeded-Session Sign-In

The seeded-session endpoint requires the `X-Test-Auth-Secret` header, so
opening `/test-auth/seeded-session?...` directly in the browser will not work.

Use this browser-based helper instead:

1. Open `http://localhost:5010/login` in the same browser profile you will use
   for the review.
2. Open the browser DevTools console on that page.
3. Paste and run one of the snippets below.
4. Wait for the page to reload into the signed-in shell.

Reusable helper:

```javascript
window.seededSessionSignIn = async (email) => {
  const response = await fetch(`/test-auth/seeded-session?email=${encodeURIComponent(email)}`, {
    method: 'GET',
    headers: { 'X-Test-Auth-Secret': 'acme-local-seeded-session' },
    credentials: 'include'
  });

  if (!response.ok) {
    throw new Error(`Seeded sign-in failed with status ${response.status}`);
  }

  location.href = '/';
};
```

Checklist personas:

```javascript
await window.seededSessionSignIn('admin@acmecoffee.com');
```

```javascript
await window.seededSessionSignIn('casey.hart@acmecoffee.com');
```

```javascript
await window.seededSessionSignIn('sloane.reyes@acmecoffee.com');
```

Expected starting context after sign-in:

- `admin@acmecoffee.com`: system administrator with broad backoffice access
- `casey.hart@acmecoffee.com`: starts in the internal organization, then can
  switch into one retail-business customer organization
- `sloane.reyes@acmecoffee.com`: starts in the internal organization, then can
  switch into managed retail organizations with scoped user-admin actions

If you want to clear the seeded-session cookie between checklist sections, sign
out from the UI or run this in the DevTools console:

```javascript
await fetch('/test-auth/sign-out', {
  method: 'GET',
  headers: { 'X-Test-Auth-Secret': 'acme-local-seeded-session' },
  credentials: 'include'
});
location.href = '/login';
```

## Test Data And Reset Expectations

There is no suite-wide database reset step documented for this review pass.

The current automated E2E strategy uses a seeded development baseline plus
targeted cleanup for workflows that mutate persisted state. In practice, that
means the mutating Playwright workflows restore the original state or archive
their test-created records in teardown instead of resetting the whole database
between tests.

For this UAT pass, begin from the normal seeded Development environment. If a
workflow leaves behind state that would confuse later checklist items, either
restore the changed value before moving on or stop and record the issue in the
checklist so we can decide whether a stricter reset workflow should be added.

## Review Flow

1. Open the checklist JSON in the checklist editor.
2. Work through the workflow subareas in this frozen order:
   `E2E-WF-04`, `E2E-WF-05`, `E2E-WF-02`, `E2E-WF-01`, `E2E-WF-03`.
3. Mark `Passes` only when the item genuinely passed.
4. Put any observations, bugs, or clarifications directly into that item's
   `Feedback` field and save the checklist JSON in place.
5. Use the helper scripts only when the visible UI is not enough:
   `scripts/full-app-uat/Get-SessionInfo.ps1` for current session scope and
   `scripts/full-app-uat/Get-MagicLinks.ps1` for the dev-captured magic link.

## Feedback Conventions

Keep feedback structured enough for later extraction.

When an item does not pass or needs explanation, begin the feedback with short
labels like these:

- `Classification: Defect`
- `Classification: Acceptable Difference`
- `Classification: Deferred Follow-up`
- `Severity: Critical|Major|Minor|Trivial`
- `Notes: ...`

Example:

```text
Classification: Defect
Severity: Major
Notes: The active-organization label changed, but the new customer scope was easy to miss because the label stayed low-contrast in the shell header.
```

Use the editor's automatic "Other" row inside a subarea for observations that
do not fit one of the listed items cleanly.

## Helper Usage

Current session snapshot after an organization switch:

```powershell
.\scripts\full-app-uat\Get-SessionInfo.ps1
```

Clear any prior magic links for the customer test user:

```powershell
.\scripts\full-app-uat\Get-MagicLinks.ps1 -Email james.smith@example.com -Clear
```

Read the newly captured magic link after requesting sign-in:

```powershell
.\scripts\full-app-uat\Get-MagicLinks.ps1 -Email james.smith@example.com
```

## In Scope

- the five frozen workflow IDs in this exact order:
  `E2E-WF-04`, `E2E-WF-05`, `E2E-WF-02`, `E2E-WF-01`, `E2E-WF-03`
- the listed human-only checks around active-organization clarity,
  `/admin/users` warning clarity, product-dialog validation and keyboard flow,
  real magic-link clarity, and trust in the product-availability feedback
- lightweight helper usage that stays inside the existing guarded
  Development or Test diagnostics endpoints

## Out Of Scope

- new routes, new personas, or broad exploratory coverage beyond the frozen
  representative subset
- re-validating backend truth, persistence, or mutating correctness already
  proved by the representative Playwright workflows
- broad accessibility recertification work unless a concrete workflow issue is
  observed during this pass
- documentation, packaging, or planning updates that are not directly driven
  by a confirmed UAT finding
