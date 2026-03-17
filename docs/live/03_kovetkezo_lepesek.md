# Következő lépések (élő)

## Rövid működési elv
- A `docs/00_truth_files_template/*` fájlok **nem változnak**.
- Ezt a fájlt és a `docs/02_allapot.md`-t **minden lezárt milestone után** frissítjük.
- WIP limit: egyszerre **pontosan 1** aktív fókusz lehet (WIP=1). Ha **biztonságosan összevonható** több kisebb feladat (alacsony kockázat, no behavior change jelleg, ugyanazon a területen), akkor összevonható egy menetbe. Az összevonást mindig automatikusan mérlegelni kell, és a döntést a fejlesztő asszisztens hozza meg, külön jóváhagyás kérése nélkül. Ha kockázatos, marad az 1 kijelölt feladat fejlesztése.

## Üzemeltetői gyorslinkek
- Upgrade ops README: `docs/live/ops/upgrade.md`

## Következő milestone javaslat (1 kör / stabil)
- ✅ Kész: backend `/api/admin/observability` + UI integráció.
- ✅ Kész: drift-proof “last refreshed” (serverTimeUtc alapján kalibrált idő).
- ✅ Kész: Upgrade UI `Refresh all` + polling auto-stop (csak `pending/running` esetén pollol, terminal state-nél megáll).
- ✅ Kész: Debug bundle helper gombok felül (copy header / run trace / curls / incident bundle).
- ✅ Kész: Start upgrade route fix (frontend `POST /api/admin/upgrade-runs`).

## Következő milestone javaslat (WIP=1, safe bundling megengedett)
- ✅ Kész: kompakt “Debug bundle” blokk az Upgrade UI-ban.
- ✅ Kész: ticket header egységesítés + bővítés (installationId + enforcement) a Debug bundle-ben és az Incident bundle elején.
- ✅ Kész: trace/queue frissítés stabilizálás (queue auto-refresh polling közben, terminal state-nél azonnali frissítések).
- ✅ Kész: audit paging + export (Prev/Next + Copy page JSON + Copy full audit JSON).
- ✅ Kész: audit export letöltés + `Items X–Y / Total` + szerver oldali hard limit (`max <= 10000`).
- ✅ Kész: audit panel ops gombok: Copy list URL + Copy export URL.
- ✅ Kész: incident bundle letöltés `.json` fájlba.
- ✅ Kész: admin read endpointok cache-hardening (`no-store/no-cache`).
- ✅ Kész: debug pack (copy/download) – ticket markdown + curls + incident bundle + audit export preview.
- ✅ Kész: Refresh all konzisztencia: ha nincs kiválasztott run, `latest` töltődik nem silent módon (spinner + runError reset).
- ✅ Kész: mini refaktor (no behavior change): audit URL másolás duplikáció kiváltása `copyAbsoluteUrl` helperrel.
- ✅ Kész: mini refaktor (no behavior change): audit URL builder duplikáció kiváltása `addAuditFilterParams` helperrel.
- ✅ Kész: mini refaktor (no behavior change): HTTP helper-ekben `serverTimeUtc` alkalmazás duplikáció kiváltása (`applyServerTimeFromBody`/`applyServerTimeFromResponse`).
- ✅ Kész: mini refaktor (no behavior change): `observe: 'response'` GET request options duplikáció kiváltása (`requestOptionsWithResponse`).
- ✅ Kész: mini refaktor (no behavior change): Audit state csoportosítás (`auditVm` + audit* getter/setter forward), template változtatás nélkül.
- ✅ Kész: mini refaktor (no behavior change): Run state csoportosítás (`runVm` + run* getter/setter forward), template változtatás nélkül.
- ✅ Kész: mini refaktor (no behavior change): Queue + Observability state csoportosítás (`queueVm`/`observabilityVm` + getter/setter forward), template változtatás nélkül.
- ✅ Kész: mini refaktor (no behavior change): Status state csoportosítás (`statusVm` + status* getter/setter forward), template változtatás nélkül.
- ✅ Kész: Batch C hardening: sikeres `loadLatest`/`loadRun` után `runError` stale állapot törlése.
- ✅ Kész: Batch C hardening: polling hiba esetén a run details nem nullázódik (megtartjuk az utolsó sikeres run-t).
- ✅ Kész: Batch C hardening: `loadAudit()` 30s timeout watchdog (ne maradjon stuck spinner).

## Következő konkrét lépések (ajánlott)
- ✅ Kész: Batch C sweep: Start/Retry/Cancel/DevFail flow-k stale state resetjeinek egységesítése (loading/error/info).
- ✅ Kész: Batch C sweep: audit export / incident bundle UI state (loading/error) konzisztencia ellenőrzés.
- Refaktor (WIP): upgrade-page további bontása kisebb helper/service egységekre (no behavior change).
  - ✅ Kész: type definíciók kiszervezése (`pages/upgrade/upgrade-types.ts`).
  - ✅ Kész: export/bundle builder-ek első szelete kiszervezve (`pages/upgrade/upgrade-export-builders.ts`).
  - ✅ Kész: HTTP helper-ek kiszervezve (`pages/upgrade/upgrade-api.ts`).
  - ✅ Kész: incident bundle builder kiszervezve (`pages/upgrade/upgrade-incident-bundle.ts`).
  - ✅ Kész: debug pack serialize helper kiszervezve (`pages/upgrade/upgrade-debug-pack.ts`).
  - ✅ Kész: UI utilok (download/format/badge/shortId) kiszervezve (`pages/upgrade/upgrade-ui-utils.ts`).
  - ✅ Kész: curl snippet builder-ek kiszervezve (`pages/upgrade/upgrade-curl-snippets.ts`).
  - ✅ Kész: URL builder-ek/normalizálás kiszervezve (`pages/upgrade/upgrade-url-builders.ts`).
  - ✅ Kész: audit panel snapshot builder kiszervezve (`pages/upgrade/upgrade-audit-panel-snapshot.ts`).
  - ✅ Kész: common error formatter kiszervezve (`pages/upgrade/upgrade-errors.ts`).
  - ✅ Kész: time utilok (parseDateUtc + serverNowMs/Date) kiszervezve (`pages/upgrade/upgrade-time-utils.ts`).
  - ✅ Kész: duration számolók deduplikálva helperbe (`pages/upgrade/upgrade-duration-utils.ts`).
  - ✅ Kész: polling/interval helper kiszervezve (`pages/upgrade/upgrade-polling.ts`).
  - ✅ Kész: timeout/watchdog helper kiszervezve (`pages/upgrade/upgrade-timeouts.ts`).
  - ✅ Kész: download guardrail/timestamp helper kiszervezve (`pages/upgrade/upgrade-download-utils.ts`).
  - ✅ Kész: ticket/curl download fileName builder-ek kiszervezve (`pages/upgrade/upgrade-export-builders.ts`).
  - ✅ Kész: download wrapper metódusok eltávolítva (közvetlen `downloadTextFile*` helper hívások a komponensben).
  - ✅ Kész: confirm helper kiszervezve (`pages/upgrade/upgrade-confirm-utils.ts`).
  - ✅ Kész: soft_block confirm deduplikálva (`confirmSoftBlockContinue` a `pages/upgrade/upgrade-confirm-utils.ts`-ben).
  - ✅ Kész: enforcement guardrail helper kiszervezve (`pages/upgrade/upgrade-guardrails.ts`).
  - ✅ Kész: server build header parse kiszervezve (`pages/upgrade/upgrade-server-build.ts`).
  - ✅ Kész: action error reset logika tisztítva (a `resetUpgradeActionState` kisebb reset metódusokra bontva), no behavior change.
  - ✅ Kész: `serverBuild` típus egységesítve a helper `ServerBuildInfo` típusára, no behavior change.
  - ✅ Kész: server build header sorok deduplikálva helperrel (`serverBuildHeaderLines`), no behavior change.
  - ✅ Kész: ticket markdown builder-ek kiszervezve és deduplikálva (`pages/upgrade/upgrade-ticket-markdown.ts`), no behavior change.
  - ✅ Kész: ticket markdown paraméter-összeszedés deduplikálva komponens helperrel (`ticketMarkdownArgs`), no behavior change.
  - ✅ Kész: HTTP wrapper cleanup (közös server time applier + unused metódusok eltávolítva), no behavior change.
  - ✅ Kész: "Copy triage snapshot" gomb (egyben copy: debug header + server build + trace header + linkek), no behavior change a flow-kban.
  - ✅ Kész: Debug bundle preview UX: külön blokkokban jelenik meg a debug header / server build / trace header / curl snippets, no behavior change.
  - ✅ Kész: queue lista highlightolja a kiválasztott run-t + jelzi, ha manuálisan van kiválasztva, no behavior change.
  - ✅ Kész: recent dropdown jelöli a kiválasztott run-t (▶ prefix), no behavior change.
  - ✅ Kész: triage snapshot linkek bővítve audit list/export URL-ekkel (aktuális filterek + traceId-s variáns), no behavior change.
  - ✅ Kész: triage snapshot text builder kiszervezve helperbe (`pages/upgrade/upgrade-triage-snapshot.ts`), no behavior change.
  - ✅ Kész: audit list/export URL build deduplikálva helperrel (`pages/upgrade/upgrade-audit-urls.ts`), no behavior change.
  - ✅ Kész: komponens audit filter összeszedés deduplikálva (`auditUrlFilters()`), no behavior change.
  - ✅ Kész: debug pack audit preview request/filter objektumok deduplikálva (`toNullableAuditUrlFilters`), no behavior change.
  - ✅ Kész: debug pack audit filter snapshot is a közös `auditFiltersNullable`-t használja (dupla számolás kiváltva), no behavior change.
  - ✅ Kész: debug pack builder logika kiszervezve helperbe (`pages/upgrade/upgrade-debug-pack-builder.ts`), no behavior change.
  - ✅ Kész: debug pack export (copy/download + supersede seq + large confirm + state) wiring kiszervezve helperbe (`pages/upgrade/upgrade-debug-pack-export-runner.ts`), no behavior change.
  - ✅ Kész: incident bundle builder logika kiszervezve helperbe (`pages/upgrade/upgrade-incident-bundle-builder.ts`), no behavior change.
  - ✅ Kész: incident bundle export (copy/download + large confirm + state) wiring kiszervezve helperbe (`pages/upgrade/upgrade-incident-bundle-export-runner.ts`), no behavior change.
  - ✅ Kész: ticket markdown download (large confirm + fileName + download + status) wiring kiszervezve helperbe (`pages/upgrade/upgrade-ticket-markdown-download-runner.ts`), no behavior change.
  - ✅ Kész: ticket markdown copy (build+copy) wiring kiszervezve helperbe (`pages/upgrade/upgrade-ticket-markdown-copy-runner.ts`), no behavior change.
  - ✅ Kész: curl snippets download (large confirm + fileName + download + status) wiring kiszervezve helperbe (`pages/upgrade/upgrade-curl-snippets-download-runner.ts`), no behavior change.
  - ✅ Kész: curl snippets copy (build+copy) wiring kiszervezve helperbe (`pages/upgrade/upgrade-curl-snippets-copy-runner.ts`), no behavior change.
  - ✅ Kész: simple copy wiring (runId/traceId/trace header/server headers/triage snapshot) kiszervezve helperbe (`pages/upgrade/upgrade-simple-copy-runner.ts`), no behavior change.
  - ✅ Kész: clipboard helper (copyText fallback + status) kiszervezve helperbe (`pages/upgrade/upgrade-clipboard.ts`), no behavior change.
  - ✅ Kész: HTTP observable→Promise wrapper helper kiszervezve (`pages/upgrade/upgrade-http-promises.ts`), és a komponensből több `new Promise(...)` wrapper kiváltva, no behavior change.
  - ✅ Kész: queue/observability load wiring (loading/error/lastRefreshed/serverBuild capture/autoselect) kiszervezve runner helperbe (`pages/upgrade/upgrade-queue-load-runner.ts`, `pages/upgrade/upgrade-observability-load-runner.ts`), no behavior change.
  - ✅ Kész: status/recent runs load wiring (loading/error/serverBuild capture) kiszervezve runner helperbe (`pages/upgrade/upgrade-status-load-runner.ts`, `pages/upgrade/upgrade-recent-runs-load-runner.ts`), no behavior change.
  - ✅ Kész: loadLatest/loadRun polling wiring (silent/fromPoll, pollInFlight, terminal transition refresh) kiszervezve runner helperbe (`pages/upgrade/upgrade-run-load-runner.ts`), no behavior change.
  - ✅ Kész: run actions wiring (start/retry/cancel/devFailStep) kiszervezve runner helperbe (`pages/upgrade/upgrade-run-actions-runner.ts`), no behavior change.
  - ✅ Kész: dev set installation wiring (loading/error + POST + refresh) kiszervezve runner helperbe (`pages/upgrade/upgrade-dev-set-installation-runner.ts`), no behavior change.
  - ✅ Kész: audit filter preset/action wiring (apply filters + only upgrades + only current run) kiszervezve helperbe (`pages/upgrade/upgrade-audit-filter-actions.ts`), no behavior change.
  - ✅ Kész: audit paging actions wiring (prev/next page) kiszervezve helperbe (`pages/upgrade/upgrade-audit-paging-actions.ts`), no behavior change.
  - ✅ Kész: audit panel actions (clear filters + copy page JSON) wiring kiszervezve helperbe (`pages/upgrade/upgrade-audit-panel-actions.ts`), no behavior change.
  - ✅ Kész: audit URL copy wiring (copy absolute URL + copy list/export URL) kiszervezve helperbe (`pages/upgrade/upgrade-url-copy-actions.ts`), no behavior change.
  - ✅ Kész: audit details copy wiring (copy traceId/details/curl by traceId) kiszervezve helperbe (`pages/upgrade/upgrade-audit-copy-actions.ts`), no behavior change.
  - ✅ Kész: duration számolók (queue/recent/run/step) wiring kiszervezve helperbe (`pages/upgrade/upgrade-duration-actions.ts`), no behavior change.
  - ✅ Kész: recent UI wiring (recentLabel + selectedRunRowStyle) kiszervezve helperbe (`pages/upgrade/upgrade-recent-ui-actions.ts`), no behavior change.
  - ✅ Kész: preview text wiring (server build / trace header / curl snippets preview + copy ticket header) kiszervezve helperbe (`pages/upgrade/upgrade-preview-text-actions.ts`), no behavior change.
  - ✅ Kész: small wiring wrapper-ek (copy/download full audit JSON + curl snippets copy/download delegálás) kiszervezve helperbe (`pages/upgrade/upgrade-small-actions.ts`), no behavior change.
  - ✅ Kész: misc small handlers (onAuditFilterChanged + incident bundle/debug pack delegáló metódusok) kiszervezve helperbe (`pages/upgrade/upgrade-misc-actions.ts`), no behavior change.
  - ✅ Kész: refreshAll wiring kiszervezve helperbe (`pages/upgrade/upgrade-refresh-actions.ts`), no behavior change.
  - ✅ Kész: polling UI handlers (toggle/enable polling) kiszervezve helperbe (`pages/upgrade/upgrade-polling-ui-actions.ts`), no behavior change.
  - ✅ Kész: ticket markdown copy/download wiring kiszervezve helperbe (`pages/upgrade/upgrade-ticket-markdown-actions.ts`), no behavior change.
  - ✅ Kész: client trace id (generate/clear) wiring kiszervezve helperbe (`pages/upgrade/upgrade-client-trace-actions.ts`), no behavior change.
  - ✅ Kész: run id manuális kiválasztás wiring kiszervezve helperbe (`pages/upgrade/upgrade-run-selection-actions.ts`), no behavior change.
  - ✅ Kész: auto-select runId helper (pickActiveRunIdFromCandidates) kiszervezve helperbe (`pages/upgrade/upgrade-run-autoselect-actions.ts`), no behavior change.
  - ✅ Kész: auto-select active run wiring (tryAutoSelectActiveRun) kiszervezve helperbe (`pages/upgrade/upgrade-run-autoselect-actions.ts`), no behavior change.
  - ✅ Kész: run state pure helpers (isActiveRunState/isCancelable/hasAnyActiveRun/hasOtherActiveRun) kiszervezve helperbe (`pages/upgrade/upgrade-run-state-actions.ts`), no behavior change.
  - ✅ Kész: drift-proof time actions (clientNowMs/applyServerTimeUtc/offsetMinutesAbs) kiszervezve helperbe (`pages/upgrade/upgrade-time-drift-actions.ts`), no behavior change.
  - ✅ Kész: drift simulation handler (setSimulatedClientDriftMinutes) kiszervezve helperbe (`pages/upgrade/upgrade-drift-simulation-actions.ts`), no behavior change.
  - ✅ Kész: enforcement+drift apró pure helper-ek (clockDriftWarning + isHardBlocked/isSoftBlocked + serverTimeApplier + simulatedClientDriftMs) kiszervezve helperbe (`pages/upgrade/upgrade-clock-drift-warning-actions.ts`, `pages/upgrade/upgrade-enforcement-state-actions.ts`, `pages/upgrade/upgrade-server-time-applier-actions.ts`), no behavior change.
  - ✅ Kész: isRefreshingAll aggregate getter kiszervezve helperbe (`pages/upgrade/upgrade-loading-aggregate-actions.ts`), no behavior change.
  - ✅ Kész: dev preset wiring + toNumber helper kiszervezve helperbe (`pages/upgrade/upgrade-dev-preset-actions.ts`, `pages/upgrade/upgrade-number-actions.ts`), no behavior change.
  - ✅ Kész: triage snapshot + curl snippets text builder wiring kiszervezve helperbe (`pages/upgrade/upgrade-triage-snapshot-actions.ts`, `pages/upgrade/upgrade-curl-snippets-actions.ts`), no behavior change.
  - ✅ Kész: action-state reset wiring (run/export/reset all) kiszervezve helperbe (`pages/upgrade/upgrade-action-reset-actions.ts`), no behavior change.
  - ✅ Kész: enforcement block error mapping (details extract + formatting) kiszervezve helperbe (`pages/upgrade/upgrade-enforcement-block-error-actions.ts`), no behavior change.
  - ✅ Kész: ticket header unified text builder kiszervezve helperbe (`pages/upgrade/upgrade-ticket-header-unified-actions.ts`), no behavior change.
  - ✅ Kész: short ticket header text + copy wiring kiszervezve helperbe (`pages/upgrade/upgrade-short-ticket-header-actions.ts`), no behavior change.
  - ✅ Kész: audit export text builder + error mapping kiszervezve helperbe (`pages/upgrade/upgrade-audit-export-actions.ts`), no behavior change.
  - ✅ Kész: ticket markdown args builder kiszervezve helperbe (`pages/upgrade/upgrade-ticket-markdown-args-actions.ts`), no behavior change.
  - ✅ Kész: ticket header preview wiring kiszervezve helperbe (`pages/upgrade/upgrade-ticket-header-preview-actions.ts`), no behavior change.
  - ✅ Kész: ticket markdown text builder wiring kiszervezve helperbe (`pages/upgrade/upgrade-ticket-markdown-text-actions.ts`), no behavior change.
  - ✅ Kész: export file name wiring (audit export + debug pack) kiszervezve helperbe (`pages/upgrade/upgrade-export-file-name-actions.ts`), no behavior change.
  - ✅ Kész: upgrade page mini wiring util delegálók (requestOptions/requestOptionsWithResponse/clientTraceHeaderArg/toAbsoluteUrl/formatError) konszolidálva helperbe (`pages/upgrade/upgrade-component-wiring-utils-actions.ts`), no behavior change.
  - ✅ Kész: time utils mini wiring delegálók (parseDateUtc/serverNowMs/serverNowDate) konszolidálva helperbe (`pages/upgrade/upgrade-component-wiring-utils-actions.ts`), no behavior change.
  - ✅ Kész: audit export run (copy/download) wiring kiszervezve helperbe (`pages/upgrade/upgrade-audit-export-runner.ts`), no behavior change.
  - ✅ Kész: audit load (seq/watchdog/timeout/state) wiring kiszervezve helperbe (`pages/upgrade/upgrade-audit-load-runner.ts`), no behavior change.

## Rövid smoke checklist (ha valami furcsaság van)
- **Swagger**
  - Schemas/Models alatt minden admin DTO-ban látszik-e `serverTimeUtc` + `items`.
- **UI drift-proof time**
  - Diagnosztika blokk frissül-e minden művelet után.
  - +5 perc drift esetén megjelenik-e a drift warning.
- **Incident bundle**
  - `Copy incident bundle` kimenetben van-e `timeDiagnostics` + `curlSnippets` + `serverTimeUtc` mezők.

## Nyitott kérdések
- Később kell-e incident bundle-be még:
  - további extra kontextus / export formátum
  - ticket sablon standardizálás: mi legyen a “Copy ticket (Markdown)” minimum mezőkészlete, és kell-e több preset (csapatonként eltérő sablon)
