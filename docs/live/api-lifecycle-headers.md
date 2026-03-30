# API életciklus — válaszfejlécek (iter 65a–b)

> **Cél:** a publikus **`/api/*`** JSON felületen előrelátható kompatibilitási jelzés (`X-API-Version`), illetve **RFC 8594** stílusú **`Deprecation`** / **`Sunset`** jelölés elavuló végpontokon.

## 65a — `X-API-Version`

**Szállítás:** **PR #112** (merge a **`main`** ágra).

### Viselkedés

- Minden olyan válaszra, amelynek az útvonala **`/api`** prefixszel kezdődik (kis- és nagybetűtől függetlenül), a pipeline hozzáadja:
  - **`X-API-Version`:** érték a konfigból (`Api:PublicVersion`), alapértelmezés **`1`**.
- A gyökér **`/health`**, **`/health/live`**, **`/health/ready`** végpontok **nem** `/api` alatt vannak — ott **nincs** `X-API-Version` (külön „ops” felület).
- Ha `Api:PublicVersion` üres vagy csak whitespace, a fejléc **kimarad** (pl. ideiglenes kikapcsolás).

### Konfiguráció (`appsettings`)

```json
"Api": {
  "PublicVersion": "1"
}
```

### Megvalósítás

- `ApiLifecycleMiddleware` — `SecurityHeadersMiddleware` után, `ExceptionHandlingMiddleware` előtt.
- `ApiLifecycleOptions` — szekció: `Api`.

## 65b — `Deprecation` / `Sunset`

### Viselkedés

- Controller vagy action szinten: **`[ApiDeprecated]`** attribútum (`Filters` névtér).
- Opcionális: **`SunsetUtcIso`** — ISO 8601 dátum/idő; ha érvényesen parse-olható, a válasz tartalmazza a **`Sunset`** fejlécet (HTTP-date, `R` formátum). Érvénytelen vagy üres értéknél **`Sunset` kimarad**, de **`Deprecation: true`** továbbra is megjelenik.
- A **`Deprecation`** fejléc értéke jelenleg a szöveg **`true`** (RFC 8594 megfelelő boolean jelölés).

### Megvalósítás

- `ApiDeprecationFilter` — globális MVC filter (`Program.cs` → `AddControllers`).
- `ApiDeprecatedAttribute` — `Class` és `Method` célokra.

### Tesztek

- `ApiDeprecationFilterTests` — egységteszt a fejléc-logikára.
- `DeprecationHeadersIntegrationTests` — teszt assembly-beli probe controller (`ConfigureTestServices` + `AddApplicationPart`).
