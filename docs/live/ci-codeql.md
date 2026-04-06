# CI: CodeQL

**Workflow:** [`.github/workflows/codeql.yml`](../../.github/workflows/codeql.yml) — `push` / `pull_request` (**`main`**, **`master`**) + heti ütemezés.

**Konfig:** [`.github/codeql/codeql-config.yml`](../../.github/codeql/codeql-config.yml) — pl. `paths-ignore` a tudatosan kezelt SPA session fájlra (lásd [`02_allapot.md`](02_allapot.md) OIDC / CodeQL sor).

## Build mode (matrix)

Az `init` lépés **explicit `build-mode`** értéket kap (GitHub `codeql-action/init`):

| Nyelv | Mód | Indoklás |
|--------|-----|----------|
| **csharp** | `manual` | A workflow a `init` és az `analyze` között futtatja a `dotnet build`-et. |
| **javascript-typescript** | `none` | Forráselemzés; a `npm run build` opcionális sanity lépés maradhat. |

Ez csökkenti a „overlay database / build-mode undefined” típusú figyelmeztetéseket a manuális C# build mellett.

## Megjegyzés (GitHub UI)

A **„Starting April 2026…”** jellegű annotation a CodeQL PR-**coverage** viselkedésére vonatkozik — platform oldali változás; a repó workflow-jában nincs külön kapcsoló.
