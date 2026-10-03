# Generator Acquisition — Recorded Architecture Decision

> **Session 1 of 2 deliverable** (`access-migration-generator-consumption`). No code this
> session — this is the recorded acquisition decision with the tri-provider verdict that
> Session 2 (implement + clean-machine consumer-validation gate) ships.
> **Date:** 2026-06-20 · **Status:** DECIDED, pending owner ratification.
> **Prerequisite:** `dabbler-nuget-feed-publication` complete (locked feed target,
> versioning scheme, and trusted-publishing mechanics are adopted, not relitigated).

## Participants
- **Claude Opus 4.8 (orchestrator)** — contributed directly with full repository context
  (both generator `.csproj` + READMEs, `Dabbler.CrudSlice/Program.cs`, `tools/pack.ps1`,
  `feed-publication-decision.md`, cross-repo `platform-output-contract.md`).
  See [session-1-review/opus.md](session-1-review/opus.md).
- **GPT-5.4 (OpenAI)** — self-contained prompt. [session-1-review/gpt-5-4.md](session-1-review/gpt-5-4.md). ~$0.0847.
- **Gemini 2.5 Pro (Google)** — self-contained prompt. [session-1-review/gemini-pro.md](session-1-review/gemini-pro.md). ~$0.0136.

> **Context-asymmetry disclosure** (project-guidance Convention "Tri-Provider Review —
> Orchestrator Contribution Uses Full Context"): the Opus contribution was written
> in-session with full repo context; the two routed reviewers received self-contained
> prompts. Intentional, not a protocol defect. The self-contained prompt is
> [session-1-review/_prompt.md](session-1-review/_prompt.md).

---

## Locked inputs (adopted from the feed set — not relitigated)
- **Feed target = nuget.org, anonymous read; publish via GitHub OIDC trusted publishing.**
- **Versioning = `MAJOR.MINOR.PATCH-preview.<n>`, git-tag-sourced, single shared
  `DabblerVersion`, consumers pin exact.** One pack pipeline (`tools/pack.ps1`).
- **`Dabbler.Security.DevIdp.Tool` is already published as a `dotnet tool`** (`PackAsTool=true`,
  `ToolCommandName=dabbler-devidp`) inside the 15-package allowlist — the established
  in-repo precedent for shipping a build/dev-time command, explicitly "never a
  `<PackageReference>`".
- **`Dabbler.Templates` is in the published allowlist** (`dotnet new install Dabbler.Templates`).
- `pack.ps1` carries two lists (`$LibraryProjects`, `$InRepoPackageIds`); publish CI builds
  a solution filter (`Dabbler.Publish.slnf`), not the full solution.

## Generators — confirmed current shape (Session 1 Step "confirm")
- **`tools/Dabbler.SchemaApplicator`** — `Microsoft.NET.Sdk` console `Exe`, net10.0,
  framework-dependent, no RID needs, `IsPackable=false`. Pure file transformer
  (`--entity`, `--directory`, optional `--schema`).
- **`tools/Dabbler.CrudSlice`** — `Microsoft.NET.Sdk` console `Exe`, net10.0,
  `IsPackable=false`. The front-door wrapper: (1) shells out to `dotnet new
  dabbler-crud-service` / `dabbler-crud-page` (`TemplateRunner`), (2) runs
  SchemaApplicator **in-process** (project reference → `SchemaApplicatorRunner`),
  (3) emits DI registration helpers. Embeds `Templates/*.template` resources.
  ~12 required CLI flags.
- Neither has a `PackageId` or RID/self-contained requirement today.

---

## Decision 1 — Mechanism: **Option A, publish as a `dotnet tool` on nuget.org** *(unanimous A)*
All three reviewers independently chose **A**. The generators are build-time commands;
`dotnet tool` is the .NET-idiomatic primitive for one, and the repo already ships
`Dabbler.Security.DevIdp.Tool` this exact way. Option A removes the clone+build friction
for hundreds of independently-CI'd consumer repos while reusing the locked version scheme,
`pack.ps1`, and trusted-publishing CI **unchanged** — a category-consistent extension of an
existing pattern, not a new mechanism.

- **Option B (publish as ordinary library/CLI packages) — rejected.** It publishes the
  wrong *shape*: a library-shaped package invites a `<PackageReference>`, dragging
  build-time code into the app's runtime restore graph — the exact footgun the allowlist
  guards. A `dotnet tool` makes the build-time/runtime boundary explicit; B erases it. The
  cost is permanent (public nuget.org IDs cannot be deleted).
- **Option C (formalize source checkout) — runner-up.** No new permanent artifact, lowest
  maintenance, keeps `tools/...` anchoring literally true — but leaves every consumer CI
  cloning+building the platform, i.e. it does **not** solve the stated problem. Correct
  fallback only if the owner is not ready to mint any new permanent public ID this set.

> **Runner-up divergence (recorded):** Opus and GPT-5.4 rank **C** as runner-up
> (B's permanent wrong-shape is worse than C's "does nothing new", given the explicit
> instruction to weigh reversibility). Gemini ranks **B** as runner-up. The decision adopts
> **C as runner-up** on the reversibility argument.

## Decision 2 — Surface: **one tool now (`Dabbler.CrudSlice.Tool`), second tool sanctioned-but-deferred** *(divergence resolved on reversibility)*
Publish **one** tool now; pre-name the second so adding it later is a rubber-stamp.
- The named consumer (orchestrator Step 7) calls only **CrudSlice**, which bundles
  SchemaApplicator's assembly in-process when packed `PackAsTool`. One ID fully serves it.
- Minting two permanent public IDs when only one has a proven consumer is the riskier
  direction; "easier to add a tool than to retract one" is decisive.
- **Accepted caveat (GPT-5.4):** standalone SchemaApplicator is a genuinely independent
  generator (re-stamp an existing scaffold) with better ergonomics than driving CrudSlice
  with `--skip-templates --skip-registrations`. There is **no named clean-machine consumer
  for it today**, so its public ID is deferred — not rejected — and pre-named below.
- **Traceability preserved:** the cross-repo contract anchors both generators by `tools/...`
  path; bundling SchemaApplicator inside `CrudSlice.Tool` keeps the mapping intact.

> **Divergence (recorded):** GPT-5.4 recommended publishing **both** tools now for
> independent-generator visibility + 1:1 source↔artifact mapping. Gemini recommended
> **one**. The decision adopts **one now + one deferred** — Gemini's reversibility with
> GPT-5.4's caveat captured as the deferral trigger.

## Decision 3 — Naming *(convergent on the precedent)*
- **PackageId:** `Dabbler.CrudSlice.Tool` — `.Tool` suffix marks "never a
  `<PackageReference>`", matching `Dabbler.Security.DevIdp.Tool`.
- **Command:** `dabbler-crud-slice` (short branded command; precedent `dabbler-devidp`).
- **Deferred second tool, pre-named:** `Dabbler.SchemaApplicator.Tool` / `dabbler-schema-applicator`.
- Traceability mapping for `platform-output-contract.md`: `tools/Dabbler.CrudSlice` →
  `Dabbler.CrudSlice.Tool` (which additionally carries `tools/Dabbler.SchemaApplicator`).

> GPT-5.4 used `dabbler-crudslice` (no hyphens); Gemini and Opus used `dabbler-crud-slice`.
> Adopted the hyphenated form (2-word command reads cleaner). Cosmetic; finalize in Session 2.

## Decision 4 — Versioning: **rides the single shared `DabblerVersion`, no new scheme** *(unanimous)*
The tool ships at the same `0.1.0-preview.<n>` as everything else in the same pack run.
Consumers pin reproducibly via a **local tool manifest** (`.config/dotnet-tools.json` +
`dotnet tool restore`) — the tool analog of the locked "pin exact" rule. Mechanics
(Session 2): add `tools/Dabbler.CrudSlice` to `pack.ps1` `$LibraryProjects` and
`Dabbler.CrudSlice.Tool` to `$InRepoPackageIds` (15 → 16) plus the publish solution filter —
the same two-list edit `DevIdp.Tool` already made. Set `PackAsTool=true`,
`ToolCommandName=dabbler-crud-slice`, `PackageId`, and `IsPackable=true` on the project,
and complete package metadata (license, RepositoryUrl, readme, tags) before the first push.

## Decision 5 — The `dotnet new` templates dependency: **two-install acquisition; binds the Session 2 gate** *(independently flagged by both routed reviewers)*
CrudSlice shells out to `dotnet new dabbler-crud-service` / `dabbler-crud-page`, so the
acquisition story is **two installs at the same pinned `DabblerVersion`**, not one:
1. `dotnet new install Dabbler.Templates::<DabblerVersion>` (already in the allowlist),
2. `dotnet tool install Dabbler.CrudSlice.Tool --version <DabblerVersion>` (this set).

Carried into Session 2 as binding acceptance constraints:
- **The clean-machine consumer-validation gate MUST install BOTH** — a tool-only gate fails
  at the first `dotnet new` call. This is the single most likely way to ship a "green"
  decision that breaks on a real clean machine.
- **Version-skew is the top runtime risk:** tool and templates must share the exact
  `DabblerVersion`; the gate should assert it; the orchestrator playbook Step 7/Step 00 note
  must pin both.
- **Preflight check (recommended Session 2 implementation, not a blocker):** CrudSlice should
  detect missing/short templates and fail fast with the exact
  `dotnet new install Dabbler.Templates::<version>` command. `--skip-templates` remains the
  registrations-only escape hatch.

---

## Tri-provider verdict summary
| # | Decision | Opus | GPT-5.4 | Gemini 2.5 Pro | Outcome |
|---|----------|------|---------|----------------|---------|
| 1 | Mechanism = `dotnet tool` (A) | A | A | A | **Unanimous A** |
| 1b| Runner-up | C | C | B | **C** (reversibility: B's wrong-shape is permanent) |
| 2 | Surface = one tool vs two | one now + one deferred | two now | one now | **One now (`CrudSlice.Tool`) + `SchemaApplicator.Tool` deferred** |
| 3 | Naming `Dabbler.CrudSlice.Tool` / `dabbler-crud-slice` | ✓ | ✓ (`crudslice`) | ✓ | **Convergent** (hyphenated command) |
| 4 | Single shared `DabblerVersion`, pin via tool manifest | ✓ | ✓ | ✓ | **Unanimous** |
| 5 | Two-install (templates + tool); gate installs both | ✓ | ✓ | ✓ | **Convergent** (both routed reviewers flagged independently) |

## Consistency with the locked feed architecture
**Fully consistent** — nuget.org anonymous read, trusted publishing, single shared version
scheme, the explicit tool-vs-library distinction, and the existing `DevIdp.Tool` `dotnet
tool` precedent. Option A reinforces the architecture; B would strain it; C declines to use it.

## Session 2 inputs (what the implementation session opens with)
1. Add `PackAsTool`/`ToolCommandName=dabbler-crud-slice`/`PackageId=Dabbler.CrudSlice.Tool`/
   `IsPackable=true` + package metadata to `tools/Dabbler.CrudSlice`; add it to `pack.ps1`'s
   two lists and the publish solution filter; no new versioning scheme.
2. Clean-machine consumer-validation gate that, with **no platform source checkout**, installs
   **both** `Dabbler.Templates::<ver>` and `Dabbler.CrudSlice.Tool@<ver>`, then generates a
   CRUD slice from a sample `.dabbler-entity.json` that **builds green**.
3. Update `platform-output-contract.md`-facing guidance + notify the orchestrator repo so its
   playbook Step 7/Step 00 source-checkout note is replaced with the two-command acquisition
   (cross-repo follow-up).
4. (Recommended, non-blocking) CrudSlice templates preflight fail-fast.

## Owner ratification needed before Session 2
- Confirm **one tool now** (vs. publishing `SchemaApplicator.Tool` immediately too).
- Confirm the **permanent public IDs/commands**: `Dabbler.CrudSlice.Tool` / `dabbler-crud-slice`
  (IDs are forever; a typo here is permanent).
