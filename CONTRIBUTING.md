# Contributing

Thanks for your interest in contributing.

## Development setup

**Local quickstart (ports, proxy, BFF):** see [README — Local development](README.md#local-development). Below: exact commands and optional auth flows.

### Backend

- Run (HTTP profile, **http://localhost:5002**):
  - `dotnet run --project backend/LowCodePlatform.Backend.csproj --launch-profile http`
- Build:
  - `dotnet build backend/LowCodePlatform.Backend.csproj`
- Test:
  - `dotnet test backend/LowCodePlatform.Backend.Tests/LowCodePlatform.Backend.Tests.csproj`

### Frontend

- Install:
  - `cd frontend && npm ci`
- Dev server — `frontend/proxy.conf.json` proxies **`/api`** → **`http://localhost:5002`** with **`changeOrigin: false`** (keep backend `Host` aligned with the SPA origin for tenant + BFF):
  - `npm start` or `ng serve` (default **http://localhost:4200**)
- Lint:
  - `npm run lint`
- Build:
  - `npm run build`
- Optional **BFF OAuth** (same proxy rules as above): [`docs/live/auth-bff-httponly.md`](docs/live/auth-bff-httponly.md) — **62c+ — helyi dev smoke** (proxy, `redirect_uri`) and **62c+ — teszt IdP regisztráció** (IdP app / PKCE checklist).

## Pull requests

- Use the PR template.
- Prefer small, focused PRs.
- Ensure CI is green.

## Project workflow (context reset safe)

- Ground truth (what is done / what is next):
  - `docs/live/02_allapot.md`
  - `docs/live/03_kovetkezo_lepesek.md`
  - Roadmap / completed iter 63 + next focus: [`docs/live/roadmap-next-iterations.md`](docs/live/roadmap-next-iterations.md)
- Credit-aware batching & risk tiers: `docs/00_workmode.md`
- Quality gates: `docs/01_quality_gates.md`
- Git / PR cadence and handoff: [`docs/DEVELOPMENT_WORKFLOW.md`](docs/DEVELOPMENT_WORKFLOW.md)

Rule of thumb:
- Work in iteration branches (feature/fix/docs), then open a PR to `main`.
- After each completed milestone, update the two live docs above so progress survives context resets.

## Reporting issues

Please use GitHub Issues and the provided templates.
