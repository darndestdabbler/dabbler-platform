# Verification Round 1



## Evaluation

### 1. Internal Consistency

**Method names, semantics, lifecycle rules** — The sketch is internally consistent:

- All five string methods follow the same `AddXxx(string)` pattern.
- `AddCreationDate()` / `AddCreationDate(DateTime)` / `SuppressCreationDate()` form a coherent trio with a clear "last write wins" rule.
- `DocumentMetadata` record has fields for every staged value plus the suppression flag, which is the minimal representation needed by `PdfWriter.Close()`.
- The `Close()` wire-up code correctly consults `metadata.CreationDate ?? (metadata.CreationDateSuppressed ? null : DateTime.UtcNow)`, which faithfully implements the three-state logic (explicit date, suppressed, or auto-now).

One thing to note: the `CreationDateSuppressed` field in `DocumentMetadata` is redundant when `CreationDate` is non-null (since "last call wins" means a subsequent `AddCreationDate` overrides suppression). But the wire-up code handles this correctly via the `??` chain, so the redundancy is harmless and arguably clearer.

**No inconsistencies found.**

### 2. Faithfulness to the Spec's Goals

The spec calls for five `/Info` keys (Title, Author, Subject, Keywords, Creator) plus CreationDate, additive, backward compatible.

- ✅ All five keys are present.
- ✅ CreationDate is covered with auto-now default + explicit override + suppression opt-out.
- ✅ Additive: no existing API is removed or changed.
- ✅ Backward compatible: if nothing is called, the only new key is `/CreationDate` (auto-now). If even that is unwanted, `SuppressCreationDate()` restores the 1.0.0 shape exactly.
- ✅ `/Producer` remains hardcoded — not exposed for mutation.

### 3. Faithfulness to DabblerPdf's "Scoped OpenPDF Port" Lineage

- ✅ Method names (`AddTitle`, `AddAuthor`, etc.) are direct PascalCase translations of OpenPDF's `addTitle`, `addAuthor`, etc.
- ✅ Staging semantics (buffer then flush at `Close()`) match OpenPDF's model.
- ✅ `PdfDate.Format` implements PDF 1.7 §7.9.4, same as OpenPDF's `PdfDate` class.
- ✅ ModDate reserved but not written — avoids scope creep while maintaining parity with OpenPDF's constant set.
- ✅ No XMP, no structure trees — stays within the "scoped" boundary.

### 4. C# Idiomaticity

- ✅ `ArgumentNullException.ThrowIfNull` — idiomatic .NET 6+ pattern.
- ✅ `sealed record` for `DocumentMetadata` — appropriate for an internal data carrier.
- ✅ Pattern matching (`is { Length: > 0 }`) in the wire-up — idiomatic modern C#.
- ✅ `internal` visibility for `GetMetadata()` and `DocumentMetadata` — correct access boundary.
- ✅ `partial class` for additive file separation — standard practice.

**Minor observation:** The "whitespace-only is a silent no-op" rule is somewhat surprising in .NET (most APIs would either trim or throw). But the decision is explicitly documented and the rationale (don't overwrite a good value with whitespace) is defensible.

### 5. Implementability in the Three-Session Plan

- ✅ Session 2 scope is well-bounded: 6 PdfName constants, ~80 lines in Document, ~15 lines in PdfWriter.Close(), ~30-line PdfDate helper, tests, CHANGELOG, NuGet pack.
- ✅ No missing abstractions — `PdfDictionary.Put`, `PdfString`, `PdfName` are all already available.
- ✅ No cross-cutting concerns — the change is localized to Document + PdfWriter.Close().
- ✅ Session 3 scope (wire PdfExporter.Title/Author through) is trivial once the NuGet package is available.
- ✅ Risk call-outs are explicit and actionable.

**No blockers identified.**

### 6. Clarity for Session 2 Implementation

- ✅ Every public method is named with signature and XML-doc-level description.
- ✅ Validation rules are enumerated (null → throw, whitespace → no-op, after-Close → throw).
- ✅ The exact `PdfWriter.Close()` replacement code is provided.
- ✅ `PdfDate.Format` is fully implemented in the sketch with examples.
- ✅ Test expectations are specified (table-driven: UTC, positive offset, negative offset, DST, Unspecified).
- ✅ File placement is specified (PdfDate beside CoordinateTranslator; tests in DabblerPdf.Tests/).
- ✅ Version target (1.1.0) and publish target (nupkg/) are explicit.

**One minor gap:** The sketch doesn't specify the exact backing fields in `Document` (e.g., `private string? _title;`, `private bool _creationDateSuppressed;`), but these are trivially derivable from the `DocumentMetadata` record shape and the stated behavior rules. This is appropriate for a design session — it leaves implementation latitude without ambiguity.

---

## Issues

### Minor Issue 1 — `AddCreationDate()` default vs. auto-now at `Close()` creates a subtle semantic overlap

**Text reference:** "Stages CreationDate as UtcNow at call time" vs. "If never called, Close() auto-stages CreationDate = UtcNow."

The difference is *when* UtcNow is captured (at `AddCreationDate()` call time vs. at `Close()` time). This is documented and intentional, but the practical difference is typically sub-second. Session 2 should add a brief code comment at the `Close()` fallback noting this distinction so future maintainers don't collapse the two paths.

**Severity:** Minor — no design change needed; a comment suffices.

### Minor Issue 2 — `DateTimeKind.Unspecified` treated as local time lacks explicit justification

**Text reference:** "AddCreationDate with DateTimeKind.Local or Unspecified => stored as-is; the PdfDate helper formats with the local offset."

This is a reasonable default (and matches `TimeZoneInfo.Local.GetUtcOffset()` behavior), but `Unspecified` is a common foot-gun in .NET. The design doesn't consider whether to log a warning or document this more prominently. Acceptable for an internal library, but Session 2's test coverage should include an `Unspecified` input to lock this behavior.

**Severity:** Minor — already listed in test plan ("DateTimeKind.Unspecified treated as local"), so covered.

### Minor Issue 3 — ModDate reservation without a suppression mechanism

**Text reference:** "ModDate is added as a constant but not written by 1.1.0 — reserving the name avoids a second round-trip."

This is fine. Just noting that if ModDate is added in a future version, it will need the same suppression pattern as CreationDate. The reservation doesn't create any current issue.

**Severity:** Minor — future concern only.

---

## VERDICT: VERIFIED

The design is internally consistent, faithful to spec goals and OpenPDF lineage, idiomatic in C#, and specified with sufficient precision for Session 2 to implement mechanically. The three minor issues noted are documentation/comment-level concerns, none of which require design changes.
