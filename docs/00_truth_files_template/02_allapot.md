# Állapot (TRUTH)

> **Enterprise megjegyzés:** A **futó repó** aktuális állapotának követése: `docs/live/02_allapot.md`. Ez a fájl **sablon / blueprint** másolathoz; a kanonikus folyamat: `docs/DEVELOPMENT_WORKFLOW.md`, `docs/GOVERNANCE.md`.

## 0) Canon / használati szerződés

Ez a fájl a projekt **pillanatnyi állapotának** egyetlen kanonikus forrása **a sablon készletben**.

Szabályok:
- Stratégiai truth: a `docs/00_truth_files_template/` alatti 4 truth fájl + a `docs/live/` operatív másolatok (lásd `GOVERNANCE.md`).
- Ez a dokumentum nem “terv”, hanem “műszerfal”.
- Csak olyan állítást írj ide, amit ellenőrizni tudsz (build, teszt, smoke, mérőszám, demo link, verzió).
- Ha a valóság eltér, ezt a fájlt kell frissíteni.

## 1) Snapshot

- Dátum: 2026-03-07
- Verzió / commit: N/A (koncepció / blueprint állapot)
- Környezet: local/dev/stage/prod

## 2) Rövid összefoglaló (1-2 bekezdés)

A blueprint (vízió + alap architektúra döntések) rögzítve: multi-tenant control plane + tenant plane, DDD ERP (GL/WMS) irány, customization stratégia (custom fields alap + governált DDL), konszolidáció külön store-ban, intercompany és multi-currency mint elsőrendű igények, archival/read-only modell.

Kiegészítő döntések rögzítve: tenanton belül több jogi entitás és telephely közös tenant DB-ben (D018), valamint külső könyvelés integráció posting kontrakt + outbox + konfigurálható REST adapter mintával (D019).

A fejlesztési keretrendszer-elvek is rögzítve: schema-first governance és enforcement (D023), egységes error contract (D024), platform-szintű idempotencia/retry policy (D025), audit+retention baseline (D026).

Tervezett kiegészítők szintén rögzítve: AI-assisted builder/governance (D016) és marketplace (telepíthető csomagok) (D017), de ezek még nem kerültek implementálásra.

Implementációs szinten a részletes domain szabályok, adatfolyamok (outbox/CDC), release/promote folyamat, valamint a governance (approval, audit, rollback policy) még részletezendő és iteratív prototipizálást igényel.

## 3) Egészség / build & smoke

- Backend build: Unknown
- Frontend build: Unknown
- DB migrations: Unknown
- Alap smoke: Unknown

## 4) Modul / komponens érettség (durva %)

- Runtime (screen/workflow execution): 40%
- Builder UI (screen/workflow/datasource): 35%
- Permissions / RBAC: 30%
- Multi-tenant (izoláció, provisioning): 45%
- Schema evolúció / customization: 30%
- Observability (logs/trace/error contract): 25% (policy rögzítve, implementáció még hiányos)

## 5) Legnagyobb kockázatok (Top 5)

1) Governance hiányosságok schema change és definition release körül (approval, audit, rollback).
2) Konszolidációs adatfolyam késői érkezések / idempotencia / újrajátszás (rebuild) komplexitása.
3) Intercompany + multi-currency domain szabályok pontatlansága (rate locking, close, eliminációk).
4) Tenant izoláció “rejtett” pontjai (cache, background jobs, logs, shared infra).
5) Read-only archival megoldás üzemeltetési költsége (snapshot kezelés, retention, kereshetőség).
6) AI és marketplace compliance/supply-chain kérdések (adatkezelés, audit, csomag aláírás) pontosítása és későbbi implementációja.
7) Külső könyvelés integráció helytelen idempotencia/retry/error contract miatti duplikáció vagy adatvesztés (D019/D024/D025).

## 6) Utolsó fontos változások

- 2026-03-06: Truth file-ok és alap blueprint döntések rögzítve.
- 2026-03-07: Schema-first enforcement, error contract, idempotencia/retry, audit+retention policy-k rögzítve (D023–D026).

## 7) Nyitott döntések / blokkolók (ha vannak)

- Konszolidáció store technológia és adatmodell (read model vs warehouse) (roadmap: F8).
- Custom fields fizikai tárolás standardja (indexelés, riportolhatóság) (roadmap: F5).
- Release/promote mechanika (verzió pinning, környezetek közti diff) (roadmap: F4).
- AI provider stratégia és BYOC policy részletek (D016) (roadmap: F3).
- Marketplace csomag formátum + aláírás + uninstall/rollback policy (D017) (roadmap: F4/F8).
- Posting queue admin UX és mapping UI/validáció részletei (D019/D020) (roadmap: F6).
