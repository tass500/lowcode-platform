# Fejlesztési playbook / Decision flow (TRUTH-alapú)

> **Enterprise megjegyzés:** Operatív folyamat a repóban: `docs/DEVELOPMENT_WORKFLOW.md` · dokumentum-osztályok: `docs/GOVERNANCE.md`. A `docs/live/02` + `03` a napi WIP; ez a playbook **sablon** a truth készlethez.

## 0) Cél

Ez a dokumentum egy gyakorlati “játékkönyv”: leírja, hogy a projekt **aktuális állapotában** milyen fejlesztési lépéseken érdemes végigmenni enterprise best practice szerint, és közben **milyen döntéseket kell meghozni**.

A döntések rögzítése mindig a truth file-okban történik:
- `00_vizio.md` (mit építünk, miért, scope)
- `01_dontesek.md` (ADR-ek, azaz mi mellett döntöttünk)
- `02_allapot.md` (mi a valós állapot)
- `03_kovetkezo_lepesek.md` (WIP=1 fókusz, következő lépések)

Megjegyzés: a mappában lévő egyéb dokumentumok (pl. roadmap/becslés) **kiegészítők**; nem írhatják felül a fenti truth fájlokat.

## 1) Alapszabály: Truth-file driven development

- Ha valami nincs benne a truth file-okban, akkor az **nem követelmény**.
- Ha valami ellentmond a truth file-oknak, a truth file a kanonikus.
- Minden iteráció végén az `02_allapot.md` és `03_kovetkezo_lepesek.md` frissül.

## 1.1) Iteráció előtti brief (timebox, WIP=1)

Az iteráció előtti brief célja, hogy a fejlesztés ne találgatásból és ne “scope creep”-ből álljon.

- Brief formátum: 10–20 sor, a következőkkel:
  - cél (1 mondat)
  - nem-célok (2–6 bullet)
  - szállítandók / DoD (3–8 bullet)
  - smoke / acceptance lépések
  - nyitott döntési pontok (ha van → ADR)

- Kötelező brief, ha:
  - a WIP=1 fókusz változik (`03_kovetkezo_lepesek.md`)
  - publikus kontrakt/schema/DB irány érintett
  - publish/promote vagy runtime enforcement változik
  - a munka várhatóan > 1 nap

- Opcionális (rövid task leírás elég), ha:
  - kicsi, jól körülhatárolt bugfix/UX finomítás
  - nincs publikus kontrakt/schema változás

- Ritmus javaslat:
  - 1 brief / iteráció; tipikus iteráció 1–5 nap
  - ha folyamatos fejlesztés megy: heti 1–2 brief (pl. hét eleje + közepe)

## 2) Állapot-vezérelt fejlesztési ciklus (ismétlődő)

Minden ciklus ugyanazt a sémát követi:

### 2.1) Állapotfelmérés (02_allapot.md)

- Frissítsd a snapshotot:
  - dátum
  - verzió/commit
  - környezet
- Health tények:
  - build OK?
  - migrations OK?
  - smoke OK?
- Top 5 kockázat frissítése.

Kimenet:
- 3-10 soros “mi kész / mi nem” összefoglaló.

### 2.2) Fókusz kijelölés (03_kovetkezo_lepesek.md)

- WIP=1: pontosan 1 aktív fókusz.
- A fókusz legyen mérhető és ellenőrizhető.

Példák jó fókuszra:
- “Supported release pipeline: canary + migration orchestration + rollback runbook.”
- “Tenant isolation enforcement: tenant-context minden DB műveletnél és background jobnál.”

### 2.3) Döntés-igény felismerése (01_dontesek.md)

Ha a fókusz bármely ponton vitatható irányválasztást igényel, ADR kell.

ADR-t igénylő tipikus témák:
- Multi-tenant modell változik (DB-per-tenant vs pooled vs sharded).
- Tenanton belüli domain topológia változik (company/site scope, intercompany invariánsok) (D018).
- Release/upgrade policy (nálunk: csak aktuális verzió supported).
- Konszolidáció adatfolyam (outbox vs CDC vs ETL).
- Custom fields fizikai modell.
- Archival snapshot stratégia.
- Külső rendszer integráció feladás mintája (posting contract + outbox + adapter) és error/idempotency szabályok (D019).

### 2.4) Implementáció + DoD

A kiválasztott fókuszhoz tartozó DoD (Definition of Done) minimum:
- működő build + releváns smoke
- legalább alap teszt (unit/integration) a kritikus logikára
- observability alapok (log/trace id)
- dokumentálás frissítése a truth file-okban (ha változik állapot / döntés / fókusz)

Schema-first minimum (ha definíció/kontrakt változik):
- minden definíció és integrációs payload verziózott schema/contract szerint érvényes (request/response + error contract + kompatibilitási szabályok)
- publish/promote csak schema-validált artefakttal engedett
- data-first jellegű következtetés (profiling/javaslat) csak javaslat lehet; a kanonikus kimenet schema-first

Enforcement összefoglaló (D023):
- Save (draft): warning/soft validáció
- Publish/Promote: hard gate (schema + compat)
- Runtime load/execute: hard gate (csak támogatott schema verzió)

Naming/boundary check (00_vizio.md):
- publikus kontraktok és tartós artefaktok kövessék a bounded context prefix + casing + ID konvenciókat

Platform baseline check (D024/D025/D026):
- error contract egységes (errorCode + traceId + details path)
- idempotencia és retry osztályozás: 4xx = no auto-retry, 5xx/timeout = backoff
- audit események és traceId kötelezők a kritikus admin műveleteknél
- retention/read-only elv sérthetetlen (archív nem írható)

## 3) Enterprise “product-ready” lépcsők (milestone-ok)

Ez a rész előre leírja, milyen nagyobb lépéseken érdemes végigmenni. A milestone-ok sorrendje cserélhető, de a függőségeket érdemes tartani.

### M0) Célok és határok rögzítése (vizio + dontesek)

Döntések / kimenetek:
- Tenant topológia: “holdingonként külön installation (BYOC)”.
- Support policy: “csak a kijelölt (aktuális) verzió supported”.
- Nincs cross-holding riport.
- Microservices granularitás: induláskor moduláris monolith, service kivágás explicit kritériumokkal (D015).

Truth file frissítés:
- `00_vizio.md`: scope + NFR.
- `01_dontesek.md`: ADR-ek (pl. D008, D009).

### M1) Biztonsági baseline (authN/authZ + tenant isolation)

Fő döntések:
- Hol és hogyan enforce-oljuk a tenant-contextet (request scope, background jobs, cache).
- RBAC modell (builder-admin vs runtime, least privilege).

Szállítandó elemek:
- konzisztens error contract + trace id
- “no cross-tenant data access” guardrail
- company/site scope invariánsok (minden üzleti művelet explicit companyId/siteId dimenzióval) (D018)

Truth file frissítés:
- `02_allapot.md`: security érettség és kockázatok.

### M2) Release / upgrade / supported version enforcement

Fő döntések:
- Release cadence (pl. havi) és upgrade ablak.
- Migrációs stratégia: canary -> wave rollout -> completion.
- Rollback szabályok (mikor lehet, mikor tilos).

Szállítandó elemek:
- migráció orchestration (állapotgép)
- “supported version” ellenőrzés a runtime belépési pontokon (API/UI)
- runbook: upgrade + rollback + restore drill

Truth file frissítés:
- `01_dontesek.md`: ha új policy döntés kell.
- `02_allapot.md`: release readiness.

### M3) Governance pipeline: definíciók és tenant customization

Fő döntések:
- Definíciók promote mechanikája (dev/stage/prod) és verzió pinning.
- Schema change governance: preview -> approval -> apply -> audit.

Szállítandó elemek:
- admin folyamat és audit log
- drift check (mi fut a tenantnál vs mi a base)

### M4) Data flow és konszolidáció (holdingon belül)

Fő döntések:
- outbox/CDC vs batch ETL
- napi close cut-off és snapshot

Szállítandó elemek:
- consolidation store read model minimum
- replay/rebuild stratégia

Megjegyzés:
- A külső könyvelés integráció (posting outbox + REST adapter) a domain posting események egyik első “produktív” outbox use-case-e (D019).

### M5) Archival / read-only

Fő döntések:
- snapshot technika (DB snapshot/export/read model)
- retention és kereshetőség

Szállítandó elemek:
- policy enforcement (read-only)
- audit-biztos megtekintés

### M6) Minőségkapuk és teszt-stratégia

Fő döntések:
- milyen tesztek kötelezőek (unit/integration/e2e)
- CI quality gate (lint, tests, security scanning)

Szállítandó elemek:
- automatikus pipeline
- környezetfüggetlen reprodukálhatóság

#### Ajánlott teszt-stratégia (DDD + low-code + microservices)

Minimum (enterprise baseline):
- Unit tesztek:
  - domain szabályok (invariánsok, pénzügyi kerekítések, állapotgépek)
  - “pure” komponensek (pl. JSON definíció validáció, mapping)
- Integrációs tesztek:
  - DB műveletek (migrációk, repository-k, tranzakciók)
  - outbox -> consumer/poller útvonal
  - authorization/policy enforcement kritikus endpointokra
- Contract tesztek (service boundaries):
  - API contract (request/response, error contract, versioning)
  - event schema contract (ha van Kafka/outbox event)
- E2E tesztek (smoke + kritikus user journey):
  - login + tenant routing
  - builder: definíció mentés + verziózás
  - runtime: workflow futtatás
- Non-functional:
  - performance smoke (alap endpoint latency, batch futások)
  - security baseline (dependency scanning, secret scanning, SAST minimum)

CI/CD quality gate (Azure DevOps):
- Minden PR-ben:
  - build
  - unit + integration tesztek minimum
  - lint/format
  - security scan minimum
- Release pipeline-ban:
  - canary deploy + smoke
  - migráció rehearsal
  - (időszakosan) restore drill validáció

## 4) “Mikor kell új ADR?” döntési fa (gyors)

Új ADR kell, ha:
- új, visszavonható döntést hozol, ami később vitát okozhat.
- változik a tenancy vagy adatmodell alapja.
- bevezetsz új külső függőséget (DB tech, message bus, identity).
- a support policy kivételt kap.

Nem kell ADR, ha:
- csak implementációs részlet (pl. egy endpoint elnevezése), ami nem befolyásolja a rendszerszintű működést.

## 5) Ajánlott “next step” a jelenlegi truth file-ok alapján

- A jelenlegi fókusz logikusan: **Release/upgrade + governance pipeline** (mert D009 miatt a támogatás alapja az upgrade fegyelem).
- Következő konkrét döntési pontok:
  - release cadence és upgrade ablak
  - migration orchestration stratégia
  - rollback policy
