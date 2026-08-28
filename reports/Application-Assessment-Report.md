# 🧭 Application Assessment Report – eShopLegacyMVC

> **Phase 2 – Assess Project**
> Deep code, dependency, API-surface, database, and security analysis driving the Phase 3 migration.
> **Date:** 2026-08-28 · **Analyzed source:** `src/eShopLegacyMVC/`

---

## 0. 🧾 Assessment Summary

| Metric | Finding |
|--------|---------|
| **Migration complexity** | 🟡 **Medium** — small, well-structured monolith; classic full-framework web dependencies |
| **Cloud readiness (as-is)** | 🔴 Low — runs only on IIS / .NET Framework 4.7.2 |
| **Estimated effort** | Moderate — single project, ~7 code folders, 3 entities, ~4 controllers |
| **Recommended approach** | Fresh port to ASP.NET Core (.NET 8), reusing view/model logic |
| **Critical blockers** | `System.Web` pipeline · EF6 · Autofac MVC5 integration · **`BinaryFormatter`** |
| **Security findings** | 1 High (insecure deserialization), 2 Medium, several config hardening items |

---

## 1. 📦 Application Overview

**eShopLegacyMVC** is a monolithic e-commerce catalog reference application built on the classic
ASP.NET MVC 5 stack running on the full .NET Framework. It manages catalog items, brands, and types,
with server-rendered Razor views and Entity Framework 6 for data access.

| Attribute | Detail |
|-----------|--------|
| **Framework** | .NET Framework 4.7.2 |
| **Web Framework** | ASP.NET MVC 5 (`System.Web`) |
| **UI** | Razor views (server-side), Bootstrap 4, jQuery |
| **Data Access** | Entity Framework 6 (Code First + Migrations) |
| **DI Container** | Autofac 4.9 (`Autofac.Mvc5`) |
| **Logging** | log4net 2.0 |
| **Telemetry** | Application Insights 2.9 |
| **Hosting (current)** | IIS / `System.Web` pipeline |

---

## 2. 🎯 Target Architecture (User Selections)

| Concern | Current | Target |
|---------|---------|--------|
| **Runtime** | .NET Framework 4.7.2 | .NET 8 (ASP.NET Core) |
| **Hosting** | On-prem IIS | **Azure App Service** |
| **IaC** | None | **Bicep** |
| **Database** | SQL Server LocalDB | **Azure SQL Database** |
| **Secrets/Config** | `Web.config` | App Service Configuration + Key Vault |
| **Identity to DB** | Integrated Security | Managed Identity (recommended) |

---

## 3. 🔍 Key Components & Migration Notes

| Component | Current Implementation | Migration Consideration |
|-----------|------------------------|--------------------------|
| **Controllers** | `CatalogController`, `PicController`, API `BrandsController`, `FilesController` | Port MVC + Web API to unified ASP.NET Core controllers |
| **Models / EF** | `CatalogDBContext`, EF6 Code-First migrations | Migrate to EF Core 8; regenerate migrations |
| **DI** | Autofac via `ApplicationModule` | Use built-in `Microsoft.Extensions.DependencyInjection` (optionally keep Autofac) |
| **Logging** | log4net (`log4Net.xml`) | Move to `Microsoft.Extensions.Logging` (+ App Insights SDK) |
| **Config** | `Web.config`, `connectionStrings`, `appSettings` | `appsettings.json` + environment config + Key Vault |
| **Views** | Razor `.cshtml`, `BundleConfig` | Reuse Razor; replace `System.Web.Optimization` bundling |
| **Static assets** | `Content/`, `Scripts/`, `Pics/`, `Images/` | Serve via `wwwroot/` static files |
| **Startup** | `Global.asax`, `App_Start/*` | `Program.cs` / minimal hosting model |
| **Telemetry** | App Insights web module | App Insights for ASP.NET Core |

---

## 4. 🗄️ Database Assessment

| Item | Detail |
|------|--------|
| **Current** | SQL Server (LocalDB) – `CatalogDBContext` |
| **Entities** | `CatalogItem`, `CatalogBrand`, `CatalogType` |
| **Migration Path** | EF6 → EF Core 8; schema is relational and maps cleanly |
| **Target** | ✅ **Azure SQL Database** (fully compatible with existing relational model) |
| **Recommendation** | Use **Microsoft Entra ID authentication + Managed Identity**; store connection string in Key Vault; enable Advanced Threat Protection & automatic backups |

> The current workload is **relational** with a small, well-defined schema — Azure SQL Database is the
> right fit. Cosmos DB is **not** recommended for this workload.

---

## 4a. 🧩 Detailed Component Inventory (Phase 2)

| Layer | File(s) | Key APIs / Concerns | Migration Action |
|-------|---------|---------------------|------------------|
| **Bootstrap** | `Global.asax.cs` (`MvcApplication`) | `HttpApplication`, `Application_Start`, `Session_Start`, `Application_BeginRequest`, Autofac container build | Re-implement in `Program.cs` (minimal hosting); move filters/routes/DI to `builder.Services` |
| **DI** | `Modules/ApplicationModule.cs`, `RegisterContainer()` | Autofac `Module`, `AutofacDependencyResolver`, `AutofacWebApiDependencyResolver` | Port to `Microsoft.Extensions.DependencyInjection`; lifetimes: `CatalogService` → Scoped, `HiLoGenerator` → Singleton |
| **MVC Controller** | `Controllers/CatalogController.cs` | `Controller`, `ActionResult`, `[Bind]`, `[ValidateAntiForgeryToken]`, `Server.MapPath`, `Url.RouteUrl` | Port to ASP.NET Core MVC `Controller`; replace `HttpNotFound()`/`HttpStatusCodeResult` with `NotFound()`/`BadRequest()` |
| **Image Controller** | `Controllers/PicController.cs` | `Server.MapPath("~/Pics")`, `File(buffer, mime)`, attribute routing | Replace `Server.MapPath` with `IWebHostEnvironment.WebRootPath`/`ContentRootPath`; keep `FileResult` |
| **Web API** | `Controllers/WebApi/BrandsController.cs`, `FilesController.cs` | `ApiController`, `IHttpActionResult`, `HttpResponseMessage` | Merge into ASP.NET Core `ControllerBase` `[ApiController]`; return `IActionResult` |
| **Stub API** | `Controllers/Api/CatalogController.cs` (`CatalogController2`) | Returns `Json("Hello World")` | Trivial — port or drop |
| **Service** | `Services/CatalogService.cs`, `CatalogServiceMock.cs`, `ICatalogService.cs` | EF6 LINQ, `Include()`, `SaveChanges()`, `IDisposable` | Port to EF Core 8; keep interface; DbContext lifetime handled by DI (drop manual `Dispose`) |
| **Data / EF** | `Models/CatalogDBContext.cs` | EF6 `DbContext`, `DbModelBuilder`, `EntityTypeConfiguration<T>`, `HasRequired().WithMany()` | EF Core 8 `DbContext`, `ModelBuilder`, `IEntityTypeConfiguration<T>`, `HasOne().WithMany().IsRequired()` |
| **Key generation** | `Models/CatalogItemHiLoGenerator.cs` | Raw SQL `SELECT NEXT VALUE FOR catalog_hilo` via `db.Database.SqlQuery<long>` | Use EF Core `UseHiLo()` **or** `FromSqlRaw`; `SqlQuery<T>` API changed in EF Core |
| **Entities** | `CatalogItem`, `CatalogBrand`, `CatalogType` | POCOs + data annotations | Reuse as-is |
| **Seeding** | `Models/Infrastructure/CatalogDBInitializer.cs`, `PreconfiguredData.cs`, `Setup/*.csv` | EF6 `IDatabaseInitializer`, HiLo sequence `.sql` scripts | Replace with EF Core migrations + `HasData`/seeding; port `.sql` sequence creation |
| **Config (App_Start)** | `BundleConfig`, `FilterConfig`, `RouteConfig`, `WebApiConfig` | `System.Web.Optimization` bundling, global filters, route tables | Replace bundling with static files/bundler; routes via endpoint routing |
| **Views** | `Views/**/*.cshtml`, `_Layout`, `_ViewStart` | Razor + `System.Web`-based helpers | Reuse Razor; update `@Scripts/@Styles` bundling helpers and tag helpers |
| **Utilities** | `eShopLegacy.Utilities/Serializing.cs` | ⚠️ **`BinaryFormatter`** serialize/deserialize | **Remove** — replace with `System.Text.Json` (see Security) |
| **Logging** | `log4Net.xml`, `LogManager.GetLogger` | log4net, `LogicalThreadContext` | Adopt `ILogger<T>`; optionally log4net.Ext for ASP.NET Core |
| **Telemetry** | `ApplicationInsights.config`, AI web modules | Full-framework AI SDK | `Microsoft.ApplicationInsights.AspNetCore` |

---

## 4b. 🌐 API & Route Surface

| Route | Verb | Handler | Notes |
|-------|------|---------|-------|
| `/` , `/Catalog/Index` | GET | `CatalogController.Index` | Paginated catalog list |
| `/Catalog/Details/{id}` | GET | `CatalogController.Details` | |
| `/Catalog/Create` | GET/POST | `CatalogController.Create` | Anti-forgery token |
| `/Catalog/Edit/{id}` | GET/POST | `CatalogController.Edit` | Anti-forgery token |
| `/Catalog/Delete/{id}` | GET/POST | `CatalogController.Delete` | Anti-forgery token |
| `items/{catalogItemId:int}/pic` | GET | `PicController.Index` | Serves catalog image bytes |
| `api/brands` | GET/DELETE | `BrandsController` | Web API; DELETE is a no-op demo |
| `api/files` | GET | `FilesController` | ⚠️ Returns **binary-serialized** payload |
| `api` | GET | `CatalogController2` | Stub |

---

## 5. ⚠️ Risks & Blockers

| Risk | Impact | Mitigation |
|------|--------|------------|
| `System.Web` pipeline (`HttpApplication`, `HttpContext.Current`, modules, `Server.MapPath`) | High | Replace with ASP.NET Core hosting, DI, `IWebHostEnvironment` |
| **`BinaryFormatter`** in `Serializing.cs` (used by `FilesController`) | **High (security)** | Remove; use `System.Text.Json`. `BinaryFormatter` is obsolete/removed in modern .NET |
| Autofac MVC5/WebApi integration (`AutofacDependencyResolver`, `AutofacWebApiDependencyResolver`) | Medium | Use built-in DI, or `Autofac.Extensions.DependencyInjection` |
| EF6 → EF Core 8 (`EntityTypeConfiguration`, `HasRequired`, `Database.SqlQuery<T>`, initializers) | Medium | Rewrite context config, HiLo, and seeding for EF Core |
| `Global.asax` / `App_Start` bootstrapping | Medium | Re-implement in `Program.cs` |
| ASP.NET Web API (`ApiController`, `HttpResponseMessage`) | Medium | Port to `[ApiController]` + `IActionResult` |
| Bundling via `System.Web.Optimization` / WebGrease | Low | Use static files or a modern bundler |
| log4net + `LogicalThreadContext` correlation | Low | Adopt `ILogger` + AI correlation |
| Existing `eShopPorted/` project drift | Medium | Decide reuse vs. fresh port (see §8) |

---

## 6. 🛣️ High-Level Migration Plan

1. **Assess** – Deep-dive code, dependency, and API surface analysis (Phase 2).
2. **Port Runtime** – Recreate the app on ASP.NET Core (.NET 8); migrate controllers, views, DI, config, logging.
3. **Data Layer** – Convert EF6 → EF Core 8; regenerate migrations targeting Azure SQL Database.
4. **Infrastructure (Bicep)** – Provision Azure App Service, Azure SQL Database, Key Vault, App Insights.
5. **Configuration & Security** – Move secrets to Key Vault; enable Managed Identity for DB access.
6. **Validate** – Build, run tests, functional verification against Azure SQL.
7. **Deploy** – CI/CD to Azure App Service.

---

## 7. 🔒 Security & Compliance Findings

| # | Severity | Finding | Location | Remediation |
|---|----------|---------|----------|-------------|
| 1 | 🔴 **High** | Insecure deserialization via `BinaryFormatter` (OWASP A08) | `eShopLegacy.Utilities/Serializing.cs`, `FilesController` | Remove `BinaryFormatter`; serialize with `System.Text.Json` |
| 2 | 🟠 Medium | Plaintext DB connection string in `Web.config`; `Integrated Security` won't work on App Service | `Web.config` `connectionStrings` | Move to App Service config / Key Vault; use **Entra ID + Managed Identity** for Azure SQL |
| 3 | 🟠 Medium | `compilation debug="true"` enabled | `Web.config` | Disable debug in production builds |
| 4 | 🟡 Low | Image path built from DB `PictureFileName` + `Path.Combine` (potential traversal if data untrusted) | `PicController` | Validate/whitelist filename; use `Path.GetFileName` |
| 5 | 🟡 Low | No HTTPS-only / HSTS enforcement | app-wide | Add `UseHttpsRedirection` + HSTS in Core |
| 6 | 🟡 Low | Outdated packages (EF6.2, Newtonsoft 12, AI 2.9) | `packages.config` | Superseded by target-framework equivalents during port |

---

## 8. 🔀 `eShopPorted/` Reuse Decision

A partial ASP.NET Core port already exists at `eShopPorted/` (has `Program.cs`, `Startup.cs`, EF Core `Migrations/`, ported controllers/models). **Recommendation:**

- ✅ **Use `eShopPorted/` as the migration baseline** — it already contains EF Core migrations, `appsettings.json`, and a Core hosting model, reducing Phase 3 effort.
- Reconcile it against `src/eShopLegacyMVC/` to ensure **feature parity** (Pic serving, Web API endpoints, HiLo generation, seeding).
- Retarget its data layer and config to **Azure SQL Database + Managed Identity**.
- ⚠️ Confirm with the user before choosing reuse vs. clean re-port.

---

## 9. ✅ Migration Readiness Checklist

| Item | Status |
|------|--------|
| Framework & dependencies identified | ✅ |
| API/route surface mapped | ✅ |
| Data model & EF migration path defined | ✅ |
| Security findings documented | ✅ |
| Target Azure services confirmed (App Service, Azure SQL, Bicep) | ✅ (Phase 1) |
| `eShopPorted/` reuse decision | ⏳ Needs user confirmation |
| Azure subscription / region / naming | ⏳ Needed before Phase 4 |

---

## 10. ⏭️ Next Step

➡️ Proceed to **`/phase3-migratecode`** to begin code modernization.
Priority order: (1) remove `BinaryFormatter`, (2) stand up ASP.NET Core host + DI, (3) port EF6 → EF Core 8, (4) port controllers/views, (5) wire config to Azure SQL + Key Vault.
