# End-of-set reorganization review — access-migration-generator-consumption

> Step 9 (last session of the set). Per the end-of-set reorg expectation: the final
> session must propose (or decline) reorganization of `project-guidance.md` +
> `lessons-learned.md`, applying the two-context promotion threshold.

## Decision: one promotion + one new lesson (executed this session)

### Promoted (two-context threshold met)
**"Author Template Content And Consumer Smoke Test In The Same Session"** →
generalized and promoted to a `project-guidance.md` → Conventions → Workflow
Expectations convention: *"Ship a checkout-free consumer-acquisition smoke gate in
the same session that publishes or changes a distributable artifact."*

- **Context 1:** `packaging-and-template-readiness` Sessions 8 & 9 — template content
  authored without a consumer smoke; 7 quality issues found late.
- **Context 2 (this set):** `access-migration-generator-consumption` Session 2 — the
  `Dabbler.CrudSlice.Tool` consumer-validation gate acquired the *packaged tool*
  checkout-free (`dotnet tool install --tool-path … --add-source … --version <pinned>`)
  and built a generated CRUD slice green, in the same session that made the tool packable.
- The original template-only lesson is collapsed to a reference pointing at the new
  convention (original preserved for context), matching the file's established collapse
  pattern. The generalization is the substantive change: the principle now covers
  `dotnet tool` and NuGet acquisition, not just `dotnet new` templates.

### New lesson captured (single context — not yet promotion-eligible)
**"Greenfield Matrix Runs: Pre-Create `matrix-run/` And Restore Committed Golden Output
Before Measuring."** Two operational frictions hit during this set's Step-6 matrix run
(the matrix tool does not `mkdir` its `--out` dir; committed `pack-output/` golden output
poisons the source diff if regenerated). This repo is the LEAD greenfield pilot signal
and will run more source-bearing matrices, so the tactic is recorded now; it stays a
lesson until a second context confirms it.

## Declined
- **No broader structural reorg.** `project-guidance.md` and `lessons-learned.md` are
  well-organized; no sprawl warranting a split or re-section this set.
- **No spec-flag correction.** The spec's `requiresUAT: false` / `requiresE2E: false` were
  correct (packaging/distribution, no user-visible UI surface); the consumer-validation
  gate was the right acceptance proof.

## Set retrospective (for the corpus)
- The two-session shape (decide → implement+prove) worked cleanly; Session 1's Decision 5
  (two-install acquisition) correctly pre-bound the Session 2 gate to install **both** the
  templates and the tool, which is the single most likely way a "green" packaging decision
  breaks on a real clean machine.
- Greenfield finding-power (LEAD signal): 0 TP / 7 FP on this small, clean, packaging-leaning
  source diff; all FPs were push-elision or pull-out-of-sandbox. One data point shipped to
  the canonical Set 075 corpus; the comparative verdict remains the synthesis set's job.
