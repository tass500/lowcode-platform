#Requires -Version 5.1
<#
.SYNOPSIS
  CI/local (Windows): start backend (5002) + Angular dev (4200), run Playwright smoke, then stop servers.
  Mirrors scripts/e2e-smoke-ci.sh for environments without Git Bash.
.EXAMPLE
  powershell -File scripts/e2e-smoke-ci.ps1
  pwsh scripts/e2e-smoke-ci.ps1
#>
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

function Test-E2eUrl([string] $Uri) {
  try {
    $r = Invoke-WebRequest -Uri $Uri -UseBasicParsing -TimeoutSec 5
    return $r.StatusCode -eq 200
  }
  catch {
    return $false
  }
}

$beArgs = @('run', '--project', 'backend/LowCodePlatform.Backend.csproj', '--launch-profile', 'http')
$be = Start-Process -FilePath 'dotnet' -ArgumentList $beArgs -WorkingDirectory $Root -PassThru -WindowStyle Hidden
$feDir = Join-Path $Root 'frontend'
$fe = Start-Process -FilePath 'cmd.exe' -ArgumentList @('/c', 'npm run start') -WorkingDirectory $feDir -PassThru -WindowStyle Hidden

try {
  $ready = $false
  for ($i = 0; $i -lt 180; $i++) {
    if ((Test-E2eUrl 'http://127.0.0.1:5002/health') -and (Test-E2eUrl 'http://localhost:4200/')) {
      Write-Host 'E2E: backend + frontend are up.'
      $ready = $true
      break
    }
    Start-Sleep -Seconds 2
  }
  if (-not $ready) {
    throw 'E2E: backend or frontend did not become ready in time.'
  }
  if (-not (Test-E2eUrl 'http://127.0.0.1:5002/health')) { throw 'E2E: backend health check failed.' }
  if (-not (Test-E2eUrl 'http://localhost:4200/')) { throw 'E2E: frontend probe failed.' }

  Set-Location (Join-Path $Root 'frontend')
  $env:PW_NO_WEBSERVER = '1'
  $env:CI = 'true'
  & npm run e2e:smoke
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
  if ($fe -and -not $fe.HasExited) {
    Stop-Process -Id $fe.Id -Force -ErrorAction SilentlyContinue
  }
  if ($be -and -not $be.HasExited) {
    Stop-Process -Id $be.Id -Force -ErrorAction SilentlyContinue
  }
}
