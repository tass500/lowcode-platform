# E2E smoke — terv (következő iteráció)

## Cél

Minimális **end-to-end** ellenőrzés a low-code demó útvonalon: böngésző → Angular dev szerver → proxy → backend, **egy** stabil forgatókönyv, CI-ben is futtatható (headless).

## Ajánlott eszköz

**Playwright** (`@playwright/test`) — gyors, headless Chromium, trace screenshot; külön npm devDependency a `frontend/` alatt vagy repo-gyökér `e2e/` mappa (döntés a PR-ben).

**Alternatíva:** Cypress — ha a csapat azt preferálja; ugyanaz a DoD: egy zöld smoke, dokumentált parancs.

## Előfeltételek (lokális / CI)

| Szolgáltatás | Port | Megjegyzés |
|--------------|------|------------|
| Backend | `5002` | `Testing` nélkül: `dotnet run` a `backend/`-ből |
| Frontend | `4200` | `npm start` a `frontend/`-ben; `proxy.conf.json` → `/api` |

CI-ben: backend + frontend indítás háttérben, majd `npx playwright test` (vagy egyeztetett parancs).

## Első forgatókönyv (MVP)

1. Navigálás: `http://localhost:4200/lowcode/workflows` (vagy `/` → átirányítás ellenőrzése).
2. Assert: oldal cím / kulcsszöveg megjelenik (pl. „Workflow” vagy fejléc), **nem** üres hiba.
3. Opcionális 2. lépés: auth nélküli demó útvonal — ha a build auth guardot használ, stub vagy `dev-token` flow egyeztetése.

## DoD (E2E iteráció)

- `package.json` script: pl. `npm run e2e` (vagy repo gyökérből).
- Dokumentált parancs a PR-ban; **nem** blokkoló a meglévő backend unit tesztek helyett — kiegészítő gate.
- **Új dependency** csak explicit jóváhagyással / külön review — lásd `docs/GOVERNANCE.md`.

## Kapcsolódó

- Termék útvonalak: [`roadmap-iter-67-product.md`](roadmap-iter-67-product.md)
- Minőség 68 (API szerződés lezárva): [`roadmap-iter-68-quality.md`](roadmap-iter-68-quality.md)
