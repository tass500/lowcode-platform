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

- **65a–c** ✅ lezárva (**PR #112–114**). Utolsó: **65c** — **PR #114** — [`api-lifecycle-headers.md`](api-lifecycle-headers.md) (§ 65c).

## Következő lépés (ajánlás, nem kötelező sorrend)

1. **Integrációs minőség** — a **63b** admin/auth vonal lefedése: *kész:* `AuthSpaOidcConfigTests`, `AdminObservabilityTests`, `AdminUpgradeRunsTests` + `AdminUpgradeRunsMutationTests`, `AdminInstallationTests`, `AdminAuditTests`, `AdminTenantsTests`, BFF (`BffAuthEndpointsTests`, `BffSessionBearerWorkflowTests`) — [`roadmap-next-iterations.md`](roadmap-next-iterations.md).  
2. **Supply-chain / security CI (iter 66+)** — NuGet audit + vulnerable check — [`ci-supply-chain.md`](ci-supply-chain.md); titok-szűrés — Gitleaks — [`ci-secret-scanning.md`](ci-secret-scanning.md); opcionális: GitHub Dependency graph + dependency-review; további: SBOM artefakt / npm audit kapu (Angular upgrade után) — [`roadmap-iter-64-plus.md`](roadmap-iter-64-plus.md).  
3. **Termék** — workflow / tenant / low-code UI a backlog és a termék prioritás szerint.

## ACTIVE

- **65+ hullám:** ✅ kész (**65a–c**, **PR #112–114**). **66+ supply-chain:** [`ci-supply-chain.md`](ci-supply-chain.md). További fókusz: [`03_kovetkezo_lepesek.md`](03_kovetkezo_lepesek.md).

## Szállítás (történeti)

- **65c** ✅ — [`api-lifecycle-headers.md`](api-lifecycle-headers.md) (**PR #114**).  
- **65b** ✅ — [`api-lifecycle-headers.md`](api-lifecycle-headers.md) (**PR #113**).  
- **65a** ✅ — [`api-lifecycle-headers.md`](api-lifecycle-headers.md) (**PR #112**).  
- Előző hullám (64): [`roadmap-iter-64-plus.md`](roadmap-iter-64-plus.md).
