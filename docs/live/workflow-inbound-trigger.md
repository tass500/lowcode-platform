# Inbound workflow trigger (webhook MVP, Iter 51)

## Cél

JWT **nélkül** indítható workflow futás, ha a workflow-hoz be van állítva egy megosztott titok. A titok **nem** jelenik meg a GET válaszban; csak SHA-256 digest kerül tárolásra (`inbound_trigger_secret_sha256_hex`).

## Titok beállítása (JWT-vel, tenant user)

- **PUT** ` /api/workflows/{workflowDefinitionId}/inbound-trigger`  
  Body: `{ "secret": "<min. 16 karakter>" }`
- **DELETE** ` /api/workflows/{workflowDefinitionId}/inbound-trigger` — titok törlése (inbound kikapcsolva)

Opcionálisan **POST** ` /api/workflows` (create) body-ban: `inboundTriggerSecret` (ugyanaz a minimum hossz).

## Külső hívás (JWT nélkül)

- **Host**: tenant aldomain, pl. `https://{tenant}.yourdomain.com` vagy devben `http://t1.localhost:PORT`
- **POST** ` /api/inbound/workflows/{workflowDefinitionId}/runs` (üres body is lehet)
- **Header**: `X-Workflow-Inbound-Secret: <ugyanaz a titok, mint amit PUT-tal megadtál>`

Sikeres válasz: `{ "serverTimeUtc": "...", "workflowRunId": "..." }` (ugyanaz a minta, mint a belső `POST /api/workflows/{id}/runs`).

## Hibák

| HTTP | Kód | Mikor |
|------|-----|--------|
| 401 | `inbound_secret_missing` | Hiányzik a header |
| 403 | `inbound_secret_invalid` | Rossz titok |
| 404 | `workflow_not_found` | Ismeretlen workflow id |
| 404 | `workflow_inbound_not_configured` | Nincs beállítva titok (PUT nem történt) |
| 400 | `tenant_not_resolved` | Nem oldható fel tenant a hostból (pl. `localhost` nélkül subdomain) |

## Megjegyzés

Élesben a tenant feloldás **host** alapján történik; a **Development** környezetben `X-Tenant-Id` is használható a többi API-hoz, de az inbound route ugyanúgy a `TenantContext`-et használja — érdemes a végleges integrációt **tenant host**-ra tervezni.
