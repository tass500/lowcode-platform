# Állapot (élő)

## Cél
Drift-proof observability egy greenfield lowcode platformban.

## Jelenlegi állapot – kész
- **Backend (ASP.NET Core)**
  - Admin endpointok válaszaiban **`serverTimeUtc`** elérhető (installation/status, upgrade-runs: recent/latest/queue/get/start/retry/cancel/dev-fail-step, audit list).
  - Admin response-ok **DTO-sítva** (Swagger Models/Schemas alatt látszanak a mezők).
  - Külön **observability endpoint**: `GET /api/admin/observability` (active runs + last audit + enforcement summary + `serverTimeUtc`).

- **Frontend (Angular)**
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

## Portok
- Backend: `http://localhost:5002`
- Frontend: `http://localhost:4200`
