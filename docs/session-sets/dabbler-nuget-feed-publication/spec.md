# Dabbler NuGet Feed Publication Spec

> **Purpose:** Publish the `Dabbler.*` packages to a real, restorable NuGet
> feed so downstream consumers — generated apps, the Access harvester, and CI —
> restore them from a feed instead of hand-copying `pack-output/*.nupkg` into a
> per-repo local `packages/` directory. Removes the only thing currently
> forcing consumers to vendor binaries into git.
> **Created:** 2026-05-29
> **Session Set:** `docs/session-sets/dabbler-nuget-feed-publication/`
> **Prerequisite:** `packaging-and-template-readiness` complete (the packable
> surface + `tools/pack.ps1` + versioning baseline this set publishes).
> **Workflow:** Orchestrator -> AI Router -> Cross-provider verification.
> **Status of this document:** **FINALIZED & SCHEDULED** (platform team, 2026-05-29).
> Originally drafted by a downstream consumer (`Dabbler.NorthwindToBlazor`, Set 001
> Session 3); the platform team finalized it (see *Finalization checklist*) and Session 1
> recorded the feed/versioning/auth decision — see `feed-publication-decision.md`.

---

## Session Set Configuration

```yaml
requiresUAT: false
requiresE2E: false
uatScope: none
tier: full
prerequisites: [packaging-and-template-readiness]
totalSessions: 3   # provisional — see Finalization checklist
```

> Rationale: this is packaging/distribution infrastructure with no new
> user-visible UI surface, so no UAT/E2E. The acceptance proof is a
> **consumer-validation gate**: scaffold a fresh `dabbler-app`, restore the
> `Dabbler.*` packages from the published feed on a clean machine (no local
> `pack-output/`), and build green. Cross-provider verification per session.

---

## Why This Set, Why Now

The first real downstream consumer of the packaged platform — the
`Dabbler.NorthwindToBlazor` migration repo — hit the gap on 2026-05-29. Its
generated app references seven `Dabbler.*` preview packages
(`0.1.0-preview.1`: Components, Reports.*, Security.*, Testing.Blazor) plus a
transitive closure of 11. There is **no published feed**, so:

- Local builds only work because the developer hand-copied `pack-output/*.nupkg`
  into the app's `packages/` directory (wired via `nuget.config`).
- To get **CI** to build at all, that consumer had to **vendor 11 `.nupkg`
  files into git** (un-ignoring them) — a documented interim hack.

This does not scale. The stated target is migrating **hundreds** of Access
databases into generated apps; each generated repo + its CI cannot be expected
to vendor platform binaries or hand-sync `pack-output/`. The durable fix is a
feed the template wires up once and every consumer restores from.

This mirrors the precedent already set by `dabbler-access-harvester`, which
ships its binary via **GitHub Releases** (tagged `v<semver>`, deterministic,
checksum-verified) so consumers install without contacting the maintainer. The
platform needs the NuGet equivalent.

---

## Sessions

### Session 1 of 3: Feed approach + versioning decision (architecture)

**Goal:** Choose the feed and the versioning scheme; settle the consumer-auth
tradeoff before any pipeline work. Route as `architecture` + tri-provider
review — this is a hard-to-reverse distribution decision.

**Decisions to make (with the tradeoffs to weigh):**
- **Feed target:**
  - *nuget.org* — frictionless **anonymous** restore for any consumer/CI; but
    publishing `Dabbler.*`-prefixed preview packages publicly is effectively
    permanent (IDs can't be reused; unlist ≠ delete) and claims the public ID
    namespace.
  - *GitHub Packages (NuGet)* — private/org-scoped, fits the existing GitHub
    release pattern; **but requires authentication even for read**, re-introducing
    a token requirement for every consumer + CI (the very friction we're removing
    for the hundreds-of-DBs case).
  - *Azure Artifacts / other hosted feed* — middle ground; auth model TBD.
- **Versioning scheme:** today both `0.1.0-preview.1` and `0.1.0-session5`
  variants exist in `pack-output/`. Pick a single, monotonic preview scheme
  (e.g. CI-stamped `0.1.0-preview.<n>` or `-rc.<n>`) so consumers pin a real
  version, and define how a consumer upgrades.
- **Public vs. private contract:** are these packages intended for arbitrary
  external consumers, or only for first-party generated apps + the harvester?
  This drives the feed choice more than anything else.

**Ends with:** a recorded decision (feed + versioning + auth model) with the
tri-provider verdict; no code yet.

### Session 2 of 3: Publish pipeline

**Goal:** A deterministic publish workflow that packs and pushes `Dabbler.*` to
the chosen feed on a version tag — the NuGet analogue of the harvester's
`release.yml`.

**Steps:**
1. Extend/parameterize `tools/pack.ps1` to stamp the agreed version and pack the
   public package set (exclude internal-only packages).
2. Add a GitHub Actions workflow triggered on a `nuget-v<semver>` tag that runs
   the pack script and pushes to the feed (with the auth secret if the feed
   needs one). Mirror the harvester release pattern: draft/review where
   appropriate, deterministic output.
3. Publish the first real version of the current package set.

**Ends with:** the package set is restorable from the feed at a pinned version;
the publish is repeatable from a tag.

### Session 3 of 3: Consumer wiring + remove the vendoring hack

**Goal:** Generated apps and CI restore from the feed with zero hand-copying.

**Steps:**
1. Update the `dabbler-app` template's `nuget.config` to reference the published
   feed as the `Dabbler.*` source (keep the local `./packages` source as an
   optional offline/dev fallback, documented).
2. Update packaging/consumer docs (and the template README) to describe
   feed-based restore + how to pin/upgrade versions; remove the "copy from
   pack-output/" instruction as the primary path.
3. **Consumer-validation gate:** scaffold a fresh app on a clean checkout (no
   `pack-output/`, empty `packages/`), restore from the feed, build + test green.
4. Note the downstream cleanup this unblocks: `Dabbler.NorthwindToBlazor` can
   delete its vendored `packages/*.nupkg` and its `harvest`-style interim
   `.gitignore` exceptions, and its CI can drop the local feed. Closeout +
   ROADMAP/state.

**Ends with:** the template points at the feed; a clean-machine scaffold builds
from the feed; the vendoring workaround is documented as removable downstream.

---

## Open decisions for the platform team

1. **Public (nuget.org) vs. authenticated (GitHub Packages)** — the crux. The
   hundreds-of-DBs + per-repo-CI goal strongly favors an **anonymous-read** feed
   (no token in every consumer/CI); weigh that against publishing preview IDs
   publicly. Consumer's lean: anonymous-read feed.
2. **Which packages are public** vs. internal-only (e.g. the 20 MB
   `Dabbler.Security.DevIdp.Tool` is a dev tool, not an app dependency).
3. **Version scheme** and the consumer upgrade story.
4. Whether to keep the local `packages/` fallback in the template at all once
   the feed exists.

## Finalization checklist (platform team, in this repo) — COMPLETE (2026-05-29)

- [x] Confirm/adjust slug, `totalSessions`, Session Set Configuration block. (3 sessions, config unchanged.)
- [x] Author `ai-assignment.md` (per-session orchestrator + routing). (Routed analysis → Gemini 2.5 Pro.)
- [x] Create the `session-state.json` stub (v4 shape, omit-null). (Present; Session 1 now complete/VERIFIED.)
- [x] ~~Add a ROADMAP row~~ — N/A: this repo does not keep a session-set ROADMAP
      (`phase-3-platform-roadmap.md` tracks platform phases, not session sets).

---

> **Provenance:** drafted from `Dabbler.NorthwindToBlazor` Set 001 Session 3
> (2026-05-29), where the absence of a feed forced vendoring 11 `Dabbler.*`
> preview `.nupkg` files into the consumer repo so CI could build offline.
