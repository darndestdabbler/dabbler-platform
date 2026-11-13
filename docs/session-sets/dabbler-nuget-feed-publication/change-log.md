# Change Log — dabbler-nuget-feed-publication

> End-of-set change log (authored after the final session closes, per repo convention).
> Set goal: publish the `Dabbler.*` packages to a real, restorable NuGet feed so
> downstream consumers (generated apps, the Access harvester, CI) restore from a
> feed instead of hand-copying `pack-output/*.nupkg` into a per-repo local
> `packages/` directory.

## Outcome

The `Dabbler.*` platform packages are **published and live on nuget.org** at
`0.1.0-preview.1` (anonymous restore, no auth token), the publish is repeatable
from a `nuget-v<semver>` git tag via OIDC trusted publishing, and the
`dabbler-app` template now restores them **feed-only** — proven end-to-end by a
clean-machine consumer-validation gate. The vendoring workaround that forced the
first downstream consumer to commit 11 `.nupkg` files into git is now removable.

## Session 1 — Feed approach + versioning decision (architecture)

- Recorded the feed/versioning/auth decision (`feed-publication-decision.md`) with
  a tri-provider review (Opus full-context + GPT-5.4 + Gemini 2.5 Pro).
- **Decision 1 — Feed + auth:** nuget.org, **anonymous read** (publish authenticated).
  Only option meeting the hard anonymous-read requirement at hundreds-of-repos scale;
  GitHub Packages rejected (read token required).
- **Decision 2 — Versioning:** `MAJOR.MINOR.PATCH-preview.<n>`, **tag-sourced**
  (`nuget-v<semver>`), plain integer `<n>` (no zero-pad), all packages share one
  `<DabblerVersion>`, consumers **pin exact** versions (no floating).
- **Decision 3 — Publish split:** **publish all 15 IDs** (the template hard-depends on
  `Dabbler.Platform.Stubs`, so excluding the `Stubs*` packages would break clean
  scaffold restore — ground-truth tiebreaker over GPT-5.4's exclude-Stubs suggestion).
- **Decision 4 — Local `packages/` fallback:** **remove from the default template**;
  feed-only `nuget.config` with `<clear/>`; offline restore becomes an opt-in documented
  procedure.
- **Decision 5 — Pipeline mechanics:** tag-trigger, build-once/promote, `--skip-duplicate`,
  `.snupkg` symbols, pre-first-push guardrails (dry-run, metadata/license completeness,
  package validation, prefix reservation).
- Verdict: VERIFIED (cross-provider).

## Session 2 — Publish pipeline + first publish

- `tools/pack.ps1`: added `-CleanOutput` / `-TreatWarningsAsErrors`; packs the 15-ID allowlist.
- `.github/workflows/build.yml` (renamed from `nuget-publish.yml`): `nuget-v<semver>`
  tag-triggered; `resolve-context` parses the tag → `DabblerVersion`; `validate` builds via
  the `Dabbler.Publish.slnf` solution filter + packs + runs PublicSurface/Packaging.Smoke
  tests; `publish-nuget-org` pushes to nuget.org.
- **Auth changed during execution:** scoped API key → **OIDC trusted publishing**
  (`NuGet/login@v1`, `permissions: id-token: write`; no long-lived secret). No approval
  gate possible (private repo without environment protection rules) — a `nuget-v…` tag
  auto-publishes.
- Four CI-environment-only failures debugged + fixed (tag-regex double-anchor; full-solution
  build pulling a sibling-repo feed → solution filter; two packable libs not `.sln` members →
  direct builds; `.slnx` + inline `--source` restore quirk → `nuget.config`).
- **First publish:** all 15 `Dabbler.*` packages (+ `.snupkg` symbols) pushed at
  `0.1.0-preview.1` (every push `Created`); owner accepted the full public set.
- Verification: ISSUES_FOUND (Gemini), dispositioned to **no blocking defects** — SDK-downgrade
  finding rejected as a false positive (platform targets net10.0), the SHA-pin-actions finding
  accepted as a deferred hardening follow-up (since applied in commit `dbf9b73`), minors
  applied/declined.

## Session 3 — Consumer wiring + remove the vendoring hack

- `templates/.../dabbler-app/nuget.config` → **feed-only** (`<clear/>` + nuget.org); removed
  the `local`/`./packages` source and the `packages/.gitkeep` scaffold (Decision 4).
- `src/DabblerApp.Web/Dockerfile` → removed `COPY packages/ packages/`; kept `COPY nuget.config .`
  so the in-image restore uses the feed-only config.
- `.gitignore` comment updated (ignore rules retained to protect an opt-in offline feed).
- Docs rewritten for feed-based restore + version pin/upgrade (template `README.md`
  "NuGet packages & versions" + "Offline restore"; `docs/packaging/consumer-readiness.md`
  status → published and "Local-Feed Only" limitation marked RESOLVED). `docs/getting-started.md`
  already used `dotnet new install Dabbler.Templates` and needed no change.
- **Consumer-validation gate (acceptance criterion) — GREEN:** packed the updated template →
  installed it → `dotnet new dabbler-app` → confirmed the generated `nuget.config` is feed-only
  with no `packages/` dir → cleared the NuGet http-cache and built with an **isolated package
  cache**, forcing a real fetch → 12 `Dabbler.*` packages (+ DabblerPdf) restored from nuget.org;
  build + scaffolded bUnit test passed (`session-3-review/consumer-gate.log`).
- Verification: **VERIFIED** (Gemini 2.5 Pro, cross-provider), zero Critical/Major.

## Downstream cleanup unblocked

`Dabbler.NorthwindToBlazor` (the consumer that originally hit the no-feed gap and had to vendor
11 `.nupkg` files into git so CI could build) can now:

- delete its vendored `packages/*.nupkg`,
- drop the interim `.gitignore` exceptions that un-ignored those binaries, and
- remove the local-feed source from its CI — restoring `Dabbler.*` from nuget.org instead.

## Notes / future considerations

- **No publish approval gate:** a `nuget-v…` tag auto-publishes (private repo without environment
  protection rules). A real gate would need GitHub Pro or a public repo.
- **Template→Platform.Stubs dependency:** whether the template *should* depend on
  `Dabbler.Platform.Stubs` (vs. a deliberate public stub-host contract) is a template-design
  question flagged for a future template review, not this set.
- **Preview binary compatibility:** preview packages are not guaranteed binary-compatible until
  `1.0`; a consumer rebuild is required when upgrading between previews.
