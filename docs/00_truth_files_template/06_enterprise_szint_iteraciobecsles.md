# Enterprise szint elérésének iteráció-becslése (greenfield)

## 0) Mire vonatkozik ez a becslés?

Ez a dokumentum egy **irányadó** becslés arra, hogy hány iteráció (pl. 2 hetes sprint) után érhető el az "enterprise product-ready" szint egy greenfield low-code ERP platformnál.

## Executive summary (1 oldal)

Feltételek:
- 1 sprint = 2 hét
- Csapat: 3–5 fő
- Üzemeltetés vegyes: saját felhőben vendor-managed, ügyfél felhőben customer-managed

### Platform enterprise-ready (platform fókusz)

| Üzemeltetési profil | Sprint (reális sáv) | Idő (reális sáv) |
|---|---:|---:|
| Vendor-managed domináns | 20–24 | ~10–12 hónap |
| Customer-managed domináns | 24–28 | ~12–14 hónap |

### Platform + ERP domain enterprise-ready (GL/WMS + konszolidáció + archival)

| Üzemeltetési profil | Sprint (reális sáv) | Idő (reális sáv) |
|---|---:|---:|
| Vendor-managed domináns | 42–48 | ~21–24 hónap |
| Customer-managed domináns | 48–56 | ~24–28 hónap |

Megjegyzések (mi tolja az időt):
- D009 (csak aktuális verzió supported) miatt a release/upgrade/migráció orchestration és runbookok kritikusak.
- Customer-managed környezetben több idő megy el supportability-re (diagnosztika, remote support policy, training, upgrade fegyelem).
- A domain (GL/WMS) “close/audit/invariáns” jellegű, ezért a platform mellett jelentős extra iterációt igényel.
- AI-assisted builder/governance (D016) és marketplace (D017) képességek tervezettek, de ebben a becslésben csak akkor számolunk velük, ha explicit scope-ként bekerülnek a kritikus útba.

### Kétpályás fejlesztés: dobozos motor vs domain (screens/workflows/datasources)

Ha a low-code **motor (engine/platform)** fejlesztése külön track, és a **domain app tartalom** (képernyők, workflow-k, datasource-ok, ERP szabályok) külön track, akkor az iterációk “másképp” értendők:

- A motor track adja a **kritikus utat** az enterprise platform-ready küszöbig (security, release/upgrade, tenancy, observability).
- A domain track már korán el tud indulni, de csak olyan ütemben halad élesbe, amilyen ütemben a motor eléri:
  - stabil definíciós sémát,
  - verziózást/rollback-et,
  - éles kiadási pipeline-t (D009 miatt).

Praktikus következmény:
- A korábbi “platform+domain” becslés (pl. 42–56 sprint) **összideje csökkenhet**, mert a domain egy része párhuzamosan készül.
- Viszont a “platform enterprise-ready” küszöb (20–28 sprint) **nem nagyon rövidül**, mert ez jellemzően rendszerszintű munka.

Durva, 3–5 fős csapatra és vegyes üzemeltetésre (reális sáv) adott kétpályás becslés:

| Mérföldkő | Mit jelent | Sprint (reális) | Idő |
|---|---|---:|---:|
| Engine platform-ready | enterprise platform baseline (security+upgrade+obs+quality+core runtime) | 20–28 | ~10–14 hónap |
| Domain “first usable” | 1–2 kritikus end-to-end folyamat képernyő+workflow+datasource szinten, korlátozott scope-pal | +8–12 (párhuzamosan) | jellemzően a 12–18. hónap körül kezd használhatóvá válni |
| Domain enterprise-ready | GL/WMS + konszolidáció + archival a cél szintre hozva | 42–56 (de párhuzamosítható) | ~21–28 hónap |

Értelmezés:
- A “Domain first usable” nem egy plusz blokk a végén, hanem **párhuzamos track**, ami akkor válik ténylegesen deployolhatóvá, amikor az engine elég stabil (release/upgrade + versioning).
- Ha a csapatból 1–2 fő folyamatosan domainen dolgozik, miközben 2–3 fő engine-en, akkor a **first usable domain** megjelenhet jóval a teljes domain-ready előtt.

Nem a jelenlegi kódbázisból indul ki, hanem a `05_roadmap_greenfield_enterprise.md` fázisaiból és a truth file döntéseiből:
- holdingonként külön installation (BYOC)
- nincs cross-holding riport
- csak a kijelölt (aktuális) verzió supported

## 1) Mit nevezünk itt “enterprise product-ready”-nek? (küszöb)

A küszöb akkor tekinthető teljesítettnek, ha legalább az alábbiak igazak:

- Security baseline:
  - authN/authZ rendben, RBAC és audit log alapok
  - tenant isolation guardrail-ok (request + background job + cache)
- Release/upgrade alapok (a supported version policy miatt kritikus):
  - canary/wave rollout installation szinten
  - migráció orchestration és visszaállási (rollback/restore) runbook
  - supported version enforcement (policy technikailag érvényesül)
- Observability:
  - strukturált log + trace-id end-to-end
  - alap metrikák és riasztás (error rate, latency)
- Quality gates:
  - CI-ben build + tesztek + minimál security scanning
- Runtime core használható:
  - definíciók (screen/workflow/datasource) verziózva, legalább minimál futtatási képesség

Megjegyzés: a teljes GL/WMS domain teljessége nem feltétele az enterprise “platform” readiness-nek, de a platformot úgy kell felépíteni, hogy a domain később biztonságosan bővíthető legyen.

## 2) Iteráció egység és csapatfeltételezés

A becsléshez egy tipikus iterációt feltételezünk:
- 1 iteráció = 2 hét

Alap sávok (általános): a számok erősen függnek a csapatmérettől és senioritástól. Ezért 3 sávot adok meg:
- Optimista: tapasztalt csapat, tiszta döntések, kevés újratervezés
- Reális: átlagos enterprise fejlesztés, néhány újragondolás
- Pesszimista: bizonytalan scope, sok compliance kör, több újraírás

### 2.1) Pontosítás a te esetedre (3–5 fő + vegyes üzemeltetés)

Input:
- Csapatméret: 3–5 fő.
- Üzemeltetés:
  - saját felhőben futó installationök: vendor-managed (ti üzemeltettek)
  - ügyfél felhőben futó installationök: customer-managed (ők üzemeltetnek)

Hatás:
- 3–5 fős csapatnál a “reális” sáv jellemzően a korábbi reális érték körül alakul, de a vegyes üzemeltetés miatt plusz idő kell a:
  - runbookok, diagnosztika, supportability és “remote support” folyamatok miatt,
  - upgrade fegyelem (D009) kikényszerítéséhez customer-managed környezetben.

A továbbiakban adok egy szűkebb, “3–5 fős reális” becslést is.

## 3) Fázis -> iteráció becslés (2 hetes sprintek)

A fázisok a `05_roadmap_greenfield_enterprise.md` alapján:

### F0 – Product foundation
- Optimista: 1 sprint
- Reális: 2 sprint
- Pesszimista: 3 sprint

### F1 – Control plane + installation lifecycle
- Optimista: 2 sprint
- Reális: 3 sprint
- Pesszimista: 5 sprint

### F2 – Tenant plane alapok (isolation + RBAC)
- Optimista: 2 sprint
- Reális: 4 sprint
- Pesszimista: 6 sprint

### F3 – Definition platform core (screens/workflows/datasources)
- Optimista: 3 sprint
- Reális: 5 sprint
- Pesszimista: 8 sprint

### F4 – Release & upgrade rendszer + supported version enforcement
- Optimista: 2 sprint
- Reális: 4 sprint
- Pesszimista: 6 sprint

### F8 – Quality gates + compliance hardening (explicit fókusz)
- Optimista: 1 sprint
- Reális: 2 sprint
- Pesszimista: 4 sprint

## 4) Összesített becslés az “enterprise platform-ready” küszöbig

A minimálisan szükséges fázisok: F0 + F1 + F2 + F3 + F4 + F8.

- Optimista: 1 + 2 + 2 + 3 + 2 + 1 = **11 sprint** (~22 hét, ~5-6 hónap)
- Reális: 2 + 3 + 4 + 5 + 4 + 2 = **20 sprint** (~40 hét, ~9-10 hónap)
- Pesszimista: 3 + 5 + 6 + 8 + 6 + 4 = **32 sprint** (~64 hét, ~15-16 hónap)

### 4.1) 3–5 fős csapat – szűkített reális sáv (vegyes üzemeltetéssel)

Az alap “reális” 20 sprintet tekintve:
- Vendor-managed domináns (több saját felhős installation, kevesebb customer-managed): **20–24 sprint** (~10–12 hónap)
- Customer-managed domináns (sok ügyfél felhős installation, szigorú runbook/training): **24–28 sprint** (~12–14 hónap)

Megjegyzés: a különbség főleg F4 (upgrade/release), F1 (installation lifecycle) és F8 (quality/compliance) területeken jelenik meg.

## 5) Mikor lesz “enterprise ERP product-ready” (GL/WMS is éles szint)?

Ha nem csak a platform, hanem a GL/WMS domain is enterprise szintre kerül (multi-currency, intercompany, close, konszolidáció holdingon belül, archival), akkor tipikusan hozzáadódik:

- F5 (Customization & schema governance):
  - Optimista: +3 sprint
  - Reális: +6 sprint
  - Pesszimista: +10 sprint

- F6 (Intercompany + multi-currency domain baseline):
  - Optimista: +4 sprint
  - Reális: +8 sprint
  - Pesszimista: +14 sprint

- F7 (Consolidation + archival/read-only):
  - Optimista: +4 sprint
  - Reális: +8 sprint
  - Pesszimista: +12 sprint

Összesen (platform-ready + domain-ready):
- Optimista: 11 + (3+4+4) = **22 sprint** (~44 hét, ~10-11 hónap)
- Reális: 20 + (6+8+8) = **42 sprint** (~84 hét, ~19-21 hónap)
- Pesszimista: 32 + (10+14+12) = **68 sprint** (~136 hét, ~31-32 hónap)

### 5.1) 3–5 fős csapat – szűkített reális sáv (platform + domain)

Kiinduló alap: **42 sprint**.

Vegyes üzemeltetés hatása (tipikusan):
- Vendor-managed domináns: **42–48 sprint** (~21–24 hónap)
- Customer-managed domináns: **48–56 sprint** (~24–28 hónap)

Miért ilyen nagy a szórás?
- A domain (GL/WMS) önmagában is sok “definíció + policy + audit + close” jellegű kényszert hoz.
- Customer-managed környezetben a supportability (diagnosztika, upgrade fegyelem, incident runbook) aránya nő, és a hibajavítás visszacsatolása lassabb.

## 6) Mi rövidíti / mi hosszabbítja meg a valóságban?

Rövidíti:
- fix döntések korán (ADR-ek), kevés scope csúszás
- managed szolgáltatások (DB, observability, CI) tudatos választása
- pilot holding (1 installation) mint canary

Hosszabbítja:
- “active-active multi-cloud” igény
- egyedi tenant DDL tömegesen (drift)
- sok compliance kör (audit, pen test) későn bevonva

## 7) Ajánlott iterációs mérföldkő-ritmus

- Minden 2. sprint végén: “release rehearsal” (upgrade/migráció próba) egy canary installationön.
- Minden 4. sprint végén: restore drill (RTO/RPO validálás).

## 8) Következő döntési kérdés, hogy pontosítsunk

A fenti becslés a megadott (3–5 fő, vegyes üzemeltetés) input alapján frissítve van.

Ha tovább szeretnéd szűkíteni a sávot, a következő 2 paraméter számít a legtöbbet:
- hány installation lesz tipikusan (1 pilot + N ügyfél) az első 12 hónapban,
- az ügyfél felhőben mennyire engedélyezett a vendor “break-glass” (diagnosztika/upgrade segítség) és mennyire “air-gapped”.
