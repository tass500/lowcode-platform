# Upgrade funkció (üzemeltetői README)

## Cél (miért van?)
Az **Upgrade** funkció a platform **kontrollált, auditálható változtatási mechanizmusa** olyan esetekre, amikor a rendszer állapota változik (adat/séma/állapot migrációk, kompatibilitási lépések, enforcement miatti verzióemelés).

Ezzel elkerüljük:
- kézi, ad-hoc prod módosításokat
- „ki mit csinált” típusú homályos helyzeteket
- lassú incident kezelést (nincs meg gyorsan a teljes kontextus)

## Fogalmi modell

### Upgrade run
Egy **upgrade run** egy állapotgép:
- `pending` → `running` → `succeeded | failed | canceled`

A run **lépésekből (step-ekből)** áll, amiknek külön van:
- állapot, próbálkozásszám, időbélyegek, hibakód/hibaüzenet

### Egyszerre csak 1 aktív run (invariáns)
Egyszerre maximum **1** aktív (`pending` vagy `running`) run lehet.
- csökkenti a konkurens futásokból jövő race condition / adat-korrupció kockázatát
- egyszerűsíti az üzemeltetést és a hibakeresést
- a UI blokkolja a `Start`-ot, ha van aktív run

## Mit építettünk hozzá? (röviden)

### Drift-proof időkezelés a UI-ban
- Backend válaszokban van `serverTimeUtc`
- UI számol offsetet (`serverNowOffsetMs`)
- „Last refreshed” / „ago” / duration szerverhez kalibrált (kliens óra drift esetén is)
- UI kiír drift diagnosztikát (`serverTimeUtc`, offset, source) és figyelmeztet nagy drift esetén

### Observability snapshot
- Backend: `GET /api/admin/observability`
- UI-ban látszik:
  - enforcement összegzés
  - aktív run-ok
  - utolsó audit esemény

### Operátori UX
- **Refresh all**: status + observability + queue + audit + run (ha van kiválasztott; ha nincs, akkor `latest` töltődik)
- **Polling**:
  - csak `pending/running` esetén fut
  - terminal state-nél megáll
  - közben időszakosan frissíti a queue/observability-t is
  - terminal átmenetnél azonnal frissít: queue/observability/recent/audit
- **Supported-version enforcement**:
  - `soft_block`: `Start` / `Retry` engedélyezett, de UI megerősítést kér
  - `hard_block`: `Start` / `Retry` blokkolt
  - `Cancel` engedélyezett (operátori stop mindig elérhető)
- **Dev: simulate enforcement** (csak fejlesztői környezetben):
  - `Preset ok`: current == supported (enforcement: `ok`)
  - `Preset warn`: supported verzió kicsit előrébb (enforcement: `warn`)
  - `Preset soft`: supported verzió előrébb, de még az upgrade window-n belül (enforcement: `soft_block`)
  - `Preset hard`: supported verzió jóval előrébb, window-n túl (enforcement: `hard_block`)
- **Dev: fail step** (csak fejlesztői környezetben):
  - egy futó run-nál adott step egyszeri elrontása (pl. `canary-migrate` / `wave1-migrate`), hogy a retry/cancel/audit flow kipróbálható legyen
- **Drift-proof debug headerek** (admin read válaszokban):
  - `X-LCP-Server-Version`: backend assembly version
  - `X-LCP-Server-Revision`: deploy/build revision (ha elérhető)
  - `X-LCP-Server-Environment`: futtatási környezet (`Development`/`Production`)
- **Debug bundle blokk** (run details alatt):
  - 1 kattintásos másolás:
    - ticket header
    - rövid ticket one-liner (`Copy short header`)
    - ticket sablon (`Copy ticket (Markdown)`)
    - ticket sablon letöltés (`Download ticket (Markdown)`)
    - `X-Trace-Id` header snippet
    - curl csokor
    - curl csokor letöltés (`Download curls`)
    - incident bundle JSON
    - incident bundle letöltés (`Download incident bundle`)
    - debug pack másolás/letöltés (`Copy debug pack` / `Download debug pack`) – 1 JSON fájlban: ticket markdown + curls + incident bundle + audit export paramok/URL-ek + audit export preview (cap: 200)
  - nagy incident bundle payloadnál a UI megerősítést kér letöltés előtt
- **Audit log eszközök**:
  - listázás filterekkel (actor/actionContains/traceId/sinceUtc)
  - paging (`Prev/Next`)
  - listában kijelzés: `Items X–Y / Total`
  - `Copy list URL` (aktuális filter + paging alapján)
  - `Copy export URL` (aktuális filter alapján)
  - `Copy page JSON` (aktuális oldal + filter kontextus)
  - `Copy full audit JSON` (export endpoint, max limittel; a JSON tartalmaz `totalCount` + `returnedCount` mezőket)
  - `Download full audit JSON` (ugyanez fájlba, `.json`)
  - nagy `Export max` értéknél a UI megerősítést kér
  - exportnál szerver oldali hard limit van: `max <= 10000` (különben 413)
  - admin audit list/export válaszok cache-elése tiltott (`no-store/no-cache`)

## Hogyan használjuk? (Ops playbook)

### 1) Run indítása
1. Nyisd meg: `/upgrade`
2. (Opcionális) állíts be **Client TraceId**-t (`Generate`), ha tickethez összefűznéd a hívásokat
3. Add meg a target verziót és nyomj **Start**-ot
4. Kövesd a run állapotát és a step-eket az **Upgrade run details** panelen

Várható:
- polling automatikusan indul aktív állapotnál
- terminalnál megáll

### 2) Ha nem enged Start-olni („already active run”)
Ok: van aktív run a queue-ban.

Teendő:
- **Queue (pending/running)** panel → `Open` az aktív run-ra (vagy `Load latest`)
- várj a befejezésig vagy `Cancel` (ha indokolt)

Megjegyzés: a `Load latest` már a legutóbbi/aktív run **teljes részleteit** adja vissza (steps-szel), nincs külön `GET /{id}` kör.

### 3) Ha `failed`
1. **Debug bundle** blokk:
   - `Copy ticket header`
   - `Copy incident bundle`
2. Döntés:
   - `Retry failed` (ha javítottuk az okot / transient volt)
   - vagy `Cancel` és új run

### 4) Ticket/incident csomag (ajánlott standard)
A **Debug bundle** blokkból:
- **Copy ticket header** (ticket tetejére)
- **Copy incident bundle** (alá)

A csomag tartalma:
- installation + enforcement + run/trace + drift információ
- status + queue + observability + run + audit snapshot
- curl snippetek reprodukcióhoz
- timeDiagnostics (offset/drift/source)

## API / rövid referencia

### Frontend (proxy)
A hívások `/api/...` alatt mennek, és dev módban proxyn át a backend `http://localhost:5002` felé.

### Használt backend endpointok
- `GET /api/admin/installation/status`
- `GET /api/admin/observability`
- `GET /api/admin/upgrade-runs/queue`
- `GET /api/admin/upgrade-runs/recent?take=...`
- `GET /api/admin/upgrade-runs/latest`
- `GET /api/admin/upgrade-runs/{id}`
- `POST /api/admin/upgrade-runs` (Start)
- `POST /api/admin/upgrade-runs/{id}/retry`
- `POST /api/admin/upgrade-runs/{id}/cancel`
- `GET /api/admin/audit?...`
- `GET /api/admin/audit/export?max=...&actor=...&actionContains=...&traceId=...&sinceUtc=...`

Opcionális header:
- `X-Trace-Id: <client trace id>`

## Hibaelhárítás

### UI: eltűnt gomb / nem frissül / gyanúsan régi állapot
1. Nézd meg a run details alatti **Debug bundle** blokk alján a `UI rev` értéket.
2. Böngészőben **hard refresh**:
   - Chrome/Edge: `Ctrl+Shift+R`
   - Firefox: `Ctrl+F5`
3. Dev módban ellenőrizd, hogy a frontend szerver tényleg fut:
   - `ng serve` / `npm start` (ne állítsd le `Ctrl+C`-vel)
4. Ha `UI rev` nem változik (vagy a fordítás időnként NG1 hibát dob): állítsd le a `ng serve`-t, majd indítsd újra.

### „An upgrade run is already active…”
- ok: van `pending/running` run
- megoldás: queue → open → vár/cancel

### Queue „pending” beragad
- a UI polling közben frissíti a queue/observability-t is, terminal átmenetnél azonnali frissítés van
- ha mégis stale: tipikusan régi dev server fut / nincs hard refresh

### „Last refreshed” gyanús
- nézd a drift diagnosztikát:
  - `serverTimeUtc`
  - `offset`
  - `source`
- nagy drift esetén lokális óra / szerveridő vizsgálat

## Hatókör / nem cél
- Nem release/deploy pipeline helyett van (ez runtime migráció/karbantartási futás)
- Nem támogat párhuzamos upgrade-eket (szándékosan)

## Lehetséges jövőbeli bővítések
- incident bundle export formátumok
- többféle „header preset” (rövid ticket one-liner vs teljes debug header)
