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
| `bash scripts/e2e-smoke-ci.sh` | Elindítja a backendet + `ng serve`-et, vár az **health** + **dev szerver** URL-re, majd `PW_NO_WEBSERVER=1 npm run e2e` (ajánlott **Git Bash** / Linux / macOS). |
| `npm run e2e` | Playwright teszt. Ha **nincs** `PW_NO_WEBSERVER`, a konfig megpróbálja saját `webServer` blokkal indítani a `dotnet run` + `ng serve`-et (lokálisan ez néha kényelmetlen; CI **nem** ezt használja). |
| `PW_NO_WEBSERVER=1 npm run e2e` | Csak teszt — **előbb** kézzel indítsd a 5002-es backendet és a 4200-as dev szervert két terminálban. |

**Megjegyzés (Windows):** az `ng serve` gyakran **`http://localhost:4200`**-on válaszol; a **`127.0.0.1:4200`** nem mindig ugyanaz a stacken. A Playwright **`baseURL`** és a CI script **`localhost:4200`**-at használ.

## Első forgatókönyv (MVP) — kész

1. Navigálás: `/lowcode/workflows`.
2. Assert: **„Workflows”** fejléc + **„New”** link látható.

## CI

- Workflow: **`.github/workflows/ci.yml`** — job **`frontend-e2e`** (a **`frontend-quality`** után; a **Docker** job erre is vár).
- Script: **`scripts/e2e-smoke-ci.sh`** — `npx playwright install --with-deps chromium` a jobban, majd a script.

## Következő lépések (backlog)

- További útvonalak (auth / entitás lista), ha a termék prioritást ad.
- Opcionális: Windowsra dedikált **PowerShell** wrapper a `e2e-smoke-ci.sh` mellé.

## DoD (E2E iteráció — MVP)

- `package.json` script: `npm run e2e`, `npm run e2e:install-browsers`.
- CI-ben zöld **`frontend-e2e`** job.
- Új dependency: **`@playwright/test`** — governance szerint dependency review / jóváhagyás; lásd `docs/GOVERNANCE.md`.

## Kapcsolódó

- Termék útvonalak: [`roadmap-iter-67-product.md`](roadmap-iter-67-product.md)
- Minőség 68 (API szerződés): [`roadmap-iter-68-quality.md`](roadmap-iter-68-quality.md)
