# Döntések (TRUTH)

## 0) Canon / használati szerződés

Ez a fájl a projekt döntésnaplója (ADR). Ami itt szerepel, az tekintendő elfogadott döntésnek.

Szabályok:
- Elsődleges truth fájlok: `00_vizio.md`, `01_dontesek.md`, `02_allapot.md`, `03_kovetkezo_lepesek.md`.
- A mappában lévő egyéb dokumentumok (pl. playbook/roadmap/becslés) **kiegészítők**: nem írhatják felül a fenti 4 truth fájlt.
- Minden döntésnek legyen:
  - egyedi azonosítója,
  - dátuma,
  - státusza,
  - kontextusa,
  - döntése,
  - következményei,
  - és (ha van) alternatívák.
- Ha egy döntés megváltozik:
  - ne írd át a régit “nyomtalanul”,
  - hozz létre új döntést, ami felülírja és hivatkozik rá.

## 0.1) Policy index (1 oldal, gyors referencia)

Az alábbi policy-k **normatívak**; fejlesztésnél ezekhez kell igazodni. A részletes szabályok az adott ADR-ben vannak.

| Policy | Normatív forrás (ADR) | 1 soros értelmezés | Hol van még átvezetve? |
|---|---|---|---|
| BYOC / holding-per-installation | D008 | Holdingonként külön installation, nincs cross-holding üzleti adatmozgás | `00_vizio.md`, `05_roadmap_greenfield_enterprise.md`, `04_fejlesztesi_playbook.md` |
| Supported version policy | D009 | Support csak a kijelölt (aktuális) verzióra, upgrade kötelező | `00_vizio.md`, `05_roadmap_greenfield_enterprise.md`, `04_fejlesztesi_playbook.md`, `03_kovetkezo_lepesek.md`, `06_enterprise_szint_iteraciobecsles.md` |
| Supported version enforcement | D011 | Warning → soft-block → hard-block; admin műveletek kikényszerítve | `03_kovetkezo_lepesek.md`, `04_fejlesztesi_playbook.md`, `05_roadmap_greenfield_enterprise.md` |
| Microservices granularitás | D015 | Indulás moduláris monolith; microservice kivágás csak explicit triggerrel | `00_vizio.md`, `04_fejlesztesi_playbook.md` |
| AI-assisted builder/governance | D016 | AI csak kontrolláltan: adatkezelés, audit, kikapcsolhatóság BYOC-ban | `00_vizio.md`, `05_roadmap_greenfield_enterprise.md`, `03_kovetkezo_lepesek.md`, `02_allapot.md` |
| Marketplace (telepíthető csomagok) | D017 | Csomagok verziózva, kompatibilitás+RBAC+audit+aláírás (supply-chain) | `00_vizio.md`, `05_roadmap_greenfield_enterprise.md`, `03_kovetkezo_lepesek.md`, `02_allapot.md` |
| Multi jogi entitás + telephely tenanton belül | D018 | Tenanton belül több company+site, közös tenant DB-ben (companyId/siteId elsőrendű dimenziók) | `00_vizio.md` |
| Külső könyvelés integráció (posting outbox + REST adapter) | D019 | Belső posting kontrakt → outbox/idempotencia/retry → konfigurálható REST adapter (auth+ack módok) | `00_vizio.md` |
| Posting kontrakt schema + mapping modell | D020 | Posting kontrakt minimál mezők és mapping réteg (postingKey→account, company/site external kódok) rögzítve | `00_vizio.md`, `05_roadmap_greenfield_enterprise.md` |
| Company/site RBAC + default scope | D021 | Jogosultság és alap scope: user szerepkör + company/site hozzáférés determinisztikus szabályokkal | `00_vizio.md` |
| Külső könyvelés REST integráció MVP szerződés | D022 | REST adapter minimál szerződés: auth+ack+idempotencia+retry+error contract+mapping config | `05_roadmap_greenfield_enterprise.md` |
| Schema-first enforcement policy | D023 | Soft validáció szerkesztéskor, hard gate publish/promote és runtime load pontokon; kompatibilitás és error contract kötelező | `00_vizio.md`, `04_fejlesztesi_playbook.md` |
| Egységes error contract | D024 | Standard hiba payload: machine-readable code + traceId + details; API/runtime/builder egységesen | `00_vizio.md`, `04_fejlesztesi_playbook.md` |
| Idempotencia + retry policy | D025 | Idempotency-key és retry/backoff szabályok: 4xx vs 5xx osztályozás, végállapotok, deduplikáció | `00_vizio.md`, `04_fejlesztesi_playbook.md` |
| Audit + retention minimum | D026 | Kötelező audit események + adat-életciklus/archív invariánsok (read-only, immutability) | `00_vizio.md`, `04_fejlesztesi_playbook.md` |
| Definition versioning + breaking change policy | D027 | Screen/workflow/datasource definíciók semver + compat szabályok; deprecate/publish/promote rend | `00_vizio.md`, `05_roadmap_greenfield_enterprise.md` |

## 1) Döntés sablon

Másold be minden új döntéshez:

---

## D### - [RÖVID CÍM]

- Dátum: YYYY-MM-DD
- Státusz: Proposed | Accepted | Superseded | Rejected
- Érintett terület: [pl. multi-tenant, runtime, builder, DB, UI]

### Kontextus

[Miért kellett dönteni? Milyen kényszerek, célok, ismert tények vannak?]

### Döntés

[Mi mellett döntöttünk, pontosan?]

### Következmények

- Pozitív:
  - [..]
- Negatív / trade-off:
  - [..]
- Nyitott kérdések / későbbi feladat:
  - [..]

### Alternatívák (opcionális)

- [A] ...
- [B] ...

---

## 2) Döntések listája

(A döntések jellemzően időrendben kerülnek ide, de a dokumentum olvashatósága miatt csoportosítás/átrendezés előfordulhat. Kezdésként a legfontosabb alapdöntések vannak felvéve.)

---

## D001 - Control plane (holding) + tenant plane szétválasztás

- Dátum: 2026-03-06
- Státusz: Accepted
- Érintett terület: multi-tenant, platform, security

### Kontextus

Dobozos multi-tenant platformnál külön kell választani a cross-tenant adminisztrációt és identitást attól, ami tenant-specifikus runtime és adat. Ezzel csökken a kockázat (adatkeveredés), tisztább a governance és egyszerűbb a konszolidációs folyamatok kontrollja.

### Döntés

Bevezetjük a control plane (holding) és tenant plane elkülönítését:
- Control plane: tenant registry, identity, licenc/feature flags, cross-tenant admin.
- Tenant plane: tenant DB + definíció store + runtime futtatás.

### Következmények

- Pozitív:
  - Erősebb izoláció és átláthatóbb governance.
  - Konszolidáció és cross-tenant műveletek kontrolláltan szervezhetők.
- Negatív / trade-off:
  - Plusz komponens és üzemeltetési komplexitás.
- Nyitott kérdések / későbbi feladat:
  - Tenant provisioning “happy path” és hibakezelés részletezése.

---

## D002 - Tenant DB per jogi entitás / projektcég (max ~100 tenant)

- Dátum: 2026-03-06
- Státusz: Accepted
- Érintett terület: DB, multi-tenant, compliance

### Kontextus

ERP jellegű (GL/WMS) rendszernél gyakori a jogi entitásonkénti elkülönítés (audit, adózás, zárás, jogosultság). Max ~100 tenant mellett a DB per tenant stratégia üzemeltethető.

### Döntés

Az OLTP adat tenantonként külön adatbázisban van (DB per tenant). A cross-tenant riport/konszolidáció külön adattárba kerül.

Megjegyzés:
- A tenanton belüli jogi entitás/telephely topológia (több company/site egy tenant DB-ben) a D018-ban rögzített, és felülírja a D002 korábbi “jogi entitás = tenant” értelmezését.

### Következmények

- Pozitív:
  - Erős adatizoláció, egyszerűbb mentés/restore és per-tenant archív.
- Negatív / trade-off:
  - Migrációk tenant-szintű futtatása és megfigyelése extra munka.
- Nyitott kérdések / későbbi feladat:
  - Pooled erőforrások (pl. cache) tenant-context szabályai.

---

## D003 - Customization stratégia: custom fields az alap, governált schema change kivétel

- Dátum: 2026-03-06
- Státusz: Accepted
- Érintett terület: schema evolution, builder

### Kontextus

Tenant oldali, szabad DDL gyorsan schema drift-hez és támogatási pokolhoz vezet. Ugyanakkor üzletileg kell a testreszabhatóság (custom mezők, néha index/FK).

### Döntés

Alapértelmezett kiterjesztés: custom fields (pl. JSON column / typed extension table). Tenant-specifikus DDL csak governált módon:
- preview (SQL terv),
- approval,
- apply (kontrollált migráció),
- audit.

### Következmények

- Pozitív:
  - Megmarad a base modell stabilitása és supportolhatósága.
- Negatív / trade-off:
  - Néhány igényre “drágább” lesz a megoldás, mint egy sima DDL.
- Nyitott kérdések / későbbi feladat:
  - Custom fields fizikai modell standardizálása (indexelhetőség, reporting).

---

## D004 - Definíciók (screen/workflow/datasource) verziózása és audit trail kötelező

- Dátum: 2026-03-06
- Státusz: Accepted
- Érintett terület: builder, runtime, audit

### Kontextus

Low-code rendszernél a “kód” a definíció. Audit és visszagörgethetőség nélkül nem vállalható enterprise környezetben.

### Döntés

Minden definíció verziózott, diffelhető és rollback-elhető. Mentésnél snapshot készül, a runtime determinisztikusan ugyanazt a verziót tudja futtatni.

### Következmények

- Pozitív:
  - Biztonságos kiadás, audit, reprodukálhatóság.
- Negatív / trade-off:
  - Tárhely és extra metadatakezelés.
- Nyitott kérdések / későbbi feladat:
  - Release csomagolás (verzió pinning) és környezetek közti promote.

---

## D005 - Konszolidáció/reporting külön store-ban, nem OLTP cross-tenant join-okkal

- Dátum: 2026-03-06
- Státusz: Accepted
- Érintett terület: data flow, reporting, performance

### Kontextus

Cross-tenant join az OLTP-n egyszerre biztonsági és teljesítmény kockázat. Konszolidációhoz időzített snapshot és/vagy esemény-alapú adatfolyam kell.

### Döntés

Konszolidáció és holding riportok külön “consolidation store”-ban készülnek. Tenant DB-kből outbox/event/CDC jellegű adatfolyam épül:
- közel real-time operatív állapothoz,
- napi “close” cut-off + snapshot a pénzügyi konszolidációhoz.

### Következmények

- Pozitív:
  - Stabil teljesítmény és erős izoláció.
- Negatív / trade-off:
  - Plusz pipeline és adatminőség/late arrival kezelés.
- Nyitott kérdések / későbbi feladat:
  - “Source of truth” definíció és visszajátszás (rebuild) stratégia.

---

## D006 - Intercompany és multi-currency elsőrendű (nem utólagos kiegészítés)

- Dátum: 2026-03-06
- Státusz: Accepted
- Érintett terület: domain (GL), consolidation

### Kontextus

Holding + projektcégek esetén az intercompany számlázás/anyagmozgás és a multi-currency könyvelés alapigény. Ha ez nincs “beépítve”, a későbbi javítás drága és kockázatos.

### Döntés

Az adatmodell és workflow-k az alábbiakat natívan támogatják:
- tranzakció deviza + bázis deviza (reval/opcionális),
- intercompany partner/ellenoldal jelölés,
- eliminációs dimenziók és konszolidációs mapping.

### Következmények

- Pozitív:
  - Kevesebb “one-off” workaround, megbízhatóbb konszolidáció.
- Negatív / trade-off:
  - Magasabb kezdeti komplexitás.
- Nyitott kérdések / későbbi feladat:
  - Árfolyam források, rate locking és zárási szabályok részletezése.

---

## D007 - Archival / read-only projektek: immutábilis snapshot + read-only UI policy

- Dátum: 2026-03-06
- Státusz: Accepted
- Érintett terület: compliance, runtime, operations

### Kontextus

Lezárt projektek és múltbéli állapotok audit célra kell, de írási műveletek nélkül. OLTP adatbázisban “fél-archív” állapot fenntartása kockázatos.

### Döntés

Archív mód: immutábilis snapshot (DB snapshot/export vagy verziózott read model) + read-only UI. Policy szinten tiltjuk az írást és workflow-k futtatását archív projektekre.

### Következmények

- Pozitív:
  - Audit-biztos és egyszerűen érthető üzemeltetési modell.
- Negatív / trade-off:
  - Snapshot menedzsment és tárhelyigény.
- Nyitott kérdések / későbbi feladat:
  - Snapshot retention és visszakereshetőség (indexelés) specifikáció.

---

## D008 - Holdingonként külön installation (BYOC), nincs cross-holding riport

- Dátum: 2026-03-06
- Státusz: Accepted
- Érintett terület: multi-tenant, deployment, compliance

### Kontextus

Több holding esetén az adatok és üzemeltetés elkülönítése kulcs (compliance, ügyfél-elvárások, “my cloud”). A holdingon belül lehet sok projektcég/tenant, de holdingok között nincs üzleti igény konszolidációra.

### Döntés

A terméket holdingonként külön installationként telepítjük (BYOC):
- Egy installation egy holdingot szolgál ki (pl. holding + 100 projektcég), a holding saját felhőjében.
- Cross-holding riport/konszolidáció nem része a terméknek.
- A termék cross-installation szinten legfeljebb licenc/telemetria/kiadás kezelést végezhet, de üzleti adat nem mozog holdingok között.

### Következmények

- Pozitív:
  - Erős izoláció ügyfélcsoportok között, egyszerűbb compliance.
  - Skálázás “cellákban” (installation) történik, kisebb blast radius.
- Negatív / trade-off:
  - Több deployment életciklust kell menedzselni (upgrade, monitoring, support).
- Nyitott kérdések / későbbi feladat:
  - Telepítési automatizmus és standard runbookok (provision/upgrade/rollback).

---

## D009 - Támogatási modell: csak a kijelölt (aktuális) verzió támogatott

- Dátum: 2026-03-06
- Státusz: Accepted
- Érintett terület: operations, release, support

### Kontextus

Sok installation mellett a “minden régi verziót támogatunk” gyorsan fenntarthatatlan. Enterprise környezetben is kezelhető, ha az upgrade folyamat kontrollált és auditálható.

### Döntés

Támogatási szabály:
- A support csak a kijelölt (aktuális) kiadott verzióra érvényes.
- Upgrade kötelező (definiált időablakkal), a release tartalmaz migrációs és rollback tervet.
- Incident esetén első lépés a supported verzióra emelés, ha a hiba már fixált az aktuálisban.

### Következmények

- Pozitív:
  - Support és hibajavítás fókuszált, biztonsági patch-ek gyorsabban átérnek.
- Negatív / trade-off:
  - Ügyfél oldalon upgrade fegyelem és üzemeltetési érettség szükséges.
- Nyitott kérdések / későbbi feladat:
  - Release cadence (pl. havi), SLA és “kényszer-upgrade” szabályok rögzítése.

---

## D010 - Tech stack baseline (greenfield)

- Dátum: 2026-03-06
- Státusz: Accepted
- Érintett terület: platform, devex, operations

### Kontextus

Enterprise product-ready fejlesztésnél az AI ügynök és a csapat csak akkor tud konzisztensen haladni, ha a fő technológiai választások rögzítve vannak (különben implementáció közben találgatás lesz).

### Döntés

Greenfield baseline:
- Backend: .NET (ASP.NET Core) REST API + background worker.
- Frontend: Angular + Angular Material.
- Adatbázis: DB-független cél (legalább MSSQL és PostgreSQL támogatás).
- Migráció: .NET-barát eszköz (pl. DbUp) vagy Flyway; DB vendoronként külön migrációs útvonalak engedettek, ha szükséges.
- CI/CD: Azure DevOps pipeline-ok.
- Konténerizáció: Docker (Kubernetes-kompatibilis futtatás).
- Observability: OpenTelemetry (trace), strukturált logok, metrikák.
- Cloud-native komponensek:
  - Redis: cache és/vagy distributed lock (opcionális, de támogatott)
  - Kafka: event stream / integration backbone (opcionális, bevezetése ADR-t igényel)
- Integráció/outbox: alapértelmezésben DB outbox + poller; Kafka bevezetése külön döntés.

### Következmények

- Pozitív:
  - Egységes devex és “best practice” ökoszisztéma.
- Negatív / trade-off:
  - Stack lock-in; más környezethez adaptáció külön döntés.
- Nyitott kérdések / későbbi feladat:
  - Message bus választás (Kafka vs más), ha a outbox/poller nem elég.
  - DB vendor specifikus eltérések (adattípusok, indexek) kezelési szabvány.

---

## D014 - UI styling: Tailwind használata Angular Material mellett

- Dátum: 2026-03-06
- Státusz: Accepted
- Érintett terület: frontend, design system

### Kontextus

Angular Material ad egy stabil komponenskészletet. A Tailwind hasznos lehet layout/utility szinten, de bevezetése hozhat párhuzamos styling rendszert és build komplexitást.

### Döntés

- Alapértelmezés: Angular Material + saját (korlátozott) SCSS téma.
- Tailwind:
  - nem kötelező baseline,
  - később opcionálisan bevezethető, ha a design system és a build pipeline már stabil,
  - bevezetése új ADR-t vagy ennek a döntésnek a felülírását igényli.

### Következmények

- Pozitív:
  - Kevesebb kezdeti komplexitás és konzisztens UI stack.
- Negatív / trade-off:
  - Utility-first gyors iteráció előnye kezdetben kisebb.

---

## D015 - Microservices granularitás: moduláris monolithból fokozatos service kivágás

- Dátum: 2026-03-06
- Státusz: Accepted
- Érintett terület: architecture, delivery, testing

### Kontextus

3–5 fős csapatnál a “túl korai” microservice-szétvágás aránytalanul növeli a CI/CD, contract tesztek, lokális fejlesztés és üzemeltetés költségét. Ugyanakkor enterprise terméknél fontos, hogy a bounded context-ek idővel külön deployolhatók legyenek, és a service-határok tiszták legyenek.

### Döntés

- Kezdő állapot: moduláris monolith (egy deploymenten belül), explicit modulhatárokkal (bounded context = modul).
- Service kivágás csak akkor történik, ha legalább egy kiváltó ok fennáll:
  - skálázási igény: egy modul terhelése indokolja az önálló skálázást,
  - release függetlenség: eltérő release cadence vagy kockázatprofil,
  - izoláció/biztonság: külön trust boundary kell,
  - adat-életciklus: saját adatbázis/séma és migrációs ritmus indokolt,
  - csapat-szervezés: ownership tiszta és stabil.
- Service kivágás minimális technikai követelményei:
  - API contract rögzített (és contract tesztelt),
  - error contract és verziózás szabályai adottak,
  - observability (trace/log/metrics) service-szinten mérhető.
- Eventing:
  - alapértelmezés: DB outbox + consumer/poller (egyszerűség),
  - Kafka csak akkor, ha a throughput/late-arrival/replay igény indokolja; bevezetése ADR-t igényel.

### Következmények

- Pozitív:
  - Gyorsabb iteráció a korai fázisban, kevesebb “distributed” hiba.
  - Később kontrolláltan kivághatók a service-ek, enterprise elvek sérülése nélkül.
- Negatív / trade-off:
  - Kezdetben nagyobb deploy egység, fegyelem kell a modulhatárok betartásához.
- Nyitott kérdések / későbbi feladat:
  - Modulhatár enforcement (pl. namespace/assembly szabályok) és dependency policing.

---

## D016 - Saját AI-támogatás: AI-assisted builder és governance (kontrollált)

- Dátum: 2026-03-06
- Státusz: Accepted
- Érintett terület: builder, governance, security, compliance

### Kontextus

Low-code platformnál a definíciók, validációk, migrációs előnézetek és policy-check feladatok jó jelöltek AI támogatásra. BYOC és enterprise környezetben azonban a PII/üzleti adat kezelése, auditálhatóság és reprodukálhatóság kritikus.

### Döntés

Bevezetünk AI-támogatást, de kizárólag kontrollált, auditálható módon:
- Elsődleges use-case-ek:
  - definíció javaslat (screen/workflow/datasource skeleton)
  - validáció és policy-check (pl. jogosultság, tiltott műveletek, naming)
  - “explain” és “diff summary” (verziók közti változás értelmezése)
  - schema change preview magyarázat (miért kell a migráció)
- Adatkezelés:
  - alapértelmezés: az AI nem kap tenant üzleti adatot; csak definíció meta és anonimizált kontextus.
  - BYOC customer-managed esetben az AI hívás opcionális és kikapcsolható.
- Audit:
  - az AI által generált javaslatok menthetők és verziózva vannak (ki kérte, mikor, milyen prompt contexttel).
- Determinizmus:
  - AI output nem “kötelező igazság”, csak javaslat; a rendszernek AI nélkül is működnie kell.

### Következmények

- Pozitív:
  - Builder és governance sebesség nő, kevesebb manuális hiba.
- Negatív / trade-off:
  - Extra security/compliance munka (prompt/log retention, vendor kockázat).
- Nyitott kérdések / későbbi feladat:
  - Model provider stratégia (self-host vs managed) és BYOC policy.
  - Prompt/telemetria retention és PII szabályok részletezése.

---

## D017 - Marketplace: telepíthető modulok/csomagok (verzió, kompatibilitás, supply-chain)

- Dátum: 2026-03-06
- Státusz: Accepted
- Érintett terület: extensibility, release, security

### Kontextus

Dobozos low-code platformnál a gyors értékesítés és újrafelhasználás kulcsa a telepíthető modulok/csomagok rendszere (screen/workflow/datasource csomagok, bounded context modulok). Enterprise környezetben a verziózás, kompatibilitás, jogosultság és supply-chain biztonság kötelező.

### Döntés

Marketplace-et vezetünk be telepíthető csomagokkal:
- First-party modulcsomagok:
  - a platformhoz szállított referencia domain modulok/csomagok (pl. WMS/logisztika, shipping, webshop integráció, POS, pénzügyi integráció), amelyek telepíthetők és verziózottak.
- Third-party marketplace csomagok:
  - külső vagy partner által készített csomagok, amelyekre szigorúbb supply-chain szabályok és review/validáció vonatkozik.
- Csomag tartalma:
  - definíciók (screen/workflow/datasource) és opcionális migrációk
  - metadata (név, verzió, kompatibilitás, függőségek, required roles)
- Telepítési szint:
  - installation szintű és/vagy tenant szintű telepítés policy alapján.
- Verziózás:
  - semver jellegű verziózás csomagokra
  - kompatibilitási szabályok a platform verziójához (D009/D011).
- Biztonság:
  - csomag aláírás/ellenőrzés (supply-chain)
  - telepítés RBAC-hoz kötött, auditált művelet.

### Következmények

- Pozitív:
  - Újrafelhasználás és termék-képességek gyors bővítése.
- Negatív / trade-off:
  - Kompatibilitás-mátrix és dependency menedzsment komplexitás.
- Nyitott kérdések / későbbi feladat:
  - Csomag formátum (pl. zip + manifest), aláírási kulcskezelés.
  - “Uninstall/rollback” politika (mit szabad eltávolítani és hogyan).

---

## D018 - Tenanton belül több jogi entitás és telephely (közös tenant DB)

- Dátum: 2026-03-06
- Státusz: Accepted
- Érintett terület: domain, multi-tenant, DB

### Kontextus

Az üzleti igény több jogi entitás/telephely kezelés egy installationen belül, intercompany tranzakciókkal. A domain és a low-code definíciók újrafelhasználhatósága miatt célszerű, hogy a tenanton belül több company és site együtt legyen kezelve, ugyanabban az OLTP adatbázisban.

### Döntés

- A tenant egy közös OLTP adatbázist kap.
- A tenanton belül több jogi entitás (company) és telephely (site) létezik.
- Minden üzleti objektum explicit companyId (és ha releváns: siteId) dimenzióval rendelkezik.
- Intercompany tranzakciók esetén az ellenoldal jelölése és az egyeztető referencia (intercompanyRef) kötelező.

Ez a döntés felülírja a D002 azon értelmezését, hogy a tenant DB per jogi entitás lenne.

### Következmények

- Pozitív:
  - Egyszerűbb definíció- és modul újrafelhasználás tenanton belül.
  - Intercompany folyamatok és kontrollok egységesen implementálhatók.
- Negatív / trade-off:
  - Extra domain fegyelem kell: minden lekérdezés/írás companyId/siteId szerint szűr.
  - Jogosultság és audit logika bonyolultabb (company/site scope).
- Nyitott kérdések / későbbi feladat:
  - Company/site RBAC modell és default scope szabályok.
  - Intercompany “kétoldali” bizonylat és státusz egyeztetés részletei.

---

## D019 - Külső könyvelés integráció: posting kontrakt + outbox + konfigurálható REST adapter

- Dátum: 2026-03-06
- Státusz: Accepted
- Érintett terület: integration, finance, reliability

### Kontextus

A könyvelés külső rendszerben történik, és a cél, hogy a WMS/logisztika (és később webshop/POS/shipping) domain eseményekből megbízható, auditálható feladás történjen. A célrendszer típusa változhat (auth, ack), ezért a platform oldalon stabil belső kontrakt és adapter-stratégia szükséges.

### Döntés

- Belső, rendszerfüggetlen posting kontraktot vezetünk be (journal entry jellegű payload): postingId, companyId, documentType/no/date, currency, lines, externalRefs, intercompanyRef.
- A postingokat outboxban tároljuk státuszokkal és idempotencia kulccsal:
  - pending/sending/sent/accepted/rejected.
- Külső könyvelés felé a default integráció REST adapter, amely konfiguráció alapján támogat legalább:
  - auth: OAuth2 client credentials vagy API key.
  - ack: szinkron (200/201) vagy aszinkron (202 + poll/webhook) jellegű feldolgozás.
- Retry/backoff szabályok: 5xx/network → retry; 4xx validation → rejected.

### Következmények

- Pozitív:
  - Rendszerfüggetlen és hosszú távon evolválható integráció.
  - Megbízható feladás (idempotencia, retry) és auditálhatóság.
- Negatív / trade-off:
  - Konfiguráció/mapping réteg szükséges company és postingKey/account megfeleltetéshez.
  - Több komponens (dispatcher/worker, outbox UI) és üzemeltetési feladat.
- Nyitott kérdések / későbbi feladat:
  - Mapping UI és “posting queue” admin képernyők implementációja.

---

## D020 - Posting kontrakt schema és mapping modell (külső könyveléshez)

- Dátum: 2026-03-06
- Státusz: Accepted
- Érintett terület: integration, data contract, governance

### Kontextus

A külső könyvelés REST integráció (D019) csak akkor lesz stabil és cserélhető célrendszerrel, ha a platformon belül a posting kontrakt minimál mezői és a megfeleltetés (mapping) szabványosított. Enélkül minden integráció “ad-hoc” lesz és a visszamenőleges kompatibilitás kezelhetetlenné válik.

### Döntés

Rögzítjük a posting kontrakt és a mapping minimumát:

- Posting kontrakt minimál mezők:
  - postingId (UUID, idempotencia kulcs)
  - companyId
  - documentType
  - documentNo
  - documentDate, postingDate
  - currency
  - externalRefs (kulcs-érték)
  - intercompanyRef (opcionális, de intercompany esetén kötelező)
  - lines[]:
    - lineNo
    - postingKey (belső standard kulcs)
    - debit/credit (vagy amount + dc)
    - costCenter/taxCode/project (opcionális dimenziók)
    - reference (opcionális)

- Mapping minimum (konfigurálható, auditált):
  - postingKey → external account (company scope szerint)
  - companyId → externalCompanyCode
  - partner/item opcionális megfeleltetések később bővíthetők, de a minimum nem követeli meg őket.

- Kompatibilitás:
  - a posting kontrakt kompatibilitási szabályai a platform verziójához kötöttek; breaking change csak explicit migrációval és verzióváltással.

### Következmények

- Pozitív:
  - Integrációk cserélhetők és tesztelhetők.
  - Konzisztens audit és hibakezelés.
- Negatív / trade-off:
  - Mapping UI/DSL és validációs réteg szükséges.
- Nyitott kérdések / későbbi feladat:
  - Mapping UI és validáció részletei (szabályok, defaultok, preview).

---

## D021 - Company/site RBAC és default scope

- Dátum: 2026-03-06
- Státusz: Accepted
- Érintett terület: security, runtime, domain

### Kontextus

Tenenton belül több company/site van (D018). Ha a hozzáférési modell nem determinisztikus, akkor az admin felületek és a runtime folyamatok adatszivárgáshoz, hibás riporthoz vagy kezelhetetlen permission konfigurációhoz vezethetnek.

### Döntés

Bevezetünk explicit company/site scope-ot a jogosultságokhoz:

- A szerepkör (role) mellett a felhasználónak van:
  - engedélyezett companyId lista (company scope)
  - opcionálisan engedélyezett siteId lista (site scope)

- Default scope szabály:
  - Ha a felhasználó pontosan 1 company-ra jogosult, az a default company.
  - Ha több company-ra jogosult, a kliensben explicit company választás szükséges (session scope), auditálható módon.
  - Site szűrés a kiválasztott company-n belül történik.

- API invariáns:
  - Minden domain műveletnél a companyId (és ha releváns: siteId) kötelező és ellenőrzött.
  - “Implicit” company/site csak akkor használható, ha a default scope egyértelmű.

### Következmények

- Pozitív:
  - Kezelhető permission modell és erős adatszűrés.
- Negatív / trade-off:
  - Több UI állapot és több tesztelendő kombináció.
- Nyitott kérdések / későbbi feladat:
  - Admin UX: company/site kiválasztás és “remember last selection” policy.

---

## D022 - Külső könyvelés REST integráció MVP szerződés (adapter contract)

- Dátum: 2026-03-06
- Státusz: Accepted
- Érintett terület: integration, reliability, operations

### Kontextus

A külső könyvelési rendszer felé történő feladás (D019) REST alapú, de a célrendszerek eltérhetnek auth és visszaigazolás (ack) mintázatban. Az MVP-hez szükség van egy minimál, normatív adapter szerződésre, hogy a dispatcher/outbox viselkedése determinisztikus és tesztelhető legyen.

### Döntés

Az alábbi REST integrációs szerződést vezetjük be (MVP minimum):

- Adapter konfiguráció (installation/tenant/company scope szerint):
  - baseUrl
  - authMode: oauth2_client_credentials | api_key
  - oauth2: tokenUrl, clientId, clientSecret, scope (opcionális)
  - apiKey: headerName, value
  - ackMode: sync | async

- Kimenő HTTP kérés (journal entry feladás):
  - Method: POST
  - URL: {baseUrl}/journal-entries
  - Header: Idempotency-Key = postingId
  - Body: a mappingelt posting payload (D020 alapján; companyId→externalCompanyCode, postingKey→account feloldva)

- Válasz (ack):
  - sync:
    - 200/201 = accepted, response tartalmazhat externalId-t
  - async:
    - 202 = accepted for processing, response tartalmazhat externalRequestId-t

- Outbox státusz szabály (D019-hez igazítva):
  - 200/201 → accepted
  - 202 → sent (awaiting external completion)
  - 4xx (validation/auth/forbidden) → rejected (nem automatikus retry)
  - 5xx/timeout/network → pending (retry backoff)

- Invariánsok:
  - Ugyanazzal a postingId-val csak logikailag azonos feladás történhet (idempotencia).
  - accepted és rejected végállapotok; ezekből automatikus átmenet nincs.
  - pending állapotból csak dispatcher által lehet sending/sent/accepted/rejected.

- Retry/backoff minimum:
  - exponenciális backoff + jitter
  - max retry count vagy max age (konfigurálható)

- Error contract (belső):
  - minden sikertelen kísérletnél rögzítjük: httpStatus, response body (truncált), errorCode, errorMessage, attempt count, nextRetryAt
  - hibák tenant+company kontextusban auditálhatók

### Következmények

- Pozitív:
  - Egységes és cserélhető REST adapter viselkedés.
  - Tesztelhető idempotencia és hibakezelés.
- Negatív / trade-off:
  - A célrendszer-specifikus eltérésekhez további adapter implementációk vagy mapping bővítések kellenek.
- Nyitott kérdések / későbbi feladat:
  - async ack véglegesítése: poll endpoint vs webhook szerződés célrendszer függően.

---

## D023 - Schema-first enforcement policy (soft vs hard gate)

- Dátum: 2026-03-07
- Státusz: Accepted
- Érintett terület: governance, runtime safety, DX

### Kontextus

A platform schema-first elvet követ (definíciók és integrációs kontraktok). Ennek értéke csak akkor realizálódik, ha van determinisztikus enforcement: hol warning, hol hard fail. AI-támogatott és kézi szerkesztés mellett különösen fontos a publish/runtime kapu.

### Döntés

Bevezetjük az alábbi enforcement policy-t minden definícióra és integrációs kontraktra (screen/workflow/datasource definíciók, posting kontrakt, REST adapter payloadok):

- Szerkesztés/Save (builder):
  - soft enforcement: schema-validáció hibák és kompatibilitási figyelmeztetések megjelennek, de a draft mentés megengedett.

- Publish/Promote (definíció kiadás runtime-ba):
  - hard gate: csak schema-validált artefakt publikálható.
  - hard gate: breaking change csak major verzió emeléssel és explicit migrációs/upgrade tervvel engedett.

- Runtime load/execute:
  - hard gate: a runtime csak schema-validált, támogatott definition schema verziójú artefaktot futtathat.
  - invalid esetben determinisztikus error contracttal elutasít.

- Kompatibilitási minimum:
  - backward compatible változás: új optional mező, enum bővítés (explicit policy szerint), új endpoint optional paraméterrel.
  - breaking change: mező átnevezés/törlés, típusváltás, kötelezővé tétel, jelentés-változtatás.

- Error contract minimum (validációs hibákra):
  - gépileg feldolgozható hibatípus + path (JSON pointer), message, severity (warning/error), schemaVersion.

### Következmények

- Pozitív:
  - Runtime stabilitás és biztonság nő; AI javaslatok kontrolláltan “keretbe” kerülnek.
  - Marketplace és upgrade kompatibilitás kezelhető.
- Negatív / trade-off:
  - Több governance/validációs munka (schema verziózás, compat check).
- Nyitott kérdések / későbbi feladat:
  - Enum bővítés kompatibilitási policy részletezése (kliens oldali viselkedés).

---

## D024 - Egységes error contract (API + runtime + builder)

- Dátum: 2026-03-07
- Státusz: Accepted
- Érintett terület: DX, support, observability

### Kontextus

Enterprise low-code platformnál a hibakezelésnek skálázhatónak kell lennie support és automatizmus (retry, triage) szempontból. A platform több “felületen” hibázhat (API, runtime workflow, builder validáció), ezért egységes error contract kell.

### Döntés

Egységes error contractot vezetünk be minden publikus felületen (API válasz, runtime futás eredmény, builder validáció):

- Minimum mezők:
  - errorCode (stabil, gépi feldolgozásra alkalmas)
  - message (emberi rövid leírás)
  - traceId (request/operation trace)
  - details[] (opcionális): { path, code, message, severity }
  - timestampUtc

- Szabályok:
  - errorCode nem változtatható visszamenőleg (breaking change-nek számít).
  - A validation hibák `details[].path` mezője JSON pointer jellegű legyen.
  - Minden hiba logolása a traceId-val kötelező.

### Következmények

- Pozitív:
  - Egységes UI kezelés és automatizált support/triage.
- Negatív / trade-off:
  - Extra fegyelem: errorCode szótár és verziózás.

---

## D025 - Idempotencia + retry policy (platform-szint)

- Dátum: 2026-03-07
- Státusz: Accepted
- Érintett terület: reliability, integration, workflow runtime

### Kontextus

Workflow futtatás, datasource commandok és külső integrációk (outbox/adapter) esetén a “legalább egyszer” végrehajtás reális. Ennek biztonságos kezelése idempotencia és determinisztikus retry nélkül duplikációhoz vagy adatvesztéshez vezet.

### Döntés

- Idempotencia:
  - minden integrációs és állapotváltoztató művelethez explicit idempotency key tartozik (pl. postingId, operationId, workflowRunId+stepId).
  - ugyanazzal a kulccsal csak logikailag azonos művelet hajtható végre.

- Retry osztályozás:
  - 4xx validation/forbidden → nem automatikus retry (rejected/final error)
  - 5xx/timeout/network → automatikus retry (backoff + jitter)

- Backoff minimum:
  - exponenciális + jitter, max retry count vagy max age alapján leáll

- Végállapotok:
  - accepted/succeeded és rejected/failed végállapotok; ezekből automatikus átmenet nincs

### Következmények

- Pozitív:
  - Párhuzamos és újrapróbálkozós futásoknál is kontrollált viselkedés.
- Negatív / trade-off:
  - Idempotency key menedzsment és deduplikáció implementációs költség.

---

## D026 - Audit + retention minimum (platform baseline)

- Dátum: 2026-03-07
- Státusz: Accepted
- Érintett terület: compliance, operations

### Kontextus

Auditálhatóság és adat-életciklus (archív/read-only) enterprise környezetben nem “később ráér”. A platformon belül a definíciók és admin műveletek változtatják a futtatott rendszert, ezért kötelező audit baseline kell.

### Döntés

- Kötelező audit események (minimum):
  - definíció publish/promote, rollback
  - RBAC/role változás
  - schema change: preview/approval/apply
  - integration/outbox: manuális retry/requeue és státusz override (ha van)

- Audit rekord minimum:
  - actor (user/service), action, target, tenantId, companyId/siteId (ha releváns), timestampUtc, traceId

- Retention/archival elv:
  - lezárt projektek/időszakok read-only módba tehetők; archív snapshot immutábilis.
  - retention policy léte kötelező (számok nélkül is), és nem ad-hoc.

### Következmények

- Pozitív:
  - Compliance és incident visszakövethetőség.
- Negatív / trade-off:
  - Audit tárolás és kereshetőség üzemeltetési költség.

---

## D027 - Definition versioning + breaking change policy (screens/workflows/datasources)

- Dátum: 2026-03-07
- Státusz: Accepted
- Érintett terület: definition store, governance, marketplace

### Kontextus

A screen/workflow/datasource definíciók a platform “futtatható szerződései”. Mivel ezek csomagolhatók, verziózhatók és telepíthetők (D017), szükség van normatív verziózási és kompatibilitási szabályokra, különben a publish/promote és upgrade kezelhetetlen.

### Döntés

- Verziózás:
  - Minden definíció és csomag semver jellegű verziót kap (major.minor.patch).
  - A publish/promote mindig konkrét verzióra történik (pinning), nem “latest”-re.

- Kompatibilitás (minimum):
  - patch: bugfix, metadata/description változás, viselkedés nem törhet.
  - minor: új optional mezők/feature flaggel bevezetett új capability; backwards compatible.
  - major: breaking change.

- Breaking change példák:
  - mező törlés/átnevezés, típusváltás, kötelezővé tétel
  - workflow step jelentésének vagy default értékeinek incompat változtatása
  - datasource query/command kontrakt megváltoztatása, ami a runtime flow-kat törheti

- Deprecation rend:
  - breaking change előtt deprecate jelzés (legalább 1 release ciklus), és builder warning.
  - Runtime csak támogatott definition schema verziót futtathat (D023).

### Következmények

- Pozitív:
  - Promote/rollback és marketplace kompatibilitás kezelhető.
- Negatív / trade-off:
  - Verzió és compat check implementációs overhead.

---

## D011 - Supported version enforcement részletes szabályai

- Dátum: 2026-03-06
- Státusz: Accepted
- Érintett terület: release, support, operations

### Kontextus

D009 kimondja, hogy csak az aktuális verzió támogatott. Ehhez technikai és operációs enforcement kell, különben a policy nem tartható.

### Döntés

- A supported verzió definíciója: installation szinten a vendor által kijelölt “current” kiadás (exact verzió, nem tetszőleges régi LTS).
- Upgrade ablak: 60 nap a release dátumától.
- Enforcement:
  - 0–30 nap: warning (UI + admin API header).
  - 30–60 nap: soft block a builder/admin funkciókra (mentés/promote/schema change csak read-only módban).
  - 60+ nap: hard block az admin műveletekre; runtime read-only mód fenntartható az archív és megtekintési use-case-ekhez.
- Incident kezelés: ha az issue a current verzióban javított, akkor az upgrade a prerequisite.

### Következmények

- Pozitív:
  - Support skálázható és biztonsági fixek gyorsan átérnek.
- Negatív / trade-off:
  - Ügyfél oldali upgrade fegyelem nélkül konfliktusos lehet.
- Nyitott kérdések / későbbi feladat:
  - Kivétel-kezelés (pl. jogi freeze időszak) és dokumentált waiver folyamat.

---

## D012 - SLO / RTO / RPO minimum baseline

- Dátum: 2026-03-06
- Státusz: Accepted
- Érintett terület: reliability, DR, operations

### Kontextus

Enterprise readiness-hez kell egy minimum üzemfolytonossági cél, különben a backup/restore, archív és upgrade tervezés “vaktában” történik.

### Döntés

Minimum baseline célok installation szinten:
- SLO (API): 99.9% havi rendelkezésre állás (tervezett karbantartási ablakokkal).
- RTO: 4 óra.
- RPO: 15 perc.

### Következmények

- Pozitív:
  - DR és runbookok objektíven tervezhetők.
- Negatív / trade-off:
  - Megnő az infra és operációs igény (backup, monitoring, drill).
- Nyitott kérdések / későbbi feladat:
  - Tenant/holding szintű “close” időszakokra eltérő SLA kell-e.

---

## D013 - BYOC vegyes üzemeltetés: határok, telemetria, break-glass

- Dátum: 2026-03-06
- Státusz: Accepted
- Érintett terület: compliance, operations, support

### Kontextus

Vegyes üzemeltetésnél a saját felhőben vendor-managed, ügyfél felhőben customer-managed. Ennek határait és a támogatás technikai eszközeit rögzíteni kell (különben a support és az upgrade policy nem tartható).

### Döntés

- Saját felhő (vendor-managed): teljes operáció (monitoring, upgrade, incident) a vendor felelőssége.
- Ügyfél felhő (customer-managed):
  - a vendor “break-glass” hozzáférés alapértelmezésben nincs,
  - opcionálisan, explicit szerződéssel engedélyezhető időszakos diagnosztikai hozzáférés,
  - vendor telemetria: csak anonimizált/aggregált metrikák és verzió/health állapot, üzleti adat nélkül.
- Upgrade:
  - vendor biztosít release artefaktot + runbookot,
  - customer-managed esetben az upgrade végrehajtása az ügyfél felelőssége (vendor támogatással, policy szerint).

### Következmények

- Pozitív:
  - Compliance-kompatibilis BYOC modell.
- Negatív / trade-off:
  - Customer-managed környezetben lassabb hibaelhárítás, több training/runbook igény.
- Nyitott kérdések / későbbi feladat:
  - Break-glass jóváhagyási workflow (audit, time-box, scope).
