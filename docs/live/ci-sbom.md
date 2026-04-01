# CI — SBOM (backend + frontend, CycloneDX)

**Cél:** a szállított **backend** (NuGet) és **frontend** (npm) függőségek **CycloneDX JSON** SBOM-ja legyen elérhető **CI artefaktként** (supply-chain átláthatóság, audit, OWASP / executive order irányelvekhez illeszkedő gyakorlat) anélkül, hogy a futtató környezetbe be kellene építeni a generálást.

## Backend (.NET)

### Hol fut

- **GitHub Actions** `CI` workflow — job: **`Backend - SBOM (CycloneDX)`** (`.github/workflows/ci.yml`).
- **Eszköz:** [`cyclonedx`](https://www.nuget.org/packages/cyclonedx) **6.1.0** — rögzítve a repo gyökerében: **`.config/dotnet-tools.json`** (`dotnet tool restore`).

### Mit generál

- **Bemenet:** `backend/LowCodePlatform.Backend.csproj` (NuGet `project.assets.json` alapján).
- **Kimenet:** `artifacts/sbom/lowcode-backend.cdx.json` — **CycloneDX JSON** (`-F Json`), metadata név: `LowCodePlatform.Backend`, verzió: aktuális **`github.sha`** (commit azonosíthatóság).
- **`-dpr`:** explicit `dotnet restore` után nem fut újra restore a generálásban (gyorsabb, determinisztikus CI).

### Artefakt

- **`sbom-backend-cyclonedx-json`** (JSON fájl), **90 nap** megőrzés.

## Frontend (npm)

### Hol fut

- **GitHub Actions** `CI` workflow — job: **`Frontend - SBOM (npm CycloneDX)`**.
- **Eszköz:** beépített **`npm sbom`** — **Node 20** a CI-ben (npm **10+**; a `npm sbom` parancs ehhez tartozik).

### Mit generál

- **Bemenet:** `frontend/package-lock.json` (**`--package-lock-only`** — nem kell `npm ci`, gyors, reprodukálható).
- **Kimenet:** `artifacts/sbom/lowcode-frontend.cdx.json` — **CycloneDX JSON** (`--sbom-format cyclonedx`), típus: **application**.

### Artefakt

- **`sbom-frontend-cyclonedx-json`** (JSON fájl), **90 nap** megőrzés.

## Letöltés (CI)

- Run → **Artifacts** → `sbom-backend-cyclonedx-json` / `sbom-frontend-cyclonedx-json`.

## Lokálisan — backend

```bash
dotnet restore backend/LowCodePlatform.Backend.csproj
dotnet tool restore
mkdir -p artifacts/sbom
dotnet tool run dotnet-CycloneDX -- backend/LowCodePlatform.Backend.csproj \
  -o artifacts/sbom -fn lowcode-backend.cdx.json -F Json -dpr \
  -sn LowCodePlatform.Backend -sv "$(git rev-parse HEAD)"
```

## Lokálisan — frontend

```bash
mkdir -p artifacts/sbom
( cd frontend && npm sbom --package-lock-only --sbom-format cyclonedx --sbom-type application \
  > ../artifacts/sbom/lowcode-frontend.cdx.json )
```

Az `artifacts/` mappa **gitignore**-olt (lokális kimenet).

## Kapcsolódó

- NuGet sebezhetőség kapu: [`ci-supply-chain.md`](ci-supply-chain.md) · titok-szűrés: [`ci-secret-scanning.md`](ci-secret-scanning.md) · frontend `npm audit` kötelező kapu: külön hullám (Angular / toolchain upgrade után) — ugyanott.
