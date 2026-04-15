# Stubbed UAT Reviewer Guide

This guide supports Session 2 of `docs/session-sets/stubbed-uat/`.

Use it with `docs/session-sets/stubbed-uat/stubbed-uat-checklist.json` to run
the early design and navigation pass against the frozen `/platform/stub`
surface.

## Purpose

This review is intentionally narrow.

It is for shell, workspace, query-page, detail-target, and report navigation
proof through the app-hosted stub route.

It is not a substitute for later representative E2E, security, or full-app
UAT work.

## Quick Start

Run the web app with the dedicated launch profile:

```powershell
dotnet run --project src/Apps/AcmeCoffeeOnline.Web --launch-profile "Stub UAT"
```

The `Stub UAT` launch profile opens `/platform/stub` directly and explicitly
forces `PlatformStubMode__Enabled=true` in a Development host so the review
surface is repeatable even if future defaults change.

IDE fallback:

- set `src/Apps/AcmeCoffeeOnline.Web` as the startup project
- choose the `Stub UAT` launch profile
- run the project and confirm the browser lands on `/platform/stub`

## Review Flow

1. Start at `/platform/stub`.
2. Work through each checklist item in
   `docs/session-sets/stubbed-uat/stubbed-uat-checklist.json`.
3. Capture only findings that fit the frozen scope from
   `docs/planning/stubbed-uat-scope-ledger.md`.
4. When you notice something outside the frozen scope, record it as a deferred
   observation instead of treating it as an early-UAT defect.

## How To Read The Checklist

Each checklist item includes:

- `reviewMode`: whether the item is `manual-only`,
  `shared-with-automation`, or `defer-if-out-of-scope`
- `automatedCoverageRefs`: existing proof that already covers related
  semantics or guardrails
- `humanObservationFocus`: the part a human reviewer should concentrate on in
  the live browser

Use those fields to keep the pass deliberate:

- `manual-only` means the judgment depends on visible layout, focus styling,
  or interaction feel that automation does not currently prove
- `shared-with-automation` means automation already covers related semantics,
  so only file a defect when the live hosted experience still looks or behaves
  wrong
- `defer-if-out-of-scope` means capture the note, then hand it to the named
  downstream owner instead of expanding this session's scope

## Issue Capture Rules

Every captured issue should include the Session 1 fields:

- `id`
- `title`
- `surface`
- `reproductionSteps`
- `observedBehavior`
- `expectedBehavior`
- `severity`
- `status`
- `screenshotUrl`

Severity guidance:

- `High`: blocks navigation or a core interaction inside the frozen scope
- `Medium`: incorrect but still usable layout, focus, labeling, or navigation
- `Low`: minor cosmetic or non-blocking presentation issue

Status guidance:

- start confirmed in-scope issues as `New`
- move clearly later-owned observations to `Defer`

## In Scope

- shell load and layout stability at `/platform/stub`
- skip-link behavior, tab order, and visible focus through shell chrome
- menu-driven navigation to the retail-business query page
- menu-driven navigation to recent retail-business detail tabs
- report navigation, report toolbar access, and report-viewer readability
- workspace tab switching between the open stub surfaces

## Out Of Scope

- backend truth, business-rule correctness, or real data validation
- multi-organization security and role-scoped behavior
- deep organization-model behavior beyond the accepted distributor framing
- representative mutation workflows or end-to-end business validation
- packaging, install validation, or broader full-application acceptance
