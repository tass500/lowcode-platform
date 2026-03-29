# Következő lépések (élő)

> **Folyamat:** [`DEVELOPMENT_WORKFLOW.md`](../DEVELOPMENT_WORKFLOW.md) · **Takarékos Cursor:** [`ai-cursor-token-efficiency.md`](ai-cursor-token-efficiency.md) + ugyanott **§10**. A `docs/00_truth_files_template/*` fájlok **nem** szerkeszthetők (sablonok).

## Takarékos dokumentációs mód (Cursor / AI)

- **Új chat / kontextus reset:** elég **ez a fájl** + [`02_allapot.md`](02_allapot.md) — **ne** mellékeld [`03_ARCHIVE.md`](03_ARCHIVE.md)-t, hacsak nem kifejezetten **régi iteráció** vagy **upgrade / Batch** részlet kell.
- A hosszú, történeti részletek és checklistek: **[`03_ARCHIVE.md`](03_ARCHIVE.md)**.

## Workflow engine — ACTIVE

**ACTIVE: Iteráció 62 — auth bővítés** — **62a** tenant API key ✅ [`tenant-api-key.md`](tenant-api-key.md) (JWT alternatíva automatizáláshoz). **62a2** admin tenants oldal: tenant API key UI (`/lowcode/admin/tenants`) ✅. **62a1** JWT opcionális `iss`/`aud` + signing key kötés `IPostConfigureOptions`-szel (integrációs teszt barát) ✅ — ugyanott JWT szekció. **61** import/export ✅ [`workflow-import-export.md`](workflow-import-export.md). **60** observability + health ✅. **59** cancel ✅. **58a–b** builder ✅ [`workflow-visual-builder.md`](workflow-visual-builder.md). **58c** builder sorrend natív DnD ✅ (fogó + ejtés). Backlog: **58c+** (opcionális CDK / touch), **62b** (IdP / OAuth2, külön PR).

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
| **62** | Auth bővítés (62a–a2 ✅, 62a1 JWT iss/aud + key kötés ✅) | 62b IdP / OAuth2 külön PR |

Részletes indoklás és régebbi iterációk: [`03_ARCHIVE.md`](03_ARCHIVE.md) (§ *Ütemterv 56+*).

## Minichecklist (kontextusvesztés után)

- **PR / iteráció ritmus:** [`DEVELOPMENT_WORKFLOW.md`](../DEVELOPMENT_WORKFLOW.md) **§5a**
- Branch: `feat/<topic>` a legfrissebb `main`-ről
- ACTIVE: **62**; **61** lezárva
- `git status` → staged / unstaged
- `dotnet test backend/LowCodePlatform.Backend.Tests/LowCodePlatform.Backend.Tests.csproj`

## Kapcsolódó live docok

- Ütemezés: [`workflow-schedule.md`](workflow-schedule.md) · timeout/cancel: [`workflow-step-timeout-cancel.md`](workflow-step-timeout-cancel.md)

## Rövid működési elv

- Milestone után: **`02` + `03` (ez a fájl)** frissítése; WIP=1.
- **Részletes, régi iterációs szöveg** (régen ide írt hosszú blokkok): [`03_ARCHIVE.md`](03_ARCHIVE.md)-be tedd, hogy ez a fájl **karcsú** maradjon.
- Üzemeltetői gyorslink: [`ops/upgrade.md`](ops/upgrade.md)
