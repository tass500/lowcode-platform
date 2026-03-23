#!/usr/bin/env bash
# Same intent as gh-pr-push-merge.ps1 — push, PR, wait checks, merge.
# Optional: copy .env.example to .env and set GH_TOKEN (never commit .env).

set -euo pipefail
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

BASE="${BASE:-master}"
NO_MERGE="${NO_MERGE:-0}"
SQUASH="${SQUASH:-0}"

if [[ -f "$REPO_ROOT/.env" ]]; then
  set -a
  # shellcheck disable=SC1090
  source "$REPO_ROOT/.env"
  set +a
fi

if [[ -n "${GH_TOKEN:-}" && -z "${GITHUB_TOKEN:-}" ]]; then
  export GITHUB_TOKEN="$GH_TOKEN"
fi

command -v gh >/dev/null 2>&1 || { echo "Install GitHub CLI: https://cli.github.com/"; exit 1; }

BRANCH="$(git rev-parse --abbrev-ref HEAD)"
if [[ "$BRANCH" == "$BASE" ]]; then
  echo "You are on '$BASE'. Switch to a feature branch first." >&2
  exit 1
fi

echo "==> Fetch origin/$BASE"
git fetch origin "$BASE"

echo "==> Push branch $BRANCH"
git push -u origin HEAD

PR_NUM="$(gh pr list --head "$BRANCH" --state open --json number --jq '.[0].number' 2>/dev/null || true)"
if [[ -z "$PR_NUM" || "$PR_NUM" == "null" ]]; then
  TITLE="${TITLE:-$(git log -1 --pretty=%s)}"
  BODY="${BODY:-$(git log -1 --pretty=%b)}"
  if [[ -z "${BODY// }" ]]; then
    BODY="See commits.

Test plan: dotnet test backend/... ; npm run build (frontend/)"
  fi
  echo "==> Create PR -> $BASE"
  gh pr create --base "$BASE" --title "$TITLE" --body "$BODY"
  PR_NUM="$(gh pr view --json number --jq '.number')"
fi

echo "==> PR #$PR_NUM"

if [[ "$NO_MERGE" == "1" ]]; then
  echo "==> NO_MERGE=1 — stop."
  exit 0
fi

echo "==> Wait for checks"
gh pr checks "$PR_NUM" --watch

if [[ "$SQUASH" == "1" ]]; then
  echo "==> Squash merge"
  gh pr merge "$PR_NUM" --squash --delete-branch
else
  echo "==> Merge commit"
  gh pr merge "$PR_NUM" --merge --delete-branch
fi

echo "==> Pull $BASE"
git switch "$BASE"
git pull --ff-only origin "$BASE"
echo "Done."
