# Change Log - pdf-info-metadata-api

## Summary
This set delivered support for adding standard PDF Info Dictionary metadata (Title, Author, CreationDate, etc.) to exported PDF files. This enhances document discoverability and usability in tools that consume PDF metadata, affecting all PDF exports from the platform.

## Sessions
### Session 1 of 3: Design (2026-04-18)
### Session 2 of 3: DabblerPdf 1.1.0 library implementation (2026-04-21, in sibling repo ../dabbler-pdf)
### Session 3 of 3: Platform-side consumption (today, 2026-04-22)

## Files Changed (Session 3 only)
- Directory.Packages.props
- src/Libs/Dabbler.Reports.Export/PdfExporter.cs
- tests/Dabbler.Reports.Blazor.Tests/PdfExportTitleRegressionTests.cs

## Behavioural notes
- PDFs produced by PdfExporter will now expose Title and Author in their document properties if these values are provided.
- All new PDFs will automatically include a /CreationDate entry in their metadata.
- Downstream tools relying on blank Title/Author metadata may exhibit altered behavior.

## Related artifacts
- Pointer to the sibling repo's CHANGELOG 1.1.0 entry: ../dabbler-pdf/CHANGELOG.md (for 1.1.0, 2026-04-21)
- Pointer to session-reviews/ for raw verifier output: session-reviews/session-001.md, session-reviews/session-002.md
