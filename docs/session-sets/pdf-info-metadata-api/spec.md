# PDF Info Metadata API Spec

> **Purpose:** Add a public API to DabblerPdf for setting the standard
> PDF document information (`/Info`) dictionary fields — Title, Author,
> Subject, Keywords, Creator — and wire `Dabbler.Reports.Export.PdfExporter`
> to forward its existing `Title`/`Author` properties through. This closes
> the deferred Issue 2 from the
> [`reports-pdf-layout`](../reports-pdf-layout/change-log.md) session set.
> **Created:** 2026-04-18
> **Session Set:** `docs/session-sets/pdf-info-metadata-api/`
> **Prerequisite:** The `reports-pdf-layout` session set is complete
> (visible report-title fix already shipped on master as `2b657b7`).
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Context

The `reports-pdf-layout` spec closed the visible-title bug on page 1,
but flagged a second, orthogonal issue surfaced by the Session 1
verifier:

> `PdfExporter` declares `Title` and `Author` properties, but neither
> value is ever passed into the PDF document. `PdfWriter.Close()`
> hardcodes only `Producer` into the `/Info` dictionary. The underlying
> `DabblerPdf 1.0.0` library exposes no public API for setting document
> metadata.

This means any PDF opened outside the report viewer (Adobe Reader,
browser PDF viewers, OS file previews) shows a blank "Title" /
"Author" in its properties dialog — and any tooling that indexes PDFs
by metadata (SharePoint, content archives, e-mail search) has nothing
to work with. It is also a foot-gun: `PdfExporter.Title` looks like it
does something but silently no-ops.

### Current state of the code (as of 2026-04-18)

- [`../dabbler-pdf/src/DabblerPdf/Pdf/PdfWriter.cs`](../../../../dabbler-pdf/src/DabblerPdf/Pdf/PdfWriter.cs) line 261–264 —
  hardcodes `Producer` only:
  ```csharp
  var info = new PdfDictionary();
  info.Put(PdfName.Producer, new PdfString("DabblerPdf.NET"));
  _infoRef = _body.Add(info, _outputStream);
  ```
- [`../dabbler-pdf/src/DabblerPdf/Pdf/PdfName.cs`](../../../../dabbler-pdf/src/DabblerPdf/Pdf/PdfName.cs) —
  defines `Info`, `Producer`, `CreationDate`. Missing: `Title`,
  `Author`, `Subject`, `Keywords`, `Creator`, `ModDate`.
- [`../dabbler-pdf/src/DabblerPdf/Document/Document.cs`](../../../../dabbler-pdf/src/DabblerPdf/Document/Document.cs) —
  no metadata API of any kind.
- [`src/Libs/Dabbler.Reports.Export/PdfExporter.cs:42–47`](../../../src/Libs/Dabbler.Reports.Export/PdfExporter.cs#L42-L47) —
  declares `Title` and `Author`; neither is read anywhere in the
  project.
- [`Directory.Packages.props`](../../../Directory.Packages.props) —
  pins `DabblerPdf` at `1.0.0`. Will need a bump once DabblerPdf ships
  a release containing the new API.

### Why this is cross-repo

The code change lives in `../dabbler-pdf` (separate git repo, separate
NuGet package, separate session-set workflow). The consumption change
lives in this repo. This spec coordinates both, but treats the
DabblerPdf work as its own unit: we will **also** create a companion
session set in `../dabbler-pdf/docs/session-sets/pdf-info-metadata-api/`
that owns the library-side sessions, and link the two. This keeps each
repo's session-log authoritative for its own code.

## Goals

1. `DabblerPdf` exposes a public API for setting `/Info` fields Title,
   Author, Subject, Keywords, and Creator. Values are written into the
   document's info dictionary when the document is closed.
2. Unsetting / leaving a field null writes nothing (backward compatible
   with `DabblerPdf 1.0.0` output — existing PDFs round-trip
   unchanged).
3. `PdfExporter.Title` and `PdfExporter.Author` in `Dabbler.Reports.Export`
   actually flow to the generated PDF.
4. A regression test in this repo opens the exported PDF bytes, locates
   the `/Info` dictionary, and asserts the Title and Author strings
   appear in it. Cannot be proved only at the `DabblerPdf.Document`
   level.
5. `CreationDate` is written automatically (set to `DateTime.UtcNow` at
   `Close()` time) unless the caller opts out or overrides it.
   Rationale: modern PDF consumers expect this; it is cheap to produce;
   leaving it blank in 2026 is a smell.

## Design Exploration (Session 1 deliverable)

The core design question is the public API shape. Two viable options —
Session 1 must pick one before Session 2 implements it.

### Option A — OpenPDF-style `AddXxx(string)` methods

```csharp
document.AddTitle("Orders by Retail Business");
document.AddAuthor("AcmeCoffeeDistributors");
document.AddSubject("Orders report");
document.AddKeywords("orders, report, sales");
document.AddCreator("AcmeCoffeeOnline");
// CreationDate is set automatically at Close() time.
document.AddCreationDate(new DateTime(2026, 4, 18)); // optional override
```

**Pros:**
- Matches DabblerPdf's lineage — the library is a scoped port of
  OpenPDF, which uses `addTitle`, `addAuthor`, etc. Consumers porting
  from iText / OpenPDF Java will read this idiomatically.
- Natural place to validate / normalise the input (trim, reject nulls,
  reject control characters).
- Makes it clear metadata is staged, not a persistent property — you
  "add" it and it is written on close.

**Cons:**
- Less idiomatic for .NET consumers — `SetTitle(string)` or a plain
  property is more conventional.
- Verbose compared to an object-initialiser style.

### Option B — Plain properties on `Document`

```csharp
var document = new Document(pageSize)
{
    Title = "Orders by Retail Business",
    Author = "AcmeCoffeeDistributors",
    Subject = "Orders report",
    Keywords = "orders, report, sales",
    Creator = "AcmeCoffeeOnline",
    // CreationDate defaulted; override if needed
};
```

**Pros:**
- Idiomatic .NET. Works naturally in object initialisers, which is the
  dominant style elsewhere in the Dabbler stack.
- Trivial to set from a settings class or DI configuration.

**Cons:**
- Diverges from OpenPDF naming. Anyone cross-referencing the upstream
  OpenPDF docs will have a small translation layer.
- Settable at any time after construction, even after `Close()` — need
  to decide whether to ignore, throw, or silently succeed in that
  case.

### Recommendation

Lean toward **Option A** for the DabblerPdf public API (the port
already follows OpenPDF naming for `PdfWriter.GetInstance`,
`PdfContentByte.ShowText`, etc., and Title/Author have exactly the
same dictionary-key semantics OpenPDF uses), and keep a thin
`Document.Metadata` accessor for tests that want to read back what was
staged. But this is explicitly a Session 1 discussion point —
[`../dabbler-pdf/CLAUDE.md`](../../../../dabbler-pdf/CLAUDE.md)'s
stated lineage is the primary argument; if Session 1 concludes that
the .NET idiom wins, switch to Option B without penalty.

Either way, the wire-up inside `PdfWriter.Close()` is identical: read
whatever the Document staged, put non-null values into the `/Info`
dictionary next to `Producer`.

## Architecture Context

### Repositories and folders touched

- `../dabbler-pdf/` (sibling repo, separate git history, ships as
  NuGet package `DabblerPdf`):
  - `src/DabblerPdf/Pdf/PdfName.cs` — add missing metadata key
    constants.
  - `src/DabblerPdf/Document/Document.cs` — add metadata API (shape
    decided in Session 1).
  - `src/DabblerPdf/Pdf/PdfWriter.cs` — extend `Close()` to write
    staged metadata into the `/Info` dictionary.
  - `tests/DabblerPdf.Tests/` — unit tests parsing `/Info` out of the
    generated PDF bytes.
  - `CHANGELOG.md` — add a `1.1.0` entry.
  - `docs/session-sets/pdf-info-metadata-api/` — companion session set
    for the library-side sessions.
- `dabbler-platform/` (this repo):
  - `Directory.Packages.props` — bump `DabblerPdf` from `1.0.0` to
    `1.1.0` once the package is published.
  - `src/Libs/Dabbler.Reports.Export/PdfExporter.cs` — forward
    `Title`/`Author` (and optionally `Subject`/`Keywords`/`Creator`) to
    the `DabblerPdf.Document` before `document.Close()`.
  - `tests/Dabbler.Reports.Blazor.Tests/PdfExportTitleRegressionTests.cs` —
    extend to assert that the exported PDF's `/Info` dictionary
    contains the Title and Author strings.

### Known constraints

- PDF `/Info` strings are `PdfString` values. DabblerPdf currently has
  `PdfString` and uses it for `Producer`, so no new string-encoding
  work is needed.
- PDF spec-compliant date strings use the format
  `D:YYYYMMDDHHmmSS+HH'mm'` (or `Z` for UTC). A helper in
  `DabblerPdf.Utilities` should own this to keep `PdfWriter.Close()`
  readable.
- Session 1 verification for `reports-pdf-layout` flagged the
  `PdfExporter.Title` / `PdfExporter.Author` properties as dead;
  consumers may assume they already work. Once this ships, behaviour
  changes silently — call that out in the change log so anyone
  re-testing sees the new Title appear in Adobe Reader / browser PDF
  properties.

## Session Plan

### Session 1 of 3: Explore design, decide API shape, plan implementation

**Location of work:** `../dabbler-pdf/` is read-only in this session
(diagnosis / API sketch only). No code changes land in either repo.

**Steps:**
1. Read `docs/planning/project-guidance.md` and
   `docs/planning/lessons-learned.md`.
2. Read `../dabbler-pdf/CLAUDE.md` to confirm the DabblerPdf
   session-set workflow and code-style rules.
3. Re-read `PdfWriter.cs` (focus: `Close()` and existing info-dict
   path), `Document.cs` (focus: lifecycle, `Open`/`NewPage`/`Close`),
   and `PdfName.cs`.
4. Survey upstream OpenPDF's `com.lowagie.text.Document.addTitle` /
   `addAuthor` / `addSubject` / `addKeywords` / `addCreator` /
   `addCreationDate` signatures and behavioural expectations, so the
   port stays faithful where faithfulness is cheap.
5. Evaluate Options A and B side by side against: OpenPDF lineage,
   .NET idiomaticity, testability, and forward-compatibility
   (room for richer metadata later — XMP, custom keys).
6. **Decide:** Option A or Option B; name each public member; decide
   `CreationDate` default behaviour (auto-now vs explicit-only);
   decide validation (silently ignore null, throw on empty string?);
   decide whether `Close()` auto-stages or requires caller action.
7. Sketch the resulting API in the Session 1 verification artifact —
   enough that Session 2 implements it mechanically.
8. Confirm the companion `../dabbler-pdf/docs/session-sets/pdf-info-metadata-api/spec.md`
   plan (placeholder for now — Session 2 owns creating it). Decide
   whether the companion set has 1 session or 2.
9. Build and test (should pass untouched). Log via `log.log_step()`.

**Creates:** Design sketch embedded in
`session-reviews/session-001.md` (the verifier's raw output will
validate the choice).
**Touches:** Read-only across both repos.
**Ends with:** A chosen API shape, named members, and a go/no-go
decision for Session 2.
**Progress keys:** `session-1/read-planning`, `session-1/read-dabbler-pdf`,
`session-1/survey-openpdf`, `session-1/compare-options`,
`session-1/decide`, `session-1/api-sketch`, `session-1/build-and-test`

---

### Session 2 of 3: Implement the API in DabblerPdf, ship as 1.1.0

**Location of work:** `../dabbler-pdf/` (primary). All commits in
Session 2 go to the DabblerPdf repo.

**Steps:**
1. Read planning docs in both repos. Create the companion session set
   `../dabbler-pdf/docs/session-sets/pdf-info-metadata-api/` with its
   own `spec.md` (mirroring this plan from DabblerPdf's perspective)
   and `activity-log.json`.
2. Add missing `PdfName` constants: `Title`, `Author`, `Subject`,
   `Keywords`, `Creator`, `ModDate`.
3. Add the metadata API to `Document` per Session 1's decision.
4. Extend `PdfWriter.Close()` to read staged metadata from the
   `Document` and `info.Put(...)` each non-null field before the
   existing `Producer` line. Add `CreationDate` using the new
   date-formatting helper.
5. Add a PDF date-formatting helper under
   `DabblerPdf.Utilities` with a small table-driven test.
6. Add unit tests in `tests/DabblerPdf.Tests/` that:
   - Build a single-page document, set Title/Author/Subject, close,
     and locate the `/Info` indirect object in the output bytes.
   - Assert each expected key/value pair is present (using the same
     decompression strategy the dabbler-platform regression test
     already uses for content streams, if the info dict is in a
     compressed stream — it usually isn't).
   - Assert that when no metadata is staged, only `Producer` appears
     (backward-compat guarantee).
   - Assert that `CreationDate` is present and parseable.
7. Build, test. Update `CHANGELOG.md` with a `1.1.0` entry describing
   the new API. Pack `DabblerPdf.1.1.0.nupkg` into `../dabbler-pdf/nupkg/`.
8. Log via `log.log_step()`. Run cross-provider verification as usual.

**Creates:**
- `../dabbler-pdf/docs/session-sets/pdf-info-metadata-api/spec.md` +
  `activity-log.json`
- New/modified source files in `DabblerPdf.Pdf` and `DabblerPdf.Document`
- At least one new test file under `tests/DabblerPdf.Tests/`
- `../dabbler-pdf/nupkg/DabblerPdf.1.1.0.nupkg` (+ `.snupkg`)
- `CHANGELOG.md` 1.1.0 entry
**Touches:** The DabblerPdf sibling repo only.
**Ends with:** DabblerPdf 1.1.0 packed, tests green, cross-provider
verified.
**Progress keys:** `session-2/companion-set`, `session-2/pdfnames`,
`session-2/document-api`, `session-2/writer-close`,
`session-2/date-helper`, `session-2/tests`,
`session-2/changelog-pack`, `session-2/build-and-test`

---

### Session 3 of 3: Consume the new API in dabbler-platform, regression-test end-to-end

**Location of work:** `dabbler-platform/` (this repo).

**Steps:**
1. Read planning docs in this repo.
2. Bump `Directory.Packages.props` from `DabblerPdf 1.0.0` to `1.1.0`.
   If the package is resolved via a local folder feed, confirm
   the new nupkg is discovered; otherwise extend the feed
   configuration.
3. Update `PdfExporter.ExportAsync` to forward `Title` and `Author`
   (and optionally `Subject`, `Keywords`, `Creator` — add properties
   for those if Session 1 decides it is cheap) to the DabblerPdf
   `Document` before `document.Close()`.
4. Extend
   `tests/Dabbler.Reports.Blazor.Tests/PdfExportTitleRegressionTests.cs`
   with a new test: export a PDF with `Title = "Orders by Retail Business"`
   and `Author = "AcmeCoffeeDistributors"`, then parse the output
   bytes, locate the `/Info` dictionary, and assert both strings are
   present. Reuse the existing `ZLibStream` helper pattern where
   relevant.
5. Build, test, cross-provider verify.
6. If anything downstream depends on the old no-op behaviour (e.g., a
   test that expected the Title to be absent), update it and note the
   behaviour change in the session review.
7. Generate `change-log.md` via `route(task_type="documentation")`
   since this is the last session.

**Creates:** Additional regression test; updated change log.
**Touches:** `Directory.Packages.props`,
`src/Libs/Dabbler.Reports.Export/PdfExporter.cs`,
`tests/Dabbler.Reports.Blazor.Tests/PdfExportTitleRegressionTests.cs`.
**Ends with:** An exported PDF from `/reports/orders-by-customer`
shows `Title` and `Author` in Adobe Reader's document properties
dialog, proven by automated coverage.
**Progress keys:** `session-3/bump-package`, `session-3/wire-exporter`,
`session-3/regression-test`, `session-3/build-and-test`,
`session-3/change-log`

---

## Human UAT After Session 3

After Session 3 ships, the reviewer:

1. Re-exports a PDF from `/reports/orders-by-customer`.
2. Opens it in Adobe Reader (or equivalent) and checks
   **File → Properties → Description**.
3. Confirms `Title` shows the report name and `Author` shows
   `AcmeCoffeeDistributors` (or whatever the exporter passes).
4. Confirms `CreationDate` shows today's date/time.
5. Optionally spot-checks a different report.

If any of those fields is blank or wrong, capture the failure in a
follow-up spec; do not reopen this one.

## What This Set Preserves

| Existing Asset | Status |
|---|---|
| `DabblerPdf` public API surface | Preserved; Session 2 is purely additive |
| All `DabblerPdf.Tests` | Must continue passing |
| `PdfExporter` public API | Preserved; new uses of existing `Title`/`Author` properties only |
| All `Dabbler.Reports.Blazor.Tests` | Must continue passing (including the 5 from `reports-pdf-layout`) |
| Existing DabblerPdf consumers outside this stack | Backward compatible — output with no metadata staged still contains only `/Producer` |

## Risk Notes

- **Cross-repo coordination.** Session 2 publishes a new DabblerPdf
  version; Session 3 consumes it. If Session 3 runs before Session 2's
  nupkg actually lands on the feed dabbler-platform uses, restore
  fails. Session 2 must finish the pack step and Session 3 must
  confirm the nupkg is reachable before editing
  `Directory.Packages.props`.
- **Silent behaviour change.** Once `PdfExporter` starts writing Title
  and Author, any downstream system that currently assumes those
  fields are blank (e.g., a DMS that indexes on a stable filename
  because the PDF Title was always empty) may behave differently. Call
  this out in the final change log.
- **Date helper correctness.** PDF date strings have a strict format
  with apostrophe-separated timezone offsets (`D:20260418120000+00'00'`).
  Incorrect formatting causes some PDF readers to display
  "CreationDate" as a raw string or ignore it entirely. The test in
  Session 2 must assert the formatted date round-trips through a known
  parser or matches a precise regex.
- **Do not leak the lineage.** The DabblerPdf CLAUDE.md emphasises
  that this is a *scoped* port of OpenPDF — do not pull in XMP,
  structure trees, or any other OpenPDF feature as a drive-by. Stick
  to the six `/Info` keys in scope.
- **Regression test must hit real PDF bytes.** Per the
  [E2E testing standards memory](../../../.claude/projects/c--Users-denmi-source-repos-dabbler-platform/memory/feedback_e2e_testing_standards.md),
  the dabbler-platform Session 3 test should parse the actual exported
  PDF `/Info` dictionary rather than asserting on intermediate
  DabblerPdf objects.
