# DabblerPdf Multi-Page Fix Consumption Spec

> **Purpose:** Consume `DabblerPdf 1.1.1` in `dabbler-platform`,
> add the caller-level and end-to-end rendering-visibility coverage
> that would have caught the multi-page DirectContent bug before it
> reached the field, and close the Finding 2 gap left open by
> `pdf-info-metadata-uat-remediation`.
> **Created:** 2026-04-22
> **Session Set:** `docs/session-sets/pdf-newpage-multipage-fix-consumption/`
> **Prerequisite:** `DabblerPdf 1.1.1` is shipped (see sibling
> [`../dabbler-pdf/docs/session-sets/directcontent-multipage-fix-and-tests/change-log.md`](../../../../dabbler-pdf/docs/session-sets/directcontent-multipage-fix-and-tests/change-log.md)).
> The nupkg is available on the consumed feed (local folder or
> nuget.org) before this session starts.
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Context

On 2026-04-22 a production export of `/reports/orders-by-customer`
produced a 6-page PDF instead of the expected 7 pages. Physical
page 1 contained the column headers and data rows of composer-page 2
(with footer "Page 2 of 7"), while composer-page 1's title band was
overdrawn and invisible. `DabblerPdf 1.1.1` fixes the library bug —
the `Document.NewPage()` call after the first `DirectContent` write
now flushes the implicit first page instead of stacking it with
composer-page 2. See the sibling change log for the root-cause
analysis and library-side test additions.

This consumption set lands three pieces of work in `dabbler-platform`:

1. **Pull the fix in.** Bump
   [`Directory.Packages.props`](../../../Directory.Packages.props)
   from `DabblerPdf 1.1.0` to `DabblerPdf 1.1.1`.
2. **Caller-level rendering-visibility coverage.** Extend
   [`tests/Dabbler.Reports.Blazor.Tests/CallerLevelPdfExportTests.cs`](../../../tests/Dabbler.Reports.Blazor.Tests/CallerLevelPdfExportTests.cs)
   with PdfPig per-physical-page assertions. The shipped
   `ProductionCaller_RendersTitleHeadingOnFirstPage` uses raw
   byte-grep against inflated content streams — the exact pattern
   that passed while the field PDF was visibly wrong. Upgrade it to
   the rendering-aware discipline captured in
   [`dabbler-pdf/docs/planning/project-guidance.md`](../../../../dabbler-pdf/docs/planning/project-guidance.md)
   → Testing Conventions → "Visibility assertions go through a
   rendering-aware parser".
3. **Layer 3 end-to-end coverage.** Add a Playwright test that
   drives the real web app's export button, intercepts the
   downloaded PDF bytes, and runs PdfPig per-physical-page
   assertions — proving page 1 contains the title heading and that
   the physical page count matches the composer's logical page
   count. This is the layer that was missing when the field bug
   shipped: caller-level tests exercised a synthetic harness, not
   the user's actual click path.

This set also closes the Finding 2 remediation gap from
[`pdf-info-metadata-uat-remediation`](../pdf-info-metadata-uat-remediation/change-log.md).
That set dispositioned Finding 2 ("no title heading on page 1") as
"stale runtime, no code fix" on the evidence of a passing byte-grep
test. The field report on 2026-04-22 disproves that disposition:
the title literally IS in the bytes, but it is not rendered
visible. The dispositioned-as-stale finding was real; the test that
closed it was wrong. This set's rendering-visibility assertion is
the corrected closure.

## Goals

1. `dabbler-platform` consumes `DabblerPdf 1.1.1` — production
   exports produce one physical PDF page per composer page again.
2. `CallerLevelPdfExportTests.ProductionCaller_RendersTitleHeadingOnFirstPage`
   asserts through PdfPig per-physical-page text extraction rather
   than a byte grep. If the field bug regresses, this test catches
   it before human UAT.
3. A Playwright end-to-end test drives the web app's real export
   button, intercepts the downloaded PDF, and runs PdfPig
   assertions equivalent to the caller-level test plus the expected
   physical page count.
4. The Finding 2 remediation gap is formally closed: the
   corrected assertion replaces the byte-grep and the change log
   reverses the prior "stale runtime" disposition.

## Non-goals

- **No library-side work.** All library changes shipped in
  `DabblerPdf 1.1.1`. If new library gaps surface during this set,
  carve a cross-repo follow-up rather than inlining edits into
  `../dabbler-pdf`.
- **No new report content.** The work is test and dependency only;
  no RDF changes, no composer changes, no exporter changes.

---

## Session Plan

### Session 1 of 1 — Upgrade, caller coverage, Playwright coverage, close Finding 2

**Location of work:** `dabbler-platform/` (this repo).

**Steps:**

1. Read `docs/planning/project-guidance.md` and
   `docs/planning/lessons-learned.md` with special attention to:
   - Cross-Repo Mirror Convention (promoted 2026-04-22).
   - The rendering-visibility discipline mirrored in the sibling
     library's `project-guidance.md` — the principle applies to
     consumer tests too.

2. **Bump `DabblerPdf` pin.** Update
   [`Directory.Packages.props`](../../../Directory.Packages.props)
   `DabblerPdf` from `1.1.0` to `1.1.1`. Restore, build, and run the
   existing test suite to confirm no API-level breakage (there
   should be none — library change is internal).

3. **Upgrade `CallerLevelPdfExportTests` to rendering-aware
   assertions.** Add a PdfPig package reference to
   `tests/Dabbler.Reports.Blazor.Tests/Dabbler.Reports.Blazor.Tests.csproj`
   if not already present. Replace the byte-grep inside
   `ProductionCaller_RendersTitleHeadingOnFirstPage` with
   `PdfDocument.Open(bytes).GetPage(1).Text` and assert the title
   literal appears on physical page 1 specifically. Add a companion
   assertion on physical page count: `pdf.NumberOfPages` equals the
   composed logical page count. Keep the `/Info` dictionary
   assertions in `ProductionCaller_PopulatesInfoDictionaryFromRealRdf`
   byte-level — those are structural, not visibility, and match the
   carve-out in the shared convention.

4. **Layer 3 Playwright end-to-end test.** Add a new test under
   the existing Playwright project (inspect the repo for the
   current location and harness conventions) that:
   - Navigates to `/reports/orders-by-customer` in a rebuilt app.
   - Clicks the PDF export button and intercepts the downloaded
     file via Playwright's `Page.WaitForDownloadAsync` (or whatever
     the project's existing download-capture helper exposes).
   - Loads the downloaded bytes with PdfPig.
   - Asserts `pdf.NumberOfPages >= 1`, physical page 1's text
     contains `"Orders by Retail Business"`, and physical page 1's
     text contains `"Page 1 of"` (proof the footer rendered on the
     right physical page).
   - Asserts the physical page count matches the composer's
     expected logical page count for a deterministic row set —
     pick a fixed row-count parameter that produces a stable
     multi-page document.
   - Runs in the existing e2e collection; does not add a new
     collection or test runner.

5. **Close Finding 2.** Update
   [`pdf-info-metadata-uat-remediation/change-log.md`](../pdf-info-metadata-uat-remediation/change-log.md)
   with a follow-up note pointing here, OR add a "Finding 2
   Re-opened And Corrected" section to this set's change-log.md —
   the Review Session convention in `project-guidance.md` prefers
   the second shape so the disposition trail ends in the set that
   actually fixed it. The note must name the prior
   "stale runtime" disposition as incorrect, cite the 2026-04-22
   field report as the disproving evidence, and link the
   rendering-visibility test added in step 3 as the corrected
   assertion.

6. **Build and test.**
   ```bash
   dotnet build Dabbler.Platform.sln
   dotnet test Dabbler.Platform.sln --filter "FullyQualifiedName~PdfExport"
   dotnet test Dabbler.Platform.sln
   ```
   Run the Playwright suite with the stack up:
   ```bash
   dotnet test tests/Dabbler.E2E.Tests/Dabbler.E2E.Tests.csproj
   ```
   Exact Playwright project path may differ; locate it in-session.
   If Docker Desktop is not up, the Playwright tests fail at fixture
   init — disclose that in the verification prompt rather than
   treating it as a remediation blocker.

7. **Cross-provider verification** via
   `route(task_type="session-verification")`. Include the bumped
   `Directory.Packages.props`, the updated caller-level test, the
   new Playwright test, and an out-of-scope section pointing at
   the sibling library's `change-log.md` and the prior
   `pdf-info-metadata-uat-remediation` change-log.

8. **Generate `change-log.md`** via
   `route(task_type="summarization", max_tier=1)` per the
   Cross-Repo Mirror / summarization-vs-documentation conventions.

9. **Commit, push, notify.** Then run the Step 9 reorganization
   review.

**Creates:** `change-log.md`; possibly a new Playwright test file
under `tests/Dabbler.E2E.Tests/` (or wherever Playwright lives).
**Touches:** `Directory.Packages.props` (1.1.0 → 1.1.1);
`tests/Dabbler.Reports.Blazor.Tests/Dabbler.Reports.Blazor.Tests.csproj`
(PdfPig reference if not already present);
`tests/Dabbler.Reports.Blazor.Tests/CallerLevelPdfExportTests.cs`
(rendering-aware assertion); possibly
`docs/session-sets/pdf-info-metadata-uat-remediation/change-log.md`
(follow-up pointer).
**Ends with:** `dabbler-platform` is on `DabblerPdf 1.1.1`, a
rebuilt app exports `/reports/orders-by-customer` with the title
heading visible on page 1 and N+1 physical pages where the
pre-fix build produced N, Finding 2 is formally closed with a
rendering-aware test replacing the bytes-only test that masked it,
and Layer 3 Playwright coverage catches the next regression at the
user's click path rather than in the field.
**Progress keys:** `session-1/read-planning`,
`session-1/bump-dabbler-pdf`, `session-1/caller-level-upgrade`,
`session-1/playwright-e2e`, `session-1/close-finding-2`,
`session-1/build-and-test`, `session-1/verification`,
`session-1/change-log`, `session-1/commit-push-notify`,
`session-1/reorg-review`.

---

## Risk Notes

- **Playwright fixture brittleness.** If the e2e test proves
  unstable across rebuilds (download interception, app warm-up,
  Docker readiness), pull it back to a deterministic integration
  test that drives the same seam without a browser, and queue a
  follow-up set specifically to stabilize the e2e harness. Do not
  ship a flaky e2e test to block future sets.
- **Finding 2 scope creep.** If investigation shows the title
  still does not render on page 1 after the `DabblerPdf 1.1.1`
  bump — i.e., the multi-page fix alone doesn't close Finding 2 —
  that is a new finding, not a task for this set. Stop, document
  the evidence, and carve a follow-up set rather than growing
  this one.
- **Byte-level assertions in `/Info` tests.** The carve-out in
  the rendering-visibility convention is deliberate: structural
  assertions (dictionary keys, object IDs) remain byte-level. Do
  not "upgrade" the `/Info` dictionary assertions to PdfPig
  unnecessarily — PdfPig's metadata API is a different surface
  and would change the test's failure mode without improving its
  fidelity.
