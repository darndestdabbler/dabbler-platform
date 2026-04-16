# Full-App UAT Reviewer Guide

This guide supports Session 2 of `docs/session-sets/full-app-uat/`.

Use it with `docs/session-sets/full-app-uat/full-app-uat-checklist.json` to
run the narrow human-confirmation pass against the five frozen representative
workflows.

## Purpose

This review is intentionally small.

It is not a second copy of the full automated suite.

Its job is to confirm that the representative workflows still feel clear,
coherent, and trustworthy to a human reviewer after the automation baseline is
already green.

## Quick Start

Run the web app with the standard Development HTTP profile:

```powershell
dotnet run --project src/Apps/AcmeCoffeeOnline.Web --launch-profile http
```

Optional automation guardrail refresh before the live pass:

```powershell
dotnet test tests/AcmeCoffeeOnline.Playwright/AcmeCoffeeOnline.Playwright.csproj --filter "(FullyQualifiedName~E2E_WF_04)|(FullyQualifiedName~E2E_WF_05)|(FullyQualifiedName~E2E_WF_02)|(FullyQualifiedName~E2E_WF_01)|(FullyQualifiedName~E2E_WF_03)"
```

Seeded-session and dev-inbox helpers remain guarded by the existing
Development or Test loopback endpoints described in
`docs/workflow-auth-observability.md`.

## Review Flow

1. Start from `docs/session-sets/full-app-uat/full-app-uat-checklist.json`.
2. Work through the five items in their frozen order:
   `E2E-WF-04`, `E2E-WF-05`, `E2E-WF-02`, `E2E-WF-01`, `E2E-WF-03`.
3. Keep each workflow narrow. Confirm only the listed route sequence,
   guardrail signals, and human-observation focus items.
4. Use the helper scripts only when the visible UI is not enough:
   `scripts/full-app-uat/Get-SessionInfo.ps1` for current session scope and
   `scripts/full-app-uat/Get-MagicLinks.ps1` for the dev-captured magic link.
5. Record findings in
   `docs/session-sets/full-app-uat/full-app-uat-findings.json`.

## How To Read The Checklist

Each checklist item includes:

- `guardrailSignals`: automation-backed route or state expectations that
  should still be visible during the live pass
- `humanObservationFocus`: the judgment-heavy part a reviewer should actually
  concentrate on
- `helperRefs`: optional read-only diagnostics that can clarify scope or dev
  inbox state without widening the app surface

Use those fields to stay disciplined:

- treat the automation guardrails as confirmation, not a reason to branch into
  exploratory testing
- spend human attention on wording, layout, focus, trust, and visible context
- use helper output only to confirm current session or magic-link state, not
  to replace the UI review itself

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

## Issue Capture Rules

Every captured finding should include the Session 1 fields:

- `id`
- `workflowId`
- `title`
- `location`
- `stepsToReproduce`
- `expectedResult`
- `actualResult`
- `classification`
- `severity`
- `status`
- `screenshotPath`
- `deferredOwner`
- `notes`

Classification guidance:

- `Defect`: behavior conflicts with the accepted baseline or blocks a frozen
  workflow expectation
- `Acceptable Difference`: behavior differs from reviewer preference but does
  not justify remediation inside this set
- `Deferred Follow-up`: the observation is real, but the proper owner is
  outside this set or the scope is too broad for the current UAT lane

Severity guidance:

- `Critical`: blocks sign-in, navigation, or completion of a frozen workflow
- `Major`: materially harms clarity, trust, or successful completion
- `Minor`: noticeable issue with a reasonable workaround
- `Trivial`: cosmetic or very low-impact issue

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
