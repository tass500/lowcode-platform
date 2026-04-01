# Security / audit naplózás (iter 64d)

## Cél

**Egységes, SIEM-barát** jelzések a **biztonsági relevanciájú** eseményekre (auth elutasítás, konfigurációs hiba), **titkok és nyers kulcsok nélkül**.

## Csatornák

| Csatorna | Tartalom | Hol |
|----------|----------|-----|
| **HTTP access** | `http_request_finished` — method, path, status, duration, traceId, tenant | `AccessLogMiddleware` (Information) |
| **Platform audit DB** | `AuditLog` — actor, action, target, traceId, … | `AuditService` / admin API-k |
| **Security audit (log)** | `security_auth_denied` / `security_config_error` | `SecurityAuditLogger`, kategória: `LowCodePlatform.Backend.SecurityAudit` |

## Strukturált sorok (64d+)

### `security_auth_denied` (Warning)

- **Mikor:** admin vagy tenant API kulcs elutasítva, tenant nem feloldható kulcshoz.
- **Mezők:** `eventId`, `path`, `traceId`, `reasonCode` (ugyanaz, mint az API `errorCode` ahol van).
- **Példa `eventId`:** `admin_unauthorized`, `admin_api_key_fallback_disabled`, `tenant_api_key_invalid`, `tenant_not_resolved`.

### `security_config_error` (Error)

- **Mikor:** pl. Production-ben nincs beállítva `Admin:ApiKey` (admin API elérés nem konfigurált).
- **Mezők:** `eventId`, `path`, `traceId`.

## Mit nem logolunk

- **Soha** nyers API kulcs, JWT, jelszó.
- Path **max. 512** karakter (truncatelve).

## Kapcsolódó

- Ütemterv: [`roadmap-iter-64-plus.md`](roadmap-iter-64-plus.md)
