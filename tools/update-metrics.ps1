<#!
.SYNOPSIS
Run tests with coverage, generate coverage badge, run benchmarks, update baseline, and optionally launch web app.

.DESCRIPTION
Performs a full local metrics refresh:
 1. dotnet restore/build (Release)
 2. dotnet test with XPlat coverage collector
 3. Generate coverage-badge.json (same logic as CI thresholds coloring)
 4. Run benchmarks (Release)
 5. Copy latest *-report-github.md into baseline/BenchmarkBaseline.md
 6. (Optional) Launch web site to view /Performance

.PARAMETER LaunchWeb
If specified, starts the web project after updating metrics.

.PARAMETER SkipBuild
Skip restore/build (use when already built).

.PARAMETER Filter
BenchmarkDotNet filter pattern (defaults to '*').

.EXAMPLE
./tools/update-metrics.ps1 -LaunchWeb

.EXAMPLE
./tools/update-metrics.ps1 -Filter '*Export*'
#>
param(
    [switch]$LaunchWeb,
    [switch]$SkipBuild,
    [string]$Filter = '*'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Write-Host "Repo root: $repoRoot" -ForegroundColor Cyan
Push-Location $repoRoot

function Invoke-Step($name, [scriptblock]$action) {
    Write-Host "==> $name" -ForegroundColor Yellow
    try { & $action; Write-Host "✔ $name" -ForegroundColor Green }
    catch {
        Write-Host "✖ $name failed: $($_.Exception.Message)" -ForegroundColor Red
        Pop-Location
        exit 1
    }
}

if (-not $SkipBuild) {
    Invoke-Step 'Restore' { dotnet restore .\sql2csv.sln }
    Invoke-Step 'Build (Release)' { dotnet build .\sql2csv.sln -c Release --no-restore }
}

$testResultsDir = Join-Path $repoRoot 'TestResults'
if (-not (Test-Path $testResultsDir)) { New-Item -ItemType Directory -Path $testResultsDir | Out-Null }

Invoke-Step 'Test with coverage' { dotnet test .\Sql2Csv.Tests\Sql2Csv.Tests.csproj -c Release --no-build --collect:"XPlat Code Coverage" --results-directory $testResultsDir }

Invoke-Step 'Generate coverage badge' {
    $coverageFile = Get-ChildItem -Recurse -Filter coverage.cobertura.xml $testResultsDir | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
    if (-not $coverageFile) { throw 'coverage.cobertura.xml not found' }
    $xml = Get-Content $coverageFile.FullName -Raw
    $rateMatch = [regex]::Match($xml, 'line-rate="([0-9.]+)"')
    if (-not $rateMatch.Success) { throw 'line-rate attribute not found in coverage.cobertura.xml' }
    $rate = [double]$rateMatch.Groups[1].Value
    $pct = [math]::Round($rate * 100, 2)
    $color = if ($pct -ge 85) { 'brightgreen' } elseif ($pct -ge 70) { 'yellow' } else { 'orange' }
    $badge = @{ schemaVersion = 1; label = 'coverage'; message = ("{0:F2}%" -f $pct); color = $color } | ConvertTo-Json -Compress
    Set-Content -Path (Join-Path $repoRoot 'coverage-badge.json') -Value $badge -Encoding UTF8
    Write-Host "Coverage: $pct% ($color)" -ForegroundColor Green
}

Invoke-Step 'Run benchmarks' {
    Write-Host "Running benchmarks with filter: $Filter" -ForegroundColor DarkCyan
    # Quote the filter to avoid PowerShell wildcard expansion when '*'
    dotnet run -c Release --project .\Sql2Csv.Benchmarks -- --filter "$Filter" | Out-Null
    $resultsDirCheck = Join-Path $repoRoot 'Sql2Csv.Benchmarks\BenchmarkDotNet.Artifacts\results'
    if (-not (Test-Path $resultsDirCheck)) {
        Write-Warning "Benchmark results directory not found after first run. Retrying with explicit '*' literal."
        dotnet run -c Release --project .\Sql2Csv.Benchmarks -- --filter '*' | Out-Null
    }
}

Invoke-Step 'Update benchmark baseline' {
    $rootResults = Join-Path $repoRoot 'BenchmarkDotNet.Artifacts\results'
    $projectResults = Join-Path $repoRoot 'Sql2Csv.Benchmarks\BenchmarkDotNet.Artifacts\results'
    $candidateDirs = @()
    if (Test-Path $rootResults) { $candidateDirs += $rootResults }
    if (Test-Path $projectResults) { $candidateDirs += $projectResults }
    if ($candidateDirs.Count -eq 0) {
        Write-Warning "No results directories found. Listing repo root for diagnostics:"
        Get-ChildItem -Name | Write-Host
        throw "Benchmark results not found. Expected at: $rootResults or $projectResults"
    }
    Write-Host "Searching benchmark markdown in: $($candidateDirs -join '; ')" -ForegroundColor DarkCyan
    $latest = $null
    foreach ($dir in $candidateDirs) {
        $file = Get-ChildItem $dir -Filter '*-report-github.md' -ErrorAction SilentlyContinue | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
        if ($file) { $latest = $file; break }
    }
    if (-not $latest) { throw "No *-report-github.md file found in: $($candidateDirs -join '; ')" }
    $baselineDir = Join-Path $repoRoot 'Sql2Csv.Benchmarks\baseline'
    if (-not (Test-Path $baselineDir)) { New-Item -ItemType Directory -Path $baselineDir | Out-Null }
    Copy-Item $latest.FullName (Join-Path $baselineDir 'BenchmarkBaseline.md') -Force
    Write-Host "Baseline updated from $($latest.FullName)" -ForegroundColor Green
}

Invoke-Step 'Copy coverage XML to web/data' {
    $coverageFile = Get-ChildItem -Recurse -Filter coverage.cobertura.xml $testResultsDir | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
    if (-not $coverageFile) { throw 'coverage.cobertura.xml not found' }
    $webDataDir = Join-Path $repoRoot 'sql2csv.web\data'
    if (-not (Test-Path $webDataDir)) { New-Item -ItemType Directory -Path $webDataDir | Out-Null }
    $destPath = Join-Path $webDataDir 'coverage.cobertura.xml'
    Copy-Item $coverageFile.FullName -Destination $destPath -Force
    Write-Host "Copying coverage XML from: $($coverageFile.FullName)" -ForegroundColor Magenta
    Write-Host "To: $destPath" -ForegroundColor Magenta
    if (Test-Path $destPath) {
        Write-Host "✔ coverage.cobertura.xml successfully copied to web/data folder" -ForegroundColor Green
    }
    else {
        Write-Host "✖ coverage.cobertura.xml copy failed!" -ForegroundColor Red
    }
}

if ($LaunchWeb) {
    Invoke-Step 'Launch web (Performance page at /Performance)' {
        dotnet run --project .\sql2csv.web
    }
}

Pop-Location
Write-Host 'All steps completed.' -ForegroundColor Cyan
