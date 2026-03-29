# lowcode-platform

This repository was bootstrapped from the truth files template.

## Truth files
- docs/00_truth_files_template/00_vizio.md
- docs/00_truth_files_template/01_dontesek.md
- docs/00_truth_files_template/02_allapot.md
- docs/00_truth_files_template/03_kovetkezo_lepesek.md
- docs/00_truth_files_template/04_fejlesztesi_playbook.md
- docs/00_truth_files_template/05_roadmap_greenfield_enterprise.md

## Development workflow (authoritative)

- **Process, PRs, DoR/DoD, CI gates, handoff after context loss:** [`docs/DEVELOPMENT_WORKFLOW.md`](docs/DEVELOPMENT_WORKFLOW.md)
- Ongoing iteration / WIP: [`docs/live/03_kovetkezo_lepesek.md`](docs/live/03_kovetkezo_lepesek.md)
- **Next implementation iterations (63a+):** [`docs/live/roadmap-next-iterations.md`](docs/live/roadmap-next-iterations.md)

## Local development

Run the **backend** and **Angular dev server** on the same machine; the SPA calls `/api` through a dev proxy so cookies and `Host` behave like a single origin (important for optional BFF OAuth).

1. **Backend (ASP.NET Core)**  
   - From repo root: `dotnet run --project backend/LowCodePlatform.Backend.csproj --launch-profile http`  
   - Default URL: **http://localhost:5002** (see `backend/Properties/launchSettings.json`, profile `http`).  
   - Swagger UI: **http://localhost:5002/swagger**

2. **Frontend (Angular)**  
   - `cd frontend && npm ci && npm start` (or `ng serve`).  
   - Default URL: **http://localhost:4200**.  
   - `frontend/proxy.conf.json` forwards **`/api`** to **http://localhost:5002** with **`changeOrigin: false`** so the backend sees a browser-like `Host` (needed for tenant routing and BFF `redirect_uri` alignment).

3. **Optional: BFF + OIDC** (server-side session cookie, `withCredentials` from the SPA)  
   - Configuration and local smoke steps: [`docs/live/auth-bff-httponly.md`](docs/live/auth-bff-httponly.md) (sections on local dev smoke and test IdP registration).

Command cheat sheet (details and quality gates): [`CONTRIBUTING.md`](CONTRIBUTING.md).

## Iteration sizing rule
- For ongoing work, see `docs/live/03_kovetkezo_lepesek.md`:
  - Credit-aware iteration sizing (1 Q/A = 1 credit)

## API authentication (pointers)

- **Tenant automation:** optional header `X-Tenant-Api-Key` (provisioned via admin API); details and errors: [`docs/live/tenant-api-key.md`](docs/live/tenant-api-key.md).
- **JWT (dev):** `POST /api/auth/dev-token` (Development / Testing only). Optional strict `iss` / `aud` when `Auth:Jwt:Issuer` and `Auth:Jwt:Audience` are set — same doc, JWT section.
- **OIDC (optional):** when `Auth:Oidc:Authority` is set, the same `Bearer` header can carry IdP-issued JWTs (metadata validation). See [`docs/live/oidc-jwt-bearer.md`](docs/live/oidc-jwt-bearer.md).
- **BFF + httpOnly (optional):** server-side OAuth session cookie + Angular `withCredentials` when `Auth:Bff` is on; see [`docs/live/auth-bff-httponly.md`](docs/live/auth-bff-httponly.md) (sections **62c+ — helyi dev smoke** and **62c+ — teszt IdP regisztráció**).

