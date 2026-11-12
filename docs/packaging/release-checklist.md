# Pre-Push Release Checklist

> Ledger §6.5 — gate before any `dotnet nuget push` to a public feed.
> All items must pass before publishing. See `tools/pack.ps1` for the hermetic pack pipeline.

## Step 1 — Non-Playwright tests pass (ProjectReference path)

```
dotnet test Dabbler.Platform.sln --filter "Category!=Playwright&Category!=E2E"
```

All tests green. No skipped-but-should-pass tests.

## Step 2 — Playwright tests pass (ProjectReference path)

The framework runs the Playwright tests as the `e2e` suite in `dabbler.yaml`
(`pwsh -NoProfile -File tools/e2e.ps1`), from a cold start: infra stack up, a
freshly seeded `AcmeCoffeeOnline_Dev`, the app stack rebuilt. The operator only
needs Podman or Docker running and `../dabbler-uat-dsl/pack-output` present.

The `e2e` suite green.

## Step 3 — Hermetic pack succeeds

```
pwsh tools/pack.ps1 -Version <next-version>
```

- All 12 packages in the `tools/pack.ps1` allowlist produce `.nupkg` in `pack-output/`.
- `Tools/pack.ps1` exits 0 (no hermetic-check failures).
- Template + DevIdp.Tool (Session 8 deliverables) also packed before publishing those packages.

## Step 4 — Dabbler.Packaging.Smoke passes

```
dotnet test tests/Dabbler.Packaging.Smoke/ -- -e DABBLER_PACK_OUTPUT=<abs-path>/pack-output
```

`PackOutput_ContainsAllRequiredLibraryNupkgs` passes.
After Session 8/9: all smoke tests pass (sign-in, secured page, sample report, PDF export, scaffolded bUnit).

## Step 5 — Dabbler.PublicSurface.Tests passes

```
dotnet test tests/Dabbler.PublicSurface.Tests/ -- -e DABBLER_PACK_OUTPUT=<abs-path>/pack-output
```

All artifact-content assertions pass (README present, icon present, MIT license, repository metadata,
THIRD-PARTY-NOTICES.txt in Reports.* packages).

## Step 6 — Change-log entry exists

`docs/session-sets/packaging-and-template-readiness/change-log.md` has an entry for the new version
describing what changed since the last published version.

## Publish order (staged — per ledger §6.4)

1. `Dabbler.Components` · `Dabbler.Api.Querying`
2. `Dabbler.Security` · `Dabbler.Reports.Core`
3. `Dabbler.Security.Blazor` · `Dabbler.Reports.Export`
4. `Dabbler.Reports.Blazor` · `Dabbler.Platform.Stubs`
5. `Dabbler.Testing.Blazor`
6. `Dabbler.Templates` · `Dabbler.Security.DevIdp.Tool` · `Dabbler.CrudSlice.Tool`

Validate install from staging feed after each batch before pushing the next.

## Rollback

- **Bad public version:** unlist on nuget.org (do NOT delete); publish a fixed `0.x.y-preview.N+1`.
- **Bad template:** publish a fixed template version (scaffolded apps are unaffected).
- **Partial publish:** unlist partially-published versions; fix cause; re-pack with the next preview; re-push the full set.
