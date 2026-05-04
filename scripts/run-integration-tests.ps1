#Requires -Version 5.1
<#
.SYNOPSIS
    Runs WysiMd.Blazor integration tests locally in headed (visible) browser mode.

.DESCRIPTION
    1. Builds the integration test project so the Playwright CLI is available.
    2. Checks whether Chromium is installed; installs it if not.
    3. Starts the sample app on http://localhost:5100 if it is not already responding.
    4. Runs the Playwright tests with HEADED=1 so the browser is visible.
    5. Stops any sample app process this script started when done.

.PARAMETER BaseUrl
    Override the sample app URL (default: http://localhost:5100).
#>
param(
    [string]$BaseUrl = "http://localhost:5100"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot   = Split-Path $PSScriptRoot -Parent
$SampleProj = Join-Path $RepoRoot "samples\WysiMd.Blazor.Sample"
$TestProj   = Join-Path $RepoRoot "tests\WysiMd.Blazor.IntegrationTests"

function Write-Step([string]$msg) {
    Write-Host ""
    Write-Host "==> $msg" -ForegroundColor Cyan
}

function Test-AppResponding([string]$url) {
    try {
        $r = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 3 -ErrorAction Stop
        return $r.StatusCode -eq 200
    } catch {
        return $false
    }
}

# --- 1. Build test project ---------------------------------------------------

Write-Step "Building integration test project..."
dotnet build $TestProj --configuration Debug --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw "Build failed." }

# --- 2. Check / install Playwright Chromium ----------------------------------

Write-Step "Checking Playwright Chromium installation..."

$browsersPath = Join-Path $env:LOCALAPPDATA "ms-playwright"
$chromiumDirs = if (Test-Path $browsersPath) {
    @(Get-ChildItem $browsersPath -Directory -Filter "chromium*")
} else {
    @()
}
$chromiumOk = $chromiumDirs.Count -gt 0

if ($chromiumOk) {
    Write-Host "  Chromium already installed." -ForegroundColor Green
} else {
    Write-Host "  Chromium not found - installing..." -ForegroundColor Yellow

    $playwrightPs1 = Get-ChildItem (Join-Path $TestProj "bin") -Recurse -Filter "playwright.ps1" -ErrorAction SilentlyContinue |
                     Select-Object -First 1

    if ($playwrightPs1) {
        & pwsh -File $playwrightPs1.FullName install chromium
    } else {
        $pwExe = Get-ChildItem (Join-Path $TestProj "bin") -Recurse -Filter "playwright.ps1" -ErrorAction SilentlyContinue |
                 Select-Object -First 1
        if (-not $pwExe) { throw "Cannot find playwright.ps1. Ensure 'dotnet build' succeeded." }
        & pwsh -File $pwExe.FullName install chromium
    }

    if ($LASTEXITCODE -ne 0) { throw "Playwright Chromium installation failed." }
    Write-Host "  Chromium installed." -ForegroundColor Green
}

# --- 3. Start sample app if not already running ------------------------------

$sampleProcess = $null

Write-Step "Checking sample app at $BaseUrl..."

if (Test-AppResponding $BaseUrl) {
    Write-Host "  Sample app already running." -ForegroundColor Green
} else {
    Write-Host "  Starting sample app..." -ForegroundColor Yellow

    $sampleArgs    = "run --project ""$SampleProj"" --urls $BaseUrl"
    $sampleProcess = Start-Process `
        -FilePath   "dotnet" `
        -ArgumentList $sampleArgs `
        -PassThru `
        -WindowStyle Hidden

    $deadline = (Get-Date).AddSeconds(60)
    $ready    = $false
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 2
        if (Test-AppResponding $BaseUrl) { $ready = $true; break }
    }

    if (-not $ready) {
        if ($null -ne $sampleProcess) {
            Stop-Process -Id $sampleProcess.Id -Force -ErrorAction SilentlyContinue
        }
        throw "Sample app did not become ready within 60 s."
    }
    Write-Host "  Sample app ready." -ForegroundColor Green
}

# --- 4. Run integration tests (headed) ---------------------------------------

Write-Step "Running integration tests (headed)..."

$testExitCode = 0
try {
    $env:HEADED          = "1"
    $env:WYSIMD_BASE_URL = $BaseUrl

    $runSettings = Join-Path $TestProj "integration.runsettings"
    dotnet test $TestProj --settings $runSettings --verbosity normal
    $testExitCode = $LASTEXITCODE
} finally {
    if ($null -ne $sampleProcess) {
        Write-Step "Stopping sample app (PID $($sampleProcess.Id))..."
        Stop-Process -Id $sampleProcess.Id -Force -ErrorAction SilentlyContinue
    }
    Remove-Item Env:\HEADED          -ErrorAction SilentlyContinue
    Remove-Item Env:\WYSIMD_BASE_URL -ErrorAction SilentlyContinue
}

if ($testExitCode -ne 0) {
    Write-Host ""
    Write-Host "Test run FAILED (exit $testExitCode)." -ForegroundColor Red
    exit $testExitCode
}

Write-Host ""
Write-Host "All integration tests passed." -ForegroundColor Green
