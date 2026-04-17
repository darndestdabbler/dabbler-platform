# Reports PDF Layout Spec

> **Purpose:** Fix two report-rendering issues that surfaced in the
> `security-dashboard-uat` cross-cutting "Other" feedback — (1) the report
> title is missing from the PDF export, and (2) the column headers should
> appear under the report title on page 1 rather than at the top of the
> page.
> **Created:** 2026-04-17
> **Session Set:** `docs/session-sets/reports-pdf-layout/`
> **Prerequisite:** None. Can run independently of the two Security
> Dashboard remediation sets.
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Context

The Security Dashboard UAT reviewer flagged two issues unrelated to the
dashboard itself in the cross-cutting "Other" row of
[`security-dashboard-uat-checklist.json`](../security-dashboard-uat/security-dashboard-uat-checklist.json):

> "Although it is desirable for most pages to have the column headers at
> the top, they should appear below the report title on the first page.
> Alternatively, we could have the report title repeat on each page. That
> would be acceptable, if we can't get the other way to work. One
> additional issue: the report title doesn't print in the PDF export."

The reviewer accepts either behavior — title on page 1 only with column
headers below it, **or** title repeated on every page. This set resolves
both complaints.

### Why the title is missing (initial hypothesis)

[`PdfExporter.cs:42`](../../../src/Libs/Dabbler.Reports.Export/PdfExporter.cs#L42)
exposes a `Title` property, but it is only assigned to PDF document
metadata — nothing in
[`PdfPageRenderer.cs`](../../../src/Libs/Dabbler.Reports.Export/PdfPageRenderer.cs)
draws it onto the content surface. If the report title is expected to
appear on the page, it must come from the composed `PageLayout`
elements — i.e., from the report definition's title/header band.

Session 1 confirms the hypothesis before any fix lands.

## Goals

1. The report title is visible on page 1 of the PDF export for both
   `OrdersByCustomer` and `SalesByProduct`.
2. Column headers on page 1 appear **below** the report title (preferred
   behavior), or the title repeats on every page (fallback, if the
   layout engine can't cleanly do the former).
3. A regression test asserts the title appears in the rendered PDF, so
   the problem can't silently return.

## Architecture Context

### Files likely to be touched

- `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Reports/OrdersByCustomer.razor`
- `src/Apps/AcmeCoffeeOnline.Web/Components/Pages/Reports/SalesByProduct.razor`
- Report definition files (`.rdf.json`) consumed by the two report pages
  — locate under `src/Apps/AcmeCoffeeOnline.Web/Reports/` or the
  `Dabbler.Reports` library; confirm in Session 1.
- `src/Libs/Dabbler.Reports.Export/PdfExporter.cs`
- `src/Libs/Dabbler.Reports.Export/PdfPageRenderer.cs`
- `src/Libs/Dabbler.Reports.Core/Layout/` — band composition lives here
  (exact file identified in Session 1).

### Known facts from earlier exploration

- `PdfExporter.ExportAsync` consumes a fully-composed
  `IReadOnlyList<PageLayout>` — the renderer does not add any title; it
  renders what's already in the page elements.
- `PdfExporter.Title` only flows into PDF metadata (`document.AddTitle`
  equivalent), not visible content.
- `DabblerReportPage` with `ShowPdfExport="true"` is the common host
  component wrapping both reports.

## Session Plan

### Session 1 of 2: Diagnose and decide

**Steps:**
1. Read `docs/planning/guiding-principles.md` and
   `docs/planning/lessons-learned.md`.
2. Locate the `.rdf.json` report definitions for OrdersByCustomer and
   SalesByProduct. Read their band layouts — does either define a
   Title/PageHeader/ReportHeader band that includes the title text?
3. Export a PDF from each report via the UI and inspect the layout:
   - Does page 1 include a title element that renders at zero height?
   - Is the title defined in a band that the composer drops when the
     body's row count pushes it off page 1?
4. Trace the band composer in `Dabbler.Reports.Core.Layout`. Identify
   whether it supports:
   - A "ReportHeader" band that only renders on page 1, with the
     ColumnHeader band positioned below it.
   - A "PageHeader" band that repeats on every page.
5. Pick the target behavior in priority order:
   a. **Preferred:** ReportHeader (title) on page 1 above a PageHeader
      (column headers) that repeats on every subsequent page.
   b. **Fallback:** PageHeader containing both title and column headers,
      repeating on every page.
6. Write up findings inline in the session-1 verification artifact — no
   code changes yet beyond what's needed to diagnose.
7. Build and test (no regressions). Log via `log.log_step()`.

**Creates:** Diagnosis notes (as part of the session-1 review artifact)
**Touches:** Read-only in session 1; code change deferred to session 2
**Ends with:** Target behavior chosen (a or b) and the one or two
files that need to change are identified.
**Progress keys:** `session-1/find-rdfs`, `session-1/inspect-pdf`,
`session-1/trace-composer`, `session-1/decision`,
`session-1/build-and-test`

---

### Session 2 of 2: Implement + regression test

**Steps:**
1. Read planning docs.
2. Apply the change decided in session 1:
   - **Path a:** edit the `.rdf.json` definitions so ReportHeader
     contains the title and ColumnHeader immediately follows; confirm
     the band composer keeps the ColumnHeader band on every page.
   - **Path b:** edit the `.rdf.json` definitions so PageHeader contains
     both title and column headers, and ensure the composer renders
     PageHeader at the top of every page.
3. Regenerate a PDF from each report and visually confirm title +
   column-header placement.
4. Add an end-to-end regression test using the existing
   `Dabbler.Reports.Export` test project (or the nearest equivalent):
   - Compose and export OrdersByCustomer with a tiny dataset.
   - Parse the output PDF and assert the title string appears on page 1.
   - Assert column headers appear on page 1 (and on page 2, under path
     b).
5. If session 1 uncovered that `PdfExporter.Title` should also be a
   fallback content title (not just metadata), gate that behavior
   behind a new opt-in property to avoid surprising other reports.
6. Build and test. Log via `log.log_step()`.

**Creates:** Regression test; updated `.rdf.json` definitions (and
possibly a small composer/renderer change)
**Touches:** Report definition files, possibly
`src/Libs/Dabbler.Reports.Core/Layout/` and
`src/Libs/Dabbler.Reports.Export/PdfExporter.cs`
**Ends with:** PDF export shows the title on page 1 with column
headers below it (or title on every page), and a regression test
prevents the problem from returning.
**Progress keys:** `session-2/apply-fix`, `session-2/visual-check`,
`session-2/regression-test`, `session-2/build-and-test`

---

## Human Re-run After Session 2

After session 2 completes, the reviewer re-exports PDFs from
`/reports/orders-by-customer` and `/reports/sales-by-product` and
confirms:

1. The report title is present on page 1.
2. Column headers appear under the title on page 1 — **or**, if fallback
   b was chosen, title and column headers appear together at the top of
   every page.
3. No regression in subsequent-page layout (column headers still appear
   on pages 2+, data rows not clipped).

If either check fails, capture the failure in a follow-up spec; do not
reopen this one.

## What This Set Preserves

| Existing Asset | Status |
|---|---|
| All existing Playwright tests | Must continue passing |
| All non-Playwright tests | Must continue passing |
| `PdfExporter` public API | Preserved (additive only if anything changes) |
| `PdfPageRenderer` internals | Preserved unless Session 1 identifies a renderer-level bug |
| Existing report definitions' data sections | Unchanged; only header bands may be edited |

## Risk Notes

- **Don't treat the symptom.** Do not stuff the title directly into
  `PdfPageRenderer` as a hard-coded content draw — that bypasses the
  composer and will diverge from the on-screen preview. The fix belongs
  in the report definition or the band composer.
- **Two reports, one fix.** Both `OrdersByCustomer` and `SalesByProduct`
  exhibit the bug per the UAT. Confirm whichever change is made applies
  to both, and that any future report defined via `DabblerReportPage`
  inherits the fix.
- **Regression test must hit real PDF bytes.** Per the
  [E2E testing standards memory](../../../.claude/projects/c--Users-denmi-source-repos-dabbler-platform/memory/feedback_e2e_testing_standards.md),
  the test should parse the actual exported PDF (even if only via string
  extraction) rather than asserting on intermediate layout objects.
