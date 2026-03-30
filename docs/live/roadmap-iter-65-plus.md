# API életciklus — iter 65+

> **Cél:** a publikus HTTP API **felderíthető verziója** és később **deprecation / sunset** jelzései, anélkül hogy egy PR-ba zsúfolnánk a teljes versioning modellt.  
> **WIP=1:** [`DEVELOPMENT_WORKFLOW.md`](../DEVELOPMENT_WORKFLOW.md).

## Iterációk

| Iter | Név | Mit ad | DoD (minimum) |
|------|-----|--------|----------------|
| **65a** | **`X-API-Version` baseline** | `ApiLifecycleMiddleware`; `Api:PublicVersion`; csak `/api/*`; live doc | `dotnet test …Backend.Tests` zöld; [`api-lifecycle-headers.md`](api-lifecycle-headers.md) |
| **65b** *(TBD)* | **Deprecation / Sunset** | Opcionális: `Deprecation` + `Sunset` (RFC 8594) egyedi végpontokra vagy policy szerint | Teszt + doc |

## ACTIVE

- **65a** — [`api-lifecycle-headers.md`](api-lifecycle-headers.md). Aktuális sor: [`03_kovetkezo_lepesek.md`](03_kovetkezo_lepesek.md).  
- Előző hullám (64): [`roadmap-iter-64-plus.md`](roadmap-iter-64-plus.md).
