# API életciklus — iter 65+

> **Cél:** a publikus HTTP API **felderíthető verziója** és később **deprecation / sunset** jelzései, anélkül hogy egy PR-ba zsúfolnánk a teljes versioning modellt.  
> **WIP=1:** [`DEVELOPMENT_WORKFLOW.md`](../DEVELOPMENT_WORKFLOW.md).

## Iterációk

| Iter | Név | Mit ad | DoD (minimum) |
|------|-----|--------|----------------|
| **65a** | **`X-API-Version` baseline** | `ApiLifecycleMiddleware`; `Api:PublicVersion`; csak `/api/*`; live doc | `dotnet test …Backend.Tests` zöld; [`api-lifecycle-headers.md`](api-lifecycle-headers.md) |
| **65b** | **Deprecation / Sunset** | `[ApiDeprecated]` + `ApiDeprecationFilter`; `Deprecation` + opcionális `Sunset` | `dotnet test …Backend.Tests` zöld; [`api-lifecycle-headers.md`](api-lifecycle-headers.md) |

## Hullám státusz

- **65a** ✅ — **PR #112** — [`api-lifecycle-headers.md`](api-lifecycle-headers.md) (§ 65a).  
- **65b** — [`api-lifecycle-headers.md`](api-lifecycle-headers.md) (§ 65b). Aktuális sor: [`03_kovetkezo_lepesek.md`](03_kovetkezo_lepesek.md).

## ACTIVE

- **65b** — deprecation fejlécek — [`api-lifecycle-headers.md`](api-lifecycle-headers.md).  
- **65a** ✅ — **PR #112**.  
- Előző hullám (64): [`roadmap-iter-64-plus.md`](roadmap-iter-64-plus.md).
