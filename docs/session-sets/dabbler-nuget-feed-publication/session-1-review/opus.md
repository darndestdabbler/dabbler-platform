# Opus (orchestrator) contribution — feed/versioning/auth architecture review

> **Context disclosure (per project-guidance Convention "Tri-Provider Review — Orchestrator
> Contribution Uses Full Context"):** this assessment was written by the Claude Opus
> orchestrator *in-session, with full repository context* (the dependency graph, the pack
> script, `Directory.Packages.props`, the template `nuget.config`, and `pack-output/`). The
> two routed reviewers (GPT-5.4, Gemini 2.5 Pro) received a *self-contained* prompt without
> that context. This asymmetry is intentional and is not a protocol defect.

## Locked inputs (platform owner, this session)
Public/open audience · permanent public IDs acceptable · anonymous read is a hard requirement
→ **feed target = nuget.org**. GitHub Packages rejected (read-auth). No nuget.org account yet
(creating one + API key is an accepted Session-2 prerequisite).

## Ground truth gathered this session
- **15 package IDs** stamped from one `<DabblerVersion>` (`Directory.Build.props`, default
  `0.1.0-preview.1`; `pack.ps1 -Version` overrides). Two stale variants exist locally:
  `0.1.0-preview.1` and `0.1.0-session5` — pre-publication noise, nothing is on nuget.org yet.
- **Internal dependency graph** (`Dabbler.*` → `Dabbler.*`):
  - Components, Api.Querying, Security, Testing.Blazor — leaves.
  - Security.Blazor → Components, Security.
  - Reports.Core → Components, **DabblerPdf**. Reports.Export → Reports.Core, **DabblerPdf**.
    Reports.Blazor → Components, Reports.Core, Reports.Export.
  - Components.Compat → Components (nothing references Compat).
  - Platform.Stubs.{Components,Reports,Security} and the Stubs aggregate form a closed subtree;
    **no app-dependency library references any Stubs package.**
- **External transitive dep `DabblerPdf 1.1.2`** is a versioned `PackageReference` (central
  package management). This repo's only restore source is nuget.org, so **DabblerPdf 1.1.2 is
  already public on nuget.org** → the Reports.* closure resolves anonymously. *But* DabblerPdf
  is **LGPL-2.1-only OR MPL-2.0** (copyleft), a transitive dep of the MIT Reports packages,
  documented in THIRD-PARTY-NOTICES.txt.
- **Template `nuget.config`** (`content/dabbler-app/nuget.config`) declares `nuget.org` + a
  local `./packages` source **with no `packageSourceMapping`** — the shadowing footgun.

## 1. Versioning scheme + upgrade story — **`0.1.0-preview.<n>`, tag-sourced**
- Use SemVer-2.0 prerelease `MAJOR.MINOR.PATCH-preview.<n>` with **plain integer `<n>`, no
  zero-padding.** NuGet/SemVer-2.0 compares all-numeric dot-identifiers *numerically*, so
  `preview.2 < preview.10` is correct **only without padding**; padding to `preview.02` forces
  string comparison and breaks ordering. This is the single most common versioning bug here.
- **Source the full version from the git tag**, not a CI run number: tag
  `nuget-v0.1.0-preview.7` → `DabblerVersion=0.1.0-preview.7`. The tag is the human-reviewable
  single source of truth, survives workflow renames, and the operator owns the bump. Do not use
  both tag and run-number.
- **All 15 share one version** → consumers upgrade the set in lockstep; document that. Generated
  apps pin an exact `0.1.0-preview.N` — **no floating `-preview.*`** in template
  `PackageReference`s (non-reproducible CI restores).
- Drop to `-rc.<n>` when the API surface stabilizes; drop the suffix entirely for `1.0.0`.
- First public version: start clean at `0.1.0-preview.1` on nuget.org (the local `.1`/`-session5`
  artifacts never shipped). If the hand-distributed local `preview.1` causes confusion, start at
  `0.1.0-preview.2` — minor call, lean to a clean `preview.1`.

## 2. Public-vs-internal split — **publish 14, keep DevIdp.Tool separate**
The publish allowlist **must contain the full transitive closure of what generated apps + the
template's test project restore** — excluding anything in that closure breaks anonymous restore.
- **Publish (runtime + entry point):** the 8 app-dependency libs (Api.Querying, Components,
  Reports.Core, Reports.Export, Reports.Blazor, Security, Security.Blazor, Testing.Blazor)
  **plus `Dabbler.Templates`** (the `dotnet new install` entry point). Non-negotiable.
- **Publish (cheap, avoids transitive/feature breakage):** `Dabbler.Components.Compat` and the
  four `Dabbler.Platform.Stubs*` packages. They are 8–15 KB each. The Stubs are a *supported
  platform feature* (DI-toggled bUnit/stub/demo hosts) a consumer test project may reference;
  excluding them to "keep the feed clean" trades a trivial size saving for a restore-failure
  risk in the exact stub-host scenario the platform advertises. Publish them.
- **`Dabbler.Security.DevIdp.Tool` (20 MB `dotnet tool`): publish, but as a separately-cadenced
  tool, not in the library `PackageReference` set.** The spec author is right that it is not an
  app *dependency* — so it must never appear as a `<PackageReference>`. But the audience is
  public/open and this is the local-IdP dev tool consumers use to exercise generated apps; a
  public `dotnet tool install Dabbler.Security.DevIdp.Tool` is genuinely useful and 20 MB is well
  under nuget.org's 250 MB limit. Decouple its version/cadence from the libs (it changes rarely).
  *Runner-up:* exclude entirely (internal-only) — acceptable if the owner does not want to
  support the tool publicly; this is the one genuinely debatable item.
- Net: **14 of 15 published on the library cadence; DevIdp.Tool published on its own cadence (or
  excluded).** No package is "internal-only" for correctness reasons — every exclusion is a
  product choice, not a transitive necessity.

## 3. Local `packages/` fallback — **remove from the default template; offline is opt-in**
- The current two-source-no-mapping `nuget.config` is the worst shape: a stale local `.nupkg` at
  the pinned version silently satisfies restore and shadows the feed, and the local source can
  serve an old/tampered build. This is the precise vendoring footgun the set exists to kill.
- **Default template = feed-only** (`nuget.org`), zero hand-copying. Ship the offline path as a
  documented, opt-in `nuget.offline.config` (consumer passes `--configfile`), or document adding
  the local source manually. If a single combined config is kept, it **must** carry
  `packageSourceMapping` (`Dabbler.*` → nuget.org by default; local only when explicitly
  remapped). Delete the empty `packages/.gitkeep` scaffold from the default.

## 4. Pipeline mechanics + irreversibility guardrails
- **Trigger:** push of tag `nuget-v<semver>`; CI parses the tag → `DabblerVersion`, runs
  `pack.ps1`, then `dotnet nuget push pack-output/*.nupkg --api-key $NUGET_API_KEY --source
  https://api.nuget.org/v3/index.json --skip-duplicate`. `--skip-duplicate` makes re-runs
  idempotent. `dotnet nuget push` auto-pushes the matching `.snupkg` symbol packages — **do
  publish symbols** (consumers debugging into the platform is high-value).
- **API key:** GitHub Actions secret `NUGET_API_KEY`, key **scoped on nuget.org to the
  `Dabbler.*` glob** with an expiry; gate the job behind a **GitHub Environment
  (`nuget-publish`) with required reviewers** → manual approval before any push.
- **Before the FIRST public push (IDs are permanent):**
  1. A **dry-run validate job** on PR/tag that packs + runs package validation
     (`Microsoft.CodeAnalysis.PublicApiAnalyzers`/`dotnet validate`/Meziantou validate) and
     `nuget verify`, *without* pushing.
  2. **Reserve the `Dabbler.` ID prefix** on nuget.org early (prevents squatting; claims the
     namespace the owner just decided to take publicly).
  3. **License + metadata completeness** on every package: `PackageLicenseExpression`,
     `RepositoryUrl`, `PackageReadmeFile`, icon. Critical given Reports.* carry the **copyleft
     `DabblerPdf` (LGPL/MPL) transitive dependency** — THIRD-PARTY-NOTICES.txt must ship in the
     nupkg and the license expression must be accurate. A wrong first-push license is permanent.
  4. Push the **least-coupled package first as a smoke** (e.g. Api.Querying) before the full set,
     or rely on `--skip-duplicate` + the manual gate.

## Things the owner may have under-weighted
- **The copyleft transitive dep (`DabblerPdf`, LGPL/MPL) is now going public** under MIT
  packages. Legally fine (it's a dependency, not a derivative, and MPL/LGPL permit this), but the
  metadata and notices must be correct *before* the permanent first push.
- **nuget.org account + scoped API key + ID-prefix reservation are Session-2 prerequisites** the
  owner must perform by hand (can't be automated): create the account, reserve `Dabbler.`,
  mint the scoped key, add the secret. Surface this explicitly at the start of Session 2.
- **One shared version across 15 packages couples all releases.** Acceptable now (monorepo,
  lockstep), but flag that the Templates package and the rarely-changing DevIdp.Tool are the two
  natural candidates to decouple later.
