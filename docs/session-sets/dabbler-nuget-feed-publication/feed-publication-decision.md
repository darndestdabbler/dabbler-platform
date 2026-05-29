# Dabbler NuGet Feed Publication — Recorded Architecture Decision

> **Session 1 of 3 deliverable** (`dabbler-nuget-feed-publication`). No code this session —
> this is the recorded feed + versioning + auth decision with the tri-provider verdict that
> Sessions 2 (publish pipeline) and 3 (consumer wiring) implement.
> **Date:** 2026-05-29 · **Status:** DECIDED, owner-ratified on the locked inputs.

## Participants
- **Claude Opus 4.8 (orchestrator)** — contributed directly with full repository context
  (dependency graph, `pack.ps1`, `Directory.Packages.props`, template `nuget.config`,
  `pack-output/`). See [session-1-review/opus.md](session-1-review/opus.md).
- **GPT-5.4 (OpenAI)** — self-contained prompt. [session-1-review/gpt-5-4.md](session-1-review/gpt-5-4.md). $0.1814.
- **Gemini 2.5 Pro (Google)** — self-contained prompt. [session-1-review/gemini-pro.md](session-1-review/gemini-pro.md). $0.0281.

> **Context-asymmetry disclosure** (project-guidance Convention "Tri-Provider Review —
> Orchestrator Contribution Uses Full Context"): the Opus contribution was written in-session
> with full repo context; the two routed reviewers received self-contained prompts. This is
> intentional and not a protocol defect.

---

## Locked inputs (platform owner, this session — not relitigated)
- **Audience: public / open** — anyone may discover & restore `Dabbler.*` without contacting the maintainer.
- **Permanent public IDs on nuget.org are acceptable** (unlist ≠ delete).
- **Anonymous read is a hard requirement** — no auth token in any consumer repo or CI job.
- **Existing infra: GitHub Packages** (available); **no nuget.org account yet** (creating one + API key accepted).

---

## Decision 1 — Feed target + auth model: **nuget.org, anonymous read**
nuget.org is the only option satisfying the owner's hard anonymous-read requirement at the
"hundreds of independently-CI'd consumer repos" scale. GitHub Packages was rejected: it requires
a read token even for restore, which is the exact friction being removed. All three reviewers
concurred (the prompt locked this; none dissented). **Read is anonymous; publish is authenticated**
via a scoped API key held only in CI (Decision 5).

## Decision 2 — Versioning scheme: **`MAJOR.MINOR.PATCH-preview.<n>`, tag-sourced** *(unanimous)*
- SemVer 2.0 prerelease `0.1.0-preview.<n>` with **plain integer `<n>` — never zero-padded**.
  Padding (`preview.01`) is invalid SemVer and forces lexical comparison, breaking
  `preview.2 < preview.10` ordering. The dot before `<n>` is required (`preview10` sorts lexically).
- **The git tag is the single source of truth.** Publish triggers only on tag `nuget-v<semver>`
  (e.g. `nuget-v0.1.0-preview.2`); CI parses the tag → `/p:DabblerVersion=<parsed>`. Do **not**
  derive the version from CI run numbers. Re-running the same tag reproduces the same version.
  Tag prefix is `nuget-v` (spec-aligned; namespaced away from any plain `v<semver>` release tags
  the harvester-style flows use).
- **All 15 packages share one `<DabblerVersion>`** → consumers upgrade the set in lockstep.
- **Consumers pin exact versions** (ideally centralized in `Directory.Packages.props`); the
  template must generate **pinned** references — **no floating `0.1.0-preview.*`** (non-reproducible
  CI across hundreds of repos).
- `-preview` is the honest label during platform formation; move to `-rc.<n>` only when the API is
  genuinely frozen; drop the suffix for a stable `1.0.0` (tag `nuget-v1.0.0`).
- The stale local `0.1.0-session5` / `0.1.0-preview.1` artifacts in `pack-output/` are
  pre-publication noise; the first public push starts clean at `0.1.0-preview.1` on nuget.org.

## Decision 3 — Public/internal split: **publish all 15 IDs** *(resolved on template ground-truth)*
The publish allowlist must contain the full transitive closure of what `dotnet new dabbler-app`
and its test project restore. Verified evidence:
- The generated template has a hard `PackageReference` on **`Dabbler.Platform.Stubs`**, whose
  closure pulls `Stubs.Components`, `Stubs.Reports`, `Stubs.Security`. **Excluding the
  `Platform.Stubs*` packages would break clean-machine scaffold restore — the Session 3 gate.**
- `Dabbler.Components.Compat` is the upgrade/compat path; cheap; publish.
- `Dabbler.Templates` is the entry point (`dotnet new install Dabbler.Templates`); publish.
- `Dabbler.Security.DevIdp.Tool` (20 MB `dotnet tool`) is the dev-loop local-IdP tool; publish as
  a `dotnet tool` (never a `<PackageReference>`); 20 MB is well under nuget.org's limit.

**Allowlist (15):** Api.Querying, Components, Components.Compat, Reports.Core, Reports.Export,
Reports.Blazor, Security, Security.Blazor, Testing.Blazor, Platform.Stubs, Platform.Stubs.Components,
Platform.Stubs.Reports, Platform.Stubs.Security, Security.DevIdp.Tool, Templates.

**Divergence resolved:** GPT-5.4 recommended excluding the 4 `Platform.Stubs*` packages (permanent
public support contract; `Testing.Blazor` as the intended public test surface). That reasoning is
sound in the abstract but was made *without* the template-reference fact. Because the shipped
template depends on `Platform.Stubs`, exclusion is not viable now. **Noted future consideration**
(out of scope here): whether the template *should* depend on `Platform.Stubs` is a template-design
question; if a deliberate public stub-host contract is later wanted, redesign it then rather than
fossilizing today's internal topology — flag for a future template review, not this set.

## Decision 4 — Local `packages/` fallback: **remove from the default template** *(unanimous)*
- The current template `nuget.config` declares `nuget.org` + a local `./packages` source with **no
  `packageSourceMapping`** — the shadowing footgun (a stale local `.nupkg` at the pinned version
  silently satisfies restore and shadows the feed; invisible at hundreds-of-repos scale).
- **Default generated `nuget.config` = feed-only**, with `<clear/>` to drop inherited
  machine/user sources:
  ```xml
  <configuration>
    <packageSources>
      <clear />
      <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
    </packageSources>
  </configuration>
  ```
  (A `packageSourceMapping` for `Dabbler.*` → nuget.org is optional defense-in-depth and only
  matters if a second source is ever re-added; with a single source it's moot.)
- Delete the `packages/.gitkeep` scaffold from the default template.
- **Offline/air-gapped restore becomes an opt-in documented procedure** (a sample
  `NuGet.offline.config` the consumer passes via `--configfile`, or a documented local-feed
  script) — never committed by default in every generated repo.

## Decision 5 — Publish-pipeline mechanics + irreversibility guardrails *(strong convergence)*
- **Trigger:** push of tag `nuget-v<semver>`; CI parses → `DabblerVersion`; runs `pack.ps1`.
- **Build once / promote artifacts:** pack in a validate stage, publish the *exact* packed
  artifacts in a separate stage. `--skip-duplicate` only helps if a rerun pushes the *same bits* —
  do not rebuild on rerun.
- **Push:** `dotnet nuget push pack-output/*.nupkg --api-key $NUGET_API_KEY --source
  https://api.nuget.org/v3/index.json --skip-duplicate` (idempotent partial-failure recovery).
- **Symbols:** publish `.snupkg` for the libraries (`/p:IncludeSymbols=true
  /p:SymbolPackageFormat=snupkg`) + deterministic builds + Source Link + repository/commit
  metadata. `dotnet nuget push` auto-pushes the matching `.snupkg`.
- **Auth:** nuget.org API key stored as a **GitHub Environment secret** (`NUGET_API_KEY` in env
  e.g. `nuget-publish`), scoped on nuget.org to the **`Dabbler.*` glob, push-only**, with expiry.
  **Gate the publish job behind a protected Environment with required reviewers** — for every
  push initially (one approval click is cheap; a permanent bad package is not).
- **Before the FIRST public push (IDs are permanent):**
  1. **Dry-run the full pack→push mechanic against GitHub Packages** (owner already has it) using a
     throwaway version — validates the pipeline without burning nuget.org IDs.
  2. **Metadata/license completeness** on every package: `PackageLicenseExpression`,
     `RepositoryUrl`, `PackageReadmeFile`, icon, tags, authors. Each package's
     `PackageLicenseExpression` must state **that package's own license** (MIT for the Reports
     packages) — it is **not** changed to reflect a dependency's license. The third-party copyleft
     obligation is handled separately: `Reports.Core` / `Reports.Export` carry a copyleft
     *transitive dependency* — **`DabblerPdf 1.1.2` (LGPL-2.1-only OR MPL-2.0)**, already public on
     nuget.org — so those two packages must additionally ship `THIRD-PARTY-NOTICES.txt` with the
     canonical DabblerPdf license text. A wrong first-push license expression is permanent.
  3. **Package validation in CI** (`Microsoft.DotNet.PackageValidation` / `dotnet validate`); treat
     pack warnings as errors on the publish path.
  4. **Manual ID review** of every intended ID (typos/naming) before the gated push.
  5. **`Dabbler` ID-prefix reservation** on nuget.org — applied **immediately after** the first
     successful publish (nuget.org grants prefix reservation to an account that already owns
     matching published IDs; it is not a pre-push blocker).

---

## Tri-provider verdict summary
Row numbers match the **Decision N** headings above.

| # | Decision | Opus | GPT-5.4 | Gemini 2.5 Pro | Outcome |
|---|----------|------|---------|----------------|---------|
| 1 | Feed target + auth: nuget.org, anonymous read | (owner-locked) | concur | concur | **Owner-locked**; reviewers concur (only option meeting anonymous-read) |
| 2 | Versioning `preview.<n>`, tag-sourced, pin exact | ✓ | ✓ | ✓ | **Unanimous** |
| 3 | Publish split | publish 14, tool separate | publish 11 (exclude Stubs*) | publish all 15 | **All 15** — template hard-depends on Platform.Stubs (ground-truth tiebreaker) |
| 4 | Remove local `packages/` default; feed-only `<clear/>` | ✓ | ✓ | ✓ | **Unanimous** |
| 5 | Tag-trigger, `--skip-duplicate`, symbols, protected env, scoped key, dry-run + reserve | ✓ | ✓ (build-once/promote, smoke) | ✓ (GH-Packages dry-run, prefix reservation) | **Convergent** |

## Session 2 prerequisites — owner manual actions (cannot be automated)
1. Create a **nuget.org account/org**.
2. Mint an **API key scoped to `Dabbler.*`, push-only, with expiry**.
3. Add it as a **GitHub Environment secret** (`NUGET_API_KEY`) on a protected environment with
   required reviewers.
4. (Post-first-publish) apply for **`Dabbler` ID-prefix reservation**.

These are surfaced here so Session 2 can open by confirming they exist before any pipeline work.
