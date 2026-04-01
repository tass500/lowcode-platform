#Requires -Version 5.1
<#
.SYNOPSIS
  Push current branch, open PR to main (if missing), optionally wait for CI and merge.

.DESCRIPTION
  Intended workflow (see docs/DEVELOPMENT_WORKFLOW.md):
  - You are on a feature branch with commits (not on main).
  - Script: git push, gh pr create (or reuse open PR), gh pr checks --watch, gh pr merge.

  Optional credentials: copy .env.example -> .env and set GH_TOKEN (never commit .env).

.PARAMETER Base
  Base branch (default: main).

.PARAMETER Title
  PR title (default: last commit subject).

.PARAMETER Body
  PR body (default: last commit body or placeholder).

.PARAMETER BodyFile
  Path to a markdown file used as PR body (overrides -Body when set and file exists). Example: pr-body.md (gitignored).

.PARAMETER Draft
  Create as draft PR.

.PARAMETER NoMerge
  Stop after PR exists / is created (do not wait for checks or merge).

.PARAMETER Squash
  Use squash merge instead of merge commit.

.EXAMPLE
  .\scripts\gh-pr-push-merge.ps1

.EXAMPLE
  .\scripts\gh-pr-push-merge.ps1 -NoMerge

.EXAMPLE
  $env:GH_TOKEN = 'ghp_...'; .\scripts\gh-pr-push-merge.ps1
#>
param(
  [string]$Base = "main",
  [string]$Title = "",
  [string]$Body = "",
  [string]$BodyFile = "",
  [switch]$Draft,
  [switch]$NoMerge,
  [switch]$Squash
)

$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $RepoRoot

function Import-RepoDotEnv {
  $envPath = Join-Path $RepoRoot ".env"
  if (-not (Test-Path $envPath)) { return }
  foreach ($line in Get-Content $envPath) {
    $t = $line.Trim()
    if ($t.StartsWith("#") -or $t -eq "") { continue }
    $eq = $t.IndexOf("=")
    if ($eq -lt 1) { continue }
    $key = $t.Substring(0, $eq).Trim()
    $val = $t.Substring($eq + 1).Trim()
    if (($val.StartsWith('"') -and $val.EndsWith('"')) -or ($val.StartsWith("'") -and $val.EndsWith("'"))) {
      $val = $val.Substring(1, $val.Length - 2)
    }
    [Environment]::SetEnvironmentVariable($key, $val, "Process")
  }
}

Import-RepoDotEnv

# gh accepts GITHUB_TOKEN; many docs use GH_TOKEN
if ($env:GH_TOKEN -and -not $env:GITHUB_TOKEN) {
  $env:GITHUB_TOKEN = $env:GH_TOKEN
}

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
  Write-Error "GitHub CLI (gh) not found. Install: winget install GitHub.cli"
}

$branch = (git rev-parse --abbrev-ref HEAD).Trim()
if ($branch -eq $Base) {
  Write-Error "You are on '$Base'. Create/switch to a feature branch first (e.g. feat/iter-NN-topic)."
}

Write-Host "==> Fetch origin/$Base"
git fetch origin $Base

$mergeBase = git merge-base "HEAD" "origin/$Base"
$baseTip = git rev-parse "origin/$Base"
if ($mergeBase -ne $baseTip) {
  Write-Warning "HEAD is not based on latest origin/$Base (merge-base differs). Consider: git rebase origin/$Base or merge origin/$Base"
}

Write-Host "==> Push branch $branch"
git push -u origin HEAD

function Normalize-PrList($obj) {
  if ($null -eq $obj) { return @() }
  if ($obj -is [array]) { return $obj }
  return @($obj)
}

$listed = gh pr list --head $branch --state open --json number,url 2>$null | ConvertFrom-Json
$listed = Normalize-PrList $listed
$prNumber = $null
if ($listed.Count -gt 0) {
  $prNumber = [int]$listed[0].number
  Write-Host "==> Open PR already exists: #$prNumber $($listed[0].url)"
}
else {
  if (-not $Title) {
    $Title = (git log -1 --pretty=%s)
  }
  if (-not $Body) {
    $bf = $BodyFile
    if (-not $bf) {
      $rootPr = Join-Path $RepoRoot "pr-body.md"
      if (Test-Path $rootPr) { $bf = $rootPr }
    }
    if ($bf -and (Test-Path $bf)) {
      $Body = Get-Content -LiteralPath $bf -Raw
    } else {
      $Body = (git log -1 --pretty=%b)
      if (-not $Body.Trim()) {
        $Body = "See commits.`n`nTest plan: dotnet test backend/... ; npm run build (frontend/)"
      }
    }
  }
  $draftArg = @()
  if ($Draft) { $draftArg = @("--draft") }
  Write-Host "==> Create PR -> $Base"
  gh pr create --base $Base --title $Title --body $Body @draftArg
  $prNumber = [int](gh pr view --json number -q .number)
}

if ($NoMerge) {
  Write-Host "==> Done (-NoMerge). Review PR and merge manually or re-run without -NoMerge."
  exit 0
}

if ($Draft) {
  Write-Host "==> Draft PR — skipping checks watch and merge. Publish the PR first."
  exit 0
}

Write-Host "==> Wait for checks on PR #$prNumber"
gh pr checks $prNumber --watch

$mergeable = gh pr view $prNumber --json mergeStateStatus -q .mergeStateStatus
if ($mergeable -eq "BLOCKED" -or $mergeable -eq "DIRTY") {
  Write-Warning "mergeStateStatus=$mergeable — merge may fail (reviews / conflicts). Check: gh pr view $prNumber"
}

if ($Squash) {
  Write-Host "==> Merge PR #$prNumber (squash)"
  gh pr merge $prNumber --squash --delete-branch
} else {
  Write-Host "==> Merge PR #$prNumber (merge commit)"
  gh pr merge $prNumber --merge --delete-branch
}

Write-Host "==> Pull $Base locally"
git switch $Base
git pull --ff-only origin $Base

Write-Host "Done."
