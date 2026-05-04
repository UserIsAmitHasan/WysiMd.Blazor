#Requires -Version 5.1
<#
.SYNOPSIS
    Bumps the version, commits, and pushes a NuGet release tag.
.PARAMETER Version
    The new semantic version, e.g. 1.1.0
.EXAMPLE
    .\scripts\publish.ps1 1.1.0
#>
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version
)

$ErrorActionPreference = 'Stop'

# Resolve repo root regardless of where the script is called from
$repoRoot = Split-Path $PSScriptRoot -Parent
$csproj = Join-Path $repoRoot 'src\WysiMd.Blazor\WysiMd.Blazor.csproj'

# Git commands must run from repo root
Set-Location $repoRoot

# 1. Read current version
$xml = [xml](Get-Content $csproj)
$current = $xml.Project.PropertyGroup.Version
Write-Host "Current version : $current"
Write-Host "New version     : $Version"

if ($current -eq $Version) {
    Write-Error "Version $Version is already set in the csproj. Nothing to do."
}

# 2. Confirm
$confirm = Read-Host "Bump $current → $Version, commit, tag v$Version, and push? [y/N]"
if ($confirm -notmatch '^[Yy]$') {
    Write-Host "Aborted."
    exit 0
}

# 3. Ensure working tree is clean
$status = git status --porcelain
if ($status) {
    Write-Error "Working tree is not clean. Commit or stash your changes first."
}

# 4. Ensure on main and up to date
$branch = git rev-parse --abbrev-ref HEAD
if ($branch -ne 'main') {
    Write-Error "You must be on the 'main' branch to publish. Current branch: $branch"
}
git fetch origin main --quiet
$behind = git rev-list HEAD..origin/main --count
if ([int]$behind -gt 0) {
    Write-Error "Your branch is $behind commit(s) behind origin/main. Pull first."
}

# 5. Update csproj
(Get-Content $csproj -Raw) -replace "<Version>$current</Version>", "<Version>$Version</Version>" |
    Set-Content $csproj -Encoding utf8

Write-Host "Updated $csproj"

# 6. Commit and push
git add $csproj
git commit -m "chore: bump version to $Version"
git push origin main

# 7. Tag and push
git tag "v$Version"
git push origin "v$Version"

Write-Host ""
Write-Host "Released v$Version — watch the Actions tab on GitHub for the NuGet publish."
