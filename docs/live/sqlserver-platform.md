# SQL Server — tenant platform DB (Iter 49)

## Összefoglaló

A **tenant** adat (`PlatformDbContext`) connection string alapján **SQLite** (default dev) vagy **Microsoft SQL Server** közül választ a backend (`PlatformDatabaseProvider`).

A meglévő EF migrációk a `Data/Migrations/Platform` alatt **SQLite-specifikusak**; SQL Serverhez **külön migráció-pipeline** a jövőbeli roadmap része.

## Greenfield bootstrap SQL Serveren

Amíg nincs SQL Server–kompatibilis migráció, opcionálisan a séma a modellből jöhet létre:

- Állítsd: **`LCP_SQLSERVER_ENSURE_CREATED=1`**
- Ekkor tenant bootstrap (startup, `TenantMigrationService`, upgrade orchestrator) **`EnsureCreatedAsync`**-et használ SQL Serverhez (SQLite továbbra is **`MigrateAsync`**).

**Prod** környezetben ez csak tudatos, zöldmezős / dev célra ajánlott; élesben a cél a provider-specifikus migrációk.

## Connection string

A `PlatformDatabaseProvider.IsSqlServerConnectionString` tipikus kulcsokat és `Data Source=` disambiguation-t használ (pl. `Server=…;Database=…`, `Initial Catalog=`, `Server=tcp:…`, vagy `Data Source=host,1433`).

## Design-time (`dotnet ef`)

- Alapértelmezés: SQLite `Data Source=tenant-default.db`.
- SQL Serverhez állítsd: **`LCP_PLATFORM_DESIGN_TIME_CONNECTION_STRING`** (vagy `Tenancy:DesignTimeTenantConnectionString` a `Program` EF design-time ágában, ha `LCP_EF_DESIGN_TIME=1` / `dotnet-ef`).

## SQLite-only javítások

A `PlatformSqliteSchemaRepair` csak **SQLite** provider esetén fut (SQL Serveren no-op).
