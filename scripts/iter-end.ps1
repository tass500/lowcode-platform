#Requires -Version 5.1
<#
.SYNOPSIS
  Iteráció / milestone lezárása egy menetben: quality gate-ek, majd push → PR → CI várakozás → merge.

.DESCRIPTION
  1) (opcionális) dotnet test + npm run build
  2) gh-pr-push-merge.ps1 — push, PR (body: pr-body.md vagy -BodyFile), checks --watch, merge

  Előtte: frissítsd docs/live/02 és 03-at; állítsd össze a PR szöveget (pl. docs/templates/pr-body.example.md → pr-body.md).

.PARAMETER SkipTests
  Ne futtassa a backend teszteket.

.PARAMETER SkipFrontendBuild
  Ne futtassa az npm run build-et.

.PARAMETER BodyFile
  Átadva a gh-pr-push-merge.ps1-nek (PR leírás fájl).

.PARAMETER NoMerge
  Csak push + PR, merge nélkül.

.PARAMETER Squash
  Squash merge a gh-pr-push-merge.ps1-ben.

.PARAMETER Draft
  Draft PR (nem vár merge-re).

.EXAMPLE
  .\scripts\iter-end.ps1

.EXAMPLE
  .\scripts\iter-end.ps1 -NoMerge

.EXAMPLE
  .\scripts\iter-end.ps1 -BodyFile .\pr-body.md -SkipTests
#>
param(
  [switch]$SkipTests,
  [switch]$SkipFrontendBuild,
  [string]$BodyFile = "",
  [string]$Base = "master",
  [switch]$NoMerge,
  [switch]$Squash,
  [switch]$Draft
)

$ErrorActionPreference = "Stop"
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $RepoRoot

$backendTests = "backend/LowCodePlatform.Backend.Tests/LowCodePlatform.Backend.Tests.csproj"

if (-not $SkipTests) {
  Write-Host "==> dotnet test $backendTests"
  dotnet test $backendTests
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
} else {
  Write-Warning "Skipping dotnet test (-SkipTests)."
}

if (-not $SkipFrontendBuild) {
  Write-Host "==> npm run build (frontend/)"
  Push-Location (Join-Path $RepoRoot "frontend")
  npm run build
  $fe = $LASTEXITCODE
  Pop-Location
  if ($fe -ne 0) { exit $fe }
} else {
  Write-Warning "Skipping npm run build (-SkipFrontendBuild)."
}

$mergeScript = Join-Path $PSScriptRoot "gh-pr-push-merge.ps1"

Write-Host "==> $mergeScript (push / PR / merge pipeline)"
if ($BodyFile) {
  & $mergeScript -Base $Base -BodyFile $BodyFile -NoMerge:$NoMerge -Squash:$Squash -Draft:$Draft
} else {
  & $mergeScript -Base $Base -NoMerge:$NoMerge -Squash:$Squash -Draft:$Draft
}
