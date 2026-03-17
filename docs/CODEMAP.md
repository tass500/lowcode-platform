# CODEMAP (gyors tájékozódás kontextusvesztés után)

## Cél
- **1-2 perc alatt** megtaláld, hogy a projektben *hol van a lényeg*, és hogyan tudod újraindítani a fejlesztést.
- **Low-noise**: csak a kulcs belépési pontokat és “forrás igazság” fájlokat sorolja.

## Gyorsindítás (run)
### Backend
- Port: `http://localhost:5002`
- Swagger: `http://localhost:5002/swagger`

### Frontend
- Port: `http://localhost:4200`

## Smoke (minimál ellenőrzés)
- **Swagger / Models**
  - Nézd meg, hogy a DTO-kban látszik-e a `serverTimeUtc` (és ahol kell, az `items`).
- **Upgrade page**
  - Diagnosztika blokkban frissül-e:
    - `lastServerTimeUtcApplied`
    - `serverNowOffsetMs`
    - `lastServerTimeUtcSource`
  - `Dev: simulate client clock drift` +5 percre:
    - megjelenik-e a drift warning
    - a duration-ok és “ago” értékek továbbra is értelmesek
  - `Copy incident bundle`:
    - tartalmazza-e a `timeDiagnostics` + `curlSnippets` + `serverTimeUtc` mezőket

## Backend – hol mit keress
### Admin controllerek (API felület)
- `backend/Controllers/AdminInstallationController.cs`
  - `/api/admin/installation/status`
  - `/api/admin/installation/dev-set` (dev-only)

- `backend/Controllers/AdminUpgradeRunsController.cs`
  - `/api/admin/upgrade-runs/*` (recent/latest/queue/get/start/retry/cancel/dev-fail-step)

- `backend/Controllers/AdminAuditController.cs`
  - `/api/admin/audit`

### Kontraktok / DTO-k (Swagger Models)
- `backend/Contracts/AdminDtos.cs`
  - Az admin response DTO-k forrása.
  - Itt látszik, hogy mely response-ok tartalmaznak `serverTimeUtc`-t és milyen `items` struktúrával.

### Error contract (D024)
- `backend/Services/ErrorResponses.cs` (+ kapcsolódó middleware)
  - Standardizált hibaválaszok (pl. `traceId`, `timestampUtc`, stb.).

### Audit log (perzisztencia)
- `backend/Models/AuditLog.cs`
- `backend/Data/PlatformDbContext.cs`

## Frontend – hol mit keress
### Fő UI (Upgrade oldal)
- `frontend/src/app/pages/upgrade-page.component.ts`
  - Admin API hívások + `serverTimeUtc` alkalmazása (`applyServerTimeUtc`)
  - Drift-proof “now” (`serverNowOffsetMs`)
  - “Last refreshed” + “ago”
  - Duration számítások (run/step + queue/recent)
  - Incident bundle (clipboard)
  - Audit táblában copy gombok

## Dokumentáció – “mi az igazság”
- **Stabil invariánsok**: `docs/PROJECT_CONTEXT.md`
- **Élő állapot**: `docs/live/02_allapot.md`
- **Élő következő lépések**: `docs/live/03_kovetkezo_lepesek.md`
- **Read-only template**: `docs/00_truth_files_template/*`
