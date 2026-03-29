# Workflow definition import / export (Iter 61)

## Cél

Csapatok / környezetek között **név + definition JSON** átadása egy stabil, verziózott csomagban.

## API

- **`GET /api/workflows/{id}/export`** — válasz: `exportFormatVersion` (**1**), `name`, `definitionJson`, `exportedAtUtc`, `sourceWorkflowDefinitionId`.
- **`POST /api/workflows/import`** — body: `name`, `definitionJson`, opcionális `exportFormatVersion` (**1** vagy elhagyva). Ugyanaz a séma / context-var validáció, mint `POST /api/workflows`. Új `workflowDefinitionId`; **nincs** inbound secret / schedule másolás (biztonság + MVP).

## Kompatibilitás

- Jövőbeni formátum: ha `exportFormatVersion` > **1** importnál → **400** `workflow_import_format_unsupported`.
- Csak `name` + `definitionJson` (export mezők nélkül) → elfogadott, mintha sima create lenne.

## UI

- Workflow **details**: **Export JSON** — letöltés.
- **Workflows** lista: **Import from export JSON** — fájl vagy beillesztés → új workflow részletekre navigál.

## Fájlok

- `backend/Contracts/WorkflowDtos.cs` — DTO-k
- `backend/Controllers/WorkflowsController.cs` — `export` / `import`
- `frontend/.../lowcode-workflow-details-page.component.ts`, `lowcode-workflows-page.component.ts`
