# Maintaining Dabbler Platform

This guide is for the person who keeps this repository building and
releasing. For what the platform ships, read
[`platform-overview.md`](platform-overview.md); for the detailed pre-publish
list, read [`packaging/release-checklist.md`](packaging/release-checklist.md).

## Clone, build, test and pack

You need the .NET 10 SDK and PowerShell 7. The Playwright gate also needs
Docker and the sibling repository `dabbler-uat-dsl` checked out beside this
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
| Playwright | `tests/AcmeCoffeeOnline.Playwright` against the Docker stack | Manual: run it yourself | Before each release tag, and in every session that changes the reference app's UI or the MudBlazor pin |

**The gate rule:** Playwright is in neither `dabbler.yaml` nor CI, because
it needs the Docker stack and has no skip guard. That leaves it to you. Run
it before you push a release tag, and in any session that touches the
reference app's UI (`src/Apps/AcmeCoffeeOnline.Web/`) or moves `MudBlazor`
in `Directory.Packages.props`:

```bash
docker compose -f docker/docker-compose.infra.yml up -d         # sqlserver + mailhog, leave up
docker compose -f docker/docker-compose.app.yml   up --build -d  # api + web, rebuild after changes
dotnet test tests/AcmeCoffeeOnline.Playwright/
```

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
2. **Playwright gate.** Run the Playwright suite against the bumped trunk,
   as described above.
3. **Tag by hand.** Push an annotated tag on that commit:

   ```bash
   git tag -a nuget-v<version> -m "Release <version>"
   git push origin nuget-v<version>
   ```

   `.github/workflows/build.yml` validates, packs, runs PublicSurface and
   Packaging.Smoke, then pushes to nuget.org. A manual dispatch of the same
   workflow with a version dry-runs the publish to GitHub Packages instead.
4. **Unlist retired IDs.** Once the new version is listed on nuget.org,
   unlist (never delete) every version of each package ID that is no longer
   published, so that new adopters do not find it. Today that is
   `Dabbler.Components.Compat`, `Dabbler.Platform.Stubs.Components`,
   `Dabbler.Platform.Stubs.Reports` and `Dabbler.Platform.Stubs.Security`.
   Unlisting is done on nuget.org under *Manage package*.

## Dependencies and the MudBlazor policy

`.github/dependabot.yml` opens a weekly NuGet update, grouping `MudBlazor`
in one pull request and the `Microsoft.*` 10.x packages in another (held
to 10.x). A Dependabot pull request merges only on a green check from the
push build (`.github/workflows/ci.yml`).

**MudBlazor policy:** adopt a new MudBlazor major within one quarter of its
release. A major move is its own session: move the pin, fix the libraries,
the reference app and the templates, and run the Playwright gate.
