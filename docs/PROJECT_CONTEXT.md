# Projekt kontextus (Single Source of Truth)

## Cél
Ez a dokumentum a projekt aktuális invariánsainak és működési megállapodásainak **alacsony zajú, stabil referenciája**.

- **Csak akkor** változzon, ha a projekt „világmodellje” változik (szerződések, invariánsok, portok, core flow-k).
- A napi haladás / állapot követéséhez ezeket használd:
  - `docs/live/02_allapot.md`
  - `docs/live/03_kovetkezo_lepesek.md`
  - `docs/live/ai-cursor-token-efficiency.md` (Cursor / AI: takarékos kontextus; lásd még `DEVELOPMENT_WORKFLOW.md` §10)

## Vízió és aktuális szállítási fókusz

- A hosszú távú termékvízió kanonikus szövege a sablonban van: `docs/00_truth_files_template/00_vizio.md` (ez a másolat **nem** szerkeszthető; a döntések eredeti helye a template szabályai szerint a `01_dontesek.md` lenne, ami jelenleg csak ott létezik).
- **A ténylegesen épített milestone-ok** és a WIP mindig a **`docs/live/02_allapot.md`** + **`docs/live/03_kovetkezo_lepesek.md`** alapján értelmezendők — így a vízió és a napi scope nem keveredik össze.
- **Szándékos ütemezés:** a jelenlegi szakaszban a repo fókusza a **platform / low-code workflow motor**, **tenant + auth**, **megfigyelhetőség / health / üzemeltetési demó** (és kapcsolódó minőség: tesztek, error contract, trace) körül van; a vízióban szereplő teljes **ERP bounded context** réteg, **marketplace**, **teljes definition publish/promote / governance** és hasonlók **későbbi milestone-ok**, nem elvetett irány.

## Futási végpontok
- **Backend**: `http://localhost:5002`
- **Frontend**: `http://localhost:4200`

## Auth (összefoglaló)

- **Szimmetrikus JWT** + opcionális `iss`/`aud`: [`live/tenant-api-key.md`](live/tenant-api-key.md) (JWT szekció).
- **Opcionális OIDC JWT** ugyanazon `Authorization: Bearer` fejléccel: [`live/oidc-jwt-bearer.md`](live/oidc-jwt-bearer.md).
- **BFF + httpOnly (62c, kész):** backend + Angular — `meta`, `withCredentials`, auth oldal; session süti, login/callback, **cookie→Bearer middleware** — [`live/auth-bff-httponly.md`](live/auth-bff-httponly.md). A SPA **dev-token / OIDC** út továbbra is opcionális (dev), párhuzamosan.
- **Következő szállítási hullám (iter 63a+):** [`live/roadmap-next-iterations.md`](live/roadmap-next-iterations.md).

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
- **Live doc takarékosság:** `docs/live/03_kovetkezo_lepesek.md` = rövid ACTIVE + checklist; régi, hosszú napló: `docs/live/03_ARCHIVE.md` (ne legyen alapból teljes kontextusban).
- Cursor rövid szabályok (AI asszisztens): `.cursor/rules/*.mdc` — ugyanazt a folyamatot tükrözik; részletek a fenti dokumentumban. A repo gyökerében **`.cursorignore`** csökkenti az indexelt zajt (build, `node_modules`, stb.).
- `docs/00_truth_files_template/*` **read-only**.
- Élő projekt docok:
  - `docs/live/02_allapot.md` (milestone-ok után frissül)
  - `docs/live/03_kovetkezo_lepesek.md` (milestone-ok után frissül)
- Ez a dokumentum:
  - **csak** „világmodell” változásnál frissül.
