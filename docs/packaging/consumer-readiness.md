# Consumer Readiness — Dabbler Platform 0.1.0-preview.1

> **Status:** Preview — **published to nuget.org** (anonymous restore) and
> validated end-to-end from the public feed.
> **Published:** Session 2 of `dabbler-nuget-feed-publication` — all 15 `Dabbler.*`
> packages at `0.1.0-preview.1` (verified live on nuget.org via OIDC trusted publishing).
> **Feed-restore validated:** 2026-05-29 (dabbler-nuget-feed-publication Session 3 — clean-machine scaffold + restore from nuget.org).
> **Local-pack baseline validated:** 2026-05-15 (packaging-and-template-readiness Session 9).

---

## What Has Been Validated

### NuGet Package Artifacts (14 packages)

All 14 packages are produced by `tools/pack.ps1` and the pack output passes the
presence check (`PackagingSmokeTests.PackOutput_ContainsAllRequiredLibraryNupkgs`).

The 12 library packages additionally pass artifact-content checks
(`Dabbler.PublicSurface.Tests` — the tool and template packages are excluded as
they carry different content conventions):

| Check | Target | Status |
|---|---|---|
| Package ID presence in pack output | All 14 packages | ✅ |
| README.md at package root | 12 library packages | ✅ |
| icon.png at package root | 12 library packages | ✅ |
| MIT license expression in .nuspec | 12 library packages | ✅ |
| `<repository>` pointing to GitHub | 12 library packages | ✅ |
| THIRD-PARTY-NOTICES.txt | Reports.{Core,Export,Blazor} | ✅ |

The three `Dabbler.Reports.*` packages include a `THIRD-PARTY-NOTICES.txt`
disclosing that DabblerPdf (the PDF engine) is dual-licensed
**LGPL-2.1-only OR MPL-2.0** — not MIT. Adopters performing license compliance
review must account for this.

### Template Install + Build

Both `dotnet new` templates install from the local pack feed and produce
scaffolded projects that build clean:

| Template | Short name | Build result |
|---|---|---|
| Dabbler App (full) | `dabbler-app` | ✅ installs, restores, builds clean (0 errors) |
| Dabbler App (minimal) | `dabbler-min` | ✅ installs, restores, builds clean (0 errors) |

Validation method: `PackagingSmokeTests.DabblerApp_Template_InstallsAndBuildsClean`
and `DabblerMin_Template_InstallsAndBuildsClean_SingleProjectShape` (see
`tests/Dabbler.Packaging.Smoke/PackagingSmokeTests.cs`). The `dabbler-min`
test was renamed in 2026-05-27 (`template-scaffolding-followup-fixes`
Session 2) when it gained explicit single-project shape assertions; the
build-clean contract is unchanged.

### Scaffolded bUnit Tests

The `dabbler-app` template's bUnit test project (`DabblerApp.Tests`) passes
after scaffold and restore:

| Check | Status |
|---|---|
| `SampleItemsPage_RendersGrid` bUnit test | ✅ 1 passed |

Validation method: `PackagingSmokeTests.DabblerApp_ScaffoldedBUnit_AllTestsPass`.
The test uses `Dabbler.Platform.Stubs` opt-in and `InMemory` EF Core — no
running app or database required for the bUnit pass.

---

## Known Limitations

### Not Validated In This Session

The following consumer scenarios are deferred to **Session 10 (Playwright E2E)**
and will be proven before the Session 11 human UAT handoff:

| Scenario | Reason deferred |
|---|---|
| Sign-in via magic link as `admin@local.test` | Requires a running app docker stack |
| Sample report renders + PDF export produces bytes | Requires a running app docker stack |
| `/admin/security` dashboard is reachable | Requires a running app docker stack |

### Binary Compatibility Caveat (Preview → 1.0)

The `Dabbler.Platform.Stubs` sub-package split (A3) moved types between assemblies
while preserving the namespace `Dabbler.Platform.Stubs`. This is **source compatible**
(existing `using` directives and code compile without changes) but **not binary
compatible** (already-compiled binaries referencing the old assembly location will
break without a rebuild).

For 0.1.0-preview.x: **a consumer rebuild is required after upgrading** between
preview versions if the assembly layout changed. Type forwarders are deferred to
a stable 1.0 release.

### Template Defaults: Feed-Only (RESOLVED 2026-05-29)

**This limitation is lifted.** The `Dabbler.* 0.1.0-preview.1` packages are now
published to nuget.org, and the `dabbler-app` template's generated `nuget.config`
is **feed-only** (`<clear/>` + nuget.org, anonymous). A consumer running
`dotnet restore` after scaffolding restores the full transitive closure from the
public feed — no local feed, no `pack-output/` copy step, and no auth token.
This was proven by the Session 3 clean-machine consumer gate (scaffold from the
packed template, isolated package cache, restore + build + scaffolded bUnit test
green against the live feed).

Offline / air-gapped restore is an opt-in documented procedure (see the
scaffolded app's `README.md` → "Offline restore"); it is not the default.

---

## Catch-Up Decision

**No catch-up session needed.** All packaging and template path gaps discovered
in Session 9 were fixed inline:

1. `Dabbler.Testing.Blazor` missing `icon.png` in nupkg — fixed in `Dabbler.Testing.Blazor.csproj`
2. Template `_Imports.razor` missing `@using Dabbler.Security.Authorization` and
   `@using Dabbler.Security.Blazor.Components` — fixed in both `dabbler-app` and `dabbler-min`
3. Template `Program.cs` missing `using Microsoft.AspNetCore.Authentication` — fixed in both templates
4. Template `App.razor` referencing `<DabblerAssetLinks />` (app-specific, not in template) — removed
5. Template `Routes.razor` referencing `<AccessDeniedCard />` (app-specific) — replaced with
   template-local `Components/Shared/AccessDeniedCard.razor`
6. `dabbler-min/Login.razor` condensed `@if { } else { }` breaking Razor parser — expanded to
   multi-line format
7. `SampleItemListTests.cs` referencing non-existent namespace `Dabbler.Platform.Stubs.Components`
   (all types are in `Dabbler.Platform.Stubs`) — removed wrong using + redundant csproj reference

All fixes are self-contained within this session set. Sessions 10 (E2E) and 11 (UAT)
proceed as planned.

---

## Next Steps For External Consumers

The packages are live on nuget.org — consumers no longer pack or publish anything:

1. **Install the template.** `dotnet new install Dabbler.Templates` (append
   `::0.1.0-preview.1` to pin a version).
2. **Scaffold.** `dotnet new dabbler-app -o MyApp --name MyApp` or
   `dotnet new dabbler-min -o MyApp --name MyApp`.
3. **Restore + build.** `dotnet build` — the `Dabbler.*` packages restore from
   nuget.org automatically (feed-only `nuget.config`, anonymous).
4. **Configure SQL Server.** Update `appsettings.json` `DefaultConnection` to your
   SQL Server instance.
5. **Start Mailpit/MailHog.** Needed for magic-link email delivery in development.
6. **Run.** `dotnet run` from the scaffolded project dir (or the Docker Compose
   path); sign in as `admin@local.test`.

> **Maintainers** publishing a new platform version follow
> `docs/packaging/release-checklist.md` (pack → validate → tag `nuget-v<semver>`
> → CI publishes via OIDC trusted publishing). Consumers do not run this.

See `docs/getting-started.md` for the full walkthrough.
