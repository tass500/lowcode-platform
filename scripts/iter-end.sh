#!/usr/bin/env bash
# Iteration end: run quality gates then gh-pr-push-merge.sh (push / PR / checks / merge).
# Usage: ./scripts/iter-end.sh
# Env: SKIP_TESTS=1 SKIP_FRONTEND=1 NO_MERGE=1 SQUASH=1 BODY_FILE=path ./scripts/iter-end.sh

set -euo pipefail
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

BACKEND_TESTS="backend/LowCodePlatform.Backend.Tests/LowCodePlatform.Backend.Tests.csproj"

if [[ "${SKIP_TESTS:-0}" != "1" ]]; then
  echo "==> dotnet test $BACKEND_TESTS"
  dotnet test "$BACKEND_TESTS"
else
  echo "WARN: SKIP_TESTS=1 — skipping dotnet test"
fi

if [[ "${SKIP_FRONTEND:-0}" != "1" ]]; then
  echo "==> npm run build (frontend/)"
  (cd "$REPO_ROOT/frontend" && npm run build)
else
  echo "WARN: SKIP_FRONTEND=1 — skipping npm run build"
fi

echo "==> gh-pr-push-merge"
exec "$REPO_ROOT/scripts/gh-pr-push-merge.sh"
