# Végtermék összefoglaló és részletes leírás (working draft)

## 0) Mire való ez a dokumentum?

Ez a dokumentum a projekt kanonikus “truth” fájljai és az élő ops leírások alapján összefoglalja:

- mit fog tudni a termék, amikor elkészül,
- kinek készül és milyen használati módok vannak,
- milyen minőségi/üzemeltetési elvárásokhoz igazodik,
- és hogyan néz ki a végtermék részletesen (komponensek, fő modulok, folyamatai).

Források (a projekt szabályai szerint):

- `docs/00_truth_files_template/00_vizio.md`
- `docs/00_truth_files_template/01_dontesek.md`
- `docs/00_truth_files_template/02_allapot.md`
- `docs/00_truth_files_template/03_kovetkezo_lepesek.md`

Kiegészítő (nem írhatja felül a truth-ot):

- `docs/00_truth_files_template/05_roadmap_greenfield_enterprise.md`
- `docs/live/ops/upgrade.md`
- `docs/live/02_allapot.md`
- `docs/live/03_kovetkezo_lepesek.md`


## 1) SQLite → MSSQL: érdemes-e most váltani?

### Rövid válasz

- A végcél szerint a rendszernek **DB-vendor függetlennek** kell lennie (legalább **SQL Server + PostgreSQL**).
- A fejlesztés jelenlegi szakaszában (greenfield + gyors iteráció) **nem feltétlenül érdemes azonnal** “default” DB-t MSSQL-re cserélni, ha a persistence réteg még nincs stabilan elkülönítve és nincs provider-mátrixos CI.

### Ajánlott enterprise-út (lépésekben)

1. **Marad a SQLite** lokális devhez, amíg:
   - a persistence réteg (DbContext + infra) nincs tisztán elkülönítve,
   - nincs legalább 1–2 kritikus integration teszt,
   - nincs migráció-sztenderd.
2. Bevezetünk **DB provider választást** konfigurációból (pl. `postgres|sqlserver|sqlite`).
3. Enterprise irányba a legjobb:
   - **provider-specifikus EF Core migrations** (külön migrations assembly Postgres és SQL Server)
   - CI-ben provider-mátrix (Postgres + SQL Server).
4. Amikor ez megvan, a “default” fejlesztői DB-t át lehet állítani SQL Serverre is (ha tényleg ezt akarod), de onnantól a fejlesztői környezet nehezebb lesz.

### Miért hasznos mégis a SQLite?

- gyors lokális dev (0 infra),
- olcsó TDD,
- kevesebb környezeti zaj.

### Mikor javasolt ténylegesen MSSQL-re állni?

- amikor a domain/persistence boundary már stabil (DDD modulok kezdenek “megülni”),
- amikor az EF Core migrációk és tesztek már nem “szétcsúszósak”,
- amikor van értelme már a vendor-specifikus eltérésekre figyelni (index, JSON, computed, stb.).


## 2) Egy mondatos cél (truth)

Egy enterprise-grade, dobozos low-code platform épül DDD-alapú üzleti alkalmazásokhoz (ERP mag: GL/WMS), iparági best practice-ek szerint (audit, governance, release/upgrade, megfigyelhetőség), ahol a screen/workflow/datasource definíciók verziózottak és auditálhatók, a tenantok pedig kontrolláltan testreszabhatók (custom fields + korlátozott schema change) úgy, hogy multi jogi entitás és telephely (site) kezelés, intercompany és multi-currency környezetben is működjön a napi és közel real-time konszolidáció, valamint a projekt-archívumok read-only módja.


## 3) Mit fog tudni a termék? (terjedelmes összefoglaló)

### 3.1) Platform jelleg (doboztermék, BYOC)

- A termék **dobozos multi-tenant platform**.
- Üzleti elv: **holdingonként külön installation (BYOC)**.
- Egy installationen belül több tenant/projektcég futtatható, cél: kb. 100 tenant.

### 3.2) DDD ERP domain támogatás

- A rendszer DDD elvek mentén épül.
- Bounded context-ek például:
  - GL (General Ledger)
  - WMS (Warehouse Management)
  - INV (Inventory)
- Induláskor moduláris monolith, később explicit kritériumok szerint mikroservice kivágás (D015).

### 3.3) Low-code core: definíciók és runtime

- **Screen definíciók**: UI/renderer definíció.
- **Workflow definíciók**: orchestration; idempotencia alapokkal.
- **Datasource definíciók**: query/command boundary; paraméterezés.
- Jogosultságok:
  - builder-admin szerepkörök
  - runtime szerepkörök
  - RBAC/policy enforcement.

### 3.4) Verzionálás, diff, rollback, audit trail

- A “kód” a definíció: ezért verziózás kötelező.
- A definíciók:
  - snapshotolva vannak,
  - diffelhetők,
  - rollbackelhetők,
  - minden változás auditált.

### 3.5) Tenant customization enterprise módon

- Alap: **custom fields** (stabil core modell mellett).
- Kivétel: **governált schema change** tenant szinten:
  - preview,
  - approval,
  - apply,
  - audit,
  - visszagörgetési/rollforward terv.

### 3.6) Multi-company/site, multi-currency, intercompany

- Tenanton belül több **company** és **site** elsőrendű dimenzió.
- Multi-currency és intercompany nem “extra”, hanem alap üzleti igény.

### 3.7) Konszolidáció és riport

- Nincs OLTP cross-tenant join.
- A konszolidáció/riport külön store-ban:
  - közel real-time operatív riportokhoz,
  - napi close (batch cut-off) jellegű konszolidációhoz.

### 3.8) Archival / read-only mód

- Lezárt projektek/időszakok:
  - immutábilis snapshot
  - read-only UI
  - auditálható visszakeresés.

### 3.9) Release & upgrade rendszer (supported-version policy)

- Supported version policy: csak a kijelölt verzió támogatott (D009).
- Ennek enforcement-e:
  - warn → soft-block → hard-block (D011)
- A release/upgrade folyamat célja:
  - kontrollált, auditálható változtatás,
  - reprodukálható rollout,
  - gyors incident kezelés.

### 3.10) Üzemeltethetőség és megfigyelhetőség

- Egységes error contract (D024): gépileg feldolgozható hibák (errorCode + traceId + részletek).
- Trace-id végig a kéréseken.
- Observability baseline:
  - log/trace/metrics alapok,
  - és drift-proof kliens oldali időkezelés (serverTimeUtc alapján).

### 3.11) Marketplace és AI (tervezett)

- Marketplace: telepíthető modulok/csomagok, verzió + kompatibilitás + RBAC + audit + supply-chain.
- AI: kontrollált AI-assisted builder/governance (auditálható, BYOC kompatibilis, kikapcsolható).


## 4) Kinek készül? (szerepkörök)

- **Builder admin**: definíciókat szerkeszt, verzióz, publish/promote-ol, policy ellenőrzésekkel.
- **Tenant admin**: tenant konfiguráció és korlátozott testreszabás (custom fields, governált változtatás).
- **Operátor / üzleti felhasználó**: WMS/GL folyamatokat futtat.
- **Üzemeltető (Ops/SRE)**: release/upgrade, incident, audit, megfigyelhetőség.
- **Fejlesztő / support**: diagnosztika, gyors reprodukció, ticket csomagok.


## 5) A végtermék részletes leírása (komponensek és fő folyamatok)

### 5.1) Komponensek (high-level)

- **Backend runtime (ASP.NET Core)**
  - admin API-k (observability, audit, upgrade runs)
  - runtime API-k (tenant-context)
  - error contract és trace-id végig
- **Frontend (Angular)**
  - admin/ops felületek (pl. Upgrade oldal)
  - builder felületek (screens/workflows/datasources)
  - runtime UI (a későbbi cél)
- **Adatbázis**
  - cél: legalább PostgreSQL + SQL Server
  - DB-per-tenant stratégia (max ~100)
- **Definition store**
  - screen/workflow/datasource definíciók, verziózás
- **Audit**
  - egységes audit trail, export, retention minimum (D026)
- **Release/upgrade rendszer**
  - supported version enforcement
  - migráció orchestration, rollback stratégia

### 5.2) Upgrade / operációs felület (a mai implementáció alapján)

A jelenlegi “Upgrade” funkciók (és a végtermékben elvárt működés) lényege:

- Upgrade run = állapotgép (`pending` → `running` → `succeeded|failed|canceled`)
- Egyszerre max 1 aktív run (invariáns)
- Drift-proof UI időkezelés:
  - `serverTimeUtc` alapján kliens offset
  - “last refreshed”/“ago”/duration stabil kliens drift mellett is
- Observability snapshot: enforcement összegzés + aktív run-ok + utolsó audit
- Operátori UX:
  - Refresh all
  - Polling auto-stop terminal state-nél
  - Start/Retry/Cancel + dev fail-step tesztelhetőség
- Diagnosztika/támogathatóság:
  - ticket header + short one-liner
  - curl snippetek
  - incident bundle JSON (copy + download)
  - debug pack (1 JSON: ticket + curls + bundle + audit preview)
- Audit tooling:
  - filterek + paging + export + URL copy
  - hard limit (max <= 10000)

Megjegyzés a végleges modellhez:

- Az Upgrade UI **nem a CI/CD pipeline helyett van**, hanem a runtime változtatásokhoz és a supportolható, auditálható operációhoz.

### 5.3) Definition lifecycle (későbbi “end state”)

- Definíció készítés/szerkesztés (builder admin)
- Soft validáció szerkesztéskor
- Hard gate publish/promote-nál (schema-first enforcement)
- Runtime csak validált kontraktot futtat
- Versioning + diff + rollback

### 5.4) Tenancy és izoláció (end state)

- Tenant-context explicit minden adat-hozzáférésnél
- DB-per-tenant, per-tenant mentés/restore
- RBAC és policy enforcement

### 5.5) Integrációs gerinc (end state)

- Outbox + idempotencia + retry policy (D025)
- Külső könyvelés REST adapter (D019–D022)


## 6) Mit várhatsz “kész” állapotban? (pragmatikus lista)

- **Egy telepíthető platform**, ami:
  - több tenantot kiszolgál egy installationen belül,
  - moduláris DDD ERP domain alapokra épít,
  - low-code definíciókból képes UI-t és folyamatokat futtatni,
  - auditálható és visszagörgethető módon kezeli a definíciókat,
  - és támogatott verzió policy mellett kontrolláltan frissíthető.

- **Üzemeltethető és supportolható**:
  - egységes error contract + trace-id
  - upgrade/incident csomagok 1 kattintással
  - audit export és reprodukciós eszközök

- **Enterprise irányú architektúra**, de fokozatosan bevezethető:
  - DB-agnosztikus cél (Postgres+SQL Server)
  - később marketplace és AI governance


## 7) Nyitott kérdések (olvasás után ide érdemes visszajönni)

- Mit tekintünk a “definition runtime” minimál verziójának? (workflow sandbox határok)
- Tenant provisioning pontos folyamata (DB létrehozás, baseline migrációk, rollback)
- Supported version policy részletek:
  - release cadence
  - upgrade window
  - rollback tiltások/engedések (adatmigráció kompatibilitás)
- Marketplace csomag aláírás és kompatibilitás szabályok

