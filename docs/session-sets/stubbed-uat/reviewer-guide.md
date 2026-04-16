# Stubbed UAT Reviewer Guide

This guide supports Session 2 of `docs/session-sets/stubbed-uat/`.

Use it with `docs/session-sets/stubbed-uat/stubbed-uat-checklist.json` in the
checklist editor at `https://darndestdabbler.github.io/uat-checklist-editor/`.

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

Open the editor in Chrome or Edge:

`https://darndestdabbler.github.io/uat-checklist-editor/`

Then open:

`docs/session-sets/stubbed-uat/stubbed-uat-checklist.json`

IDE fallback:

- set `src/Apps/AcmeCoffeeOnline.Web` as the startup project
- choose the `Stub UAT` launch profile
- run the project and confirm the browser lands on `/platform/stub`

## Review Flow

1. Start at `/platform/stub`.
2. Open the checklist JSON in the checklist editor.
3. Work through each checklist item.
4. Save pass or fail state and any observations directly in the checklist JSON.
5. Use the editor's automatic "Other" row when an observation does not fit one
   of the listed items cleanly.
6. When you notice something outside the frozen scope, record it as a deferred
   observation instead of treating it as an early-UAT defect.

## Feedback Conventions

Keep feedback structured enough for later extraction.

When an item does not pass or needs explanation, begin the feedback with short
labels like these:

- `Classification: Defect`
- `Classification: Deferred Follow-up`
- `Severity: High|Medium|Low`
- `Notes: ...`

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
