# CI: `dotnet format` (backend)

**Szállítás:** **PR #110** (merge a **`main`** ágra).

**Iter 64e:** a GitHub Actions **CI** (`backend-format` job) futtatja a **`dotnet format --verify-no-changes`** ellenőrzést a fő backend projektre és a teszt projektre — ugyanazokra a `.csproj` fájlokra, mint a workflow.

## Lokálisan

A repo gyökeréből vagy a `backend/` mappából:

```bash
dotnet format backend/LowCodePlatform.Backend.csproj
dotnet format backend/LowCodePlatform.Backend.Tests/LowCodePlatform.Backend.Tests.csproj
```

Csak ellenőrzés (CI-vel megegyezően):

```bash
dotnet format --verify-no-changes backend/LowCodePlatform.Backend.csproj
dotnet format --verify-no-changes backend/LowCodePlatform.Backend.Tests/LowCodePlatform.Backend.Tests.csproj
```

A .NET SDK része (`dotnet format`); nincs külön NuGet csomag a repo-ban.

## Megjegyzés

Az alapértelmezett .NET stílus szabályok érvényesülnek (nincs kötelező gyökér `.editorconfig` a backendhez). Ha a csapat később szigorítani akarja, érdemes gyökér vagy `backend/.editorconfig`-ot bevezetni és egyszer lefuttatni a `dotnet format`-ot.

Kapcsolódó CI: supply-chain (NuGet audit, vulnerable package list) — [`ci-supply-chain.md`](ci-supply-chain.md).
