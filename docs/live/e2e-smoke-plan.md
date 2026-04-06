# E2E smoke — Playwright (MVP kész)

## Cél

Minimális **end-to-end** ellenőrzés a low-code demó útvonalon: böngésző → Angular dev szerver → proxy → backend, **egy** stabil forgatókönyv, **CI-ben** is fut (headless Chromium).

## Eszköz

**Playwright** (`@playwright/test`) — `frontend/` devDependency; tesztek: `frontend/e2e/*.spec.ts`; konfig: `frontend/playwright.config.ts`.

## Előfeltételek (lokális)

| Szolgáltatás | Port | Megjegyzés |
|--------------|------|------------|
| Backend | `5002` | `dotnet run --launch-profile http` a `backend/` mappában |
| Frontend | `4200` | `npm start` a `frontend/`-ben; `proxy.conf.json` → `/api` |

Első futás előtt böngésző motor: `npm run e2e:install-browsers` (a `frontend/` könyvtárban).

## Parancsok

| Parancs | Mit csinál |
|---------|------------|
| `bash scripts/e2e-smoke-ci.sh` | Elindítja a backendet + `ng serve`-et, vár az **health** + **dev szerver** URL-re, majd `PW_NO_WEBSERVER=1 npm run e2e:smoke` (csak `e2e/smoke.spec.ts`; ajánlott **Git Bash** / Linux / macOS). |
| `powershell -File scripts/e2e-smoke-ci.ps1` | Ugyanaz, **Windows PowerShell 5.1+** vagy **pwsh** (a repo gyökeréből; `npm` és `dotnet` a PATH-on). |
| `npm run e2e` | Összes Playwright teszt az `e2e/` alatt. Ha **nincs** `PW_NO_WEBSERVER`, a konfig megpróbálja saját `webServer` blokkal indítani a `dotnet run` + `ng serve`-et (lokálisan ez néha kényelmetlen; CI **nem** ezt használja). |
| `npm run e2e:smoke` | Csak a **smoke** fájl (`e2e/smoke.spec.ts`) — ezt futtatja a CI script is. |
| `PW_NO_WEBSERVER=1 npm run e2e:smoke` | Csak smoke — **előbb** kézzel indítsd a 5002-es backendet és a 4200-as dev szervert két terminálban. |

**Megjegyzés (Windows):** az `ng serve` gyakran **`http://localhost:4200`**-on válaszol; a **`127.0.0.1:4200`** nem mindig ugyanaz a stacken. A Playwright **`baseURL`** és a CI script **`localhost:4200`**-at használ.

## Forgatókönyvek (smoke) — kész

1. **`/`** → átirányítás **`/lowcode/workflows`**, **Workflows** fejléc.
2. **`/lowcode/workflows`** — **Workflows** + **New** link.
3. **`/lowcode/entities`** — **Entities** fejléc + **All runs** link a `main` tartalomban (több „Workflows” link elkerülése: strict mode).
4. **`/lowcode/workflow-runs`** — **Workflow runs (tenant)** fejléc + **Entities** link a `main` tartalomban.
5. **`/lowcode/auth`** — **Low-code Auth (dev)** fejléc (BFF/OIDC gombok nélküli teljes login flow nélkül).
6. **`/lowcode/auth/callback`** — **Vissza az auth oldalra** link (OIDC callback shell; teljes token flow nem része a smoke-nak).
7. **`/lowcode/workflows/new`** — **New workflow** fejléc.
8. **`/lowcode/entities/new`** — **New entity** fejléc.
9. **`/lowcode/admin/tenants`** — **Admin / Tenants** fejléc (API hiba esetén is megjelenik a shell).
10. **`/upgrade`** — **Upgrade** fejléc.
11. **`/lowcode/workflows/{uuid}`** — **Workflow** fejléc + **← Workflows** (nem létező azonosító: API hiba, shell megmarad).
12. **`/lowcode/runs/{uuid}`** — **Workflow run** fejléc + **← Workflows** (ugyanígy).
13. **`/lowcode/entities/{uuid}`** — **Entity** fejléc + **← Entities** (ugyanígy).
14. **`/lowcode/entities/{uuid}/records`** — **Entity Records** fejléc + **← Entities** (ugyanígy).

A **11–14** tesztek egy rögzített, üres adatbázisban nem létező UUID-t használnak (`00000000-0000-0000-0000-000000000001`); így nincs szükség seedre, és a részletes útvonalak is lefedettek.

## CI

- Workflow: **`.github/workflows/ci.yml`** — job **`frontend-e2e`** (a **`frontend-quality`** után; a **Docker** job erre is vár).
- Script: **`scripts/e2e-smoke-ci.sh`** — `npx playwright install --with-deps chromium` a jobban, majd a script (`npm run e2e:smoke`). Lokálisan Windowson: **`scripts/e2e-smoke-ci.ps1`** (bash nélkül).

## Következő lépések (backlog)

- BFF / OIDC **happy path** (IdP round-trip, ha van stabil teszt IdP / mock).
- Részletes oldalak **létező** erőforrással (seed / fixture) — ha kell mélyebb E2E.

## DoD (E2E iteráció — MVP)

- `package.json` script: `npm run e2e`, `npm run e2e:smoke` (CI / smoke), `npm run e2e:install-browsers`.
- CI-ben zöld **`frontend-e2e`** job.
- Új dependency: **`@playwright/test`** — governance szerint dependency review / jóváhagyás; lásd `docs/GOVERNANCE.md`.

## Kapcsolódó

- Közreműködői parancsok: [`CONTRIBUTING.md`](../../CONTRIBUTING.md) · [`frontend/README.md`](../../frontend/README.md)
- Termék útvonalak: [`roadmap-iter-67-product.md`](roadmap-iter-67-product.md)
- Minőség 68 (API szerződés): [`roadmap-iter-68-quality.md`](roadmap-iter-68-quality.md)
