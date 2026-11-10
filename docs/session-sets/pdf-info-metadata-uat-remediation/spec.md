# PDF Info Metadata UAT Remediation Spec

> **Purpose:** Remediate the two findings from the one-shot human UAT
> that closed the `pdf-info-metadata-api` session set on 2026-04-22,
> and close the test-coverage gap that let the first finding ship.
> **Created:** 2026-04-22
> **Session Set:** `docs/session-sets/pdf-info-metadata-uat-remediation/`
> **Prerequisite:**
> [`pdf-info-metadata-api`](../pdf-info-metadata-api/change-log.md)
> session set complete (master commit `cfece43` shipped
> DabblerPdf 1.1.0 consumption; commit `033ffb4` shipped the Step 9
> reorganization). The `change-log.md` marks that set closed.
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Context

At the end of `pdf-info-metadata-api` Session 3, the human ran the
spec's one-shot UAT (re-export `/reports/orders-by-customer`, open in
Adobe Reader, confirm **File → Properties → Description** shows Title,
Author, and CreationDate) and reported two findings:

1. **CreationDate is present, but Title and Author are not.**
   Root cause identified: the production caller at
   [`DabblerReportPage.razor.cs:232`](../../../src/Libs/Dabbler.Reports.Blazor/DabblerReportPage.razor.cs#L232)
   constructs `new PdfExporter(_reportDefinition)` and never sets
   `Title` or `Author`. Session 3's regression test
   (`ExportedPdf_PopulatesInfoDictionaryWithTitleAndAuthor`) hard-coded
   both properties on the exporter instance, so it verified the
   exporter's wiring but never proved the production caller supplies
   values. The values are already in the RDF
   (`metadata.name`, `metadata.author` in
   [`orders-by-customer.rdf.json`](../../../src/Apps/AcmeCoffeeOnline.Web/Reports/orders-by-customer.rdf.json))
   and `ReportMetadata` exposes both as `string` properties.

2. **No title heading on the report page.** The
   [`reports-pdf-layout`](../reports-pdf-layout/change-log.md) fix
   ensures `ReportHeader` emits before `PageHeader` on page 1, and
   the RDF has a `reportHeader` band with a
   `StaticText value="Orders by Retail Business"` element (lines
   83–97 of the RDF). The session 3 test
   `ExportedPdf_ContainsTitleAndColumnHeaderText` still passes. Two
   distinct possibilities:

   - **Stale runtime.** The app the human tested against may be from
     before the DabblerPdf 1.1.0 bump + rebuild+restart, or from
     before the `reports-pdf-layout` fix shipped.
   - **Real regression specific to the production RDF.** The real
     `orders-by-customer.rdf.json` uses
     `defaultFontFamily: "Segoe UI"` and the title element has
     `styleName: "reportTitle"`. DabblerPdf natively supports
     Helvetica, Times New Roman, Courier (per the library's README);
     `"Segoe UI"` may silently fall back or fail to render. The
     synthetic test fixture used `"Helvetica"`, which would not
     surface this.

Finding 1 is clearly a code defect shipped by the `pdf-info-metadata-api`
set. Finding 2 needs investigation before deciding whether it is a
defect to fix or a non-issue.

## This Is Not Just A Fix — It Is A Test-Coverage Correction

The deeper issue behind Finding 1 is that the Session 3 regression
test verified the wrong thing. It proved the exporter respects
`Title`/`Author` **when set**, but not that the production caller
sets them. This is the exact failure mode
[`lessons-learned.md` → "Every UAT Checklist Item Must Be
Pre-Tested In Playwright Before The Human Runs The Checklist"](../../planning/lessons-learned.md)
warns about — the human found a gap the test should have found.

The remediation MUST include a caller-level test that drives the
production export path against the real RDF file, not a synthetic
fixture. Otherwise the next UAT catches the next drift.

## Session Set Configuration

```yaml
requiresUAT: false
requiresE2E: false
```

## Sessions
### Session 1 of 1 — Wire the caller, investigate Finding 2, close the coverage gap

**Location of work:** this repo only.

**Steps:**

1. Read `docs/planning/project-guidance.md` and
   `docs/planning/lessons-learned.md`. Special attention to:
   - Human UAT Philosophy → every functional claim has matching E2E
     coverage with identical parameters.
   - Convention → Workflow Expectations → the new cross-repo mirror
     convention and the summarization-vs-documentation convention
     (promoted 2026-04-22 as part of the prior set's Step 9).

2. **Wire Title and Author in the caller.** Update
   [`DabblerReportPage.razor.cs`](../../../src/Libs/Dabbler.Reports.Blazor/DabblerReportPage.razor.cs)
   `ExportToPdfAsync`:
   ```csharp
   var exporter = new PdfExporter(_reportDefinition)
   {
       Title = _reportDefinition.Metadata.Name,
       Author = _reportDefinition.Metadata.Author,
   };
   ```
   `ReportMetadata.Name` and `ReportMetadata.Author` are both
   non-nullable `string = string.Empty`; the exporter's
   `!string.IsNullOrWhiteSpace` guard handles the empty case
   (the `AddTitle`/`AddAuthor` methods in DabblerPdf 1.1.0
   also no-op on whitespace, so the defensive guard at both
   layers is belt-and-suspenders, not a bug).

3. **Close the test-coverage gap.** Add a new test that drives the
   real production caller (or the nearest testable seam) against
   the shipped `orders-by-customer.rdf.json`. Concretely:
   - If `DabblerReportPage.ExportToPdfAsync` is driveable without
     JS interop, call it directly against a bUnit-style host and
     assert `/Title (Orders by Retail Business)` and
     `/Author (AcmeCoffeeDistributors)` appear in the exported
     bytes' `/Info` dictionary.
   - Otherwise, factor the "build exporter from report definition"
     step into a small helper method on the page or a static
     factory, and test THAT helper with the loaded RDF. The goal
     is a test that would have caught the shipped defect — a test
     against the production data path, not a hard-coded synthetic
     fixture.

4. **Investigate Finding 2.** Write a transient integration test
   that:
   - Loads the real
     [`orders-by-customer.rdf.json`](../../../src/Apps/AcmeCoffeeOnline.Web/Reports/orders-by-customer.rdf.json).
   - Drives the composer and exporter end-to-end.
   - Inflates the resulting content streams and asserts the
     literal string `"Orders by Retail Business"` appears.

   Outcomes:
   - **If assertion passes:** Finding 2 is a stale-runtime artifact.
     Document the diagnosis in the session review (and in the change
     log as "no code fix — was stale runtime"); delete the transient
     test if it doesn't pull its weight, or promote it to permanent
     coverage for the real-RDF path alongside step 3's test.
   - **If assertion fails:** Finding 2 is a real regression. The
     likely root cause is font-family fallback (`"Segoe UI"`
     isn't in DabblerPdf's native support set). Decide scope:
     - Fix in-session if the fix is local and small (e.g., font
       fallback table in the renderer, or per-style font mapping).
     - If the fix needs broader library work (`../dabbler-pdf`),
       carve out a follow-up cross-repo set and mark Finding 2
       as **deferred with a specific follow-up spec path named
       here in the session review** — do not just drop it.

5. **Build and test.**
   ```bash
   dotnet build Dabbler.Platform.sln
   dotnet test Dabbler.Platform.sln --filter "FullyQualifiedName~PdfExport"
   dotnet test Dabbler.Platform.sln
   ```
   Playwright tests are expected to fail in fixture init unless
   Docker Desktop is up; that is orthogonal per CLAUDE.md. If the
   human wants Playwright green this session, bring the compose
   stacks up first. Otherwise disclose the environmental failure
   in the verification prompt.

6. **Human UAT (one-shot).** The reviewer re-exports
   `/reports/orders-by-customer` from a freshly rebuilt +
   restarted app and confirms:
   - File → Properties → Description shows `Title` =
     "Orders by Retail Business", `Author` =
     "AcmeCoffeeDistributors", and `CreationDate` present.
   - Page 1 renders the title heading above the column headers.

   If either fails, capture in a fresh follow-up spec — do not
   reopen this one. (Same rule as the originating set.)

7. **Generate `change-log.md`** via
   `route(task_type="summarization", max_tier=1)` per the new
   Convention promoted 2026-04-22. Include a "Disposition of prior
   set's UAT findings" section so the traceability from the
   original UAT → this remediation is clear.

8. **Cross-provider verification** via
   `route(task_type="session-verification")`. Include in the prompt
   the changed files, the new test, and an out-of-scope section
   naming the `pdf-info-metadata-api` change-log and the
   `reports-pdf-layout` change-log so the verifier does not hunt
   for context.

9. **Commit, push, send Pushover notification**, then run the
   Step 9 reorganization review.

**Creates:** `change-log.md`; at least one new caller-level test
that drives the production export path against the real RDF.
**Touches:**
`src/Libs/Dabbler.Reports.Blazor/DabblerReportPage.razor.cs`
(Title/Author wiring);
possibly a new integration-test file under
`tests/Dabbler.Reports.Blazor.Tests/` or
`tests/Dabbler.Reports.Export.Tests/`;
possibly a font-fallback fix under `src/Libs/Dabbler.Reports.Export/`
or a cross-repo follow-up spec for a library-side font fix,
depending on Step 4's outcome.
**Ends with:** A freshly exported PDF from
`/reports/orders-by-customer` has `Title`, `Author`, and
`CreationDate` populated in Adobe Reader's Document Properties,
and the title heading renders on page 1, proven by an automated
test that drives the production caller against the production RDF.
**Progress keys:** `session-1/read-planning`,
`session-1/wire-caller`, `session-1/caller-test`,
`session-1/finding-2-investigation`,
`session-1/finding-2-disposition`, `session-1/build-and-test`,
`session-1/change-log`, `session-1/verification`,
`session-1/commit-push-notify`, `session-1/reorg-review`.

---

## What This Set Preserves

| Existing Asset | Status |
|---|---|
| `PdfExporter` public API | Preserved; no signature changes |
| Session 3's `PdfExportTitleRegressionTests` (3 tests) | Kept as-is; the new caller-level test supplements, does not replace |
| Report-definition RDF files | Untouched |
| `DabblerPdf 1.1.0` package | Untouched; no library-side work unless Finding 2 root-cause forces it |

## Risk Notes

- **Stale-runtime false negative.** If Finding 2's investigation
  shows the title DOES render in a fresh build, be explicit in the
  change log that the original human observation was likely stale
  runtime. This is not a rebuke — it is accurate diagnosis. Do NOT
  ship a speculative "font fallback" fix just because it sounds
  plausible; only fix what the integration test shows is actually
  broken.
- **Cross-repo temptation.** If Finding 2 IS a real regression
  that needs a font-fallback fix in `../dabbler-pdf`, resist
  doing the library work in this repo's session. Carve a
  proper cross-repo follow-up set (see the new Cross-Repo Mirror
  Convention, promoted 2026-04-22) and queue that set; do not
  inline sibling-repo code edits.
- **Test-coverage regression.** The real lesson here is that
  Session 3's test was structured wrong (exporter-unit, not
  caller-level). The new test MUST drive the production call
  site or a very close proxy; otherwise we are still exposed
  to the same drift on the next field added.
