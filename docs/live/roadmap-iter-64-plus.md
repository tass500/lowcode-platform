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
| **64b** | **Kestrel request limits** | `MaxRequestBodySize` / központi limit nagy JSON import ellen; konfigurálható | [`kestrel-request-limits.md`](kestrel-request-limits.md); `dotnet test` zöld |
| **64c** | **Rate limiting** | IP szerinti fix ablak; health kizárva; `Testing`-ben alapból ki (`RateLimiting:Enabled`) | [`rate-limiting.md`](rate-limiting.md); `dotnet test` zöld |
| **64d** | **Security / audit logging** | `SecurityAuditLogger` + admin/tenant kulcs elutasítás; access + DB audit összefoglaló | [`security-audit-logging.md`](security-audit-logging.md); `dotnet test` zöld |
| **64e** | **CI / repo hygiene** | `dotnet format --verify` a CI-ban (backend + teszt projekt); SBOM továbbra is opcionális / külön döntés | CI zöld; [`ci-dotnet-format.md`](ci-dotnet-format.md) |

## Hullám státusz

- **64a–e** ✅ lezárva (**PR #106–110**). Utolsó: **64e** — **PR #110** — [`ci-dotnet-format.md`](ci-dotnet-format.md).

## Következő jelölt (nem kötelező sorrend)

- **API életciklus** — verziópolitika / deprecation (sunset) header, ha a publikus szerződés nő (lásd fent § *Sorrend* pont 4). Új iter szám / scope: **TBD** — aktuális sor [`03_kovetkezo_lepesek.md`](03_kovetkezo_lepesek.md).  
- **Supply-chain:** SBOM / további CI zaj — csapatdöntés (64e táblázat).

## Iterációk — szállítás (történeti)

- **64e** ✅ — [`ci-dotnet-format.md`](ci-dotnet-format.md) (**PR #110**).  
- **64d** ✅ — [`security-audit-logging.md`](security-audit-logging.md) (**PR #109**).  
- **64c** ✅ — [`rate-limiting.md`](rate-limiting.md) (**PR #108**).  
- **64b** ✅ — [`kestrel-request-limits.md`](kestrel-request-limits.md) (**PR #107**).  
- **64a** ✅ — [`security-http-headers.md`](security-http-headers.md) (**PR #106**).  
- Előző hullám (63): [`roadmap-next-iterations.md`](roadmap-next-iterations.md).

## ACTIVE

- **64+ hullám:** ✅ kész (**64a–e**, **PR #106–110**). Következő szállítási fókusz: **TBD** — [`03_kovetkezo_lepesek.md`](03_kovetkezo_lepesek.md).
