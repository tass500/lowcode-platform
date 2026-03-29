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

## Iteration sizing rule
- For ongoing work, see `docs/live/03_kovetkezo_lepesek.md`:
  - Credit-aware iteration sizing (1 Q/A = 1 credit)

## API authentication (pointers)

- **Tenant automation:** optional header `X-Tenant-Api-Key` (provisioned via admin API); details and errors: [`docs/live/tenant-api-key.md`](docs/live/tenant-api-key.md).
- **JWT (dev):** `POST /api/auth/dev-token` (Development / Testing only). Optional strict `iss` / `aud` when `Auth:Jwt:Issuer` and `Auth:Jwt:Audience` are set — same doc, JWT section.
- **OIDC (optional):** when `Auth:Oidc:Authority` is set, the same `Bearer` header can carry IdP-issued JWTs (metadata validation). See [`docs/live/oidc-jwt-bearer.md`](docs/live/oidc-jwt-bearer.md).
- **BFF + httpOnly (optional):** server-side OAuth session cookie + Angular `withCredentials` when `Auth:Bff` is on; see [`docs/live/auth-bff-httponly.md`](docs/live/auth-bff-httponly.md) (local dev smoke, proxy `Host` / IdP `redirect_uri`: section **62c+ — helyi dev smoke** in that doc).

