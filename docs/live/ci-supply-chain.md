# CI — supply-chain (NuGet)

**Cél:** a backend NuGet-függőségekre is érvényes legyen az iparági **vulnerability awareness** (OWASP / SSDF irányelvek) anélkül, hogy a teljes frontend stack `npm audit` zaját egyszerre kötelezővé tennénk (Angular major upgrade külön hullám).

A GitHub **`actions/dependency-review-action`** job **nincs** beépítve az alap CI-ba, mert a repóban engedélyezni kell a **Dependency graph**ot (Repo → **Settings** → **Code security** → *Dependency graph*). Ha ez nincs bekapcsolva, az action hibával leáll (`Dependency review is not supported on this repository`). Opcionálisan, graph bekapcsolása után hozzáadható külön workflow vagy job; példa lentebb.

## Backend (NuGet)

### `Directory.Build.props`

A `backend/Directory.Build.props` beállítja:

- `NuGetAudit` — bekapcsolva
- `NuGetAuditMode` — `all` (transitive is)
- `NuGetAuditLevel` — `moderate` (moderate és súlyosabb bejelentések)

A `dotnet restore` így figyelmeztet / blokkol a NuGet advisory adatbázis alapján (SDK viselkedés).

### CI job: `backend-supply-chain`

A GitHub Actions **CI** (`backend-supply-chain` job) a restore után futtatja:

```bash
dotnet list backend/LowCodePlatform.Backend.csproj package --vulnerable --include-transitive
dotnet list backend/LowCodePlatform.Backend.Tests/LowCodePlatform.Backend.Tests.csproj package --vulnerable --include-transitive
```

Ha bármelyik projektben ismert sebezhető csomag van, a parancs **nem nulla** visszatéréssel fut (CI piros).

Lokálisan ugyanez:

```bash
dotnet restore backend/LowCodePlatform.Backend.csproj
dotnet list backend/LowCodePlatform.Backend.csproj package --vulnerable --include-transitive
```

## Opcionális: GitHub Dependency review (PR)

Ha a repóban be van kapcsolva a **Dependency graph**, egy külön workflow-ban vagy jobban használható a [`dependency-review-action`](https://github.com/actions/dependency-review-action) (csak `pull_request` eseményre). Példa beállítások: `fail-on-severity: high` (npm zaj csökkentése); a workflow-ban `permissions: { contents: read, pull-requests: read }`.

A backend NuGet ellenőrzést továbbra is a **`backend-supply-chain`** job adja.

## Frontend (`npm`)

A frontend jelenlegi **Angular 17** vonal és a dev/build tool-lánc sok **transitive** bejegyzést ad az `npm audit` jelentésben; a teljes `npm audit --audit-level=high` **kötelező** CI-kapu nem vezethető be egyetlen PR-ban (koordinált Angular / toolchain upgrade kell).

Javasolt külön hullám: célzott Angular LTS / npm frissítés + utána audit szint emelése.

Kapcsolódó: [`ci-dotnet-format.md`](ci-dotnet-format.md), [`dependabot.yml`](../../.github/dependabot.yml).
