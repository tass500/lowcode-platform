# Vízió (TRUTH)

## 0) Canon / használati szerződés

Ez a fájl a projekt víziójának és “miértjének” **referencia-sablonja** (blueprint / greenfield másolható készlet). **Enterprise megjegyzés:** a repó **napi operatív** igazsága a `docs/live/02_allapot.md`, `docs/live/03_kovetkezo_lepesek.md` és a `docs/PROJECT_CONTEXT.md` — lásd `docs/GOVERNANCE.md` §2.

Szabályok (a sablon mappán belül):
- Elsődleges truth fájlok ebben a mappában (stratégiai irány + döntésnapló index):
  - `00_vizio.md`
  - `01_dontesek.md`
  - `02_allapot.md`
  - `03_kovetkezo_lepesek.md`
- A mappában lévő egyéb dokumentumok (pl. playbook/roadmap/becslés) **kiegészítők**: nem írhatják felül a fenti 4 truth fájl **szándékolt hierarchiáját**.
- Ha bármely más dokumentum, ticket, kód-komment, chat log, vagy „random jegyzet” ellentmond a **fenti szabálynak és a `DEVELOPMENT_WORKFLOW.md`-nek**, azt zajnak kell tekinteni.
- Ha valami nincs leírva a truth fájlokban, akkor az **nem követelmény** és **nem döntés** (kivéve, ha a `docs/live/*` vagy `PROJECT_CONTEXT.md` már rögzíti).
- Módosítás csak tudatosan, visszakereshetően (Git PR):
  - vízióváltozás: ebben a fájlban,
  - döntés: `01_dontesek.md` új bejegyzés vagy `docs/adr/` új ADR,
  - **aktuális szállítási állapot** (ajánlott): `docs/live/02_allapot.md`,
  - **aktuális fókusz / WIP** (ajánlott): `docs/live/03_kovetkezo_lepesek.md`.
- Új **egyedi** architektúra-döntés: `docs/adr/README.md` szerinti ADR opcionális, de ajánlott nagy hatású változásnál.

## 1) Egy mondatos cél

Egy enterprise-grade, dobozos low-code platformot építünk DDD-alapú üzleti alkalmazásokhoz (ERP mag: GL/WMS), iparági best practice-ek szerint (audit, governance, release/upgrade, megfigyelhetőség), ahol a screen/workflow/datasource definíciók verziózottak és auditálhatók, a tenantok pedig kontrolláltan testreszabhatók (custom fields + korlátozott schema change) úgy, hogy multi jogi entitás és telephely (site) kezelés, intercompany és multi-currency környezetben is működjön a napi és közel real-time konszolidáció, valamint a projekt-archívumok read-only módja.

## 2) Termék-keret / scope

### In scope (amit biztosan akarunk)
- Multi-tenant doboztermék: max ~100 tenant, erős izoláció (jogilag és üzemeltetésben is).
- DDD-alapú ERP domain appok támogatása (külön bounded context-ek: pl. GL, WMS, INV).
- Architektúra: DDD + low-code; induláskor moduláris monolith, később indokolt esetben microservice kivágások (D015).
- Low-code core:
  - Screen definíciók (UI/renderer)
  - Workflow definíciók (orchestration, idempotency alapok)
  - Datasource definíciók (query/command boundary, paraméterezés)
  - Permissions / RBAC (builder-admin vs runtime roles)
- Definíciók verziózása, diff, rollback, audit trail.
- Domain megoldások modulokként/csomagokként építhetők: WMS/logisztika, shipping, webshop integráció, pénztár (POS), pénzügyi integráció.
- Saját AI-támogatás tervezetten: AI-assisted builder/governance (definíciók, validáció, drift, migráció előnézet) kontrollált keretek között.
- Marketplace: telepíthető modulok/csomagok (bounded context modulok, screen/workflow/datasource csomagok) verziózva és kompatibilitási szabályokkal.
- Tenant customization modell:
  - Custom fields (stabil fő modell mellett)
  - Korlátozott, governált schema evolúció tenant szinten (előnézet/approval/audit)
- Multi-currency és intercompany mint elsőrendű üzleti igény.
- Konszolidáció:
  - napi “batch close”
  - közel real-time operatív riport/monitoring
- Archival/read-only projektek: snapshot/archívum megtekintés UI-ból írás nélkül.
- Deployment modell: holdingonként külön installation (BYOC), ahol egy holding a saját felhőjében futtatja a saját tenantjait/projektcégeit.
- Cloud-first futtatás: konténerizált, felhőben való futáshoz optimalizált komponensek (cache, eventing) támogatásával.

### Out of scope (amit most nem)
- Tetszőleges (korlátlan) tenant oldali DDL (pl. random PK csere, tetszőleges FK-k) önkiszolgáló módon, governance nélkül.
- “Mindenki mindent átírhat” jellegű, teljesen szabad formájú meta-model (kontroll nélkül).
- Teljes körű BI/analytics termék (helyette: stabil export/replica/warehouse csatlakozás).
- Hard multi-region active-active pénzügyi close (későbbi fázis).
- Cross-holding riport/konszolidáció (holdingok között nincs ilyen üzleti igény).

### Nem-célok (amit kifejezetten kerülünk)
- A base domain modell destabilizálása tenant specifikus “one-off” módosításokkal.
- “Schema drift” ellenőrizetlen felhalmozása (nincs audit/preview/rollforward terv).
- Runtime és builder jogosultságok összemosása.
- Konszolidáció élő OLTP cross-tenant join-okkal (helyette külön reporting store).

## 3) Felhasználói érték / fő use-case-ek

- Builder admin létrehoz/verzióz screen/workflow/datasource definíciókat, és biztonságosan kiadja őket.
- Builder/admin AI-támogatással gyorsabban készít és ellenőriz definíciókat (javaslatok + validáció + policy-check), auditálható módon.
- Tenant admin korlátozott módon bővíti a modellt (custom fields + governált változtatások) előnézettel és audit-tal.
- Operátorok WMS folyamatokat futtatnak (pick/pack/ship), készletmozgások és foglalások könyvelődnek.
- Pénzügy GL-ben multi-currency könyvelést végez, intercompany tranzakciókat kezel (IC AP/AR, eliminációk).
- Külső könyvelési rendszerhez integráció: a domain eseményekből feladási (posting) kontrakt keletkezik, outbox/idempotencia/retry mellett REST adapterrel.
- Holding szintű konzol: napi close + közel real-time státusz/riport tenantok felett.
- Archív projektek/lezárt időszakok read-only módban böngészhetők audit célból.
- Marketplace csomagok telepítése installation/tenant szinten (policy és role alapján), verzió- és kompatibilitás-kezeléssel.

## 4) Minőségi célok / nem-funkcionális elvárások

- Multi-tenant izoláció: adatszivárgás-mentes (DB, authZ, cache, logs), tenant-context mindenhol explicit.
- Auditálhatóság: definíció verziók, schema változások, “ki-mikor-mit” és trace-id végig.
- Schema-first alapelv: a core runtime és integrációs kontraktok elsődleges forrása a verziózott schema/contract; a runtime csak validált kontraktokra támaszkodik.
- Data-first ott megengedett, ahol tanulás/riport a cél: analytics/konszolidáció/observability és AI javaslatok; de a publish/promote mindig schema-validált.
- Egységes error contract: API/runtime/builder hibák gépileg feldolgozhatók (errorCode + traceId + path), hogy support és automation skálázható legyen.
- Idempotencia és retry: külső integrációk és workflow/datasource műveletek determinisztikus idempotencia kulcs + hibaosztályozás (4xx vs 5xx) mentén.
- Evolválhatóság: base schema migrációk, tenant baseline, kompatibilitási szabályok, rollback stratégia.
- Üzemeltethetőség: migrációk és rollout “pipeline”-olható, környezetek közt reprodukálható.
- Szállíthatóság: CI/CD és release automatizálható (Azure DevOps pipeline-ok), konténerizált futtatással.
- Támogathatóság: definiált release cadence és support policy; csak a kijelölt (aktuális) verzió támogatott.
- Megbízhatóság: minimum baseline SLO/RTO/RPO célok installation szinten rögzítettek (SLO 99.9%, RTO 4 óra, RPO 15 perc).
- Teljesítmény: OLTP tenant DB-k gyorsak, konszolidáció/reporting külön store-ban.
- Biztonság: least privilege RBAC, secret management, PII kezelés.
- AI megfelelőség: prompt/kontextus kezelés, auditálhatóság, PII/üzleti adat kontroll (BYOC modellel kompatibilis).
- Adat-életciklus: archiválás/read-only és retention policy előre rögzített, hogy audit és üzemeltetés tervezhető legyen.
- Extension pontok governáltak: bővítés csak definíció/csomag/adapter keretben; nincs “bypass” a tenant isolation, RBAC és audit logika körül.

Névkonvenciók és határjelölés (normatív minimum):
- Bounded context jelölés kötelező a tartós és publikált artefaktokon (DB táblák, publikus event/contract nevek): pl. `wms_*`, `gl_*`, `inv_*`.
- DB: `snake_case` (táblák + oszlopok); API/JSON: `camelCase`.
- Azonosítók: `*_id` a DB-ben és `...Id` JSON-ban; az ID-k jelentése stabil (nem újrahasznált, nem implicit).
- Multi-company/site: ahol releváns, a mező neve explicit `company_id` / `site_id` (JSON: `companyId` / `siteId`); nem elfogadható az implicit vagy “varázslatos” scope.
- Tiltott: generikus, domain-határokat elmosó nevek (`data1`, `value`, `misc`, `custom1`) publikált kontraktokban.

## 5) Architektúra-vonalvezetés (high-level)

- Bounded context-ek külön modulokként (GL/WMS/INV), közös platform szolgáltatásokkal (auth, tenant registry, definition store, auditing).
- Szolgáltatás-határok: bounded context = modulhatár; microservice csak explicit kivágási kritériumok alapján (D015).
- Holding/control plane:
  - Tenant registry, identity, licenc/feature flags, cross-tenant admin felületek.
- Tenant plane:
  - Tenant DB (OLTP) tenantonként (közös DB több jogi entitással és telephellyel); definíciók tárolása és runtime futtatása tenant kontextusban.
  - Jogi entitás és telephely elsőrendű dimenziók: minden üzleti objektum companyId/siteId szerint szűrhető.
- Adatbázis: DB-vendor független cél (legalább MSSQL + PostgreSQL), migrációkkal és kompatibilitási szabályokkal.
- Definition layer:
  - Screens/Workflows/Datasources JSON (vagy typed JSON schema) verziózva; snapshot/diff/rollback.
- Customization:
  - Alapértelmezésben custom fields (JSON column / typed extension tables),
  - ritkább esetben governált DDL: preview SQL, approval, apply, audit, visszagörgetési terv.
- Konszolidáció:
  - Event/outbox/CDC alapú adatfolyam tenant DB-kből reporting/consolidation store-ba,
  - napi close pipeline: fixált “cut-off” és időszeletelt snapshot.
- Cloud-native támogatás:
  - cache/distributed lock (pl. Redis),
  - event stream/integration backbone (pl. Kafka) – bevezetése governált döntés.
- Archival:
  - immutable snapshotok (DB snapshot/export), read-only UI nézet (policy/role alapján).

## 6) Definíciók és terminológia (szótár)

- Tenant: izolált ügyfél/holding telepítésen belüli egység, saját OLTP adattal és definíciókkal; tenanton belül több jogi entitás és telephely lehet.
- Holding / Control plane: központi admin és cross-tenant irányítás (tenant registry, identitás, governance).
- Definition: low-code artefakt (screen/workflow/datasource), amely runtime-ban futtatható és verziózott.
- Publish: egy definíció konkrét verziójának kiadása a runtime számára (pinning), hogy élesben futtatható legyen.
- Promote: már publish-olt (vagy publish-ra kész) verzió környezetek/állapotok közti előléptetése (pl. dev→stage→prod) governált folyamatban.
- Override / Customization: tenant-specifikus kiterjesztés (preferáltan custom fields), vagy governált schema change.
- Baseline / Migration: base termék-séma verziózott migrációkkal; tenantoknak baseline + incremental update.
- Consolidation store: cross-tenant riport/konszolidáció célú adattár, OLTP-től leválasztva.
- Archival / Read-only: lezárt projektek/időszakok immutábilis snapshotja csak olvasási joggal.
