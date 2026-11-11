<#
.SYNOPSIS
    Gets a demo-day scratch folder ready.

.DESCRIPTION
    Creates ./demo-{yyyyMMdd}/ and copies each stage's "before" folder into it
    as demo-{yyyyMMdd}/{stage}/, plus every extras/<name> folder as
    demo-{yyyyMMdd}/extras/<name>/, then runs dotnet build in each copy so the
    first build on stage is warm. The stage folders themselves are never
    touched. demo-????????/ is gitignored.

.PARAMETER Date
    Override the folder date (yyyyMMdd). Defaults to today.

.PARAMETER NoBuild
    Skip the dotnet build step.

.PARAMETER Force
    If the target folder already exists, delete and recreate it.

.EXAMPLE
    ./prepare-demo.ps1
    ./prepare-demo.ps1 -Date 20261112 -Force
#>
[CmdletBinding()]
param(
    [string]$Date = (Get-Date -Format 'yyyyMMdd'),
    [switch]$NoBuild,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

$repoRoot = $PSScriptRoot
$target = Join-Path $repoRoot "demo-$Date"

if (Test-Path $target) {
    if ($Force) {
        Write-Host "Removing existing $target"
        Remove-Item -Recurse -Force $target
    } else {
        throw "$target already exists. Use -Force to recreate it, or -Date to pick another."
    }
}

# Warn if the SQL Server container isn't up (skipped without Docker, e.g. a Windows
# box using its default SQL Server instance). The console apps and integration
# tests need it, the unit tests don't.
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host "Docker not found; assuming a local SQL Server (default instance, sa login)."
} else {
    $sqlRunning = (docker ps --filter 'name=^sql_server$' --format '{{.Names}}' 2>$null) -eq 'sql_server'
    if (-not $sqlRunning) {
        Write-Warning "SQL Server container 'sql_server' is not running. Start it with ./start-sql-server-container.ps1 before demoing the console app, or ignore this if you're using a local SQL Server."
    }
}

$stages = Get-ChildItem -Path $repoRoot -Directory |
    Where-Object { $_.Name -match '^\d{2}-' -and (Test-Path (Join-Path $_.FullName 'before')) } |
    Sort-Object Name

if (-not $stages) {
    throw "No stage folders (NN-name/before) found under $repoRoot."
}

New-Item -ItemType Directory -Path $target | Out-Null
Write-Host "Created $target"

# Copy a source tree to the scratch folder without bin/obj/.vs/.idea/data so
# the copy builds clean.
function Copy-DemoTree([string]$src, [string]$dst) {
    $exclude = @('bin', 'obj', '.vs', '.idea', 'data')
    Get-ChildItem -Path $src -Recurse -Force |
        Where-Object {
            $rel = $_.FullName.Substring($src.Length).TrimStart('\', '/')
            -not ($rel -split '[\\/]' | Where-Object { $exclude -contains $_ })
        } |
        ForEach-Object {
            $rel = $_.FullName.Substring($src.Length).TrimStart('\', '/')
            $out = Join-Path $dst $rel
            if ($_.PSIsContainer) {
                New-Item -ItemType Directory -Path $out -Force | Out-Null
            } else {
                New-Item -ItemType Directory -Path (Split-Path $out) -Force | Out-Null
                Copy-Item $_.FullName $out
            }
        }
}

foreach ($stage in $stages) {
    Write-Host "  $($stage.Name)/before -> demo-$Date/$($stage.Name)"
    Copy-DemoTree (Join-Path $stage.FullName 'before') (Join-Path $target $stage.Name)
}

# Extras (extras/<name>) are standalone demos with no before/after; copy each
# one as-is so recordings never type into the repo copy.
$extrasRoot = Join-Path $repoRoot 'extras'
$extras = @()
if (Test-Path $extrasRoot) {
    $extras = Get-ChildItem -Path $extrasRoot -Directory
    foreach ($extra in $extras) {
        Write-Host "  extras/$($extra.Name) -> demo-$Date/extras/$($extra.Name)"
        Copy-DemoTree $extra.FullName (Join-Path $target 'extras' $extra.Name)
    }
}

if (-not $NoBuild) {
    $buildDirs = @($stages | ForEach-Object { Join-Path $target $_.Name }) +
                 @($extras | ForEach-Object { Join-Path $target 'extras' $_.Name })
    foreach ($dst in $buildDirs) {
        $sln = Get-ChildItem -Path $dst -Filter '*.slnx' | Select-Object -First 1
        if (-not $sln) { $sln = Get-ChildItem -Path $dst -Filter '*.sln' | Select-Object -First 1 }
        if (-not $sln) {
            Write-Warning "No solution file in $dst; skipping build."
            continue
        }
        Write-Host "Building $(Split-Path $dst -Leaf) ..."
        dotnet build $sln.FullName --nologo -v quiet
        if ($LASTEXITCODE -ne 0) { throw "Build failed for $dst." }
    }
}

Write-Host ""
Write-Host "Ready. Demo from: $target"
Write-Host "Stage folders under $repoRoot are untouched."
