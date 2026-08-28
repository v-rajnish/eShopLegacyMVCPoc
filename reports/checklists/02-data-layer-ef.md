# ✅ Migration Checklist – 2. Data Layer (EF6 → EF Core)

> **Baseline:** `eShopPorted/` already on EF Core (v2.2). **Target:** EF Core 8 + Azure SQL Database.
> Existing EF Core migrations live in `eShopPorted/Migrations/`.

---

## Scope

| Item | Baseline state | Action |
|------|----------------|--------|
| `Models/CatalogDBContext.cs` | ✅ EF Core `DbContext` w/ `ApplyConfigurationsFromAssembly` | Verify against EF Core 8 |
| `Models/Config/*Config.cs` | `IEntityTypeConfiguration<T>` | Verify HasOne/WithMany mappings |
| `Migrations/20201130044339_Initial.cs` | EF Core 2.2 migration | Regenerate/verify under EF Core 8 |
| `Models/Infrastructure/PreconfiguredData.cs` | Seed data | Confirm seeding path |
| HiLo key generation | `catalog_hilo` sequence | Confirm `UseHiLo()` / sequence created |

---

## Tasks

- [ ] Upgrade `Microsoft.EntityFrameworkCore*` packages `2.2.6` → `8.x` (see checklist 8 for TFM).
- [ ] Replace EF Core 2.2 APIs with EF Core 8 equivalents (e.g. `SqlQuery<T>` usage, tracking behavior).
- [ ] Verify entity configs (`CatalogItemConfig`, `CatalogBrandConfig`, `CatalogTypeConfig`) compile and map correctly.
- [ ] Confirm HiLo sequence (`catalog_hilo`) is created via migration or `UseHiLo()`.
- [ ] Regenerate migrations against EF Core 8: `dotnet ef migrations add InitialNet8` (or verify existing).
- [ ] Update `CatalogDBContextModelSnapshot.cs` to the EF Core 8 snapshot format.
- [ ] Point provider at **Azure SQL Database**; remove `Trusted_Connection`/`Integrated Security` (see checklist 3 & 4).
- [ ] Add connection resiliency: `EnableRetryOnFailure()` for transient Azure SQL faults.
- [ ] Confirm seed data (`PreconfiguredData` / `HasData`) applies on `dotnet ef database update`.
- [ ] Remove any leftover EF6 references from `src/eShopLegacyMVC/` (source of truth = eShopPorted).

---

## Verification

- [ ] `dotnet ef database update` succeeds against local SQL and Azure SQL.
- [ ] CRUD via `CatalogService` reads/writes all three entities.
- [ ] HiLo id generation produces sequential ids without collisions.

**Status:** ⬜ Not started
