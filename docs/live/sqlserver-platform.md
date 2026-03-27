# SQL Server — tenant platform DB (Iter 49)

## Összefoglaló

A **tenant** adat (`PlatformDbContext` / SQL Serveren `PlatformSqlServerDbContext`) connection string alapján **SQLite** (default dev) vagy **Microsoft SQL Server** közül választ a backend (`PlatformDatabaseProvider.CreatePlatformDbContext`).

- **SQLite:** EF migrációk: `Data/Migrations/Platform` (`MigrateAsync`).
- **SQL Server:** EF migrációk: `Data/Migrations/PlatformSqlServer` (`MigrateAsync` a default útvonalon).

## Greenfield bootstrap SQL Serveren (opcionális)

Ha **nem** migrációból akarod a sémát (ritka / lab):

- Állítsd: **`LCP_SQLSERVER_ENSURE_CREATED=1`**
- Ekkor tenant bootstrap **`EnsureCreatedAsync`**-et használ SQL Serverhez (SQLite továbbra is **`MigrateAsync`**).

**Prod**-ban általában a **`MigrateAsync` + `PlatformSqlServer`** migráció a cél; `EnsureCreated` csak tudatos escape hatch.

## Connection string

A `PlatformDatabaseProvider.IsSqlServerConnectionString` tipikus kulcsokat és `Data Source=` disambiguation-t használ (pl. `Server=…;Database=…`, `Initial Catalog=`, `Server=tcp:…`, vagy `Data Source=host,1433`).

## Integrációs teszt (opcionális, 56d)

Ha beállítod: **`LCP_TEST_SQLSERVER_MASTER_CONNECTION_STRING`** (SQL Server **`master`** adatbázis, pl. LocalDB: `Server=(localdb)\\mssqllocaldb;Database=master;Trusted_Connection=True;TrustServerCertificate=True`), a backend tesztprojekt lefuttat egy **egyszeri smoke**-ot: létrehoz egy eldobható adatbázist, `MigrateAsync` a `PlatformSqlServerDbContext` láncra, majd törli. CI alapból **nem** adja meg ezt a változót — a teszt no-op pass.

## Design-time (`dotnet ef`)

- **SQLite** (`PlatformDbContext`): `LCP_PLATFORM_DESIGN_TIME_CONNECTION_STRING` vagy alap `Data Source=tenant-default.db`.
- **SQL Server** (`PlatformSqlServerDbContext`): **`LCP_PLATFORM_SQLSERVER_DESIGN_TIME_CONNECTION_STRING`** vagy LocalDB alap (`DesignTimePlatformSqlServerDbContextFactory`).

## SQL Server — üzemeltetői backup (nem a Helm SQLite CronJob)

A tenant adat **SQL Serveren** a klaszter **SQLite backup** CronJobját nem érinti; ott natív **FULL/DIFF backup**, **VolumeSnapshot** (ha van CSI), vagy az adatbázis szolgáltató saját mentési stratégiája a helyes irány. A **management** SQLite (`management.db`) továbbra is a backend PVC-n maradhat — arról a chart opcionális `kubectl cp` mentés szól (`backup.enabled`).

## SQLite-only javítások

A `PlatformSqliteSchemaRepair` csak **SQLite** provider esetén fut (SQL Serveren no-op).

## Helm / Kubernetes

Connection string és `LCP_SQLSERVER_ENSURE_CREATED` példák: [`k3s-home-lab.md`](k3s-home-lab.md) és [`container-deploy.md`](container-deploy.md).

## Iteráció 56–57 — EF migrációk + Helm backup

- **56a–56d (kész):** ADR [`sqlserver-ef-migrations-plan.md`](sqlserver-ef-migrations-plan.md); SS migráció + smoke teszt (`LCP_TEST_SQLSERVER_MASTER_CONNECTION_STRING`) — részletek fent.
- **57 (kész):** Helm chart **0.3.0** — opcionális `backup.enabled` SQLite CronJob — [`k3s-home-lab.md`](k3s-home-lab.md), [`container-deploy.md`](container-deploy.md).

- **56a (kész):** ADR / stratégia.
- **56b (kész):** `PlatformSqlServerDbContext`, `InitialPlatformSqlServer`, design-time factory.
- **56c (kész):** SQL Server tenant útvonalon `PlatformSqlServerDbContext` + `MigrateAsync` (nem a SQLite lánc).
- **56d (kész):** `PlatformSqlServerMigrationsTests` + opcionális master connection string.
