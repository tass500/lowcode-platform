# Állapot (élő)

## Cél
Drift-proof observability egy greenfield lowcode platformban.

## Folyamat — AI / Cursor (takarékos mód)
- Irányelvek: [`ai-cursor-token-efficiency.md`](ai-cursor-token-efficiency.md), [`docs/DEVELOPMENT_WORKFLOW.md`](../DEVELOPMENT_WORKFLOW.md) §10; `.cursorignore` a repo gyökerében.
- Git: alapértelmezett trunk **`main`** (PR base, `scripts/gh-pr-push-merge.*`, `iter-end`); új branch mindig **`main`**-ről. A remote-on maradhat történeti **`master`** védett szabállyal — új PR-ok **`main`**-re mennek.

## Jelenlegi állapot – kész
- **Backend (ASP.NET Core)**
  - Lokális **dev ergonomics**: `scripts/clean-backend-artifacts.ps1` (backend `bin`/`obj` + scratch `build-*` mappák); `backend/Directory.Build.props`: `FAST_BUILD=true` → analyzers kikapcsolva gyors iterációhoz.
  - Workflow step run: **`last_error_config_path`** (JSON path a step configban, pl. `$.recordId`) — futás közbeni hibáknál kitöltve; `GET /api/workflows/runs/{runId}` részletek.
  - **Tenant-wide futások**: `GET /api/workflows/runs` — lapozás (`take`/`skip`), szűrők (`workflowDefinitionId`, `state`, `startedAfterUtc` / `startedBeforeUtc` UTC); válaszban `workflowName` + `totalCount`; index `workflow_run.started_at_utc`.
  - Admin endpointok válaszaiban **`serverTimeUtc`** elérhető (installation/status, upgrade-runs: recent/latest/queue/get/start/retry/cancel/dev-fail-step, audit list).
  - Admin response-ok **DTO-sítva** (Swagger Models/Schemas alatt látszanak a mezők).
  - Külön **observability endpoint**: `GET /api/admin/observability` (upgrade **active** runs + tenant **workflow** `pending`/`running` darabszám + last audit + enforcement summary + `serverTimeUtc`).
  - **Health (iter 60b):** `/health`, `/api/health`, `/health/live` — JSON: `status`, `service` (`lowcode-platform-backend`), `version` (assembly); `/health/ready` ugyanígy + `managementDb: ok` ha a management DB kapcsolódik, különben **503**.

- **Low-code workflow engine (Backend + Frontend demo)**
  - Támogatott workflow step-ek:
    - `noop`
    - `delay`
    - `set`
    - `map`
    - `merge`
    - `foreach`
    - `switch`
    - `require`
    - `domainCommand`
    - `unstable`
  - **Workflow run cancel (iter 59)**: `POST /api/workflows/runs/{runId}/cancel` (csak `pending`/`running`); kooperatív leállás; low-code **run details**: **Cancel run** gomb.
  - **Workflow import/export (iter 61):** `GET /api/workflows/{id}/export` (csomag `exportFormatVersion: 1`), `POST /api/workflows/import`; low-code **details** **Export JSON**, **Workflows** lista **Import** — [`workflow-import-export.md`](workflow-import-export.md).
  - **Tenant API key (iter 62a):** management DB `tenant_api_key_sha256_hex`; header `X-Tenant-Api-Key` → `tenant_user` auth JWT nélkül; admin **POST/DELETE** `.../tenants/{slug}/tenant-api-key`; lista `tenantApiKeyConfigured` — [`tenant-api-key.md`](tenant-api-key.md). **Testing + `LCP_EF_DESIGN_TIME`:** `Program.cs` tenant-feloldott `PlatformDbContext` marad teszten (ne `tenant-default.db`).
  - Utolsó lezárt **repo** milestone (workflow vonal): **55** — **step timeout / cancellation hardening**: `foreach` / `switch` belső lépések ugyanazt a **`retry` / `timeoutMs`** policy-t kapják, mint a top-level lépések; timeout → `lastErrorConfigPath` **`$.timeoutMs`**; doc [`docs/live/workflow-step-timeout-cancel.md`](workflow-step-timeout-cancel.md). Előtte **54** ütemezés [`workflow-schedule.md`](workflow-schedule.md). **Iteráció 56–61** lezárva (56–57 SS + Helm; 58a–b builder; 59 cancel; 60 observability/health; **61** import/export) — részletek [`03_kovetkezo_lepesek.md`](03_kovetkezo_lepesek.md).
    - unknown step type → warning
    - context var referencia ismeretlen step key-re → warning
    - kihasználatlan `set`/`map`/ismert `domainCommand` kimenet → warning
  - Workflow create/update (és kapcsolódó 404) hibák egységes `details` struktúrát adnak:
    - `path`
    - `code`
    - `message`
    - `severity`
  - **Következő stratégiai ütemterv:** **62** auth (opcionális), **58c** builder — [`03_kovetkezo_lepesek.md`](03_kovetkezo_lepesek.md) (táblázat) + részletes múlt: [`03_ARCHIVE.md`](03_ARCHIVE.md); SS + Helm: [`sqlserver-platform.md`](sqlserver-platform.md); timeout/cancel: [`workflow-step-timeout-cancel.md`](workflow-step-timeout-cancel.md); ütemezés: [`workflow-schedule.md`](workflow-schedule.md); konténer: [`container-deploy.md`](container-deploy.md), [`k3s-home-lab.md`](k3s-home-lab.md).


- **Frontend (Angular)**
  - Workflow details oldalon a read-only **Viewer v2** működik (kártyák + nyilak): step típus szerinti alcím / összefoglaló, `foreach`/`switch` branch előnézet, **JSON →** ugrás a JSON szerkesztőhöz; közös `lowcode-workflow-viewer-utils` + unit teszt; a JSON/Viewer nézet váltása stabil. **Builder** (iter 58a + **58b** New workflow): lépés **palette**, **↑↓** sorrend, törlés, JSON szinkron — [`workflow-visual-builder.md`](workflow-visual-builder.md), `lowcode-workflow-builder-utils` + teszt; **New** oldalon **Builder | JSON** váltó + Prettify/Minify a JSON nézetben.
  - Workflow **lint warnings** UI: összesen hány warning, **code szerinti csoportosítás** (×darab), hosszú üzenetek törése; create + details oldalon; közös `groupLintWarningsByCode` helper + unit teszt.
  - Workflow Viewer-ben a lint warningok lépésenként is látszanak (step badge + warning részlet), így gyorsabb a hibakeresés.
  - Workflow create/details oldalon a backend validációs `details` mezők UI-ban is láthatók (path|code|message), így gyorsabb a javítás.
  - Workflow **New** + **details** JSON szerkesztő: **Prettify / Minify**; template lista **szűrő**; context var javaslatok közös `lowcode-workflow-context-suggestions` modullal (domain + foreach + `switch` branch + belső lépés) + böngészős **datalist** autocomplete a chip-ek mellett.
  - **Workflow runs lista** (`/lowcode/workflow-runs`): tenant összes futása táblázatban, link a run details-re (`/lowcode/runs/:runId`); Workflows oldalról **All runs**.
  - Workflow **details**: **Schedule (UTC)** blokk — engedélyezés, 5 mezős MVP cron, **Save schedule** → `PUT /api/workflows/{id}/schedule`.
  - Low-code **workflow run details** (`/lowcode/runs/:runId`): **Cancel run** (`pending`/`running`); step config **Original / Resolved** összehasonlítás + toggle; kereső tartalmazza az `originalStepConfigJson`-t; Config megnyitásakor ha eltér az original a resolved-tól → alapból „Show resolved”; **Copy** (config / output); reszponzív kétoszlopos rács; `lowcode-run-details-utils` + unit teszt.
  - Ugyanitt: sikertelen / hibás lépéseknél **Error path** (`lastErrorConfigPath`, pl. `$.recordId` context var hibánál); a szűrő mező erre is rákeres.
  - Drift-proof “now”: kliens oldali **`serverNowOffsetMs`** kalibráció `serverTimeUtc` alapján.
  - “Last refreshed” + “ago” kijelzés queue/audit/run panelen server-calibráltan.
  - **Clock drift warning** (>= 2 perc) + dev drift szimuláció.
  - Diagnosztika blokk: utolsó `serverTimeUtc`, offset, source.
  - **Incident bundle**: JSON diagnosztika (status/queue/run/audit + timeDiagnostics + curl snippetek):
    - vágólapra másolás
    - letöltés `.json` fájlba (nagy payloadnál megerősítés)
    - no behavior change refaktor: `copyIncidentBundle()` delegál a shared helperre (`buildIncidentBundleImpl`) a duplikált összeállítás kiváltására (PR: `refactor/upgrade-incident-bundle-wiring`)
  - **Refresh all** gomb (status/observability/queue/audit + run; ha nincs kiválasztott run, akkor `latest` töltődik, nem silent módon).
  - **Polling auto-stop**: csak `pending/running` state esetén pollol, terminal state-nél megáll.
  - Batch C hardening (loading/error konzisztencia):
    - sikeres `loadLatest`/`loadRun` után a `runError` nem maradhat stale állapotban
    - polling hiba nem üríti ki a run details panelt (transient hibánál is megmarad az utolsó sikeres run)
    - audit betöltésnél 30s timeout watchdog (ne maradjon stuck spinner)
    - Batch C sweep: Start/Retry/Cancel/DevFail akciók egységes stale state resetje (error/info)
    - Batch C sweep: incident bundle export dedikált error state (`copyBundleError`) + UI megjelenítés
  - **Debug bundle blokk** a run details alatt: ticket header + **short ticket one-liner** + **ticket (Markdown) template** + X-Trace-Id header + curls + incident bundle (1-kattintásos copy).
  - **Debug pack**: 1 JSON fájl (copy/download) ticket markdown + curls + incident bundle + audit linkek + audit export preview (cap 200).
  - `Load latest` UI gomb stabil: a backend `latest` **teljes run details**-t ad vissza (steps-szel), nincs extra `GET /{id}` kör.
  - Üzemeltetői leírás: `docs/live/ops/upgrade.md`.
  - Audit táblában soronként ops gombok: **copy traceId / copy curl / copy details**.
  - Audit panel ops gombok: **Copy list URL** + **Copy export URL**.
  - Audit paging + export:
    - `Prev/Next` lapozás (`skip` query param)
    - `Copy page JSON` (aktuális oldal)
    - listában `Items X–Y / Total`
    - `Copy full audit JSON` + `Download full audit JSON` (backend export endpoint + `max` limit, szerver oldali hard limit `max <= 10000`)
  - Admin *read* endpointok cache-hardening: `no-store/no-cache` (audit + status + observability + queue/recent/latest/get)
  - **Duration** megjelenítés drift-proof módon:
    - Run + step durations server-calibrált “now”-val
    - Queue táblában duration oszlop
    - Recent dropdown labelben duration
  - Runbook: `docs/runbooks/upgrade_v0.md` frissítve failure triage + on-call checklist résszel (UI eszközök: client trace id, curl snippets, incident bundle, debug pack, audit URL/export).

## Nyitott / következő nagy döntések
 - Kell-e további “enterprise” csiszolás a debug/incident flow-ra:
   - egységesebb ticket header formátum
   - audit paging / export
   - incident bundle bővítések (pl. extra kontextus)

 - Frontend irány a low-code motor demóhoz (döntés):
   - Most: frontend építése (tenant switch + auth + workflow CRUD + run monitor + entity defs).
   - Vizuális workflow: először read-only **viewer** (wow + megértés).
   - Később: read-write drag&drop builder, amikor stabil a step modell + validáció + verziózás.

 - Upgrade-page (ops/admin UI) munkaszál státusz (guardrail):
   - Állapot: **parkoltatva** (működő, de nem ez a következő demo fókusz).
   - Szabály: csak **bugfix** / kompatibilitási javítás (backend contract változás esetén) kerülhet bele.
   - Minimum egészség-ellenőrzés (ha hozzányúlunk): status/observability/queue/audit betölt + run details nem ürül transient hiba miatt.

 - Cél deployment/üzemeltetés (home-lab “cloud-like”, RPi5):
   - 1× Raspberry Pi 5 + 256 GB NVMe SSD
   - k3s single-node + Helm (GitOps-lite; később opcionális ArgoCD/Flux)
   - Upgrade stratégia: immutable image tag + Helm upgrade/rollback; az Upgrade UI monitor/diagnosztika szerepben
   - Minimal ops baseline: ingress + (szükség esetén) TLS + backup (CronJob) + alap monitoring/log

 - DB-agnosztikus irány (enterprise felé):
   - Támogatott provider cél: PostgreSQL + SQL Server
   - Ajánlott megoldás: provider-specifikus EF Core migrations (külön migrations assembly Postgres/SQL Server)
   - Jelenlegi fejlesztői alap (ha így van bekötve): SQLite

## Portok
 - Backend: `http://localhost:5002`
 - Frontend: `http://localhost:4200`
