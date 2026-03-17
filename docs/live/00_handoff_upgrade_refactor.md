# Handoff (context-reset safe) — Upgrade page refactor + drift-proof triage

## Cél

- Az `frontend/src/app/pages/upgrade-page.component.ts` egy nagy, folyamatosan növő komponens volt.
- Cél: a logikai blokkok kiszervezése kisebb helper modulokba **no behavior change** elvvel.
- Másodlagos cél: az üzemeltetési/triage flow dokumentálása (`runbook`, `live docs`).

## Gyors orientáció (entry points)

- UI: `http://localhost:4200/upgrade`
- Backend Swagger: `http://localhost:5002/swagger`
- Fő komponens: `frontend/src/app/pages/upgrade-page.component.ts`

## Mi lett kész (refaktor szeletek)

A komponensben a legtöbb kiszervezett metódus **megmaradt** (template kompatibilitás), de már csak delegál a helper modulokra.

- `frontend/src/app/pages/upgrade/upgrade-types.ts`
  - DTO/type definíciók
- `frontend/src/app/pages/upgrade/upgrade-export-builders.ts`
  - audit export text + export/pack/incident filename builder-ek
- `frontend/src/app/pages/upgrade/upgrade-api.ts`
  - HttpClient wrapper-ek serverTime/headers kezeléssel
- `frontend/src/app/pages/upgrade/upgrade-incident-bundle.ts`
  - incident bundle payload + output + curl snippet builder
- `frontend/src/app/pages/upgrade/upgrade-debug-pack.ts`
  - debug pack output serialize helper
- `frontend/src/app/pages/upgrade/upgrade-ui-utils.ts`
  - tiszta UI helper-ek (download/format/badge/shortId)
- `frontend/src/app/pages/upgrade/upgrade-curl-snippets.ts`
  - curl snippet string builder-ek
- `frontend/src/app/pages/upgrade/upgrade-url-builders.ts`
  - audit list/export URL builder + toAbsoluteUrl
- `frontend/src/app/pages/upgrade/upgrade-audit-panel-snapshot.ts`
  - audit panel snapshot builder
- `frontend/src/app/pages/upgrade/upgrade-errors.ts`
  - common `formatError(e,fallback)`
- `frontend/src/app/pages/upgrade/upgrade-time-utils.ts`
  - `parseDateUtc`, `serverNowMs`, `serverNowDate`
- `frontend/src/app/pages/upgrade/upgrade-duration-utils.ts`
  - duration deduplikáció (`durationMsFromStartedAtUtc`, `durationMsFromStartEndUtc`)
- `frontend/src/app/pages/upgrade/upgrade-polling.ts`
  - nowTick + run polling interval helper-ek
- `frontend/src/app/pages/upgrade/upgrade-timeouts.ts`
  - transient status (copyStatus) + watchdog timeout helper-ek
- `frontend/src/app/pages/upgrade/upgrade-download-utils.ts`
  - download guardrail (large download confirm) + compact timestamp helper
- `frontend/src/app/pages/upgrade/upgrade-confirm-utils.ts`
  - `window.confirm` boilerplate + soft-block confirm deduplikáció
- `frontend/src/app/pages/upgrade/upgrade-guardrails.ts`
  - enforcement / blocked állapot guardrail helper-ek
- `frontend/src/app/pages/upgrade/upgrade-server-build.ts`
  - `X-LCP-Server-*` header parse + `ServerBuildInfo`
- `frontend/src/app/pages/upgrade/upgrade-triage-snapshot.ts`
  - triage snapshot text builder
- `frontend/src/app/pages/upgrade/upgrade-audit-urls.ts`
  - audit list/export URL build + filter param összerakás
- `frontend/src/app/pages/upgrade/upgrade-ticket-markdown.ts`
  - ticket markdown builder-ek (deduplikálva), no behavior change
- `frontend/src/app/pages/upgrade/upgrade-http-promises.ts`
  - Http observable -> Promise wrapper-ek (async/await ergonomia)
- `frontend/src/app/pages/upgrade/upgrade-audit-load-runner.ts`
  - audit load seq/watchdog/timeout/state wiring
- `frontend/src/app/pages/upgrade/upgrade-audit-export-runner.ts`
  - audit export (copy/download) wiring
- `frontend/src/app/pages/upgrade/upgrade-debug-pack-export-runner.ts`
  - debug pack export (copy/download + large confirm + state)
- `frontend/src/app/pages/upgrade/upgrade-incident-bundle-export-runner.ts`
  - incident bundle export (copy/download + large confirm + state)
- `frontend/src/app/pages/upgrade/upgrade-ticket-markdown-download-runner.ts`
  - ticket markdown download wiring
- `frontend/src/app/pages/upgrade/upgrade-ticket-markdown-copy-runner.ts`
  - ticket markdown copy wiring
- `frontend/src/app/pages/upgrade/upgrade-curl-snippets-download-runner.ts`
  - curl snippets download wiring
- `frontend/src/app/pages/upgrade/upgrade-curl-snippets-copy-runner.ts`
  - curl snippets copy wiring
- `frontend/src/app/pages/upgrade/upgrade-simple-copy-runner.ts`
  - runId/traceId/trace header/server headers/triage snapshot copy wiring
- `frontend/src/app/pages/upgrade/upgrade-audit-filter-actions.ts`
  - audit filter preset/actions wiring (only upgrades/current run/apply)
- `frontend/src/app/pages/upgrade/upgrade-audit-paging-actions.ts`
  - audit prev/next page wiring
- `frontend/src/app/pages/upgrade/upgrade-audit-panel-actions.ts`
  - audit panel actions (clear filters + copy page JSON)
- `frontend/src/app/pages/upgrade/upgrade-url-copy-actions.ts`
  - audit URL copy wiring
- `frontend/src/app/pages/upgrade/upgrade-audit-copy-actions.ts`
  - audit details/traceId/curl copy wiring
- `frontend/src/app/pages/upgrade/upgrade-duration-actions.ts`
  - duration számolók wiring (queue/recent/run/step)
- `frontend/src/app/pages/upgrade/upgrade-recent-ui-actions.ts`
  - recent UI wiring (label + selected row style)
- `frontend/src/app/pages/upgrade/upgrade-preview-text-actions.ts`
  - preview text wiring (server build/trace header/curl snippets + ticket header copy)
- `frontend/src/app/pages/upgrade/upgrade-small-actions.ts`
  - small wrapper wiring (copy/download full audit JSON + curl snippets copy/download delegálás)

## Dokumentáció frissítések

- `docs/live/02_allapot.md`
  - “kész” állapotlista + triage capability frissítve
- `docs/live/03_kovetkezo_lepesek.md`
  - refaktor WIP lista: kiszervezett modulok pipálva
- `docs/runbooks/upgrade_v0.md`
  - failure triage bővítve UI eszközökkel
  - on-call checklist blokk hozzáadva

## Build gate-ek (ellenőrzés)

- Frontend:
  - `npm run build` (cwd: `frontend/`)
- Backend:
  - `dotnet build` (cwd: `backend/`)

Enterprise-minimum gate-ek + smoke checklist: `docs/01_quality_gates.md`.

## Fontos invariánsok / megjegyzések

- **No behavior change**: a refaktor célja csak a kód szervezése volt.
- **Trace korreláció**:
  - Backend: minden válaszban `X-Trace-Id` header.
  - UI: `Client trace id` mező küldhető headerként.
- **Drift-proof time**:
  - UI `serverNowOffsetMs` a backend `serverTimeUtc` mezőiből kalibrál.

## Munkamód (repo-szint)

- A credit-hatékony / batch munkamód repo-szinten van rögzítve: `docs/00_workmode.md`.

## Következő javasolt lépések (ha folytatjuk a refaktort)

- ✅ Kész: polling/interval kezelések tisztítása (kisebb helper / service), de óvatosan mert érzékeny.
- ✅ Kész: copy/download ops “guardrail” logika egységesítése (size limit confirm, státusz üzenetek).
- Opció C: további builder-ek / ticket formatting szeparálása, ha még nő.

## Kezdő prompt új csevegéshez (másold be)

```
Te Cascade vagy, senior pair programmer.

Projekt: /home/zoli/lowcode-platform (.NET backend + Angular frontend).

Cél: folytatni az upgrade page refaktort (no behavior change) és drift-proof triage eszközöket.

Munkamód:
- A `docs/00_workmode.md` alapján dolgozz (credit-hatékony batch).

Aktuális állapot:
- Fő komponens: frontend/src/app/pages/upgrade-page.component.ts
- Már kiszervezett helper modulok: 
  - frontend/src/app/pages/upgrade/upgrade-types.ts
  - upgrade-export-builders.ts
  - upgrade-api.ts
  - upgrade-incident-bundle.ts
  - upgrade-debug-pack.ts
  - upgrade-ui-utils.ts
  - upgrade-curl-snippets.ts
  - upgrade-url-builders.ts
  - upgrade-audit-panel-snapshot.ts
  - upgrade-errors.ts
  - upgrade-time-utils.ts
  - upgrade-duration-utils.ts
  - upgrade-polling.ts
  - upgrade-timeouts.ts
  - upgrade-download-utils.ts
  - upgrade-confirm-utils.ts
  - upgrade-guardrails.ts
  - upgrade-server-build.ts
  - upgrade-triage-snapshot.ts
  - upgrade-audit-urls.ts
  - upgrade-ticket-markdown.ts
  - upgrade-http-promises.ts
  - upgrade-audit-load-runner.ts
  - upgrade-audit-export-runner.ts
  - upgrade-debug-pack-export-runner.ts
  - upgrade-incident-bundle-export-runner.ts
  - upgrade-ticket-markdown-download-runner.ts
  - upgrade-ticket-markdown-copy-runner.ts
  - upgrade-curl-snippets-download-runner.ts
  - upgrade-curl-snippets-copy-runner.ts
  - upgrade-simple-copy-runner.ts
  - upgrade-audit-filter-actions.ts
  - upgrade-audit-paging-actions.ts
  - upgrade-audit-panel-actions.ts
  - upgrade-url-copy-actions.ts
  - upgrade-audit-copy-actions.ts
  - upgrade-duration-actions.ts
  - upgrade-recent-ui-actions.ts
  - upgrade-preview-text-actions.ts
  - upgrade-small-actions.ts

Dokumentáció:
- docs/live/02_allapot.md és docs/live/03_kovetkezo_lepesek.md legyen frissítve minden meaningful változás után.
- docs/runbooks/upgrade_v0.md tartalmazza a triage+on-call checklistet.

Build gate-ek:
- frontend: npm run build
- backend: dotnet build

Kérlek:
1) Olvasd be a docs/live/03_kovetkezo_lepesek.md és a frontend/src/app/pages/upgrade-page.component.ts releváns részeit.
2) Javasolj 2-3 következő refaktor szeletet, és implementáld a legkisebb rizikójút.
3) Tartsd a no behavior change elvet, és futtasd a build gate-et.
