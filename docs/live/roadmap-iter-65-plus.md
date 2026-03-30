# API életciklus — iter 65+

> **Cél:** a publikus HTTP API **felderíthető verziója** és később **deprecation / sunset** jelzései, anélkül hogy egy PR-ba zsúfolnánk a teljes versioning modellt.  
> **WIP=1:** [`DEVELOPMENT_WORKFLOW.md`](../DEVELOPMENT_WORKFLOW.md).

## Iterációk

| Iter | Név | Mit ad | DoD (minimum) |
|------|-----|--------|----------------|
| **65a** | **`X-API-Version` baseline** | `ApiLifecycleMiddleware`; `Api:PublicVersion`; csak `/api/*`; live doc | `dotnet test …Backend.Tests` zöld; [`api-lifecycle-headers.md`](api-lifecycle-headers.md) |
| **65b** | **Deprecation / Sunset** | `[ApiDeprecated]` + `ApiDeprecationFilter`; `Deprecation` + opcionális `Sunset` | `dotnet test …Backend.Tests` zöld; [`api-lifecycle-headers.md`](api-lifecycle-headers.md) |
| **65c** | **OpenAPI `deprecated`** | `ApiDeprecatedOperationFilter`; `deprecated` + `x-sunset` + leírás | `dotnet test …Backend.Tests` zöld; [`api-lifecycle-headers.md`](api-lifecycle-headers.md) |

## Hullám státusz

- **65a** ✅ — **PR #112** — [`api-lifecycle-headers.md`](api-lifecycle-headers.md) (§ 65a).  
- **65b** ✅ — **PR #113** — [`api-lifecycle-headers.md`](api-lifecycle-headers.md) (§ 65b).  
- **65c** — [`api-lifecycle-headers.md`](api-lifecycle-headers.md) (§ 65c). Aktuális sor: [`03_kovetkezo_lepesek.md`](03_kovetkezo_lepesek.md).

## ACTIVE

- **65c** — OpenAPI deprecated — [`api-lifecycle-headers.md`](api-lifecycle-headers.md).  
- **65b** ✅ — **PR #113**.  
- **65a** ✅ — **PR #112**.  
- Előző hullám (64): [`roadmap-iter-64-plus.md`](roadmap-iter-64-plus.md).

## Következő jelölt (nem kötelező)

- **65+ hullám lezárás** merge után: **ACTIVE TBD** — [`03_kovetkezo_lepesek.md`](03_kovetkezo_lepesek.md).  
- **Supply-chain / SBOM** (64e opció) — csapatdöntés — [`roadmap-iter-64-plus.md`](roadmap-iter-64-plus.md).
