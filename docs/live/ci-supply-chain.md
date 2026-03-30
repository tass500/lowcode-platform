# CI — supply-chain (NuGet + PR dependency review)

**Cél:** a backend NuGet-függőségekre is érvényes legyen az iparági **vulnerability awareness** (OWASP / SSDF irányelvek) anélkül, hogy a teljes frontend stack `npm audit` zaját egyszerre kötelezővé tennénk (Angular major upgrade külön hullám).

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

## Pull request: `dependency-review`

A **CI** `dependency-review` job csak **`pull_request`** eseményre fut (pushra nem).

- Action: `actions/dependency-review-action`
- `fail-on-severity: high` — a PR-ban **újonnan bevezetett** vagy **módosított** függőségek közül a **high** (és súlyosabb) ismert sebezhetőség buktatja a checket. A **moderate** szándékosan nincs kötelező PR-kapu (npm / Angular toolchain zaj); a backend NuGet oldalt a `backend-supply-chain` job fedezi.

A job szintjén: `contents: read`, `pull-requests: read`. A workflow gyökérszintű `permissions` mellett is szerepelhetnek.

## Frontend (`npm`)

A frontend jelenlegi **Angular 17** vonal és a dev/build tool-lánc sok **transitive** bejegyzést ad az `npm audit` jelentésben; a teljes `npm audit --audit-level=high` **kötelező** CI-kapu nem vezethető be egyetlen PR-ban (koordinált Angular / toolchain upgrade kell).

Javasolt külön hullám: célzott Angular LTS / npm frissítés + utána audit szint emelése.

Kapcsolódó: [`ci-dotnet-format.md`](ci-dotnet-format.md), [`dependabot.yml`](../../.github/dependabot.yml).
