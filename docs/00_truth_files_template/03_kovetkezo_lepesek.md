# Következő lépések (TRUTH)

> **Enterprise megjegyzés:** A **futó repó** aktív fókusza: `docs/live/03_kovetkezo_lepesek.md`. Ez a fájl **sablon**; WIP=1 és folyamat: `docs/DEVELOPMENT_WORKFLOW.md`.

## 0) Canon / használati szerződés

Ez a fájl a **következő fejlesztési lépések** kanonikus forrása **a sablon készletben**.

Szabályok:
- Irányadó: a 4 truth fájl ebben a mappában + a `docs/live/03_kovetkezo_lepesek.md` operatív tartalom (nem ellentmondásban a `DEVELOPMENT_WORKFLOW.md`-del).
- WIP limit: egyszerre **pontosan 1** aktív fókusz lehet (WIP=1). Ha **biztonságosan összevonható** több kisebb feladat (alacsony kockázat, no behavior change jelleg, ugyanazon a területen), akkor összevonható egy menetbe. Az összevonást mindig automatikusan mérlegelni kell, és a döntést a fejlesztő asszisztens hozza meg, külön jóváhagyás kérése nélkül. Ha kockázatos, marad az 1 kijelölt feladat fejlesztése.
- A fókusz csak itt változhat (nem chatben, nem issue-ban, nem “ma ezt érzem”).
- Ha új munka felmerül, előbb ide kerül “Intake” státuszba, és csak ezután lehet fejleszteni.

## 1) Aktív fókusz (WIP=1)

- [ACTIVE] Release/upgrade pipeline és supported-version enforcement implementálása (D009/D011), runbookokkal és restore drill-lel.

## 2) Következő 3-7 lépés (prioritási sorrend)

1) Release cadence és upgrade ablak megvalósítása a gyakorlatban (D011):
   - warning/soft-block/hard-block állapotok
   - admin UI jelzések + API enforcement
2) Migráció orchestration + canary/wave rollout (installation szinten):
   - állapotgép (pending/running/succeeded/failed)
   - automatikus retry és manuális beavatkozás pontok
3) Runbook csomag (vendor-managed és customer-managed BYOC):
   - upgrade, rollback (ha engedett), restore
   - incident triage + diagnosztika
4) DR baseline validáció (D012):
   - backup/restore drill, RTO/RPO mérés
   - minimum monitoring/alerting küszöbök
5) Governance pipeline (definition promote + schema preview/approval/apply/audit) részletezése és implementációs tervre bontása

## 3) Intake / parkoló

- “Schema builder” felület bővítése: index/FK változtatások és ordering (PK drop/recreate) governance mellett.
- Cross-tenant admin UI (holding console) wireframe + RBAC.
- Tenant provisioning automatizmus (seed, baseline, drift check).
- Reporting: self-service exportok, warehouse integráció.
- Multi jogi entitás + telephely (company/site) scope szabályok és RBAC modell kidolgozása (D018).
- Külső könyvelés integráció MVP: posting kontrakt schema + outbox + REST adapter + posting queue admin UI (D019).
- AI-assisted builder/governance backlog: scope és adatkezelési policy implementáció (D016).
- Marketplace backlog: csomag formátum + aláírás + telepítés + kompatibilitás (D017).

## 4) Definíció: Kész (Mini-DoD)

Egy lépés akkor “kész”, ha:
- a változás össze van kötve a vízióval (link a `00_vizio.md` megfelelő részére),
- ha döntést igényelt, akkor van róla bejegyzés a `01_dontesek.md`-ben,
- ha publikus kontrakt/definíció érintett, akkor a schema-first enforcement és baseline policy-k teljesülnek (D023–D026),
- build OK és minimális smoke OK (ami az adott lépéshez releváns),
- az `02_allapot.md` frissítve van (ha a lépés érdemben változtat a státuszon).
