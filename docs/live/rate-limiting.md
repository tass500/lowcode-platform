# Rate limiting (iter 64c)

## Cél

**Abuse / túlterhelés** csökkentése: IP-nként **fix ablakos** kvóta a kérésekre (ASP.NET Core beépített `RateLimiter`).

## Konfiguráció

| Kulcs | Alapértelmezés | Megjegyzés |
|-------|----------------|------------|
| `RateLimiting:PermitLimit` | `400` | kérés / ablak / IP |
| `RateLimiting:WindowSeconds` | `60` | ablak hossza |
| `RateLimiting:Enabled` | *(nincs)* | **`Testing`** környezetben alapból **ki**; integrációs tesztben `true` → limit aktív |

Ha `PermitLimit` vagy `WindowSeconds` **≤ 0**, a limiter **kikapcsol** (NoLimiter).

## Viselkedés

- **Partíció:** távoli IP (`Connection.RemoteIpAddress`), ismeretlen → `"unknown"`.
- **Válasz:** **429 Too Many Requests** (`RejectionStatusCode`).
- **Health:** `/health`, `/api/health`, `/health/live`, `/health/ready` — **`DisableRateLimiting`** (Kubernetes probe / monitorozás ne akadjon be).

## Kapcsolódó

- Ütemterv: [`roadmap-iter-64-plus.md`](roadmap-iter-64-plus.md)
