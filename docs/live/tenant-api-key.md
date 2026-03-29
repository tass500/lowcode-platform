# Tenant API key (iter 62, MVP)

## Cél

Gépi / automatizált hívásokhoz **JWT helyett** (vagy mellett) tenant-szintű titkos kulcs: ugyanazok a `tenant_user` védett endpointok, mint Bearer tokennel.

## Beállítás (admin)

- **POST** `/api/admin/tenants/{slug}/tenant-api-key`  
  - Body (opcionális): `{ "apiKey": "<min. 24 karakter, ha saját kulcs>" }` — ha hiányzik vagy üres, a szerver **véletlen** kulcsot generál.  
  - Válasz (egyszer látható): `{ "serverTimeUtc", "apiKey" }` — csak ebben a válaszban jelenik meg a nyers kulcs; DB-ben **SHA-256 hex** tárolódik (`tenant.tenant_api_key_sha256_hex`).

- **DELETE** `/api/admin/tenants/{slug}/tenant-api-key` — kulcs törlése (headeres auth kikapcsolva).

- **GET** `/api/admin/tenants` listaelemekben: `tenantApiKeyConfigured` (bool).

Admin hívásokhoz továbbra is **JWT `admin` szerep** vagy (ahogy eddig) `X-Admin-Api-Key` / konfiguráció szükséges; a tenant claim és a localhost „default” tenant **nem** ütközik az `/api/admin` útvonalakon (lásd `TenantClaimEnforcementMiddleware`).

## Kliens hívás

- Header: **`X-Tenant-Api-Key: <nyers kulcs>`**
- Tenant feloldás: élesben host aldomain; **Development**-ben `X-Tenant-Id` (Swaggerben is fel van véve).
- Ha **érvényes Bearer JWT** is van, az **elsőbbséget** élvez; a tenant API kulcs felesleges vagy hibás headerje nem írja felül a JWT-t.

## Hibák

| HTTP | errorCode (példa) | Mikor |
|------|-------------------|--------|
| 401 | `tenant_api_key_invalid` | Hiányzik / rossz kulcs, de a header jelen van |
| 400 | `tenant_not_resolved` | Kulcs van, de tenant nem oldható fel |

## Megjegyzés (fejlesztői környezet)

Ha a shellben **`LCP_EF_DESIGN_TIME=1`** maradt egy `dotnet ef` parancs után, a **Testing** környezetben a host továbbra is a tenant-feloldott `PlatformDbContext`-et használja (`Program.cs`), hogy az integrációs tesztek ne a design-time `tenant-default.db`-re csatlakozzanak.
