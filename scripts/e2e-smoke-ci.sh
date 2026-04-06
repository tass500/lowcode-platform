#!/usr/bin/env bash
# CI/local (Unix): start backend (5002) + Angular dev (4200), run Playwright smoke, then stop servers.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

dotnet run --project backend/LowCodePlatform.Backend.csproj --launch-profile http &
BE_PID=$!
(cd frontend && npm run start) &
FE_PID=$!

cleanup() {
  kill "${BE_PID}" "${FE_PID}" 2>/dev/null || true
}
trap cleanup EXIT

# Use localhost for the dev server probe: on some hosts `ng serve` answers on localhost but not 127.0.0.1.
for _ in $(seq 1 180); do
  if curl -sf "http://127.0.0.1:5002/health" >/dev/null && curl -sf "http://localhost:4200/" >/dev/null; then
    echo "E2E: backend + frontend are up."
    break
  fi
  sleep 2
done

curl -sf "http://127.0.0.1:5002/health" >/dev/null
curl -sf "http://localhost:4200/" >/dev/null

cd frontend
export PW_NO_WEBSERVER=1
export CI=true
npm run e2e:smoke
