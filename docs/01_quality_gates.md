# Quality gates (enterprise-minimum) — gyors, de biztonságos

## Cél

- A mostani credit-hatékony batch fejlesztési módhoz olyan minimum ellenőrzéseket adni, amik enterprise környezetben is vállalhatóak.
- Elv: **minimális zaj + gyors gate**. A gate-ek alapból **a batch végén** futnak (lásd `docs/00_workmode.md`).

## Fogalmak

- **Gate**: automatizált ellenőrzés (build/lint/test/scan), ami megállíthatja a változtatást.
- **Smoke**: rövid, célzott manuális ellenőrzés (UI flow vagy endpoint).

## Gate-mátrix (ajánlott minimum)

## Repo-supported commands (jelenlegi állapot)

### Frontend (cwd: `frontend/`)

- Build:
  - `npm run build`
- Test:
  - `npm test` (Angular: `ng test`)
- Lint:
  - `npm run lint` (Angular ESLint)
  - Konfig fájlok:
    - `frontend/.eslintrc.json`
    - `frontend/tsconfig.eslint.json`

### Backend (cwd: `backend/`)

- Build:
  - `dotnet build`
- Test:
  - Jelenleg nincs külön teszt projekt (`*Test*.csproj`) a `backend/` alatt.
  - Ha később lesz, akkor: `dotnet test`

### Frontend (Angular)

- **Mindig** (bármilyen FE változásnál):
  - `npm run build`
- **Ha elérhető** a repóban:
  - `npm run lint`
  - `npm test` (vagy `ng test --watch=false`)

### Backend (.NET)

- **Mindig** (bármilyen BE változásnál):
  - `dotnet build`
- **Ha van teszt**:
  - `dotnet test`

### Full-stack / szerződés érzékeny változás

Szerződés érzékenynek számít:
- DTO / API response shape
- auth / RBAC
- trace / header konvenciók
- error contract
- időszinkron (`serverTimeUtc`) viselkedés

Ajánlott minimum:
- Frontend gate-ek + Backend gate-ek

## Batch Definition of Done (DoD)

A batch akkor “kész”, ha:
- A választott gate-ek **zöldek**.
- Nincs félbehagyott átnevezés / broken import / TypeScript error.
- Live docs frissítve (ha az adott workstreamen elvárt):
  - `docs/live/03_kovetkezo_lepesek.md`

## Minimál release / smoke checklist (upgrade page jellegű admin UI)

- **UI**
  - `/upgrade` oldal betölt.
  - `Refresh all` működik.
  - `Copy ticket header` / `Copy short header` működik (nem üres run esetén).
  - `Copy triage snapshot` működik.
- **Drift-proof time**
  - diagnosztika blokk frissül legalább egy API hívás után.
- **Backend**
  - Swagger elérhető.

## Biztonsági minimum (ajánlás)

Ez nem refaktor-gate, hanem “projekt egészség” gate:
- dependency / vulnerability scan (SCA) futtatása CI-ben
- secret scanning (pl. repository scan)
- SBOM generálás (ha van CI/CD)

Megjegyzés: az `npm install` után előfordulhat `npm audit` által jelzett sérülékenység. Ez önmagában nem jelenti azt, hogy futásidőben kihasználható (sokszor devDependency), de enterprise környezetben érdemes CI-ben láthatóvá tenni és ütemezetten kezelni.

### `npm audit` triage policy (javaslat)

- **Gate-elés**
  - Dev környezetben: `npm audit` inkább információs (nem blokk), a `npm run build` + `npm run lint` a gyors fő gate.
  - CI-ben: érdemes láthatóvá tenni (pl. külön “security job”), és csak akkor blokkolni, ha a sérülékenység runtime útvonalon érint.

- **Fix stratégia**
  - Preferált: célzott frissítés (`npm update` / `ng update` / konkrét csomag bump), nem “vak” tömeges javítás.
  - OK automatikus:
    - `npm audit fix` (ha nem hoz breaking change-t)
  - Kerülendő default:
    - `npm audit fix --force` (Angular toolchain esetén gyakran major ugrást kényszerít, pl. `@angular/cli` / `@angular-devkit/build-angular` → breaking change)

- **Kategorizálás**
  - **Runtime dependency** (prod bundle/Node backend runtime): gyorsabb reakció, akár hotfix.
  - **DevDependency** (CLI, build eszközök, webpack/vite, test runner): ütemezett frissítés, release-ablakban.

- **Gyakorlati workflow (frontend)**
  - `npm audit`
  - döntés: devDependency vs runtime
  - ha devDependency és a javasolt fix major upgrade-et kér: inkább `ng update`-tel kezeld verziókompatibilisen

## Recommended upgrades (opcionális, enterprise-irány)

Ezek már **dependency / projekt struktúra** változások, ezért nem “refaktor batch” gate-ek. Céljuk, hogy a minőségkapuk automatizálhatóak legyenek.

### Frontend: ESLint + `npm run lint`

- Cél: legyen stabil, gyors lint gate a batch végén.
- Lehetséges irány (Angular): Angular ESLint integráció.
- Kimenet:
  - `frontend/package.json` script: `lint`
  - futtatható gate: `npm run lint`

### Frontend: headless/unit test gate

- A repóban van: `npm test` (Angular: `ng test`).
- CI-ben / batch végén jellemzően headless mód kell:
  - `ng test --watch=false`
  - (ha szükséges) ChromeHeadless konfiguráció a Karma-ban

### Backend: teszt projekt + `dotnet test`

- Jelenleg csak egy `LowCodePlatform.Backend.csproj` van a `backend/` alatt.
- Enterprise minimumhoz érdemes:
  - egy `*.Tests.csproj` (unit/integration) felvétele
  - onnantól gate: `dotnet test`

## PR / commit minimum (low-noise)

- Preferált: **1 commit / milestone** (`docs/WORKSTYLE.md`).
- PR leírásban minimum:
  - Mit változtatott / mit nem (no behavior change?)
  - Gate-ek: melyek futottak és zöldek
