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
- Dev server (uses `proxy.conf.json` → backend `http://localhost:5002` for `/api`):
  - `npm start` or `ng serve`
- Lint:
  - `npm run lint`
- Build:
  - `npm run build`
- Optional **BFF OAuth** local flow: [`docs/live/auth-bff-httponly.md`](docs/live/auth-bff-httponly.md) — **62c+ — helyi dev smoke** (proxy, `redirect_uri`) and **62c+ — teszt IdP regisztráció** (IdP app / PKCE checklist).

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
- Git / PR cadence and handoff: [`docs/DEVELOPMENT_WORKFLOW.md`](docs/DEVELOPMENT_WORKFLOW.md)

Rule of thumb:
- Work in iteration branches (feature/fix/docs), then open a PR to `main`.
- After each completed milestone, update the two live docs above so progress survives context resets.

## Reporting issues

Please use GitHub Issues and the provided templates.
