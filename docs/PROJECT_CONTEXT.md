# Projekt kontextus (Single Source of Truth)

## Cél
Ez a dokumentum a projekt aktuális invariánsainak és működési megállapodásainak **alacsony zajú, stabil referenciája**.

- **Csak akkor** változzon, ha a projekt „világmodellje” változik (szerződések, invariánsok, portok, core flow-k).
- A napi haladás / állapot követéséhez ezeket használd:
  - `docs/live/02_allapot.md`
  - `docs/live/03_kovetkezo_lepesek.md`
  - `docs/live/ai-cursor-token-efficiency.md` (Cursor / AI: takarékos kontextus; lásd még `DEVELOPMENT_WORKFLOW.md` §10)

## Futási végpontok
- **Backend**: `http://localhost:5002`
- **Frontend**: `http://localhost:4200`

## Admin API konvenciók
### Trace korreláció
- A frontend kliens oldalon generált trace id-t tud küldeni headerben:
  - `X-Trace-Id: <string>`
- A backend ezt a trace id-t továbbviszi az audit log bejegyzésekbe és a hibaválaszokba.

### Error contract (D024)
- A hibák egy standard JSON szerződést követnek, minimum:
  - `errorCode`
  - `message`
  - `traceId`
  - `timestampUtc`
  - `details` (opcionális)

### Drift-proof idő
- Az admin válaszok tartalmaznak egy **szerver időbélyeget**:
  - `serverTimeUtc` (UTC)
- A frontend a `serverTimeUtc` alapján számolja a `serverNowOffsetMs` értéket, és erre támaszkodva rendereli:
  - `Last refreshed ... (ago)` értékeket
  - futás közben a run/step duration-öket
- Drift warning küszöb:
  - `abs(serverNowOffsetMs) >= 120_000 ms` (~2 perc)

## Fő domain fogalmak
### Upgrade run-ok
- **States**: `pending`, `running`, `succeeded`, `failed`, `canceled`
- **Strict queue policy** (invariáns):
  - Installation-önként egyszerre legfeljebb egy run lehet `pending`/`running`.

### Version enforcement
- Enforcement states:
  - `ok`, `warn`, `soft_block`, `hard_block`
- Upgrade műveletek blokkolva vannak `soft_block` és `hard_block` esetén.

## Fő admin endpointok (jelenlegi)
### Installation
- `GET /api/admin/installation/status`
- `POST /api/admin/installation/dev-set` (dev-only)

### Upgrade run-ok
- `GET /api/admin/upgrade-runs/recent?take=10`
- `GET /api/admin/upgrade-runs/latest`
- `GET /api/admin/upgrade-runs/queue`
- `GET /api/admin/upgrade-runs/{id}`
- `POST /api/admin/upgrade-runs` (start)
- `POST /api/admin/upgrade-runs/{id}/retry`
- `POST /api/admin/upgrade-runs/{id}/cancel`
- `POST /api/admin/upgrade-runs/{id}/dev-fail-step` (dev-only)

### Audit
- `GET /api/admin/audit?take=...&actor=...&actionContains=...&traceId=...&sinceUtc=...`

## Frontend observability segédek (Upgrade page)
- A diagnosztika blokk mutatja:
  - az utoljára alkalmazott `serverTimeUtc` értéket
  - a számolt `serverNowOffsetMs` értéket
  - az utolsó frissítés `source` mezőjét
- Incident bundle:
  - A “Copy incident bundle” összegyűjti: status/queue/run/audit + time diagnostics + curl snippetek.

## Dokumentációs szabályok (anti-drift)
- **Fejlesztési folyamat (PR, DoR/DoD, gate-ek, handoff):** `docs/DEVELOPMENT_WORKFLOW.md` (authoritative). **Több iteráció egy ágon, egy PR / milestone:** ugyanott **§5a** (alapértelmezett ritmus emberek + AI).
- Cursor rövid szabályok (AI asszisztens): `.cursor/rules/*.mdc` — ugyanazt a folyamatot tükrözik; részletek a fenti dokumentumban. A repo gyökerében **`.cursorignore`** csökkenti az indexelt zajt (build, `node_modules`, stb.).
- `docs/00_truth_files_template/*` **read-only**.
- Élő projekt docok:
  - `docs/live/02_allapot.md` (milestone-ok után frissül)
  - `docs/live/03_kovetkezo_lepesek.md` (milestone-ok után frissül)
- Ez a dokumentum:
  - **csak** „világmodell” változásnál frissül.
