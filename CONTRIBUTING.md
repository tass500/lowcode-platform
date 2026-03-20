# Contributing

Thanks for your interest in contributing.

## Development setup

### Backend

- Build:
  - `dotnet build backend/LowCodePlatform.Backend.csproj`
- Test:
  - `dotnet test backend/LowCodePlatform.Backend.Tests/LowCodePlatform.Backend.Tests.csproj`

### Frontend

- Install:
  - `cd frontend && npm ci`
- Lint:
  - `npm run lint`
- Build:
  - `npm run build`

## Pull requests

- Use the PR template.
- Prefer small, focused PRs.
- Ensure CI is green.

## Project workflow (context reset safe)

- Ground truth (what is done / what is next):
  - `docs/live/02_allapot.md`
  - `docs/live/03_kovetkezo_lepesek.md`
- Credit-aware batching & risk tiers: `docs/00_workmode.md`
- Quality gates: `docs/01_quality_gates.md`
- Standard git branch/commit/push/PR flow: `.windsurf/workflows/commit-and-pr.md`

Rule of thumb:
- Work in iteration branches (feature/fix/docs), then open a PR to `master`.
- After each completed milestone, update the two live docs above so progress survives context resets.

## Reporting issues

Please use GitHub Issues and the provided templates.
