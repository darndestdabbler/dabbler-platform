#Requires -Version 7
<#
.SYNOPSIS
    Runs the Playwright E2E gate from a cold start: the `e2e` suite in dabbler.yaml.

.DESCRIPTION
    Picks a container engine (DABBLER_CONTAINER_ENGINE, else podman when it is on
    PATH and its machine answers, else docker), brings docker/docker-compose.infra.yml
    up, drops the AcmeCoffeeOnline_Dev database so the API creates and seeds it
    fresh (the volume and the other databases stay), rebuilds and starts
    docker/docker-compose.app.yml, waits for api and web to answer /health, then
    runs tests/AcmeCoffeeOnline.Playwright and exits with its exit code.

    A missing engine or a missing ../dabbler-uat-dsl/pack-output stops the script
    with a plain message and exit code 2 before any test runs.

.PARAMETER ResultsDirectory
    Where dotnet test writes e2e.trx. Default: TestResults/e2e under the repo root.

.PARAMETER HealthTimeoutSeconds
    How long to wait for sqlserver, and then for api and web, to report healthy.
#>
[CmdletBinding()]
param(
    [string]$ResultsDirectory,
    [int]$HealthTimeoutSeconds = 300
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $ResultsDirectory) { $ResultsDirectory = Join-Path $repoRoot 'TestResults/e2e' }

function Stop-Gate([string]$message) {
    Write-Host "e2e: $message"
    exit 2
}

function Invoke-Engine {
    & $script:engine @args
    if ($LASTEXITCODE -ne 0) { Stop-Gate "'$script:engine $($args -join ' ')' failed with exit code $LASTEXITCODE." }
}

function Test-EngineUp([string]$name) {
    if (-not (Get-Command $name -ErrorAction SilentlyContinue)) { return $false }
    & $name info *> $null
    return $LASTEXITCODE -eq 0
}

# --- Preconditions --------------------------------------------------------------

$engine = $env:DABBLER_CONTAINER_ENGINE
if ($engine) {
    if (-not (Test-EngineUp $engine)) { Stop-Gate "DABBLER_CONTAINER_ENGINE is '$engine', but '$engine info' does not answer. Start it and run again." }
} elseif (Test-EngineUp 'podman') {
    $engine = 'podman'
} elseif (Test-EngineUp 'docker') {
    $engine = 'docker'
} else {
    Stop-Gate 'Neither Podman nor Docker is running. Start one (podman machine start, or Docker Desktop) and run again.'
}
Write-Host "e2e: container engine is $engine"

$uatFeed = Join-Path $repoRoot '../dabbler-uat-dsl/pack-output'
if (-not (Test-Path $uatFeed)) {
    Stop-Gate "$uatFeed is missing. Check out dabbler-uat-dsl beside this repository and pack it to its pack-output/."
}

$infra = Join-Path $repoRoot 'docker/docker-compose.infra.yml'
$app = Join-Path $repoRoot 'docker/docker-compose.app.yml'

# --- Infrastructure and a fresh database -----------------------------------------

Invoke-Engine compose -f $infra up -d

$deadline = (Get-Date).AddSeconds($HealthTimeoutSeconds)
do {
    $status = & $engine inspect --format '{{.State.Health.Status}}' dabbler-sqlserver 2>$null
    if ($status -eq 'healthy') { break }
    if ((Get-Date) -gt $deadline) { Stop-Gate "dabbler-sqlserver is not healthy after $HealthTimeoutSeconds seconds (last status: '$status')." }
    Start-Sleep -Seconds 5
} while ($true)

Invoke-Engine compose -f $app down

$dropSql = "IF DB_ID('AcmeCoffeeOnline_Dev') IS NOT NULL BEGIN ALTER DATABASE [AcmeCoffeeOnline_Dev] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [AcmeCoffeeOnline_Dev]; END"
Invoke-Engine exec dabbler-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'DabblerPlatform_2026!' -C -b -Q $dropSql
Write-Host 'e2e: dropped AcmeCoffeeOnline_Dev; the API creates and seeds it on start'

# --- App stack -------------------------------------------------------------------

Invoke-Engine compose -f $app up --build -d

$endpoints = 'http://localhost:5011/health', 'http://localhost:5010/health'
$deadline = (Get-Date).AddSeconds($HealthTimeoutSeconds)
foreach ($url in $endpoints) {
    do {
        try {
            $code = (Invoke-WebRequest -Uri $url -TimeoutSec 5 -SkipHttpErrorCheck).StatusCode
        } catch {
            $code = 0
        }
        if ($code -eq 200) { Write-Host "e2e: $url is healthy"; break }
        if ((Get-Date) -gt $deadline) { Stop-Gate "$url is not healthy after $HealthTimeoutSeconds seconds (last status: $code)." }
        Start-Sleep -Seconds 5
    } while ($true)
}

# --- Playwright ------------------------------------------------------------------

New-Item -ItemType Directory -Force -Path $ResultsDirectory | Out-Null
Remove-Item -Force -ErrorAction SilentlyContinue (Join-Path $ResultsDirectory 'e2e.trx')

dotnet test (Join-Path $repoRoot 'tests/AcmeCoffeeOnline.Playwright/') `
    --blame-hang-timeout 10m `
    --logger 'trx;LogFileName=e2e.trx' `
    --results-directory $ResultsDirectory
exit $LASTEXITCODE
