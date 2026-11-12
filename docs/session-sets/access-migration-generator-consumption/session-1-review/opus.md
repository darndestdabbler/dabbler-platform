# Opus 4.8 (orchestrator) — generator-acquisition assessment

> **Context-asymmetry disclosure** (project-guidance Convention "Tri-Provider Review —
> Orchestrator Contribution Uses Full Context"): this assessment was written
> in-session with full repository context — I read the two generator `.csproj` files
> and READMEs, `tools/Dabbler.CrudSlice/Program.cs`, `tools/pack.ps1`, the locked
> `feed-publication-decision.md`, and the cross-repo `platform-output-contract.md`.
> GPT-5.4 and Gemini 2.5 Pro received a self-contained prompt with no repo access.
> This is intentional and not a protocol defect.

## Recommendation: Option A — publish as a `dotnet tool`. Runner-up: Option C.

Option A is the only choice that removes the stated friction (hundreds of
independently-CI'd consumer repos each cloning+building the platform to generate)
*and* stays inside the locked feed architecture without inventing anything. The
generators are build-time commands; `dotnet tool` is the .NET-idiomatic primitive
for a build-time command, and the repo **already does exactly this** with
`Dabbler.Security.DevIdp.Tool` (`PackAsTool=true` / `ToolCommandName=dabbler-devidp`),
which is already in the published 15-package allowlist and already flows through
`tools/pack.ps1` + the trusted-publishing CI. So Option A is a *category-consistent
extension of an existing pattern*, not a new mechanism.

**Why not B.** Option B is a false economy. The fear it addresses — "build-time
tools shouldn't live in the runtime feed" — is already answered by the locked
architecture itself: a `dotnet tool` published to nuget.org is **not** a runtime
library and is explicitly "never a `<PackageReference>`" (feed Decision 3). Option B
would publish the generators in *library shape*, inviting a consumer to add them as a
`<PackageReference>` and drag build-time code into the app's runtime restore graph —
the precise footgun the allowlist guards. A `dotnet tool` (Option A) makes the
build-time/runtime boundary explicit; Option B erases it. And the cost is permanent:
public nuget.org IDs cannot be deleted, so a wrong-shape public ID is forever.

**Why C is the runner-up, not B.** C ("formalize the source checkout") ships no new
permanent public artifact, so it is *architecturally* safer than B even though it is
*operationally* worse — it leaves every one of the hundreds of consumer CI lanes
cloning and building the whole platform, which is the exact cost this set exists to
remove. C is the correct fallback only if the owner is not ready to mint any new
permanent public ID this set; it is not a solution to the stated problem. (I disagree
with Gemini ranking B as runner-up: B's permanence-of-wrong-shape is worse than C's
"does nothing new," given the spec's explicit instruction to weigh reversibility.)

## Sub-decision: one tool now, second tool sanctioned-but-deferred

**Publish one tool now — `Dabbler.CrudSlice.Tool` — and name `Dabbler.SchemaApplicator.Tool`
as the pre-sanctioned second tool to add when a standalone consumer appears.**

This is the split between the two external reviewers (GPT-5.4: publish both now;
Gemini: publish only CrudSlice), and I come down on the reversibility side, with a
caveat GPT is right about:

- The **named** consumer — the orchestrator's Step 7 — calls only CrudSlice, which
  runs SchemaApplicator **in-process** (`SchemaApplicatorRunner`, via the project
  reference). Packing CrudSlice `PackAsTool` bundles the SchemaApplicator assembly
  inside the one tool package automatically, so Step 7 is fully served by one ID.
- Permanent public IDs + "weigh reversibility": minting two forever-IDs when only one
  has a proven consumer is the riskier direction. "Easier to add a tool than to
  retract one" is decisive here.
- GPT's real point — SchemaApplicator is a *genuinely independent* generator (re-stamp
  an existing scaffold), and its standalone ergonomics are better than driving
  CrudSlice with `--skip-templates --skip-registrations` (which still demands
  CrudSlice's full ~12-flag surface vs. SchemaApplicator's two: `--entity`,
  `--directory`). I accept that. The resolution is *sequencing*, not rejection:
  there is **no named clean-machine consumer of standalone SchemaApplicator today**,
  so we defer its public ID until one exists, and record the name/command now so the
  addition is a rubber-stamp, not a re-decision.
- **Traceability is preserved either way.** The cross-repo contract anchors both
  generators by `tools/...` path; bundling SchemaApplicator inside `CrudSlice.Tool`
  keeps that mapping intact (the contract note becomes: `CrudSlice.Tool` carries both
  `tools/Dabbler.CrudSlice` and `tools/Dabbler.SchemaApplicator`).

## Naming
- **PackageId:** `Dabbler.CrudSlice.Tool` (the `.Tool` suffix is the repo's marker for
  "never a `<PackageReference>`", matching `Dabbler.Security.DevIdp.Tool`).
- **Command:** `dabbler-crud-slice` (short branded command, precedent `dabbler-devidp`).
- Deferred second tool, pre-named: `Dabbler.SchemaApplicator.Tool` / `dabbler-schema-applicator`.

## Versioning — rides the locked scheme unchanged
Single shared `DabblerVersion`, git-tag-sourced, no second scheme. The tool ships at
the same `0.1.0-preview.<n>` as everything else in the same pack run. Consumers pin
reproducibly via a **local tool manifest** (`.config/dotnet-tools.json` +
`dotnet tool restore`) — the tool analog of the locked "pin exact versions" rule.
Mechanically this is: add the `tools/Dabbler.CrudSlice` project to `pack.ps1`'s
`$LibraryProjects` and `Dabbler.CrudSlice.Tool` to `$InRepoPackageIds` (15 → 16),
plus the publish solution filter — the same two-list edit `DevIdp.Tool` already made.

## The `dotnet new` templates dependency — a first-class finding for the Session 2 gate
CrudSlice's `TemplateRunner` shells out to `dotnet new dabbler-crud-service` and
`dotnet new dabbler-crud-page`. So the acquisition story is **two installs at the same
pinned version**, not one:
1. `dotnet new install Dabbler.Templates::<DabblerVersion>` (already in the allowlist),
2. `dotnet tool install Dabbler.CrudSlice.Tool --version <DabblerVersion>` (this set).

Implications I want carried into Session 2:
- The **clean-machine consumer-validation gate must install BOTH** — a gate that
  installs only the tool will fail at the first `dotnet new` call. Both reviewers
  independently flagged this; it is the single most likely way to ship a "green"
  decision that breaks on a real clean machine.
- **Preflight check (recommended Session 2 implementation, not a blocker):** CrudSlice
  should detect missing/short templates and fail fast with the exact
  `dotnet new install Dabbler.Templates::<version>` command, rather than surfacing a
  raw `dotnet new` error. (`--skip-templates` remains the escape hatch for
  registrations-only runs.)
- **Version-skew is the top runtime risk:** tool and templates must be the *same*
  `DabblerVersion`; the shared scheme makes that natural, and the gate should assert it.

## Consistency with the locked feed architecture
Fully consistent — nuget.org anonymous read, trusted publishing, single shared
version scheme, the explicit tool-vs-library distinction, and the existing
`DevIdp.Tool` `dotnet tool` precedent. Option A *reinforces* the architecture rather
than straining it; B strains it; C declines to use it.
