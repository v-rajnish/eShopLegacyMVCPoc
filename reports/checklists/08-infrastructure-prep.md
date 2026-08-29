# ✅ Migration Checklist – 8. Infrastructure Preparation

> **Baseline:** `eShopPorted/` targets `net461` + ASP.NET Core 2.2 (Autofac host).
> **Target:** .NET 8 on **Azure App Service** + **Azure SQL Database**, IaC via **Bicep**.

---

## Project / Runtime Prep

| Item | Baseline state | Action |
|------|----------------|--------|
| TFM | `net461` | Retarget to `net8.0` |
| Web SDK | ASP.NET Core `2.2.0` | Upgrade to ASP.NET Core 8 (shared framework) |
| EF Core | `2.2.6` | Upgrade to `8.x` (checklist 2) |
| Host model | `Startup.cs` + `WebHost` + `IHostingEnvironment` | Modern minimal hosting / `WebApplication` |
| DI | Autofac (`AutofacServiceProvider`) | Keep via `Autofac.Extensions.DependencyInjection` **or** built-in DI |

---

## Tasks – App Modernization

- [x] Change `<TargetFramework>net461</TargetFramework>` → `net8.0`.
- [x] Remove `<PackageReference Include="Microsoft.AspNetCore" ...>` (Web SDK provides `Microsoft.AspNetCore.App`).
- [x] Remove legacy `Autofac.Mvc5`, `WebGrease`, `Antlr` packages (not needed on .NET 8).
- [x] Update `Program.cs` to `WebApplication.CreateBuilder` with .NET 8 minimal hosting.
- [x] Replace `IHostingEnvironment` → `IWebHostEnvironment`; `app.UseMvc` → endpoint routing (`MapControllerRoute`).
- [x] Update Autofac wiring to `.UseServiceProviderFactory(new AutofacServiceProviderFactory())`.
- [x] `dotnet build` + `dotnet run` succeed on .NET 8 locally.

---

## Tasks – Azure Infrastructure (Bicep)

- [ ] Author Bicep for **App Service Plan + App Service** (Linux, .NET 8).
- [ ] Author Bicep for **Azure SQL Server + Database** (with Entra ID admin).
- [ ] Add **Key Vault** + secret references for connection strings.
- [ ] Add **Application Insights** + **Log Analytics workspace** (checklist 5).
- [ ] Enable **System-Assigned Managed Identity** on App Service (checklist 4).
- [ ] Configure App Service app settings (connection string ref, `UseMockData=false`).
- [ ] Add HTTPS-only + minimum TLS 1.2 on App Service.
- [ ] Validate templates: `get_errors` on Bicep + `azure_check_predeploy` before deploy.

---

## Verification

- [ ] App runs on .NET 8 locally with no legacy framework references.
- [ ] Bicep validates with no errors.
- [ ] Provisioned App Service can reach Azure SQL via Managed Identity.

**Status:** ⬜ Not started
