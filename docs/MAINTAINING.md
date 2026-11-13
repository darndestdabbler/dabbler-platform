# Maintaining Dabbler Platform

This guide is for the person who keeps this repository building and
releasing. For what the platform ships, read
[`platform-overview.md`](platform-overview.md); for the detailed pre-publish
list, read [`packaging/release-checklist.md`](packaging/release-checklist.md).

## Clone, build, test and pack

You need the .NET 10 SDK and PowerShell 7. The `e2e` suite (Playwright) also
needs Podman or Docker running and the sibling repository `dabbler-uat-dsl` checked out beside this
one (`../dabbler-uat-dsl`) and packed to its `pack-output/`, as
`solution-dependencies.json` declares; `dabbler deps check` verifies it.

```bash
git clone https://github.com/darndestdabbler/dabbler-platform
cd dabbler-platform

dotnet build Dabbler.Platform.sln

# The `unit` suite, as dabbler.yaml declares it. -m:1 because the template
# tests share one `dotnet new` cache.
dotnet test Dabbler.Unit.slnf -m:1 --blame-hang-timeout 5m

# Pack all twelve packages to pack-output/.
pwsh tools/pack.ps1 -OutputDir pack-output -CleanOutput
```

With `pack-output/` present, the tests in `Dabbler.PublicSurface.Tests`,
`Dabbler.Templates.Tests` and `Dabbler.CrudSlice.Tests` that read the
nupkgs run instead of skipping.

## Where every gate lives

| Gate | What runs | Where | When |
|---|---|---|---|
| `unit` suite | `dotnet test Dabbler.Unit.slnf -m:1 --blame-hang-timeout 5m` | `dabbler.yaml` | Every session, as the run of record |
| `smoke` suite | `Dabbler.Packaging.Smoke` against `pack-output/` (scaffolds and builds `dabbler-app` and `dabbler-min`) | `dabbler.yaml` | Every bump session, after `pwsh tools/pack.ps1 -OutputDir pack-output -CleanOutput` |
| Push build | The same `unit` suite command | CI: `.github/workflows/ci.yml` | Every push to `master` and every pull request, Dependabot's included: the gate on `master` and on Dependabot pull requests |
| Package surface | `Dabbler.PublicSurface.Tests` against the packed nupkgs | CI: the *Validate And Pack* job in `.github/workflows/build.yml` | Every `nuget-v*` tag and every manual dispatch |
| Consumer smoke | `Dabbler.Packaging.Smoke` against the packed nupkgs | CI: the same *Validate And Pack* job | Every `nuget-v*` tag and every manual dispatch |
| `e2e` suite | `pwsh -NoProfile -File tools/e2e.ps1`: `tests/AcmeCoffeeOnline.Playwright` against the container stack, from a cold start | `dabbler.yaml` | Every session that changes `src/Apps`, `src/Libs`, `docker`, the Playwright tests or `Directory.Packages.props` (the MudBlazor pin among them), and before each release tag |

**The gate rule:** the framework runs Playwright as the `e2e` suite; it is
not in CI, because CI has no `../dabbler-uat-dsl` feed. Your part is to have
Podman or Docker running and `../dabbler-uat-dsl` packed. `tools/e2e.ps1`
picks Podman when its machine is up, otherwise Docker
(`DABBLER_CONTAINER_ENGINE` overrides), brings the infra stack up, drops
`AcmeCoffeeOnline_Dev` so the API seeds it fresh, rebuilds the app stack,
waits for `api` and `web` to be healthy, and runs the tests with a trx
logger into `TestResults/e2e/`. It stops with a plain message, not a test
failure, when no engine is up or the sibling feed is missing. The suite is
required for close (green since Session 22). `-Filter <expr>` runs a subset
of the tests after the same cold start.

To run the CI gates locally before tagging:

```bash
pwsh tools/pack.ps1 -OutputDir pack-output -CleanOutput
dotnet test tests/Dabbler.PublicSurface.Tests/
dotnet test tests/Dabbler.Packaging.Smoke/ --filter "FullyQualifiedName!~ServesBelowFiveHundred"
```

## Releasing

All twelve packages share one version, `DabblerVersion` in
`Directory.Build.props`. No nuget.org credential lives on a developer
machine: CI publishes through trusted publishing (OIDC).

1. **Bump session.** Run a session whose only work is to set
   `DabblerVersion` in `Directory.Build.props` to the new version. The
   `unit` suite runs as its run of record. The session also packs
   (`pwsh tools/pack.ps1 -OutputDir pack-output -CleanOutput`) and runs the
   `smoke` suite, which scaffolds the templates at the new version and
   builds them: the same consumer smoke CI runs before it publishes.
2. **E2E gate.** The framework runs the `e2e` suite against the bumped
   trunk, as described above; have Podman or Docker running.
3. **Tag by hand.** Push an annotated tag on that commit:

   ```bash
   git tag -a nuget-v<version> -m "Release <version>"
   git push origin nuget-v<version>
   ```

   `.github/workflows/build.yml` validates, packs, runs PublicSurface and
   Packaging.Smoke, then pushes to nuget.org. A manual dispatch of the same
   workflow with a version dry-runs the publish to GitHub Packages instead.
4. **Deprecate, then unlist, retired IDs.** Once the new version is listed
   on nuget.org, take every version of each package ID that is no longer
   published through both actions on nuget.org under *Manage package*:
   first deprecate it as *Legacy*, then unlist it (never delete it).
   Deprecation warns existing consumers at restore and names the
   replacement; unlisting hides the ID from new adopters. Today the
   retired IDs are:
   - `Dabbler.Platform.Stubs.Components`, `Dabbler.Platform.Stubs.Reports`
     and `Dabbler.Platform.Stubs.Security`: alternate package
     `Dabbler.Platform.Stubs`.
   - `Dabbler.Components.Compat`: no alternate package.

## Dependencies and the MudBlazor policy

`.github/dependabot.yml` opens a weekly NuGet update, grouping `MudBlazor`
in one pull request and the `Microsoft.*` 10.x packages in another (held
to 10.x). A Dependabot pull request merges only on a green check from the
push build (`.github/workflows/ci.yml`).

**MudBlazor policy:** adopt a new MudBlazor major within one quarter of its
release. A major move is its own session: move the pin, fix the libraries,
the reference app and the templates; the framework runs the `e2e` suite.
