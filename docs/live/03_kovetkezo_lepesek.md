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
- Következő (arch/infra): home-lab “cloud-like” telepítés előkészítése Raspberry Pi 5-re.
  - k3s single-node + Helm (GitOps-lite; később opcionális ArgoCD/Flux)
  - multi-arch container image build (linux/arm64 elsődlegesen)
  - upgrade stratégia: immutable image tag + Helm upgrade/rollback; Upgrade UI monitor/diagnosztika szerepben
  - minimal ops baseline: ingress + (szükség esetén) TLS + DB backup (CronJob) + alap monitoring/log
- Következő (backend): DB-agnosztikus irány enterprise felé.
  - dev környezetben jelenleg: SQLite (ha így van bekötve)
  - cél provider: PostgreSQL + SQL Server
  - megoldás: provider-specifikus EF Core migrations (külön migrations assembly Postgres/SQL Server)
- Refaktor (WIP): upgrade-page további bontása kisebb helper/service egységekre (no behavior change).
  - ✅ Kész: type definíciók kiszervezése (`pages/upgrade/upgrade-types.ts`).
  - ✅ Kész: export/bundle builder-ek első szelete kiszervezve (`pages/upgrade/upgrade-export-builders.ts`).
  - ✅ Kész: HTTP helper-ek kiszervezve (`pages/upgrade/upgrade-api.ts`).
  - ✅ Kész: incident bundle builder kiszervezve (`pages/upgrade/upgrade-incident-bundle.ts`).
  - ✅ Kész (PR): komponens `copyIncidentBundle()` delegálás a shared helperre (`buildIncidentBundleImpl`) – PR: `refactor/upgrade-incident-bundle-wiring`.
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
---

# Iterációs roadmap (látható haladás)

## Cél
Az iterációk célja, hogy **minden kör végén kipróbálható (demo-zható) funkció** legyen, miközben a platform “enterprise baseline” (hibakezelés, trace, tenancy, health, security) **folyamatosan erősödik**.

## Működési mód
- **WIP=1**: egyszerre egy aktív iteráció.
- Minden iteráció végén:
  - **zöld tesztek**
  - **demo lépések** (curl / Swagger / UI)
  - **DoD checklist** kipipálva

## Frontend stratégia (low-code demó, builder nélkül)
- Frontendet építünk már most (auth + tenant switch + workflow CRUD + run monitor + entity defs).
- Vizuális workflow: először read-only **viewer**.
- Drag&drop szerkesztő csak később, amikor a step modell + validációk stabilak (elkerülendő a korai UI lock-in-t).

## Guardrail: párhuzamos munkaszálak (WIP=1 megtartása)
- Az ops/admin jellegű **upgrade-page** karbantartás külön “track”, jelenleg **parkoltatva**.
- Csak bugfix/kompatibilitási javítás mehet bele, amíg a fő fókusz a low-code frontend demó.

## Credit-aware iteráció sizing (költséghatékony fejlesztés)
- A fejlesztési mód figyelembe veszi, hogy **1 kérdés–válasz = 1 credit**.
- Ennek megfelelően az iterációk célja, hogy **egy körben** (minél kevesebb chat-fordulóval) **a lehető legtöbb, még biztonságosan vállalható** feladat elkészüljön.
- Biztonsági korlátok (ne csomagoljuk össze ugyanabba a körbe):
  - **auth/security** változtatás + **adatmodell törő** változás + **nagy refaktor** egyszerre
  - több „ismeretlen kockázatú” függőség (külső IdP, Kubernetes, storage) egy lépésben
- Vállalási szabály (enterprise): csak olyan csomagot teszünk egy iterációba, ami a végén:
  - **futtatható**
  - **zöld tesztekkel** zár
  - **demo-lépésekkel** dokumentált
  - és ha közben kockázat nő, az iteráció **szétvágható** (de alapértelmezés a batching).

## Enterprise baseline (folyamatos)
- Standard JSON error response ([ErrorResponse](cci:2://file:///home/zoli/lowcode-platform/backend/Contracts/ErrorResponse.cs:4:0-10:2)) + globális exception middleware.
- Trace ID propagáció (`X-Trace-Id`) + log scope.
- K8s kompatibilis health endpointok (`/health/live`, `/health/ready`).
- Multi-tenant alapok (tenant feloldás + per-tenant DB + migrations).
- Admin endpoint védelem (minimum: API key), később RBAC.

## Iterációk

### Iteráció 0 — Platform alapok (kész)
**Deliverables**
- Egységes hibakezelés + traceId.
- Health endpointok.
- Tenant DB migrációk + admin migráció endpoint.
- Tenant DB secretRef feloldás (dev config resolver).
- Admin API key védelem + access log.
- Tenant provisioning endpoint (create tenant + azonnali migráció).

**Demo**
- `GET /health/live`
- `GET /health/ready`
- `POST /api/admin/tenants` (tenant provisioning + azonnali migráció)
- `POST /api/admin/tenants/migrate`

### Iteráció 1 — “Workflow minimum” (látható low-code első szelet)
**Cél**: legyen minimális workflow CRUD, hogy már “van mit építeni” érzés legyen.

**Deliverables (backend)**
- `WorkflowDefinition` entitás + migrations (tenant DB-ben).
- API:
  - `POST /api/workflows` (create)
  - `GET /api/workflows` (list)
  - `GET /api/workflows/{id}` (details)
  - `PUT /api/workflows/{id}` (update)
  - `DELETE /api/workflows/{id}` (delete)

**Definition of Done**
- Input validáció + standard error response.
- Audit log: create/update/delete események.
- Multi-tenant izoláció (minden tenant a saját DB-jében).
- Legalább 2-3 integrációs teszt (CRUD happy path + tenant izoláció).

**Demo**
- ✅ Kész: backend CRUD + migrations + integrációs tesztek.
- Swaggerből vagy curl-lel: workflow létrehozás, listázás, update.

```bash
# Tenant provisioning (csak egyszer / ha még nincs):
curl -sS -X POST http://localhost:5000/api/admin/tenants \
  -H 'Content-Type: application/json' \
  -H 'X-Admin-Api-Key: <ADMIN_API_KEY>' \
  -d '{"slug":"t1","connectionStringSecretRef":"t1"}'

# Tenant secret mapping (dev): appsettings.Development.json
# Tenancy:Secrets:t1 = "Data Source=tenant-t1.db"

# Workflow create (tenant host alapján):
curl -sS -X POST http://t1.localhost:5000/api/workflows \
  -H 'Content-Type: application/json' \
  -d '{"name":"wf1","definitionJson":"{\\"steps\\":[]}"}'

# Workflow list:
curl -sS http://t1.localhost:5000/api/workflows

# Workflow get:
curl -sS http://t1.localhost:5000/api/workflows/<WORKFLOW_ID>

# Workflow update:
curl -sS -X PUT http://t1.localhost:5000/api/workflows/<WORKFLOW_ID> \
  -H 'Content-Type: application/json' \
  -d '{"name":"wf1-updated","definitionJson":"{\\"steps\\":[{\\"type\\":\\"noop\\"}]}"}'

# Workflow delete:
curl -sS -X DELETE http://t1.localhost:5000/api/workflows/<WORKFLOW_ID>
```

### Iteráció 19 — “Run details trace timeline + deep-link”
**Cél**: a futás részleteinek (run details) valódi trace nézetté alakítása: lépések szűrése, config megjelenítés, gyors navigáció.

**Backend**
- Run details DTO bővült: `WorkflowStepRunDto.StepConfigJson` visszaadása.

**Frontend**
- Run details oldal (`/lowcode/runs/:runId`):
  - step lista szűrhető `state` / `type` / `search` alapján
  - state színezés
  - step config megnyitható (read-only textarea, pretty-print JSON)
- Workflow details `Runs` tab:
  - `Open latest` link a legfrissebb runra

**Demo (frontend)**

```bash
# Backend:
dotnet run --project backend/LowCodePlatform.Backend.csproj

# Frontend:
npm start --prefix frontend

# UI flow:
# 1) http://localhost:4200/lowcode/workflows -> Open egy workflowt
# 2) Runs tab -> Open latest
# 3) Run details -> filter + Config kibontás
```

### Iteráció 20 — “Domain commands v2: entityRecord.updateById”
**Cél**: domainCommand képességek bővítése egy alap (de hasznos) módosító művelettel: meglévő entity record frissítése azonosító alapján.

**Backend**
- Új domain command: `entityRecord.updateById`
  - Kötelező: `recordId` (GUID string)
  - Opcionális: `data` (JSON object vagy string; ha nincs megadva, `{}` lesz)
  - Művelet: a rekord `DataJson` mezője felülíródik, `UpdatedAtUtc` frissül
- Új hibakódok:
  - `entity_record_id_missing`
  - `entity_record_id_invalid`
  - `entity_record_not_found`
  - `entity_record_data_invalid`
- Új integrációs teszt: create + update flow

**Frontend**
- Új “executable template” a workflow create oldalon:
  - `Domain: update record` (a `recordId` helyére GUID-ot kell írni)

**Példa definition JSON**

```json
{
  "steps": [
    {
      "type": "domainCommand",
      "command": "entityRecord.updateById",
      "recordId": "<RECORD_ID_GUID>",
      "data": {
        "name": "Acme Updated",
        "status": "inactive"
      }
    }
  ]
}
```

### Iteráció 21 — “Domain commands v3: step outputs (OutputJson) + Run details Output panel”
**Cél**: a workflow step-eknek legyen “kimenete”, amit eltárolunk és UI-n is látszik, illetve később context-ként felhasználható.

**Backend**
- Új mező a step run-on: `WorkflowStepRun.OutputJson` (DB-ben: `workflow_step_run.output_json`).
- Run details DTO bővült: `WorkflowStepRunDto.outputJson`.
- Domain command outputok:
  - `entityRecord.createByEntityName` → `{"entityDefinitionId":"...","entityRecordId":"..."}`
  - `entityRecord.updateById` → `{"entityRecordId":"..."}`

**Frontend**
- Run details oldalon step-enként kibontás: **Output** panel (read-only JSON megjelenítés).

**Megjegyzés**
- Ez az alapja annak, hogy később “context variables” legyenek a workflow-ban (pl. guard step-ekhez).

### Iteráció 22 — “Guards: require step (fail fast) + context outputs”
**Cél**: egyszerű feltételek/guardok bevezetése a workflow-ba úgy, hogy egy lépés tudjon fail fast-olni a korábbi step outputok alapján.

**Backend**
- Új step típus: `require`
  - Kötelező: `path` (pl. `000.entityRecordId`)
  - Opcionális: `equals` (string)
- A runner a sikeres step-ek `OutputJson`-jait step-key szerint contextbe rakja, és a `require` ebből olvas.
- Hibakódok:
  - `require_config_missing`
  - `require_path_missing`
  - `require_equals_invalid`
  - `require_failed`

**Példa definition JSON**

```json
{
  "steps": [
    {
      "type": "domainCommand",
      "command": "entityRecord.createByEntityName",
      "entityName": "Company",
      "data": { "name": "Acme Ltd", "status": "active" }
    },
    {
      "type": "require",
      "path": "000.entityRecordId"
    },
    { "type": "noop" }
  ]
}
```

**Frontend**
- Új executable template: `Require (guard)`.

### Iteráció 23 — “Domain command: entityRecord.upsertByEntityName”
**Cél**: “create or update” egyetlen parancsból, entityName + unique kulcs alapján.

**Backend**
- Új domain command: `entityRecord.upsertByEntityName`
  - Kötelező: `entityName` (string)
  - Kötelező: `uniqueKey` (string)
  - Kötelező: `uniqueValue` (string) *(vagy alternatívaként `data[uniqueKey]`)*
  - Opcionális: `data` (object vagy string)
- Output:
  - `action`: `created` vagy `updated`
  - `entityDefinitionId`, `entityRecordId`

**Példa definition JSON**

```json
{
  "steps": [
    {
      "type": "domainCommand",
      "command": "entityRecord.upsertByEntityName",
      "entityName": "Company",
      "uniqueKey": "externalId",
      "uniqueValue": "c-1",
      "data": {
        "externalId": "c-1",
        "name": "Acme Upsert",
        "status": "active"
      }
    }
  ]
}
```

**Frontend**
- Új executable template: `Domain: upsert record`.

### Iteráció 18 — “domainCommand step scaffold (echo + entityRecord.createByEntityName)”
**Cél**: új workflow step típus, ami domain parancsokat hív. Ez a híd a későbbi modulok felé (DDD jellegű parancsok).

**Backend**
- Új step típus: `domainCommand`
- `command` támogatás:
  - `echo` (demó; no-op)
  - `entityRecord.createByEntityName` (entity name alapján entity record létrehozása)

**Példa definition JSON**

```json
{
  "steps": [
    {
      "type": "domainCommand",
      "command": "entityRecord.createByEntityName",
      "entityName": "Company",
      "data": { "name": "Acme Ltd", "status": "active" }
    }
  ]
}
```

**Frontend**
- Új workflow template-ek a `/lowcode/workflows/new` oldalon:
  - `Domain: echo`
  - `Domain: create record`

**Demo (end-to-end)**

```bash
# Backend:
dotnet run --project backend/LowCodePlatform.Backend.csproj

# Frontend:
npm start --prefix frontend

# UI flow:
# 1) http://localhost:4200/lowcode/auth
# 2) http://localhost:4200/lowcode/workflows/new -> Domain: create record -> Create
# 3) Workflow details -> Start run -> Runs tabon succeeded
# 4) http://localhost:4200/lowcode/entities -> megjelenik a Company entity (ha korábban nem volt)
# 5) Company -> Records -> látszik a létrejött record
```

### Iteráció 17 — “Run trace UX + executable workflow templates”
**Cél**: futások áttekintése (trace, error) és kényelmes demó workflowk egy kattintásból.

**Deliverables**
- Frontend:
  - Workflow details `Runs` tab bővítés:
    - traceId + error mezők megjelenítése
    - futás állapot színezése
    - polling amíg van `running` run (2s)
    - `Start run` után automatikus átállás `Runs` tabra
  - Workflow létrehozásnál “Templates (executable)” gombok (`noop`, `delay`).

**Demo (frontend)**

```bash
# Backend:
dotnet run --project backend/LowCodePlatform.Backend.csproj

# Frontend:
npm start --prefix frontend

# UI flow:
# 1) http://localhost:4200/lowcode/auth  (tenant + token)
# 2) http://localhost:4200/lowcode/workflows/new
#    - válassz egy template-et (pl. Delay 250ms)
#    - Create
# 3) Workflow details -> Start run
# 4) Automatikusan átvált Runs tabra
#    - traceId + error látszik
#    - futás alatt polling (2s)
```

### Iteráció 16 — “Entity records dedicated page”
**Cél**: runtime entity records külön oldalon (áttekinthető lista + create/update/delete), nem csak az entity details alján.

**Deliverables**
- Frontend:
  - `GET /lowcode/entities/:id/records` oldal records listával és JSON szerkesztéssel.
  - Link az entity details oldalról a records oldalra.
- Backend: nincs új endpoint (a meglévő `/api/entities/{entityId}/records` CRUD-ra épít).

**Demo (frontend)**

```bash
# Backend:
dotnet run --project backend/LowCodePlatform.Backend.csproj

# Frontend:
npm start --prefix frontend

# UI flow:
# 1) http://localhost:4200/lowcode/auth  (tenant + token)
# 2) http://localhost:4200/lowcode/entities -> Open
# 3) Entity details -> Records link
# 4) Create record + edit/save/delete
```

### Iteráció 15 — “Tenant UX v1 (Admin / Tenants)”
**Cél**: demózható tenant provisioning és tenant adatbázis migráció UI-ból.

**Deliverables**
- Backend:
  - `GET /api/admin/tenants` (lista)
  - `POST /api/admin/tenants` (create + migrate)
  - `POST /api/admin/tenants/migrate` (migrate all)
- Frontend:
  - `Admin / Tenants` oldal: lista + create + migrate.

**Demo (frontend)**

```bash
# Backend:
dotnet run --project backend/LowCodePlatform.Backend.csproj

# Frontend:
npm start --prefix frontend

# UI flow:
# 1) http://localhost:4200/lowcode/auth
#    - Roles: admin
#    - Mint dev token
# 2) http://localhost:4200/lowcode/admin/tenants
#    - Refresh
#    - Create tenant (slug + secretRef vagy connectionString)
#    - Migrate all tenant DBs
```

### Iteráció 14 — “Workflow run history UI”
**Cél**: workflow details oldalon látszódjanak a korábbi futások (run history), és egy kattintással megnyitható legyen a run details.

**Deliverables**
- Backend: `GET /api/workflows/{workflowDefinitionId}/runs` (lista).
- Frontend: Workflow details oldalon `Runs` tab (lista + link `run details` oldalra).

**Demo (frontend)**

```bash
# Backend:
dotnet run --project backend/LowCodePlatform.Backend.csproj

# Frontend:
npm start --prefix frontend

# UI flow:
# 1) http://localhost:4200/lowcode/auth  (tenant + token)
# 2) http://localhost:4200/lowcode/workflows -> Open
# 3) Start run (hogy legyen futás)
# 4) Workflow details -> Runs tab
# 5) Open egy run-t -> run details
```

### Iteráció 13 — “Runtime entity records (Vendor instances)”
**Cél**: ne csak definíciókat lehessen kezelni, hanem tényleges entitás példányokat (adat rekordok) is.

**Deliverables**
- Backend: `EntityRecord` tárolás + CRUD endpointok `DataJson` payload-dal.
- Frontend: Entity details oldalon records lista + JSON szerkesztés + create/update/delete.

**Demo (frontend)**

```bash
# Backend:
dotnet run --project backend/LowCodePlatform.Backend.csproj

# Frontend:
npm start --prefix frontend

# UI flow:
# 1) http://localhost:4200/lowcode/auth  (tenant + token)
# 2) http://localhost:4200/lowcode/entities -> Open (pl. Vendor)
# 3) Records szekció:
#    - New record JSON -> Add record
#    - record sorban JSON edit -> Save
#    - Delete
```

### Iteráció 2 — “Workflow futtatás minimum”
**Cél**: egy workflow példány futtatható legyen (engine skeleton), hogy látszódjon a runtime.

**Deliverables**
- `WorkflowRun` + `WorkflowStepRun` táblák.
- API:
  - `POST /api/workflows/{id}/runs` (start)
  - `GET /api/workflows/runs/{runId}` (status)
- Minimális engine: 1–2 step típus (pl. `noop`, `delay`, `http-request` stub).

**DoD**
- Idempotencia (pl. client request id opcionális) vagy legalább “dupla start” kezelés.
- Observability: run state lekérdezhető, auditált.

**Demo**
- ✅ Kész: run táblák + start/status endpointok + minimál engine (`noop`, `delay`) + integrációs teszt.

```bash
# Workflow create (példa definíció: noop + delay):
WF_ID=$(curl -sS -X POST http://t1.localhost:5000/api/workflows \
  -H 'Content-Type: application/json' \
  -d '{"name":"wf-run-demo","definitionJson":"{\\"steps\\":[{\\"type\\":\\"noop\\"},{\\"type\\":\\"delay\\",\\"ms\\":50}]}"}' \
  | sed -n 's/.*"workflowDefinitionId"\s*:\s*"\([^"]*\)".*/\1/p')

echo "WF_ID=$WF_ID"

# Run start:
RUN_ID=$(curl -sS -X POST http://t1.localhost:5000/api/workflows/$WF_ID/runs \
  | sed -n 's/.*"workflowRunId"\s*:\s*"\([^"]*\)".*/\1/p')

echo "RUN_ID=$RUN_ID"

# Run status/details:
curl -sS http://t1.localhost:5000/api/workflows/runs/$RUN_ID
```

### Iteráció 3 — “Data model minimum”
**Cél**: per-tenant adatmodell definíció és első CRUD API generálás alapja.

**Deliverables**
- `EntityDefinition`/`FieldDefinition` (tenant DB-ben) + minimál CRUD.
- Validációs szabályok (field name, type, required, maxLength).

**Demo**
- ✅ Kész: entitás + mező definíciók tenant DB-ben (migrations) + CRUD API + integrációs tesztek + tenant izoláció.

```bash
# Entity create:
ENTITY_ID=$(curl -sS -X POST http://t1.localhost:5000/api/entities \
  -H 'Content-Type: application/json' \
  -d '{"name":"Customer"}' \
  | sed -n 's/.*"entityDefinitionId"\s*:\s*"\([^"]*\)".*/\1/p')

echo "ENTITY_ID=$ENTITY_ID"

# Entity list:
curl -sS http://t1.localhost:5000/api/entities

# Entity get:
curl -sS http://t1.localhost:5000/api/entities/$ENTITY_ID

# Field create:
FIELD_ID=$(curl -sS -X POST http://t1.localhost:5000/api/entities/$ENTITY_ID/fields \
  -H 'Content-Type: application/json' \
  -d '{"name":"email","fieldType":"string","isRequired":true,"maxLength":320}' \
  | sed -n 's/.*"fieldDefinitionId"\s*:\s*"\([^"]*\)".*/\1/p')

echo "FIELD_ID=$FIELD_ID"

# Field update:
curl -sS -X PUT http://t1.localhost:5000/api/entities/$ENTITY_ID/fields/$FIELD_ID \
  -H 'Content-Type: application/json' \
  -d '{"name":"email","fieldType":"string","isRequired":true,"maxLength":512}'

# Field delete:
curl -sS -X DELETE http://t1.localhost:5000/api/entities/$ENTITY_ID/fields/$FIELD_ID

# Entity delete:
curl -sS -X DELETE http://t1.localhost:5000/api/entities/$ENTITY_ID
```

### Iteráció 4 — “Auth/RBAC baseline”
**Cél**: admin + runtime végpontokhoz jogosultsági alapok.

**Deliverables**
- API key helyett (vagy mellé) JWT skeleton.
- Role-based guard (admin vs. tenant user).

**Demo**
- ✅ Kész: JWT bearer auth + `tenant_user` policy a tenant runtime endpointokra.
- ✅ Kész: Dev/Testing token-mint endpoint (`POST /api/auth/dev-token`) demohoz + integrációs tesztekhez.

```bash
# Dev/Testing token mint (12h, csak dev/testing):
TOKEN=$(curl -sS -X POST http://localhost:5000/api/auth/dev-token \
  -H 'Content-Type: application/json' \
  -d '{"subject":"demo-user","tenantSlug":"t1","roles":[]}' \
  | sed -n 's/.*"accessToken"\s*:\s*"\([^"]*\)".*/\1/p')

echo "TOKEN=${TOKEN:0:20}..."

# Authenticated call (példa: workflow list):
curl -sS http://t1.localhost:5000/api/workflows \
  -H "Authorization: Bearer $TOKEN"

# Entity create (auth required):
curl -sS -X POST http://t1.localhost:5000/api/entities \
  -H 'Content-Type: application/json' \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"name":"Customer"}'
```

### Iteráció 5 — “Auth hardening + tenant izoláció”
**Cél**: multi-tenant biztonság erősítése: token ne legyen újrahasznosítható más tenant ellen, és admin auth legyen kompatibilis átmenettel.

**Deliverables**
- Tenant claim enforcement: ha a token tartalmaz `tenant` claim-et, akkor egyeznie kell a feloldott tenanttal (különben 403).
- Admin auth hardening: `/api/admin/*` elérhető **JWT `admin` role**-lal vagy meglévő `X-Admin-Api-Key`-vel.
- Swagger: Bearer auth scheme a kényelmes manuális demóhoz.

**Demo**

```bash
# 1) Token t1 tenant claim-mel:
T1_TOKEN=$(curl -sS -X POST http://localhost:5000/api/auth/dev-token \
  -H 'Content-Type: application/json' \
  -d '{"subject":"demo-user","tenantSlug":"t1","roles":[]}' \
  | sed -n 's/.*"accessToken"\s*:\s*"\([^"]*\)".*/\1/p')

# 2) Ugyanazzal a tokennel t2 tenant ellen hívás => 403 tenant_mismatch:
curl -sS -i http://t2.localhost:5000/api/workflows \
  -H "Authorization: Bearer $T1_TOKEN"

# 3) Admin token (role=admin), amivel /api/admin/* mehet API key nélkül (dev/testing):
ADMIN_TOKEN=$(curl -sS -X POST http://localhost:5000/api/auth/dev-token \
  -H 'Content-Type: application/json' \
  -d '{"subject":"demo-admin","tenantSlug":"t1","roles":["admin"]}' \
  | sed -n 's/.*"accessToken"\s*:\s*"\([^"]*\)".*/\1/p')

# Példa admin endpoint (ha van ilyen):
curl -sS -i http://localhost:5000/api/admin/tenants \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

### Iteráció 6 — “Tenant claim required + admin policy”
**Cél**: runtime tenant végpontoknál a tenant scope legyen kötelező, az admin végpontok pedig RBAC policy alapján legyenek védve.

**Deliverables**
- `tenant_user` policy: `tenant` claim **kötelező** (tenant runtime API-k csak tenant-scoped tokenekkel hívhatók).
- `/api/admin/*` controllerek: `[Authorize(Policy = "admin")]`.

**Demo**

```bash
# 1) Token tenant claim nélkül => tenant runtime endpoint 403 (tenant_user policy miatt):
NO_TENANT_TOKEN=$(curl -sS -X POST http://localhost:5000/api/auth/dev-token \
  -H 'Content-Type: application/json' \
  -d '{"subject":"demo-user","tenantSlug":null,"roles":[]}' \
  | sed -n 's/.*"accessToken"\s*:\s*"\([^"]*\)".*/\1/p')

curl -sS -i http://t1.localhost:5000/api/workflows \
  -H "Authorization: Bearer $NO_TENANT_TOKEN"

# 2) Admin endpoint: admin role token kell (vagy X-Admin-Api-Key), pl. tenant provisioning:
ADMIN_TOKEN=$(curl -sS -X POST http://localhost:5000/api/auth/dev-token \
  -H 'Content-Type: application/json' \
  -d '{"subject":"demo-admin","tenantSlug":"t1","roles":["admin"]}' \
  | sed -n 's/.*"accessToken"\s*:\s*"\([^"]*\)".*/\1/p')

curl -sS -i http://localhost:5000/api/admin/tenants \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{"slug":"t3","connectionStringSecretRef":"t3"}'
```

### Iteráció 7 — “Admin fallback switch + Swagger tenancy UX”
**Cél**: admin auth kivezethető legyen API key-ről JWT-re, és a Swaggerben egyértelmű legyen a tenant routing.

**Deliverables**
- `Admin:AllowApiKeyFallback` config:
  - ha `false`, akkor `/api/admin/*` csak **admin JWT**-vel (API key fallback letiltva).
  - ha nincs megadva, akkor **Development/Testing** alatt engedett, más környezetben ajánlott tiltani.
- Swagger (Development): minden műveletnél megjelenik az opcionális `X-Tenant-Id` header, és leírás jelzi, hogy prodban host-based tenancy az ajánlott.

**Demo**

```bash
# 1) Swagger tenancy: nyisd meg tenant hosttal
xdg-open http://t1.localhost:5002/swagger

# 2) Admin API key fallback tiltása (példa env var):
export Admin__AllowApiKeyFallback=false

# 3) Admin tokennel hívás (role=admin):
ADMIN_TOKEN=$(curl -sS -X POST http://localhost:5002/api/auth/dev-token \
  -H 'Content-Type: application/json' \
  -d '{"subject":"demo-admin","tenantSlug":"t1","roles":["admin"]}' \
  | sed -n 's/.*"accessToken"\s*:\s*"\([^"]*\)".*/\1/p')

curl -sS -i http://localhost:5002/api/admin/tenants \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

### Iteráció 8 — “Low-code frontend shell”
**Cél**: legyen egy egyszerű, de stabil frontend váz, amin keresztül a low-code motor demózható.

**Deliverables**
- Navigáció (upgrade + low-code oldalak) + alap layout.
- Tenant választó (dev: `X-Tenant-Id` headerrel) + token tárolás.
- Dev-token UI (`/api/auth/dev-token`) + “Paste token” lehetőség.
- Workflows list UI (read-only list) a backend `GET /api/workflows` alapján.

**Demo (frontend)**

```bash
# 1) Backend indítás (külön terminál):
dotnet run --project backend/LowCodePlatform.Backend.csproj

# 2) Frontend indítás (külön terminál):
npm start --prefix frontend

# 3) Nyisd meg:
# - http://localhost:4200/lowcode/auth
# - add meg a tenant slugot (pl. t1)
# - Mint dev token (vagy Paste token)
#
# 4) Ezután a listák:
# - http://localhost:4200/lowcode/workflows
# - http://localhost:4200/lowcode/entities
```

### Iteráció 9 — “Workflows CRUD UI + JSON editor”
**Cél**: workflow létrehozás/szerkesztés JSON definícióval (vizuális builder nélkül).

**Deliverables**
- Workflows: create/update/delete UI.
- Details oldal + JSON editor + alap validáció/hibák.

**Demo (frontend)**

```bash
# Backend:
dotnet run --project backend/LowCodePlatform.Backend.csproj

# Frontend:
npm start --prefix frontend

# UI flow:
# 1) http://localhost:4200/lowcode/auth  (tenant + token)
# 2) http://localhost:4200/lowcode/workflows -> New
# 3) Add name + definition JSON -> Create
# 4) Workflow details oldalon:
#    - módosíts name/definitionJson -> Save
#    - Start run (Iteráció 10)
#    - Delete (törlés után visszadob a listára)
```

### Iteráció 10 — “Workflow runs UI (start + monitor)”
**Cél**: látszódjon a motor futása a frontendben.

**Deliverables**
- Run indítás workflow detailsből.
- Run details + step state lista + traceId.
- Polling: csak `pending/running` állapotban.

**Demo (frontend)**

```bash
# Backend:
dotnet run --project backend/LowCodePlatform.Backend.csproj

# Frontend:
npm start --prefix frontend

# UI flow:
# 1) http://localhost:4200/lowcode/auth  (tenant + token)
# 2) http://localhost:4200/lowcode/workflows  -> Open
# 3) Workflow details oldalon: Start run
# 4) Átirányít a run details oldalra (polling), ahol látszik:
#    - state
#    - traceId
#    - steps (pending/running/succeeded/failed)
```

### Iteráció 11 — “Entity definitions UI (Vendor domain slice)”
**Cél**: DDD-szerű slice demó: `Vendor` entitás és mezők kezelése.

**Deliverables**
- EntityDefinition + FieldDefinition CRUD UI.
- Demo entity: `Vendor` (name/taxNumber/riskScore/status).

**Demo (frontend)**

```bash
# Backend:
dotnet run --project backend/LowCodePlatform.Backend.csproj

# Frontend:
npm start --prefix frontend

# UI flow:
# 1) http://localhost:4200/lowcode/auth  (tenant + token)
# 2) http://localhost:4200/lowcode/entities -> New
# 3) Create: name=Vendor
# 4) Entity details oldalon add field-ek:
#    - name: string required maxLength 200
#    - taxNumber: string required maxLength 64
#    - riskScore: number optional
#    - status: string required maxLength 32
# 5) Field sorokban Save/Delete műveletek
```

### Iteráció 12 — “Workflow viewer (read-only vizualizáció)”
**Cél**: wow faktor vizuális megjelenítéssel, de még szerkesztés nélkül.

**Deliverables**
- Read-only workflow viewer (steps → graph/layout minimal).
- JSON és viewer nézet közti váltás.

**Demo (frontend)**

```bash
# Backend:
dotnet run --project backend/LowCodePlatform.Backend.csproj

# Frontend:
npm start --prefix frontend

# UI flow:
# 1) http://localhost:4200/lowcode/auth  (tenant + token)
# 2) http://localhost:4200/lowcode/workflows -> New (ha nincs még workflow)
# 3) Workflow details oldalon:
#    - Viewer: steps vizuális (kártyák + nyilak)
#    - JSON: nyers definitionJson szerkeszthető
```

---

## Következő aktív iteráció
- **Iteráció 8 — Low-code frontend shell**
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
