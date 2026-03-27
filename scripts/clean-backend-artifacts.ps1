<#
.SYNOPSIS
  Deletes MSBuild bin/obj and leftover backend artifact folders (see repo .gitignore).

.DESCRIPTION
  bin/ and obj/ are regenerated on every dotnet build — not part of source control.
  Folders like backend/build-*, ef-*, bin-verify-*, test-* are local scratch outputs.
  Deep nested copies (from past dotnet build -o … experiments) can exceed Windows path
  limits; this script uses \\?\ + cmd rmdir for reliable removal.

.PARAMETER WhatIf
  List paths that would be removed without deleting.

.EXAMPLE
  ./scripts/clean-backend-artifacts.ps1
.EXAMPLE
  ./scripts/clean-backend-artifacts.ps1 -WhatIf
#>
param(
    [switch]$WhatIf
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$backend = Join-Path $repoRoot 'backend'

if (-not (Test-Path (Join-Path $backend 'LowCodePlatform.Backend.csproj'))) {
    Write-Error "backend directory invalid: $backend"
    exit 1
}

function Get-LongPathPrefix([string]$LiteralPath) {
    if (-not (Test-Path -LiteralPath $LiteralPath)) { return $null }
    $full = (Get-Item -LiteralPath $LiteralPath).FullName
    if ($full.Length -ge 4 -and $full.Substring(0, 2) -eq '\\') { return $full }
    return '\\?\' + $full.TrimEnd('\')
}

function Remove-TreeRobust([string]$LiteralPath) {
    if (-not (Test-Path -LiteralPath $LiteralPath)) { return }
    $long = Get-LongPathPrefix $LiteralPath
    if ($null -eq $long) { return }
    if ($WhatIf) {
        Write-Host "Would remove: $LiteralPath"
        return
    }
    cmd /c "rmdir /s /q `"$long`""
    if (Test-Path -LiteralPath $LiteralPath) {
        Write-Warning "Could not fully remove: $LiteralPath (try closing IDE / retry)"
    }
    else {
        Write-Host "Removed: $LiteralPath" -ForegroundColor Green
    }
}

Write-Host "Backend root: $backend" -ForegroundColor Cyan

$dirPatterns = @('build-*', 'ef-*', 'bin-verify-*', 'test-*')
foreach ($pat in $dirPatterns) {
    Get-ChildItem -Path $backend -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like $pat } |
        ForEach-Object { Remove-TreeRobust $_.FullName }
}

$fixedPaths = @(
    (Join-Path $backend 'bin'),
    (Join-Path $backend 'obj'),
    (Join-Path $backend 'LowCodePlatform.Backend.Tests\bin'),
    (Join-Path $backend 'LowCodePlatform.Backend.Tests\obj')
)

foreach ($p in $fixedPaths) {
    Remove-TreeRobust $p
}

Write-Host "Done. Next: dotnet build backend/LowCodePlatform.Backend.csproj" -ForegroundColor Cyan
