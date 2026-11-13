# Architecture decision: how should external consumers acquire two build-time code generators?

You are one of three independent reviewers on an architecture decision. Give your
own reasoned recommendation. This prompt is **self-contained** — everything you
need is below; do not assume access to a repository.

## Background

"dabbler-platform" is a .NET 10 / Blazor platform. It ships a family of runtime
libraries as NuGet packages (`Dabbler.*`) on **nuget.org**, plus two **build-time
code generators** that turn a reviewed entity schema into CRUD scaffolding:

- **`Dabbler.SchemaApplicator`** — a cross-platform .NET 10 console `Exe`. Pure
  file transformer: given `--entity` and `--directory`, it stamps grid columns,
  filter properties, and form fields into existing `.razor` files from a
  `.dabbler-entity.json` schema. No runtime services, no RID-specific needs,
  framework-dependent. Currently `IsPackable=false` (not published).
- **`Dabbler.CrudSlice`** — a higher-level .NET 10 console `Exe` "wrapper". Given an
  entity name + schema + project roots, it (1) shells out to `dotnet new
  dabbler-crud-service` and `dotnet new dabbler-crud-page` (the platform's
  `dotnet new` templates), (2) runs SchemaApplicator **in-process** (it has a
  project reference to it), and (3) emits convention-correct DI registration
  helper files. It embeds a few `*.template` resources. Currently `IsPackable=false`.
  **Key dependency:** because it invokes `dotnet new dabbler-crud-service/-page`, a
  machine running CrudSlice must also have the platform's **`Dabbler.Templates`**
  package installed (`dotnet new install Dabbler.Templates`). CrudSlice has a
  `--skip-templates` flag to emit registrations only, but the full slice needs the
  templates.

So `CrudSlice` is the front-door generator; `SchemaApplicator` is both its
in-process sub-step **and** a standalone tool (it can re-stamp an existing scaffold).

## The consumer and the problem

A separate repo, the **migration orchestrator**, has a deterministic lane (playbook
"Step 7") that runs these generators to turn a reviewed `.dabbler-entity.json` into
POCO/DTO + CRUD scaffolding. Today the generators exist **only as `tools/` source**
in the platform repo and are **excluded from the published NuGet allowlist**, so the
orchestrator must **clone and build the entire platform** just to generate.

The stated scale target is **hundreds of Microsoft Access databases, each migrated
in its own repo/CI**. "Clone and build the platform to run the generator" is the
exact friction that was already removed for *runtime* libraries when they moved to
nuget.org. This decision is about removing it for the *generators*, one layer up.

## Locked inputs (already decided in a prior session set — do NOT relitigate)

A prior "NuGet feed publication" session set locked these and they are **not open**:

1. **Feed target = nuget.org, anonymous read.** No auth token in any consumer repo
   or CI for restore. Publish is authenticated via GitHub OIDC trusted publishing
   (short-lived key; no long-lived secret).
2. **Versioning = `MAJOR.MINOR.PATCH-preview.<n>`, git-tag-sourced, single shared
   `DabblerVersion` across all packages, consumers pin exact versions.** One pack
   pipeline (`tools/pack.ps1`) stamps every package with the same version. No second
   versioning scheme is allowed.
3. **15-package allowlist published**, including — importantly — **`Dabbler.Security.DevIdp.Tool`,
   which is published as a `dotnet tool`** (`PackAsTool=true`, `ToolCommandName=dabbler-devidp`,
   `PackageId=Dabbler.Security.DevIdp.Tool`). This is the **established precedent** in this
   repo for shipping a build/dev-time command via nuget.org. The allowlist deliberately
   distinguishes a `dotnet tool` ("never a `<PackageReference>`") from runtime library packages.
4. `Dabbler.Templates` (the `dotnet new` template package) **is** in the published
   allowlist (`dotnet new install Dabbler.Templates`).
5. The pack pipeline `pack.ps1` carries two lists: a project list and a PackageId
   allowlist; the DevIdp tool is already in both. Adding a new published artifact
   means adding it to both lists. Publish CI builds a solution *filter*, not the full
   solution.

## A cross-repo traceability constraint

The orchestrator's `platform-output-contract.md` currently anchors the generators by
their **`tools/...` source path**. Whatever mechanism is chosen must keep that
traceability valid (a published artifact's identity must still map cleanly back to
the `tools/...` source).

## The three options to weigh

- **Option A — Publish as `dotnet tool`(s) on nuget.org.** Pack `Dabbler.CrudSlice`
  (and possibly `SchemaApplicator`) as `dotnet tool`(s), mirroring the existing
  `Dabbler.Security.DevIdp.Tool`. One-line install (`dotnet tool install ...`);
  framework-dependent. Reuses the locked version scheme + pack pipeline + trusted
  publishing unchanged.
- **Option B — Add the generators to the feed allowlist as ordinary library/CLI
  packages.** Extend the published set, accepting that build-time tools then live in
  the runtime feed as library-style packages (consumable potentially as
  `<PackageReference>`).
- **Option C — Keep source-checkout, formalize it.** No new published artifact;
  document a pinned clone+build step and leave the orchestrator playbook's
  source-checkout path as the supported one.

## What to deliver

Give your independent recommendation:

1. **Pick exactly one option (A, B, or C)** and name a close runner-up, with reasoning.
2. If you pick A: address the **sub-decisions** —
   - **One tool or two?** (CrudSlice as the front-door tool, which bundles
     SchemaApplicator via its project reference; vs. also publishing SchemaApplicator
     as its own tool.) The named consumer (orchestrator Step 7) calls CrudSlice, which
     runs SchemaApplicator in-process.
   - **Naming** of the PackageId(s) and tool command(s), consistent with the
     `Dabbler.Security.DevIdp.Tool` / `dabbler-devidp` precedent.
   - **Versioning**: confirm it rides the single shared `DabblerVersion` with no new
     scheme; how a consumer pins a tool version reproducibly.
   - **The `dotnet new` templates dependency**: CrudSlice shells out to `dotnet new
     dabbler-crud-service/-page`. What must a clean-machine consumer install for the
     full slice to work, and what does that imply for a "clean-machine
     consumer-validation gate" (the next session's acceptance proof)?
3. Note the **risks / failure modes** of your choice and how to mitigate them.
4. State explicitly whether your choice is **consistent with the locked feed
   architecture** (nuget.org, single version scheme, tool-vs-library distinction) or
   whether it strains it.

Be concise and decisive. This is a hard-to-reverse distribution decision (public
nuget.org IDs are permanent), so weigh reversibility.
