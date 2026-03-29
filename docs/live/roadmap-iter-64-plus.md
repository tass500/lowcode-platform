# Enterprise következő hullám — iter 64+

> **Cél:** a platform **üzemeltethetőségét, biztonságát és API-kompatibilitását** iparági gyakorlatok szerint erősíteni anélkül, hogy a termék-funkciókat egy PR-ba zsúfolnánk.  
> **WIP=1:** egyszerre egy aktív iteráció a táblázatból — [`DEVELOPMENT_WORKFLOW.md`](../DEVELOPMENT_WORKFLOW.md).

## Sorrend (miért így)

1. **Biztonsági baseline** (fejlécek, HSTS, később body limit) — olcsó, nagy NIS2 / OWASP API alignment.  
2. **Deny-by-default méret / abuse** — Kestrel limit + opcionális rate limit.  
3. **Megfigyelhetőség / audit** — egységes security-releváns eseménynapló, ha még hiányzik.  
4. **API életciklus** — verziópolitika / deprecation header, ha a publikus szerződés nő.  
5. **CI keményítés** — `dotnet format` / supply-chain ellenőrzések, ha a csapat vállalja a zajt.

## Iterációk

| Iter | Név | Mit ad | DoD (minimum) |
|------|-----|--------|----------------|
| **64a** | **HTTP security headers + HSTS** | `SecurityHeadersMiddleware`; Production: `UseHsts` + meglévő HTTPS redirect; live doc | `dotnet test …Backend.Tests` zöld; [`security-http-headers.md`](security-http-headers.md) |
| **64b** | **Kestrel request limits** | `MaxRequestBodySize` / központi limit nagy JSON import ellen; konfigurálható | teszt vagy dokumentált default; deploy doc frissítés |
| **64c** | **Rate limiting** | Globális vagy `/api` részfa (pl. anon health) — `.NET` beépített rate limiter | `dotnet test` + konfig minta |
| **64d** | **Security / audit logging** | Egységes séma (pl. auth failure, admin kulcs) — hol van, mit bővítünk | doc + minimális kódbővítés |
| **64e** | **CI / repo hygiene** | Opcionális: `dotnet format --verify`, SBOM — csapatdöntés | CI zöld, zaj elfogadva |

## ACTIVE

- **64a** — HTTP security headers + HSTS — **PR #106** — részletek: [`security-http-headers.md`](security-http-headers.md). Aktuális sor: [`03_kovetkezo_lepesek.md`](03_kovetkezo_lepesek.md).  
- Előző hullám (63): [`roadmap-next-iterations.md`](roadmap-next-iterations.md).
