#Requires -Version 7.0
<#
.SYNOPSIS
    Hermetic pack pipeline for Dabbler.* libraries (ledger §6.3 / A1).

.DESCRIPTION
    Packs all in-repo Dabbler.* packages to a local feed in pack-output/.
    Uses NuGet source mapping to guarantee that every package ID in the
    in-repo allowlist resolves from the local scratch feed rather than from
    nuget.org or the developer's global cache.  External packages (MudBlazor,
    bUnit, EF Core, DabblerPdf, Dabbler.Uat.Dsl.*, etc.) continue to restore
    from their approved feeds.

.PARAMETER Version
    The DabblerVersion to stamp across all packages (e.g. 0.1.0-preview.2).
    Defaults to the current Directory.Build.props value (0.1.0-preview.1).

.PARAMETER OutputDir
    Directory for produced .nupkg files. Default: ./pack-output

.PARAMETER ScratchCacheDir
    Isolated NuGet package cache used during the pack run.
    Default: ./pack-output/.nuget-cache

.PARAMETER CleanOutput
    Removes existing package artifacts and the hermetic config from OutputDir,
    and resets the scratch cache. Recommended for CI publication runs.

.PARAMETER TreatWarningsAsErrors
    Fails the pack when dotnet pack emits warnings. Recommended for publish CI.

.EXAMPLE
    pwsh tools/pack.ps1 -Version 0.1.0-preview.2
#>
param(
    [string] $Version       = "0.1.0-preview.1",
    [string] $OutputDir     = "pack-output",
    [string] $ScratchCacheDir = "pack-output/.nuget-cache",
    [switch] $CleanOutput,
    [switch] $TreatWarningsAsErrors
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ---------------------------------------------------------------------------
# Canonical in-repo package allowlist (ledger §6.3)
# ---------------------------------------------------------------------------
$InRepoPackageIds = @(
    "Dabbler.Api.Querying"
    "Dabbler.Components"
    "Dabbler.Components.Compat"
    "Dabbler.Platform.Stubs"
    "Dabbler.Platform.Stubs.Components"
    "Dabbler.Platform.Stubs.Reports"
    "Dabbler.Platform.Stubs.Security"
    "Dabbler.Reports.Core"
    "Dabbler.Reports.Export"
    "Dabbler.Reports.Blazor"
    "Dabbler.Security"
    "Dabbler.Security.Blazor"
    "Dabbler.Security.DevIdp.Tool"
    "Dabbler.CrudSlice.Tool"
    "Dabbler.Testing.Blazor"
    "Dabbler.Templates"
)

# ---------------------------------------------------------------------------
# Packable library projects (in dependency order)
# ---------------------------------------------------------------------------
$LibraryProjects = @(
    "src/Libs/Dabbler.Api.Querying/Dabbler.Api.Querying.csproj"
    "src/Libs/Dabbler.Components/Dabbler.Components.csproj"
    "src/Libs/Dabbler.Components.Compat/Dabbler.Components.Compat.csproj"
    "src/Libs/Dabbler.Reports.Core/Dabbler.Reports.Core.csproj"
    "src/Libs/Dabbler.Reports.Export/Dabbler.Reports.Export.csproj"
    "src/Libs/Dabbler.Reports.Blazor/Dabbler.Reports.Blazor.csproj"
    "src/Libs/Dabbler.Security/Dabbler.Security.csproj"
    "src/Libs/Dabbler.Security.Blazor/Dabbler.Security.Blazor.csproj"
    "src/Libs/Dabbler.Testing.Blazor/Dabbler.Testing.Blazor.csproj"
    "src/Libs/Dabbler.Platform.Stubs.Components/Dabbler.Platform.Stubs.Components.csproj"
    "src/Libs/Dabbler.Platform.Stubs.Reports/Dabbler.Platform.Stubs.Reports.csproj"
    "src/Libs/Dabbler.Platform.Stubs.Security/Dabbler.Platform.Stubs.Security.csproj"
    "src/Libs/Dabbler.Platform.Stubs/Dabbler.Platform.Stubs.csproj"
    # Session 8 deliverables
    "tools/Dabbler.Security.DevIdp.Tool/Dabbler.Security.DevIdp.Tool.csproj"
    # access-migration-generator-consumption Session 2: CrudSlice as a `dotnet tool`
    "tools/Dabbler.CrudSlice/Dabbler.CrudSlice.csproj"
    "templates/Dabbler.Templates/Dabbler.Templates.csproj"
)

# ---------------------------------------------------------------------------
# Setup
# ---------------------------------------------------------------------------
$RepoRoot = $PSScriptRoot | Split-Path -Parent
Push-Location $RepoRoot
try {
    $OutputAbsolute      = [IO.Path]::GetFullPath($OutputDir,     $RepoRoot)
    $ScratchCacheAbsolute = [IO.Path]::GetFullPath($ScratchCacheDir, $RepoRoot)

    if ($CleanOutput -and (Test-Path $ScratchCacheAbsolute)) {
        Remove-Item -Path $ScratchCacheAbsolute -Recurse -Force
    }

    New-Item -ItemType Directory -Path $OutputAbsolute      -Force | Out-Null
    New-Item -ItemType Directory -Path $ScratchCacheAbsolute -Force | Out-Null

    if ($CleanOutput) {
        Get-ChildItem -Path $OutputAbsolute -File |
            Where-Object {
                $_.Name -eq "hermetic.nuget.config" -or
                $_.Extension -in @(".nupkg", ".snupkg")
            } |
            Remove-Item -Force
    }

    Write-Host "==> Pack version : $Version"
    Write-Host "==> Output dir   : $OutputAbsolute"
    Write-Host "==> Scratch cache: $ScratchCacheAbsolute"
    Write-Host ""

    # ---------------------------------------------------------------------------
    # Write a hermetic nuget.config that source-maps in-repo IDs to local feed
    # ---------------------------------------------------------------------------
    $HermeticConfig = Join-Path $OutputAbsolute "hermetic.nuget.config"
    $LocalFeedUri   = $OutputAbsolute -replace '\\', '/'

    # Build packageSourceMapping entries for in-repo IDs
    $InRepoMappings = ($InRepoPackageIds | ForEach-Object {
        "      <package pattern=`"$_`" />"
    }) -join "`n"

    $NugetConfigXml = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local-pack-output" value="$LocalFeedUri" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <!-- In-repo packages MUST resolve from the local feed only -->
    <packageSource key="local-pack-output">
$InRepoMappings
    </packageSource>
    <!-- All other packages resolve from nuget.org -->
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
  <config>
    <add key="globalPackagesFolder" value="$($ScratchCacheAbsolute -replace '\\', '/')" />
  </config>
</configuration>
"@
    Set-Content -Path $HermeticConfig -Value $NugetConfigXml -Encoding UTF8
    Write-Host "==> Hermetic nuget.config written: $HermeticConfig"
    Write-Host ""

    # ---------------------------------------------------------------------------
    # Pack each library in order
    # ---------------------------------------------------------------------------
    $Failed = @()
    foreach ($Project in $LibraryProjects) {
        $ProjectPath = Join-Path $RepoRoot $Project
        if (-not (Test-Path $ProjectPath)) {
            Write-Warning "Skipping (not found): $Project"
            continue
        }

        Write-Host "--- Packing: $Project"
        $PackArgs = @(
            "pack"
            $ProjectPath
            "--configuration"
            "Release"
            "--no-build"
            "--output"
            $OutputAbsolute
            "/p:DabblerVersion=$Version"
            "/p:PackAsDependency=true"
            "/p:ContinuousIntegrationBuild=false"
        )

        if ($TreatWarningsAsErrors) {
            $PackArgs += "/p:TreatWarningsAsErrors=true"
        }

        $Result = dotnet @PackArgs 2>&1

        if ($LASTEXITCODE -ne 0) {
            Write-Warning "FAILED: $Project"
            $Result | Where-Object { $_ -match "error" } | ForEach-Object { Write-Warning $_ }
            $Failed += $Project
        } else {
            Write-Host "    OK"
        }
    }

    # ---------------------------------------------------------------------------
    # Post-pack: verify in-repo IDs resolve from local feed (hermeticity check)
    # ---------------------------------------------------------------------------
    Write-Host ""
    Write-Host "==> Verifying hermeticity (all in-repo packages present in local feed)..."
    $Nupkgs = Get-ChildItem -Path $OutputAbsolute -Filter "*.nupkg" | Select-Object -ExpandProperty Name
    $Missing = @()
    foreach ($Id in $InRepoPackageIds) {

        $Found = $Nupkgs | Where-Object { $_ -like "$Id.$Version.nupkg" }
        if (-not $Found) {
            $Missing += $Id
        }
    }

    if ($Missing.Count -gt 0) {
        Write-Warning "Hermeticity check: missing nupkg(s) for: $($Missing -join ', ')"
    } else {
        Write-Host "    All in-repo packages present in local feed."
    }

    # ---------------------------------------------------------------------------
    # Summary
    # ---------------------------------------------------------------------------
    Write-Host ""
    if ($Failed.Count -gt 0) {
        Write-Error "Pack FAILED for $($Failed.Count) project(s):`n$($Failed -join "`n")"
        exit 1
    } else {
        Write-Host "==> Pack complete. $($Nupkgs.Count) nupkg(s) in $OutputAbsolute"
    }
} finally {
    Pop-Location
}
