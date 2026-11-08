# UAT DSL Compiler (W0) Spec

> **Purpose:** Build a formal ANTLR-based grammar, lexer, parser, AST,
> emitter framework, and CLI for the UAT DSL — packaged as a publishable,
> framework-agnostic library in a new sibling repo. The compiler replaces
> the original W2 plan's LLM-driven naive translation layer with a
> deterministic, fast, zero-cost compilation pipeline. The grammar is the
> documentation; the compiler is the validator; the emitter is the test
> generator. Reusable across other AcmeCoffeeOnline-style apps and across
> target frameworks (the initial emitter targets Playwright C# / Blazor;
> additional emitters — Playwright Python, Cypress, Selenium — are
> additive without grammar changes).
> **Created:** 2026-04-28
> **Session Set:** `docs/session-sets/uat-dsl-compiler/`
> **Working repository:** `../dabbler-uat-dsl/` (new sibling repo, created
> in Session 1)
> **Prerequisite:** None. This is the foundational set of the four-workstream
> sequence (W0 → W1 → W2 → W3).
> **Workflow:** Orchestrator → AI Router → Cross-provider verification

---

## Session Set Configuration

```yaml
requiresUAT: false
requiresE2E: false
uatScope: none
```

> Rationale: This set creates a new sibling repo containing a library
> (grammar, parser, AST, emitters, CLI) plus one small touch to
> `docs/planning/project-guidance.md` in dabbler-platform. No Blazor UI,
> API endpoint, or user-facing behavior changes. Per the platform addendum,
> tooling and library sets confined to non-UI surfaces are
> `requiresUAT: false` / `requiresE2E: false`. The set's quality bar is:
> grammar accepts every example from the DSL definition and rejects every
> hand-crafted invalid case; emitter produces compilable C# that calls
> `PlaywrightFixture` correctly; CLI runs end-to-end against the
> remediation checklist (W1 backfill).

---

## Cross-repo working notes

This set is unusual in that most of its work lives in a new sibling repo
rather than in `dabbler-platform`. Convention for this set:

- **Session orchestration** (this spec, activity-log, session-state,
  ai-assignment, session-reviews, change-log) lives in
  `dabbler-platform/docs/session-sets/uat-dsl-compiler/` per the standard
  workflow.
- **Code work** lives in `../dabbler-uat-dsl/` (created in Session 1).
- **Commits** happen in both repos: each session commits to whichever
  repo(s) it touched. The session's commit message in dabbler-platform
  references the corresponding commit SHA in dabbler-uat-dsl when
  relevant.
- **Build verification** runs in `../dabbler-uat-dsl/`:
  ```bash
  cd ../dabbler-uat-dsl
  dotnet build && dotnet test
  ```
  The dabbler-platform `Dabbler.Platform.sln` does not consume the
  library yet — that consumption happens in W1 / W2.
- **Cross-provider verification** invocations include both repos'
  changes when relevant.

---

## Project Overview

### What this set delivers (in `../dabbler-uat-dsl/`)

1. **`grammar/DabblerUatDsl.g4`** — the canonical ANTLR4 grammar for the
   UAT DSL. Covers all verbs in `project-guidance.md` plus extended
   structured `VERIFY` forms.

2. **`src/Dabbler.Uat.Dsl.Core/`** — .NET 8 class library:
   - ANTLR-generated lexer + parser (build-time generation via
     `Antlr4BuildTasks`).
   - AST node types (`IUatStep` + record-based concrete steps per verb).
   - `UatChecklistParser` facade that reads a UAT checklist JSON and
     returns `IReadOnlyList<IUatStep>`.
   - `IUatChecklistEmitter<TOutput>` interface for code generators.
   - `ICatalogAdapter` interface + `JsonCatalogAdapter` implementation.
   - `SemanticValidator` — given AST + catalog, emits
     `IReadOnlyList<ValidationError>`.

3. **`src/Dabbler.Uat.Dsl.Emitters.Playwright.CSharp/`** — emitter
   project: produces C# code that calls a generic `IPlaywrightUatFixture`
   interface. The emitted code is framework-shape-compatible with
   `PlaywrightFixture` in dabbler-platform but does not depend on it.

4. **`src/Dabbler.Uat.Dsl.Cli/`** — console app:
   ```bash
   dabbler-uat-dsl compile <checklist.json> \
     --catalog <catalog.json> \
     --emitter playwright-csharp \
     --output <test.cs>
   dabbler-uat-dsl validate <checklist.json> --catalog <catalog.json>
   ```

5. **`tests/Dabbler.Uat.Dsl.Core.Tests/`** — xUnit test project covering
   grammar (positive and negative cases per verb), AST round-trip,
   semantic validator, emitter output.

6. **`schema/component-catalog.json`** — the catalog JSON schema (moved
   here because the compiler is the canonical consumer; W1 produces data
   conforming to this schema).

7. **NuGet packaging metadata** for `Dabbler.Uat.Dsl.Core`,
   `Dabbler.Uat.Dsl.Emitters.Playwright.CSharp`, and the CLI tool.
   Initial publish to a private feed or local pack; public NuGet publish
   deferred until the API is exercised by W1 / W2.

8. **`README.md`, `CLAUDE.md`, `.gitignore`, `.editorconfig`** for the
   new repo. The CLAUDE.md includes the same AI-router import bootstrap
   pattern as dabbler-platform's, scoped to this repo's needs.

### What this set delivers (in `dabbler-platform`)

Only one touch:

- **`docs/planning/project-guidance.md`** — UAT/E2E DSL section update:
  - State that DSL values are quoted string literals.
  - List the structured VERIFY forms (see Design Commitments below).
  - Reference the canonical grammar at
    `https://github.com/<owner>/dabbler-uat-dsl/blob/main/grammar/DabblerUatDsl.g4`
    (placeholder URL until the repo is published).

### What this set does not do

- Does not consume the compiler from dabbler-platform — that's W1 / W2.
- Does not generate the catalog (W1 does that).
- Does not run UAT checklists end-to-end against the live app
  (W2 does that).
- Does not add additional emitters beyond Playwright C#. Playwright
  Python and other targets are explicitly out of scope; the emitter
  framework is designed to make them additive, but nobody implements
  them in this set.
- Does not publish to public NuGet. Local pack + sibling-repo consumption
  is the v1 distribution model.

---

## Design Commitments (Decided Up Front)

### Repo name and library namespace

- **Repo:** `dabbler-uat-dsl` (sibling to dabbler-platform, matching the
  `dabbler-X` sibling convention used by `dabbler-ai-orchestration`).
- **Root namespace:** `Dabbler.Uat.Dsl` (matching the in-platform lib
  naming convention `Dabbler.X.Y`).
- **NuGet package IDs:** `Dabbler.Uat.Dsl.Core`,
  `Dabbler.Uat.Dsl.Emitters.Playwright.CSharp`, `Dabbler.Uat.Dsl.Cli`.

If the user prefers a different name, the rename is mechanical and can
happen at any session-end before the first NuGet publish.

### ANTLR4 over hand-rolled regex

ANTLR4 is the parser-generator. Rationale: deterministic tokenization
eliminates the regex edge cases (keywords inside quoted strings,
overlapping verb prefixes, optional clauses with conjunctions). The
grammar is published alongside the library and is the canonical
reference. ANTLR4's C# target uses `Antlr4.Runtime.Standard` at
runtime; build-time generation uses `Antlr4BuildTasks`. Generated
files are not committed (regenerated on every build).

### Quoted-string requirement for DSL values

All user-visible labels, captions, text values, and scope descriptors in
DSL steps must be **double-quoted string literals**. This makes the
grammar unambiguous (keywords like `with`, `in`, `and` inside a quoted
value are tokenized as part of the string, never as structural keywords).

Example:
- `CLICK BUTTON in "Add Mask Exemption" dialog with caption "Save"` ✓
- `CLICK BUTTON in Add Mask Exemption dialog with caption Save` ✗

This is a clarification of the existing DSL definition, not a behavior
change. Existing UAT checklists in closed sets that use unquoted values
will be flagged by the validator (and grandfathered via suppressions
per W1 — same suppression mechanism).

### Structured VERIFY forms (DSL extension)

The current DSL's `VERIFY <user-visible property or event>` is too
free-form to compile deterministically. The compiler defines a closed
set of structured VERIFY forms:

- `VERIFY ELEMENT [in <scope>] with label <label> is visible`
- `VERIFY ELEMENT [in <scope>] with label <label> is not visible`
- `VERIFY ELEMENT [in <scope>] with label <label> is enabled`
- `VERIFY ELEMENT [in <scope>] with label <label> is disabled`
- `VERIFY TEXT [in <scope>] equals <expected>`
- `VERIFY TEXT [in <scope>] contains <expected>`
- `VERIFY TEXT [in <scope>] matches <regex>`
- `VERIFY URL matches <pattern>`
- `VERIFY SNACKBAR shows <text>`
- `VERIFY ROW in <grid name> where <col> = <value> exists`
- `VERIFY ROW in <grid name> where <col> = <value> does not exist`

Each maps to a deterministic Playwright assertion. Free-form `VERIFY`
text remains supported in the grammar but emits a `// PENDING: <text>`
comment in the generated C# (human-only; not asserted automatically) —
existing checklists are not broken; new checklists are encouraged to
use structured forms via a validator warning.

### Emitter output shape

The Playwright C# emitter produces a static `static class` with a
single `public static async Task RunAsync(IPlaywrightUatFixture fixture, IPage page)`
method per checklist. The emitter does not depend on dabbler-platform's
`PlaywrightFixture`; it depends on a small `IPlaywrightUatFixture`
interface defined in `Dabbler.Uat.Dsl.Emitters.Playwright.CSharp`:

```csharp
public interface IPlaywrightUatFixture
{
    Task WaitForQuietAsync(IPage page);
    Task WaitForReadyAsync(IPage page);
    Task SeededSignInAsync(IPage page, string email, IReadOnlyList<string>? roles = null);
}
```

dabbler-platform's `PlaywrightFixture` will implement this interface in
W2 — the existing methods already match the shapes. This keeps the
library decoupled from any specific fixture implementation.

### Watermark integration is a flag, not a hard dependency

The emitter has a constructor option `bool emitWatermarkWaits`. When
true, `await fixture.WaitForQuietAsync(page)` is appended after every
state-changing verb. When false, the emitter skips it (relies on
Playwright web-first assertions). Default: `true`. W3's watermark must
be in place for the calls to be no-ops or real waits; the W2 fixture
adapter can emit a stub `WaitForQuietAsync` that's a no-op until W3
lands. This decouples W0/W2 from W3's schedule.

### Build-time grammar regeneration, no committed generated code

`Antlr4BuildTasks` regenerates the lexer/parser from the .g4 file on
every build. Generated `.cs` files are .gitignored. This guarantees
that the grammar in source is the actual grammar in use; there's no
drift from a manually-edited generated file.

---

## Sessions
### Session 1 of 5: Repo bootstrap, grammar v1, grammar tests

**Steps:**

1. Read `dabbler-platform/docs/planning/project-guidance.md` and
   `dabbler-platform/docs/planning/lessons-learned.md`.
2. Create the new sibling repo:
   ```bash
   cd ..
   mkdir dabbler-uat-dsl
   cd dabbler-uat-dsl
   git init
   ```
3. Bootstrap repo skeleton:
   - `.gitignore` (Visual Studio + ANTLR generated outputs).
   - `.editorconfig` matching dabbler-platform conventions.
   - `README.md` — purpose, install, basic usage example, link to grammar.
   - `CLAUDE.md` — AI-router bootstrap mirroring dabbler-platform's;
     scoped commands; brief project structure.
   - `LICENSE` — match dabbler-platform's license.
4. Create solution:
   ```bash
   dotnet new sln -n DabblerUatDsl
   dotnet new classlib -n Dabbler.Uat.Dsl.Core -o src/Dabbler.Uat.Dsl.Core -f net8.0
   dotnet new xunit   -n Dabbler.Uat.Dsl.Core.Tests -o tests/Dabbler.Uat.Dsl.Core.Tests -f net8.0
   dotnet sln add src/Dabbler.Uat.Dsl.Core/ tests/Dabbler.Uat.Dsl.Core.Tests/
   ```
5. Add NuGet references in `Dabbler.Uat.Dsl.Core.csproj`:
   - `Antlr4.Runtime.Standard` (runtime).
   - `Antlr4BuildTasks` (build-time generator).
6. Author `grammar/DabblerUatDsl.g4`. The grammar covers:
   - Lexer rules for keywords (`NAVIGATE`, `TO`, `CLICK`, `BUTTON`,
     `LINK`, `TAB`, `WITH`, `CAPTION`, `IN`, `AND`, `WHERE`, `EQUALS`,
     `VERIFY`, `ELEMENT`, `IS`, `VISIBLE`, `NOT`, `ENABLED`, `URL`,
     `MATCHES`, `CONTAINS`, `SHOWS`, `EXISTS`, etc.).
   - `QUOTED_STRING` token: `'"' (~["\r\n])* '"'`.
   - `WS` skip rule.
   - Parser rule per verb (clickButtonStep, clickLinkStep, ...,
     verifyTextStep, verifyElementStep, verifyUrlStep, etc.).
   - Top-level `step` rule that alternates over all verb forms.
   - Reference: include the verb list from `project-guidance.md`'s DSL
     section verbatim in a comment block at the top of the .g4 file.
7. Configure `Dabbler.Uat.Dsl.Core.csproj` to invoke `Antlr4BuildTasks`
   on the .g4 file. Verify `dotnet build` produces generated lexer +
   parser without errors.
8. Author grammar tests in `Dabbler.Uat.Dsl.Core.Tests/GrammarTests.cs`.
   For each verb form, at minimum:
   - One positive test (parse a canonical example, assert no errors).
   - One negative test (parse a malformed example, assert specific
     parse error).
   - One edge case (label containing a keyword, scope with a
     conjunction inside the quoted name, etc.).
   Aim for ≥3 tests per verb form. ~60–80 tests total.
9. `dotnet test` passes for all grammar tests.
10. `route(task_type="session-verification")` covering the grammar and
    test suite.
11. Commit in the new repo:
    `Initial repo bootstrap, ANTLR grammar v1, grammar tests`.
12. In dabbler-platform: commit the activity-log update only (no code
    changes in dabbler-platform this session).

**Creates (in `../dabbler-uat-dsl/`):** Repo skeleton, `Dabbler.Uat.Dsl.Core` project, `grammar/DabblerUatDsl.g4`, grammar test suite.
**Touches:** New repo only this session.
**Ends with:** New repo created and committed; grammar tests green.
**Progress keys:** `session-1/read-planning`, `session-1/repo-init`, `session-1/skeleton`, `session-1/solution`, `session-1/antlr-references`, `session-1/grammar`, `session-1/build-config`, `session-1/grammar-tests`, `session-1/test-pass`, `session-1/verification`, `session-1/commit-new-repo`, `session-1/commit-platform-activity-log`.

---

### Session 2 of 5: AST node types, visitor, parser facade

**Steps:**

1. Read planning docs.
2. In `Dabbler.Uat.Dsl.Core/Ast/`, define AST node types as records
   implementing `IUatStep`:
   - `NavigateStep(string url)`
   - `WaitForStep(string visibleState)`
   - `PressKeyStep(string key)`
   - `ClickButtonStep(string? scope, string caption)`
   - `ClickLinkStep(string? scope, string text)`
   - ... one per verb form, including all structured VERIFY variants.
   - Each record carries `int LineNumber` and `int ColumnNumber` for
     error reporting.
3. Implement `UatDslVisitor : DabblerUatDslBaseVisitor<IUatStep>`:
   walks the parse tree and produces an `IUatStep` per top-level step.
4. Implement `UatChecklistParser` facade in
   `Dabbler.Uat.Dsl.Core/UatChecklistParser.cs`:
   - `public IReadOnlyList<IUatStep> Parse(string dslText)` —
     parses one or more newline-separated steps.
   - `public IReadOnlyList<IUatStep> ParseChecklist(string checklistJson)`
     — parses the `Description` field of every entry in the UAT
     checklist editor schema's `Steps` array.
   - On parse error: throw `UatDslParseException(IReadOnlyList<ParseError>)`
     where each error includes step index, line, column, and message.
5. AST tests in `Dabbler.Uat.Dsl.Core.Tests/AstTests.cs`:
   - Round-trip: parse → AST → re-emit canonical text → re-parse → assert
     equal AST. (Requires a small canonical-form printer; implement
     `IUatStep.ToCanonicalText()` for this purpose.)
   - One test per verb form covering the AST shape.
   - Tests for parse-error reporting (correct line/column/message).
6. `dotnet test` passes.
7. `route(task_type="session-verification")`.
8. Commit in `dabbler-uat-dsl`:
   `AST node types, visitor, parser facade, round-trip tests`.

**Creates:** `Dabbler.Uat.Dsl.Core/Ast/*.cs`, `UatDslVisitor.cs`, `UatChecklistParser.cs`, AST tests.
**Touches:** None outside the new repo.
**Ends with:** Parser produces a typed AST for every grammar-valid input; round-trip tests green.
**Progress keys:** `session-2/read-planning`, `session-2/ast-records`, `session-2/visitor`, `session-2/parser-facade`, `session-2/ast-tests`, `session-2/round-trip`, `session-2/test-pass`, `session-2/verification`, `session-2/commit`.

---

### Session 3 of 5: Catalog adapter, semantic validation, schema

**Steps:**

1. Read planning docs.
2. Author `schema/component-catalog.json` — the JSON Schema that catalogs
   produced by W1 must conform to. Schema captures the element record
   shape (routePath, role, elementRole, userVisibleLabel, ariaLabel,
   testId, htmlTag, scope) per the original W1 design commitments.
3. Implement `ICatalogAdapter` in `Dabbler.Uat.Dsl.Core/Catalog/`:
   ```csharp
   public interface ICatalogAdapter
   {
       CatalogEntry? Lookup(string routePath, string role, string elementRole, string userVisibleLabel);
       IReadOnlyList<CatalogEntry> EntriesFor(string routePath, string role);
   }
   ```
   `JsonCatalogAdapter(string catalogJsonPath)` — concrete impl that
   loads and indexes the catalog. Loads lazily; caches in memory for
   the duration of the validation pass.
4. Implement `SemanticValidator` in
   `Dabbler.Uat.Dsl.Core/Validation/`:
   - `public IReadOnlyList<ValidationError> Validate(IReadOnlyList<IUatStep> steps, ICatalogAdapter catalog, string? activeRole = null)`.
   - For each step that names a label: look up `(currentRoute, role,
     elementRole, label)` in the catalog. Tracks `currentRoute` as it
     walks the steps (updates on `NavigateStep`).
   - Errors include step index, the missing tuple, and any
     near-matches (catalog has "Email address" but step says "Email" —
     suggest the near-match).
5. Tests in `Dabbler.Uat.Dsl.Core.Tests/SemanticValidatorTests.cs`:
   - Hand-craft a minimal catalog JSON for testing.
   - Test: valid checklist with all labels in catalog → no errors.
   - Test: checklist with one missing label → one error, correct
     near-match suggestion.
   - Test: checklist with element-type drift (step says CLICK BUTTON,
     catalog has the same label as a link) → specific error.
   - Test: route inference (NavigateStep updates current route, later
     steps validate against the new route).
6. `dotnet test` passes.
7. `route(task_type="session-verification")`.
8. Commit in `dabbler-uat-dsl`: `Catalog adapter, semantic validator, schema`.

**Creates:** `schema/component-catalog.json`, `Dabbler.Uat.Dsl.Core/Catalog/*.cs`, `Dabbler.Uat.Dsl.Core/Validation/*.cs`, validator tests.
**Touches:** None outside the new repo.
**Ends with:** Validator catches label drift, element-type drift, and route-context errors against a hand-crafted catalog.
**Progress keys:** `session-3/read-planning`, `session-3/schema`, `session-3/catalog-adapter`, `session-3/validator`, `session-3/validator-tests`, `session-3/test-pass`, `session-3/verification`, `session-3/commit`.

---

### Session 4 of 5: Playwright C# emitter

**Steps:**

1. Read planning docs.
2. New project:
   ```bash
   dotnet new classlib -n Dabbler.Uat.Dsl.Emitters.Playwright.CSharp \
     -o src/Dabbler.Uat.Dsl.Emitters.Playwright.CSharp -f net8.0
   dotnet sln add src/Dabbler.Uat.Dsl.Emitters.Playwright.CSharp/
   ```
   References `Dabbler.Uat.Dsl.Core`. References
   `Microsoft.Playwright` (for `IPage` / `AriaRole` types in the
   emitted code).
3. Define `IPlaywrightUatFixture` interface in the emitter project per
   the Design Commitments shape. The emitter targets this interface;
   dabbler-platform's `PlaywrightFixture` will implement it in W2.
4. Implement `IUatChecklistEmitter<string>` in
   `Dabbler.Uat.Dsl.Core/Emitters/`:
   ```csharp
   public interface IUatChecklistEmitter<TOutput>
   {
       TOutput Emit(IReadOnlyList<IUatStep> steps, EmitOptions options);
   }
   public record EmitOptions(string ClassName, bool EmitWatermarkWaits = true);
   ```
5. Implement `PlaywrightCSharpEmitter : IUatChecklistEmitter<string>`:
   - For each AST node, produce the corresponding Playwright C# call.
   - State-changing verbs (CLICK, FILL, OPEN DROPDOWN, PICK DATE,
     PRESS KEY, NAVIGATE TO) are followed by
     `await fixture.WaitForQuietAsync(page);` when `EmitWatermarkWaits`.
   - VERIFY verbs emit `await Assertions.Expect(...).ToHaveTextAsync(...)`
     etc. — web-first assertions, no manual waits needed.
   - Output template: a static class with `RunAsync(fixture, page)`
     method.
   - Free-form VERIFY (legacy non-structured) emits
     `// PENDING: <verbatim text>` and a TODO comment pointing to the
     structured-form alternative.
6. Verb-by-verb emitter tests in
   `Dabbler.Uat.Dsl.Emitters.Playwright.CSharp.Tests/EmitterTests.cs`:
   - For each AST node type, assert the exact emitted C# string
     (including watermark wait when enabled).
   - Test that emitted code compiles via Roslyn:
     `Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseCompilationUnit(emittedCode)`
     produces no diagnostic errors. (This is a strong correctness
     gate without needing to actually run the Playwright test.)
7. `dotnet test` passes.
8. `route(task_type="session-verification")`.
9. Commit in `dabbler-uat-dsl`:
   `Playwright C# emitter with Roslyn-compilable output`.

**Creates:** `src/Dabbler.Uat.Dsl.Emitters.Playwright.CSharp/` (full project), `Dabbler.Uat.Dsl.Core/Emitters/IUatChecklistEmitter.cs`, emitter test project.
**Touches:** None outside the new repo.
**Ends with:** Emitter produces Roslyn-compilable C# for every AST node type; emitted code references `IPlaywrightUatFixture`.
**Progress keys:** `session-4/read-planning`, `session-4/emitter-project`, `session-4/fixture-interface`, `session-4/emitter-interface`, `session-4/emitter-impl`, `session-4/verb-tests`, `session-4/roslyn-compile-test`, `session-4/test-pass`, `session-4/verification`, `session-4/commit`.

---

### Session 5 of 5: CLI, packaging, dabbler-platform integration, change log, Step 9 reorg

**Steps:**

1. Read planning docs.
2. New project:
   ```bash
   dotnet new console -n Dabbler.Uat.Dsl.Cli \
     -o src/Dabbler.Uat.Dsl.Cli -f net8.0
   dotnet sln add src/Dabbler.Uat.Dsl.Cli/
   ```
   References `Dabbler.Uat.Dsl.Core` and the Playwright C# emitter.
   Add `System.CommandLine` for argument parsing.
3. Implement two CLI subcommands:
   - `dabbler-uat-dsl validate <checklist.json> [--catalog <catalog.json>]`
     — parses the checklist; if catalog provided, runs semantic
     validation. Exits 0 on no errors, 1 with structured error JSON
     on any error.
   - `dabbler-uat-dsl compile <checklist.json> --catalog <catalog.json> --emitter playwright-csharp --output <test.cs> [--no-watermark]`
     — parses, validates, emits to the output path. Exits 0 on success,
     1 on error (validation or emit).
4. Configure NuGet packaging metadata in each library .csproj:
   - `<PackageId>`, `<Version>`, `<Authors>`, `<Description>`,
     `<RepositoryUrl>`, `<PackageTags>`, `<PackageLicenseExpression>`.
   - `dotnet pack` produces `.nupkg` files in `bin/Debug/` for all three
     packages.
   - The CLI is also packaged as a global tool (`<PackAsTool>true</PackAsTool>`).
5. Smoke test: build a minimal sample checklist JSON, run the CLI's
   `validate` and `compile` subcommands end-to-end, confirm the emitted
   C# is valid (Roslyn parse succeeds).
6. **dabbler-platform touch:** Update
   `dabbler-platform/docs/planning/project-guidance.md` UAT/E2E DSL
   section:
   - Add a paragraph: "All values in DSL steps must be double-quoted
     string literals. Keywords (`with`, `in`, `and`, `where`, etc.) are
     bare. The canonical grammar is published at `<grammar URL>`."
   - Add the structured VERIFY forms list from this spec's Design
     Commitments.
   - Note that `VERIFY <free-form>` remains supported but is human-only
     and emits a `// PENDING` comment in compiled C#; new checklists
     should prefer structured forms.
7. Generate
   `dabbler-platform/docs/session-sets/uat-dsl-compiler/change-log.md`
   via `route(task_type="summarization")`. Cover: the new repo, the
   five-session arc, the published packages, the dabbler-platform
   touch, and the open dependency on W1 (catalog data) and W2 (fixture
   adapter).
8. Full build and test in `../dabbler-uat-dsl/`:
   ```bash
   cd ../dabbler-uat-dsl
   dotnet build && dotnet test && dotnet pack
   ```
9. Cross-provider verification covering both repos.
10. Commit:
    - `../dabbler-uat-dsl/`: `Add CLI, NuGet packaging, complete W0`.
    - `dabbler-platform/`: `Document quoted-string requirement and
      structured VERIFY forms in DSL guidance` (separate commit; the
      change-log and activity-log are committed alongside).
11. Send `send_session_complete_notification()`. Then Step 9 reorg
    review:
    - Promotion candidate (high priority): `dabbler-uat-dsl` itself is
      already in a separate publishable repo. The next consumer
      (W1 / W2) is the validation step. Once W1 lands, propose moving
      `schema/component-catalog.json` into the public NuGet package's
      embedded resources so consumers don't have to maintain their
      own copy.
    - Lessons-learned candidate: "Cross-repo session sets work; the
      activity-log + commits-to-both-repos pattern is reusable."
      Propose adding to `lessons-learned.md`.

**Creates (in `../dabbler-uat-dsl/`):** `src/Dabbler.Uat.Dsl.Cli/` (full project); NuGet metadata in all three .csproj files.
**Creates (in `dabbler-platform`):** `change-log.md`.
**Touches:** `../dabbler-uat-dsl/` solution; `dabbler-platform/docs/planning/project-guidance.md`.
**Ends with:** All three packages pack cleanly; CLI runs end-to-end against a sample checklist; dabbler-platform DSL guidance is updated to reflect the formal grammar.
**Progress keys:** `session-5/read-planning`, `session-5/cli-project`, `session-5/cli-commands`, `session-5/packaging`, `session-5/cli-smoke-test`, `session-5/platform-guidance-update`, `session-5/change-log`, `session-5/full-build`, `session-5/verification`, `session-5/commit-uat-dsl`, `session-5/commit-platform`, `session-5/notify`, `session-5/step-9-reorg`.

---

## What This Set Preserves

| Existing Asset | Status |
|---|---|
| `dabbler-platform` repo (excluding the one DSL guidance touch) | Unchanged |
| All existing UAT checklists in dabbler-platform sets | Unchanged; will be flagged by validator in W1 if they don't conform; suppressions handle grandfathered cases |
| `tests/AcmeCoffeeOnline.Playwright/` | Unchanged in this set; consumed in W2 |
| `ai-router/` task types | Unchanged in this set; W1 adds `uat-script-author` |
| The existing UAT/E2E DSL section's verb list | Preserved verbatim; quoted-string note and VERIFY structured forms appended |

## Risk Notes

- **ANTLR4 build-time dependency.** `Antlr4BuildTasks` invokes the ANTLR
  Java tool during build. Requires Java in `PATH` on developer machines
  and CI. Document this in the new repo's README. If the dependency
  becomes a friction point, a fallback is to commit generated files
  to source — but that re-introduces the drift risk and is not
  recommended.

- **Grammar evolution and breaking changes.** Once the package is in
  use, grammar changes that break existing checklists are breaking
  changes to consumers. Use SemVer; major-version bumps for any
  removed or restructured verb form. Record this convention in the
  repo README.

- **Cross-repo session-set tracking.** This is the first set in this
  workflow that crosses repo boundaries. The activity-log lives in
  dabbler-platform. Commits happen in both repos. If a session is
  interrupted and resumed, the orchestrator must check both repos'
  git state. Session 1 documents this convention in the new repo's
  CLAUDE.md so future sessions don't get confused.

- **Roslyn-compile gate is necessary but not sufficient.** Roslyn
  parse-success means the emitted C# is syntactically valid. It does
  not mean the test will pass when run — that's W2's verification
  layer. Don't conflate "compiles" with "works."

- **Free-form VERIFY backwards compat.** The grammar continues to accept
  a free-form `VERIFY <text>` to avoid breaking existing checklists.
  This is intentional but creates a graceful-degradation surface
  (old VERIFY → human-only PENDING comment). Document this clearly
  in the README and the dabbler-platform DSL guidance update so authors
  know the structured forms are preferred.

- **NuGet publish target undecided.** v1 is local-pack only. Public
  NuGet publish is deferred until at least one external project (a
  sibling Dabbler app or a non-Dabbler consumer) signals intent to
  consume. This avoids a half-baked public API surface that changes
  rapidly.
