# Következő lépések (élő)

> **Folyamat:** [`DEVELOPMENT_WORKFLOW.md`](../DEVELOPMENT_WORKFLOW.md) · **Takarékos Cursor:** [`ai-cursor-token-efficiency.md`](ai-cursor-token-efficiency.md) + ugyanott **§10**. A `docs/00_truth_files_template/*` fájlok **nem** szerkeszthetők (sablonok).

## Takarékos dokumentációs mód (Cursor / AI)

- **Új chat / kontextus reset:** elég **ez a fájl** + [`02_allapot.md`](02_allapot.md) — **ne** mellékeld [`03_ARCHIVE.md`](03_ARCHIVE.md)-t, hacsak nem kifejezetten **régi iteráció** vagy **upgrade / Batch** részlet kell.
- A hosszú, történeti részletek és checklistek: **[`03_ARCHIVE.md`](03_ARCHIVE.md)**.

## Workflow engine — ACTIVE

**ACTIVE (javasolt következő, WIP=1):** **58c+** — opcionális **@angular/cdk** a workflow builderben (touch / lista UX); előtte egyeztesd a függőség-policyt — [`workflow-visual-builder.md`](workflow-visual-builder.md). **Backlog (opcionális):** **62c+ e2e** (Playwright stb.). **62c+** doksi ✅ — [`auth-bff-httponly.md`](auth-bff-httponly.md) § *helyi dev smoke* (**PR #95**), § *teszt IdP regisztráció* (**PR #97**). **62c — BFF + httpOnly** ✅ **PR #93**. **62b2** ✅ [`oidc-jwt-bearer.md`](oidc-jwt-bearer.md). **62b1** ✅. **62** ✅. **61** ✅. **60–58** ✅.

> **56–57** lezárva: SQL Server EF + Helm backup — [`sqlserver-platform.md`](sqlserver-platform.md), [`k3s-home-lab.md`](k3s-home-lab.md), [`container-deploy.md`](container-deploy.md).

## Ütemterv 56+ (összefoglaló)

| Iter | Fókusz | Megjegyzés |
|------|--------|------------|
| **56** | SQL Server EF migrációk ✅ | [`sqlserver-platform.md`](sqlserver-platform.md) |
| **57** | Helm backup CronJob ✅ | chart 0.3.0 |
| **58** | Vizuális builder (58a–b ✅, 58c natív DnD ✅) | CDK opcionális |
| **59** | Run cancel API ✅ | |
| **60** | Observability + health ✅ | OTel opcionális |
| **61** | Import/export ✅ | [`workflow-import-export.md`](workflow-import-export.md) |
| **62** | Auth (62a–a2 ✅, 62a1 ✅) | **62b** MVP ✅, **62b2** SPA+claims ✅, **62c** BFF B+C+D ✅ [`auth-bff-httponly.md`](auth-bff-httponly.md) |

Részletes indoklás és régebbi iterációk: [`03_ARCHIVE.md`](03_ARCHIVE.md) (§ *Ütemterv 56+*).

## Minichecklist (kontextusvesztés után)

- **PR / iteráció ritmus:** [`DEVELOPMENT_WORKFLOW.md`](../DEVELOPMENT_WORKFLOW.md) **§5a**
- Branch: `feat/<topic>` a legfrissebb `main`-ről
- ACTIVE: **58c+** (builder CDK) vagy **62c+ e2e**; **62c** + **62c+ doksi** (**PR #93**, **PR #95**, **PR #97**); **62b2** mergeelve; **62** / **61** lezárva
- `git status` → staged / unstaged
- `dotnet test backend/LowCodePlatform.Backend.Tests/LowCodePlatform.Backend.Tests.csproj`

## Kapcsolódó live docok

- OIDC JWT Bearer: [`oidc-jwt-bearer.md`](oidc-jwt-bearer.md) · BFF / httpOnly terv (**62c**): [`auth-bff-httponly.md`](auth-bff-httponly.md) · tenant API key: [`tenant-api-key.md`](tenant-api-key.md)
- Ütemezés: [`workflow-schedule.md`](workflow-schedule.md) · timeout/cancel: [`workflow-step-timeout-cancel.md`](workflow-step-timeout-cancel.md)

## Rövid működési elv

- Milestone után: **`02` + `03` (ez a fájl)** frissítése; WIP=1.
- **Részletes, régi iterációs szöveg** (régen ide írt hosszú blokkok): [`03_ARCHIVE.md`](03_ARCHIVE.md)-be tedd, hogy ez a fájl **karcsú** maradjon.
- Üzemeltetői gyorslink: [`ops/upgrade.md`](ops/upgrade.md)
