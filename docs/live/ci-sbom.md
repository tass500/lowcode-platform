# CI — SBOM (backend, CycloneDX)

**Cél:** a szállított backend NuGet-függőségek **CycloneDX JSON** SBOM-ja legyen elérhető **CI artefaktként** (supply-chain átláthatóság, audit, OWASP / executive order irányelvekhez illeszkedő gyakorlat) anélkül, hogy a futtató környezetbe be kellene építeni a generálást.

## Hol fut

- **GitHub Actions** `CI` workflow — job: **`Backend - SBOM (CycloneDX)`** (`.github/workflows/ci.yml`).
- **Eszköz:** [`cyclonedx`](https://www.nuget.org/packages/cyclonedx) **6.1.0** — rögzítve a repo gyökerében: **`.config/dotnet-tools.json`** (`dotnet tool restore`).

## Mit generál

- **Bemenet:** `backend/LowCodePlatform.Backend.csproj` (NuGet `project.assets.json` alapján).
- **Kimenet:** `artifacts/sbom/lowcode-backend.cdx.json` — **CycloneDX JSON** (`-F Json`), metadata név: `LowCodePlatform.Backend`, verzió: aktuális **`github.sha`** (commit azonosíthatóság).
- **`-dpr`:** explicit `dotnet restore` után nem fut újra restore a generálásban (gyorsabb, determinisztikus CI).

## Artefakt

- A workflow **`actions/upload-artifact`**-mal feltölti: **`sbom-backend-cyclonedx-json`** (JSON fájl), **90 nap** megőrzés.
- Letöltés: run → **Artifacts** → `sbom-backend-cyclonedx-json`.

## Lokálisan

```bash
dotnet restore backend/LowCodePlatform.Backend.csproj
dotnet tool restore
mkdir -p artifacts/sbom
dotnet tool run dotnet-CycloneDX -- backend/LowCodePlatform.Backend.csproj \
  -o artifacts/sbom -fn lowcode-backend.cdx.json -F Json -dpr \
  -sn LowCodePlatform.Backend -sv "$(git rev-parse HEAD)"
```

A `artifacts/` mappa **gitignore**-olt (lokális kimenet).

## Kapcsolódó

- NuGet sebezhetőség kapu: [`ci-supply-chain.md`](ci-supply-chain.md) · titok-szűrés: [`ci-secret-scanning.md`](ci-secret-scanning.md).
