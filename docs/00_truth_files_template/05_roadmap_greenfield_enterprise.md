# Roadmap (greenfield) – Enterprise product-ready low-code platform

> **Enterprise megjegyzés:** A **aktuális** szállítási ütemtervek: `docs/live/roadmap-*.md` · központi index: `docs/README.md`.

## 0) Alap (mihez igazodik a roadmap)

Ez a roadmap a `docs/00_truth_files_template/` truth file-ok víziójára és döntéseire épül, nem a jelenlegi kódbázis állapotára.

Kiemelt kötöttségek:
- Holdingonként külön installation (BYOC) (D008).
- Nincs cross-holding riport/konszolidáció (D008).
- Támogatási modell: csak a kijelölt (aktuális) verzió supported (D009).
- Multi-tenant cél: holdingon belül sok projektcég/tenant (pl. 100), erős izoláció.
- DDD ERP core: GL/WMS (intercompany + multi-currency elsőrendű).
- Tervezett kiegészítők: AI-assisted builder/governance (D016) és marketplace (telepíthető csomagok) (D017).

## 1) Fázisok és mérföldkövek

A fázisok outcome-orientáltak. Mindegyikhez tartozik DoD (Definition of Done) és tipikus ADR (döntési) pontok.

### F0 – Product foundation (2–4 hét)

Cél:
- Közös nyelv, “minimális enterprise baseline” és a fejlesztési rendszer (CI, release alapok) rögzítése.

Szállítandó (DoD):
- Truth file-ok kitöltve és használati rend (WIP=1) elfogadva.
- Repo alapok:
  - monorepo vagy multi-repo döntés rögzítve
  - modulok/bounded context-ek skeleton
- CI baseline:
  - build + unit teszt fut a PR-ben
  - code formatting/lint minimum
- Security baseline:
  - secret kezelés (local dev + cloud)
  - dependency scanning minimum

ADR pontok (01_dontesek.md):
- Tech stack (backend/runtime/frontend/DB).
- Identity provider és authN stratégia (SSO/OIDC).

### F1 – Control plane + installation lifecycle (4–6 hét)

Cél:
- BYOC telepítési modell működjön: holdingonként külön installation, standard provisioning + upgrade folyamat.

Szállítandó (DoD):
- Installation registry (vendor oldali minimal control plane):
  - installation azonosító, release channel, supported version
  - semmilyen üzleti adat nem kerül vendorhoz
- Provisioning runbook + automatizmus:
  - új installation felhúzása (infra + app + DB)
  - health check és smoke
- Standard observability csomag:
  - log/trace/metrics alapok egységesen
  - trace-id végig a requesteken

ADR pontok:
- Control plane “home” helye (egy cloud vs több) és felelősségi határok.
- Telemetria policy (mit gyűjtünk, GDPR/PII).

### F2 – Tenant plane alapok: tenancy isolation + RBAC (4–8 hét)

Cél:
- Holdingon belül több tenant/project-company biztonságosan együtt éljen.

Szállítandó (DoD):
- Tenant registry installationen belül:
  - tenant provisioning (DB per tenant)
  - tenant routing determinisztikusan
- Hard guardrail:
  - tenant-context kötelező minden adat-hozzáférésnél
  - background job és cache tenant-safe
- RBAC:
  - builder-admin vs runtime szerepkörök
  - policy enforcement API + UI
- Audit alap:
  - “ki-mikor-mit” a definíció változtatásoknál

ADR pontok:
- Holdingon belüli tenant DB stratégia (DB-per-tenant vs pooled/sharded).
- RLS (row-level security) használata vagy kerülése.

### F3 – Definition platform: screens/workflows/datasources (6–10 hét)

Cél:
- Low-code core: definíciók CRUD + verziózás + runtime execution.

Szállítandó (DoD):
- Definition store:
  - screen/workflow/datasource definíciók mentése
  - verziózás (snapshot), diff, rollback
- Runtime:
  - workflow futtatás idempotencia alapokkal
  - datasource execution (query/command boundary)
- Builder UX minimum:
  - list/edit, “new flow” 404 noise nélkül
  - JSON schema validáció
  - publish/promote csak schema-validált definícióval (kompatibilitási szabályokkal)

ADR pontok:
- Definition schema formátum és kompatibilitási szabályok (semver / breaking change policy).
- Runtime sandboxing határai (mit futhat a workflow, mit nem).
- AI-assisted builder/governance pontos scope-ja és adatkezelési határok (D016).

### F4 – Release & upgrade rendszer (D009 miatt kritikus) (4–8 hét)

Cél:
- A “csak aktuális verzió supported” policy technikailag és operációsan kikényszeríthető legyen.

Szállítandó (DoD):
- Release pipeline:
  - canary -> wave rollout installation szinten
  - migráció orchestration (állapotgép)
- Supported version enforcement:
  - admin UI figyelmeztet + blokk (policy szerint)
  - API oldali enforcement kritikus műveleteknél
- Rollback stratégia:
  - mikor engedett, mikor tiltott
  - adat-migráció kompatibilitás rögzítve
- Runbook:
  - upgrade/rollback/restore drill leírva

ADR pontok:
- Release cadence és upgrade ablak.
- Backward compatibility policy.
- Marketplace csomag kompatibilitás: platform verzió pinning és csomag dependency szabályok (D017).

### F5 – Customization & schema governance (6–12 hét)

Cél:
- Tenant testreszabás enterprise módon: custom fields + governált schema change.

Szállítandó (DoD):
- Custom fields standard:
  - fizikai modell + indexelés + validáció
  - UI megjelenítés
- Governált schema change flow:
  - preview SQL + approval + apply + audit
  - drift check tenantonként
- “Schema builder” admin felület minimum (governance-kompatibilis)

ADR pontok:
- Milyen DDL engedett (oszlop, index, FK, PK?) és milyen korlátokkal.
- Rollforward-only vs rollback támogatás schema change-nél.

### F6 – Külső könyvelés integráció MVP (posting outbox + REST adapter) (4–8 hét)

Cél:
- A domain (WMS/INV/IC) posting eseményeiből megbízható, auditálható feladás legyen a külső könyvelési rendszer felé (REST integráció).

Szállítandó (DoD):
- Posting kontrakt minimum:
  - postingId (idempotencia), companyId, documentType/no/date, currency, lines, externalRefs, intercompanyRef
- Outbox + státuszok:
  - pending/sending/sent/accepted/rejected
  - retry/backoff és 4xx vs 5xx hibaosztályozás
- REST adapter (konfigurálható):
  - auth: OAuth2 client credentials vagy API key
  - ack: sync (200/201) vagy async (202 + poll/webhook) minimum kezelési kerettel
- Admin UI minimum:
  - posting queue lista + filter company szerint
  - retry/requeue és hibanézet
  - mapping/config minimum (postingKey→account, company→externalCompanyCode)

ADR pontok:
- Posting kontrakt JSON schema és kompatibilitási szabályok.
- Idempotencia és retry invariánsok.

### F7 – Intercompany + multi-currency domain baseline (8–16 hét)

Cél:
- GL/WMS/INV alapfolyamatok úgy legyenek megtervezve, hogy később ne törjön össze a konszolidáció.

Szállítandó (DoD):
- Multi-currency szabályok:
  - rate source, locking, revaluation minimum
- Intercompany alapok:
  - partner jelölés + ellenoldal
  - elimináció mapping (account/dim)
- WMS alap flow-k és posting:
  - inventory mozgás -> könyvelési események

ADR pontok:
- Close/cut-off és posting invariánsok.
- Dimenzió modell (COA + dims) rögzítése.

### F8 – Consolidation (holdingon belül) + archival/read-only (8–16 hét)

Cél:
- Napi close + közel real-time státusz holdingon belül, és audit-biztos archív.

Szállítandó (DoD):
- Consolidation store:
  - outbox/CDC/batch adatfolyam minimum
  - replay/rebuild stratégia
  - napi close cut-off snapshot
- Archival:
  - immutábilis snapshot + read-only UI policy
  - retention, kereshetőség

ADR pontok:
- Consolidation store tech (read model vs warehouse).
- Snapshot technika és retention.

### F8 – Quality gates + compliance hardening (folyamatos, de 4–8 hét explicit fókusz)

Cél:
- Product-ready “minőségkapu” és auditálható működés.

Szállítandó (DoD):
- Teszt stratégia:
  - unit + integration + contract tesztek
  - minimális e2e kritikus user journey-kre
- Security hardening:
  - threat model (legalább STRIDE light)
  - audit log invariánsok
  - access review folyamat
- SLO/SLI:
  - latency/error rate/uptime
  - RTO/RPO célok és restore drill

ADR pontok:
- Compliance cél (SOC2/ISO) és scope.

## 2) Kritikus “előfeltételek” (ha ezek nincsenek, minden lassul)

- Automatizált upgrade/migráció (D009 miatt).
- Tenant routing és tenant-context enforcement.
- Egységes observability (különben multi-installation support lehetetlen).

## 3) Javasolt kezdő WIP=1 fókusz a greenfield projekthez

- [ACTIVE] F4: Release & upgrade rendszer + supported-version enforcement.

Indok:
- D009 miatt a termék fenntarthatósága ezen áll vagy bukik; nélküle a support “szétfolyik” installationonként.

## 4) Mit kell frissíteni a truth file-okban, amikor elindul a jövőbeni projekt?

- `02_allapot.md`: a “blueprint” helyett valós build/smoke állapot.
- `03_kovetkezo_lepesek.md`: WIP=1 fókusz és a konkrét 3–7 következő lépés.
- `01_dontesek.md`: tech stack + identity + DB stratégia ADR-ek.
