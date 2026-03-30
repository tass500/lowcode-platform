# API életciklus — válaszfejlécek (iter 65a)

> **Cél:** a publikus **`/api/*`** JSON felületen előrelátható kompatibilitási jelzés (`X-API-Version`), később bővíthető **RFC 8594** stílusú `Deprecation` / `Sunset` fejlécekkel.

## Viselkedés

- Minden olyan válaszra, amelynek az útvonala **`/api`** prefixszel kezdődik (kis- és nagybetűtől függetlenül), a pipeline hozzáadja:
  - **`X-API-Version`:** érték a konfigból (`Api:PublicVersion`), alapértelmezés **`1`**.
- A gyökér **`/health`**, **`/health/live`**, **`/health/ready`** végpontok **nem** `/api` alatt vannak — ott **nincs** `X-API-Version` (külön „ops” felület).
- Ha `Api:PublicVersion` üres vagy csak whitespace, a fejléc **kimarad** (pl. ideiglenes kikapcsolás).

## Konfiguráció (`appsettings`)

```json
"Api": {
  "PublicVersion": "1"
}
```

## Megvalósítás

- `ApiLifecycleMiddleware` — `SecurityHeadersMiddleware` után, `ExceptionHandlingMiddleware` előtt.
- `ApiLifecycleOptions` — szekció: `Api`.

## Tesztek

- `ApiLifecycleMiddlewareTests` — útvonal szűrés, konfigurált verzió, üres verzió.
- `HealthEndpointsTests` — `/api/health` tartalmazza a fejlécet; `/health` nem.
