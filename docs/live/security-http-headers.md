# HTTP biztonsági fejlécek (backend)

## Mit állítunk be

A `SecurityHeadersMiddleware` minden válaszra (hibákra is, `OnStarting` miatt) alapértelmezett fejléceket ad — **védelem mélységben**; az ingress / CDN további szabályokat tehet rá.

| Fejléc | Érték | Megjegyzés |
|--------|--------|------------|
| `X-Content-Type-Options` | `nosniff` | MIME sniffing csökkentése |
| `X-Frame-Options` | `DENY` | clickjacking ellen API JSON felületen |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | referrer szivárgás csökkentése |
| `Permissions-Policy` | szűkített feature lista | böngésző API-k kikapcsolva, ahol nem kellenek |

## HSTS

**Production** (nem Development / Testing): `UseHsts()` + `UseHttpsRedirection()` — a böngésző **HTTPS**-re erőlteti az újraküldést és `Strict-Transport-Security` fejlécet kap. Tesztkörnyezetben (`Testing`) **nincs** HSTS, hogy a `WebApplicationFactory` HTTP-n maradhasson.

## Kapcsolódó

- Következő lépések (64+ ütem): [`roadmap-iter-64-plus.md`](roadmap-iter-64-plus.md)
