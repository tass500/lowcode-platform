# SQL Server — EF migrációs stratégia (ADR, Iter 56a)

## Probléma

A tenant **`PlatformDbContext`** sémáját ma a `Data/Migrations/Platform/*` alatt lévő EF migrációk írják le. Ezek a generálás során **SQLite** providert kaptak (`TEXT`, SQLite-specifikus részletek). Ugyanez a migrációs lánc **nem** hordozható 1:1 **Microsoft SQL Serverre**.

A runtime (56b–56c után):

- **SQLite**: `MigrateAsync` — `Data/Migrations/Platform`.
- **SQL Server**: `MigrateAsync` — `Data/Migrations/PlatformSqlServer` / `PlatformSqlServerDbContext` (`PlatformDatabaseProvider.CreatePlatformDbContext`).
- **SQL Server** escape hatch: `EnsureCreatedAsync`, ha `LCP_SQLSERVER_ENSURE_CREATED=1` (nem ajánlott prod-ban).

## Döntés (két migrációs lánc, egy modell)

**Választott irány:** két **külön EF migrációs lánc** ugyanarra a **domain modellre**, két **külön `DbContext` típusra** támaszkodva (Microsoft ajánlás több providerhez: külön migráció-per-provider).

| Lánc            | Kontextus (terv)                 | Output mappa (terv)                    | Provider (design + runtime) |
|-----------------|----------------------------------|----------------------------------------|-----------------------------|
| Meglévő         | `PlatformDbContext`              | `Data/Migrations/Platform` (változatlan) | SQLite                      |
| Új (56b+)       | `PlatformSqlServerDbContext` (*) | `Data/Migrations/PlatformSqlServer`    | SQL Server                  |

\* **`PlatformSqlServerDbContext`** = `PlatformDbContext` leszármazottja, **azonos** `OnModelCreating` / entitások; migrációk és design-time factory külön. Runtime: **`CreatePlatformDbContext`** + **`AddScoped<PlatformDbContext>`** factory (`Program.cs`) választja a SS kontextust, ha a connection string SQL Server.

**Miért nem egy gyökér + feltételes SQL a migrációkban?** Nagyobb karbantartási kockázat, nehezebben reviewolható diff; a két lánc explicit és a meglévő SQLite történetet nem kell „szétcsalni” egy fájlban.

**Miért nem két külön assembly?** Lehetséges, de nagyobb repo-refaktor; egy assemblyben két kontextus + két migrációs mappa elég, ha a design-time és DI egyértelmű (56b–56c).

## Migrációs előzmény tábla

Mindkét provider saját adatbázison fut; az alapértelmezett `__EFMigrationsHistory` **ütközés nélkül** megmaradhat. Ha később szükséges, SS oldalon explicit `MigrationsHistoryTable` beállítható (pl. séma / név konvenció) — opcionális finomítás, nem blokkoló 56a–56b-ben.

## Design-time (`dotnet ef`)

- SQLite (meglévő): `LCP_PLATFORM_DESIGN_TIME_CONNECTION_STRING` → fájl-URI, `PlatformDbContext`.
- SQL Server (56b+): ugyanaz a változó vagy külön `LCP_PLATFORM_SQLSERVER_DESIGN_TIME_CONNECTION_STRING` (ha külön választjuk) + **`PlatformSqlServerDbContext`** + `dotnet ef migrations add ... --context PlatformSqlServerDbContext --output-dir Data/Migrations/PlatformSqlServer`.

Részletek: [`sqlserver-platform.md`](sqlserver-platform.md) (design-time env, `EnsureCreated`).

## CI / gate-ek (terv)

- **Kötelező (marad):** `dotnet test` + `dotnet build` a meglévő SQLite útvonalon.
- **Opcionális (56d / később):** SQL Server Docker vagy LocalDB + `dotnet ef database update --context PlatformSqlServerDbContext` smoke, vagy `[ConditionalFact]` — nem blokkolja a 56a–56c merge-t, ha a maintainer nem kér Docker-t a PR gate-ben.

## Fázis-hozzárendelés

| Fázis | Tartalom |
|-------|----------|
| **56a** | Ez a dokumentum + live doc hivatkozások. |
| **56b** | `PlatformSqlServerDbContext` + első SS baseline migráció + `DesignTimePlatformSqlServerDbContextFactory`. |
| **56c** | `CreatePlatformDbContext`, `Program.cs` scoped factory, `TenantMigrationService`; SQL Server → `MigrateAsync` a SS láncra; `LCP_SQLSERVER_ENSURE_CREATED` dokumentált escape hatch. |
| **56d** | Opcionális SS smoke: `LCP_TEST_SQLSERVER_MASTER_CONNECTION_STRING` + `PlatformSqlServerMigrationsTests` (master DB → throwaway DB → migrate → drop). |

## Hivatkozások

- Microsoft Learn: [Migrations with multiple providers](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers)
- Repo összefoglaló: [`sqlserver-platform.md`](sqlserver-platform.md)
- Ütem: [`03_kovetkezo_lepesek.md`](03_kovetkezo_lepesek.md) § *Iteráció 56 — részletes ütem*
