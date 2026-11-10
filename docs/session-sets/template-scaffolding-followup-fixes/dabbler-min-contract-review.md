# dabbler-min Contract Review

> **Session set:** `template-scaffolding-followup-fixes`, Session 2 of 3
> **Date:** 2026-05-27
> **Question:** Should `dabbler-min` remain intentionally single-project, or split to match `dabbler-app`'s web/api/shared/tests topology now that the `Dabbler.CrudSlice` wrapper supports same-root consumers?
> **Routed task:** `route(task_type="architecture")` → `claude-opus-4-6` (tier 3), $0.214725, complexity score 69
> **Prompt:** `c:/tmp/dabbler-min-arch-review-prompt.md` (snapshot at session-2 author time; not committed because it was an ad-hoc routing payload, not a durable artifact)

The text below is the raw routed response — **never edit it after this commit.** All implementation deviations are tracked in the activity log and the cross-provider verification review for this session.

---

## Verdict: A — Keep single-project. Codify it.

## Rationale

The single-project shape *is* the value proposition of `dabbler-min`. If you split it to match `dabbler-app`, the only remaining difference is "no Reports package, no sample CRUD page" — two templates producing near-identical four-project solutions differing by one NuGet reference. That's a maintenance liability with no real user benefit. The correct fix for the customer-confusion data point is documentation clarity, not structural duplication. The same-root CRUD wrapper path already works and is integration-tested; forcing a split would actually *remove* the cleanest demonstration of that single-project consumer pattern. A `dabbler-min` user who outgrows one `.csproj` should scaffold `dabbler-app` fresh — that's a feature, not a gap.

## Implementation Outline

### Template source (`templates/Dabbler.Templates/content/dabbler-min/`)

- **No structural changes.** Single `.csproj` stays.
- **Add `README.md`** to template content root (emitted into scaffolded output):
  ```
  # {{name}}
  Single-project Blazor Server starter with Dabbler Security.
  For the multi-project topology (Web + API + Shared + Tests), use `dotnet new dabbler-app`.
  ```
- **Add comment block** at top of `Program.cs` (3 lines):
  ```csharp
  // This is a single-project Dabbler Security starter.
  // Auth endpoints live here alongside Blazor components by design.
  // For a split Web/API/Shared/Tests topology, use the dabbler-app template.
  ```

### Template metadata (`.template.config/template.json`)

- **`description`**: Change from current text to `"Single-project Blazor Server starter with Dabbler Security (magic-link auth, cookie auth, role-based access). For a multi-project solution, use dabbler-app."`
- **`type`**: Keep `"project"`.
- **`classifications`**: Keep `[Web, Blazor, Server, Security]`. Do not add `Reports`.
- **Add `Framework` symbol** with `defaultValue: "net10.0"` and `replaces: "net10.0"` for parity with `dabbler-app`. Eliminates the hard-pin asymmetry.

### Docs

**`docs/getting-started.md`** — Replace the current scattered mentions with a single comparison block:

```markdown
## Template Comparison

| | `dabbler-app` | `dabbler-min` |
|---|---|---|
| Topology | Web + API + Shared + Tests | Single project |
| Packages | Security, Reports, Components | Security, Components |
| Test project | bUnit (scaffolded) | None (add your own) |
| CRUD wrapper | `--web-root` / `--api-root` point to separate projects | Both flags point to same root |
| Use when | Starting a full application | Adopting Dabbler security incrementally |
```

**`docs/scaffolding-templates/README.md`** — In the same-root section, add explicit callout:

> This is the expected pattern for `dabbler-min` consumers. The template produces a single project; pass its root to both `--web-root` and `--api-root`.

### Smoke tests (`tests/Dabbler.Packaging.Smoke/PackagingSmokeTests.cs`)

Rename existing test and add shape assertions:

```csharp
[Fact]
public async Task DabblerMin_Template_InstallsAndBuildsClean_SingleProjectShape()
{
    // ... existing scaffold + restore + build logic ...

    // Positive: exactly one csproj at expected path
    Assert.Single(Directory.GetFiles(scaffoldDir, "*.csproj", SearchOption.AllDirectories));
    Assert.True(File.Exists(Path.Combine(scaffoldDir, "DabblerApp", "DabblerApp.csproj")));

    // Negative: no split-topology artifacts
    Assert.Empty(Directory.GetFiles(scaffoldDir, "*.Api.csproj", SearchOption.AllDirectories));
    Assert.Empty(Directory.GetFiles(scaffoldDir, "*.Shared.csproj", SearchOption.AllDirectories));
    Assert.Empty(Directory.GetFiles(scaffoldDir, "*.Tests.csproj", SearchOption.AllDirectories));
    Assert.False(Directory.Exists(Path.Combine(scaffoldDir, "tests")));

    // Positive: README emitted
    Assert.True(File.Exists(Path.Combine(scaffoldDir, "README.md")));
}
```

This makes the single-project contract a regression gate. If someone accidentally splits the template, CI breaks.

### CRUD wrapper tests (`tests/Dabbler.CrudSlice.Tests/WrapperOrchestrationTests.cs`)

**No new test needed.** The existing `FullPipeline_endToEnd_against_smoke_fixture` already exercises same-root. Add a `// Covers dabbler-min single-project consumer pattern` comment to the test method doc-comment so the mapping is explicit and discoverable.

### Second-order concerns

- **Package version drift.** Both templates reference `Dabbler.Security`. Pin versions via a shared `Directory.Packages.props` or a source-generation step in the template packaging pipeline. Today they're manually coordinated — that's a latent bug. Flag for backlog but not this session.
- **`security-config.json` placement.** Stays at project root. No issue — single project means one root.
- **Magic-link auth endpoints.** Stay in `Program.cs` in the web host. Moving them would only matter if we split, which we're not doing. The `Program.cs` comment block makes this intentional and visible.
- **`net10.0` pinning.** Adding the `Framework` symbol to `dabbler-min` metadata (as noted above) eliminates the asymmetry where `dabbler-app` has the symbol and `dabbler-min` doesn't. Low-cost consistency win.

---

## Operator Clarification (added 2026-05-27 — after the raw review above)

> **Status:** The routed Option A verdict (keep `dabbler-min` single-project, codify it) is **confirmed** as this set's direction. The clarification below records the in-session human review of that verdict against a constraint the routed reviewer did not have, so future sessions and audits see both the original recommendation and the operator's evaluation of it.

### The surfaced constraint

The operator's architecture review board requires that .NET applications have physically separate Web Application and Web API layers. Under that constraint, a single-project Blazor Server starter that hosts its `/auth/*` minimal-API endpoints inside the same project as the Razor components (which is exactly what `dabbler-min`'s `Program.cs` does today) would not pass board review for any application the board governs.

### Why the routed verdict still stands

The board constraint applies to board-governed *applications*, not to platform-provided *adoption ramps*. The Dabbler platform ships two templates because there are two legitimate consumer shapes:

- `dabbler-app` is the board-compliant entry point. It scaffolds the four-project topology (Web + Api + Shared + Tests) that real applications adopt. The operator confirmed this is the "more architecturally sound" shape and the default expectation for new application work.
- `dabbler-min` is the incremental-adoption entry point. It produces the smallest possible Dabbler Security adopter — one project, one binary, one runnable host — for prototypes, spikes, internal tooling that does not need a separate API tier, and consumers who want to evaluate Dabbler Security before committing to the multi-project shape. A consumer who outgrows it migrates to `dabbler-app` by re-scaffolding.

Splitting `dabbler-min` to match `dabbler-app` would collapse the two templates into near-duplicates and remove the cleanest demonstration of the same-root `Dabbler.CrudSlice` wrapper pattern. The routed review's "two templates producing near-identical four-project solutions differing by one NuGet reference" framing applies directly to the operator's environment too.

### What this changes about the implementation

The implementation outline above is unchanged. The board-constraint context **does** raise the documentation bar: both templates' positioning must be unmistakable so an adopter picking the wrong one is making an explicit choice, not stumbling into a board-non-compliant shape by accident.

Concretely:

- The `docs/getting-started.md` comparison block adds a "Use when" row that calls out the architectural-separation expectation: `dabbler-app` for board-governed applications with required Web/API separation, `dabbler-min` for incremental adoption / prototypes / single-tier internal tools.
- The scaffolded `dabbler-min` README explicitly says "single project, by design — for a multi-project topology use `dabbler-app`" so an adopter sees the contrast in the generated repo, not only in the platform docs.
- The `Program.cs` comment block reinforces the same point inside the code the adopter will read first.

### The customer-feedback data point

The original customer report ("CLI doesn't generate the correct project structure — should be Web + API + tests + shared library, got a monolithic Blazor project") is now interpreted as a documentation-clarity failure rather than a structural failure: the customer used `dabbler-min` expecting `dabbler-app`'s shape. The fix is to ensure the next adopter does not make the same selection error — which is exactly what the enhanced docs in this session deliver.

A pure structural fix (splitting `dabbler-min`) would have masked the documentation problem and produced a long-term maintenance liability. Codifying the contract and clarifying the picker docs is the durable answer.
