# 📊 Migration Report – Status Tracker

> Living status document for the **eShopLegacyMVC → Azure** modernization effort.
> Updated at the end of each phase.

---

## 🗂️ Project Snapshot

| Item | Value |
|------|-------|
| **Application** | eShopLegacyMVC |
| **Current Stack** | ASP.NET MVC 5 · .NET Framework 4.7.2 |
| **Target Stack** | ASP.NET Core (.NET 8) |
| **Migration Type** | Re-platform + Modernize to Azure |
| **Plan Date** | 2026-08-28 |

---

## 🎯 User Selections (Phase 1)

| Decision | Selection |
|----------|-----------|
| **Hosting Platform** | ✅ Azure App Service |
| **Infrastructure as Code** | ✅ Bicep |
| **Database (Source)** | SQL Server (LocalDB) |
| **Database (Target)** | ✅ Azure SQL Database |
| **Resource Group** | `rg-eshop-poc` (placeholder) |
| **Target Region** | East US (`eastus`) |
| **SQL Authentication** | ✅ Microsoft Entra ID + Managed Identity (passwordless) |
| **Azure Subscription** | ⏳ To be provided by CAF team |

---

## 🚦 Phase Status

| Phase | Description | Status | Command |
|-------|-------------|--------|---------|
| **Phase 1** | Plan Migration (gather inputs, high-level plan) | ✅ **Complete** | `/Phase1-Plan-Migration` |
| **Phase 2** | Assess Project (deep code & dependency analysis) | ✅ **Complete** | `/phase2-assessproject` |
| **Phase 3** | Code Migration | ✅ **Complete** | `/phase3-migratecode` |
| **Phase 4** | Infrastructure (Bicep) | ✅ **Complete** — templates generated & validated (placeholders pending CAF) | `/phase4-generateinfra` |
| **Phase 5** | Validation & Deployment | 🔄 **Plan ready** — checklist prepared; execution ⚠️ blocked on CAF subscription | `/phase5-deploytoazure` |

**Legend:** ✅ Complete · 🔄 In Progress · ⬜ Not Started · ⚠️ Blocked

---

## � Phase 2 Key Findings

- **Complexity:** 🟡 Medium — small, clean monolith (3 entities, ~4 controllers, 1 service).
- **Top blockers:** `System.Web` pipeline · EF6 · Autofac MVC5 integration · `System.Web.Optimization` bundling.
- 🔴 **Security (High):** `BinaryFormatter` in `eShopLegacy.Utilities/Serializing.cs` (used by `FilesController`) — insecure deserialization; must be removed.
- 🟠 **Config:** `Integrated Security` connection string won't work on App Service — switch to **Entra ID + Managed Identity**.
- **Recommendation:** Use existing `eShopPorted/` (already on ASP.NET Core + EF Core migrations) as the migration baseline after feature-parity reconciliation.

## ✅ Baseline Decision (Confirmed)

- ✅ **Confirmed:** `eShopPorted/` is the **migration baseline / source of truth** for all modernization work (user-confirmed 2026-08-28).
- The partial port targets `net461` + ASP.NET Core 2.2 + EF Core 2.2 and will be modernized to **.NET 8 + EF Core 8**.
- `src/eShopLegacyMVC/` remains reference-only for feature-parity checks.

---

## 🧾 Phase 3 Migration Checklists

Grounded, per-area checklists created under `reports/checklists/`:

| # | Area | File | Status |
|---|------|------|--------|
| 1 | Controllers | `checklists/01-controllers.md` | ✅ Done |
| 2 | Data Layer (EF6 → EF Core) | `checklists/02-data-layer-ef.md` | ✅ Done |
| 3 | Configuration | `checklists/03-configuration.md` | 🟡 Partial (Key Vault in Phase 4) |
| 4 | Authentication | `checklists/04-authentication.md` | 🟡 Deferred to Phase 4 (Managed Identity) |
| 5 | Logging | `checklists/05-logging.md` | 🟡 Partial (App Insights wired; log4net retained) |
| 6 | API Endpoints | `checklists/06-api-endpoints.md` | ✅ Done |
| 7 | Static Assets | `checklists/07-static-assets.md` | ✅ Done |
| 8 | Infrastructure Preparation | `checklists/08-infrastructure-prep.md` | ✅ App-mod done (Bicep in Phase 4) |
| 9 | Phase 5 Validation & Deployment | `checklists/09-deployment.md` | 🔄 Ready — blocked on CAF subscription |

---

## 📝 Notes & Open Items

- ✅ **Resolved:** reuse `eShopPorted/` confirmed as baseline (no clean re-port).
- Confirm Azure subscription, resource group naming, and target region before Phase 4.
- Confirm SQL authentication approach for Azure SQL (recommend **Microsoft Entra ID / Managed Identity** over SQL auth).

---

## ⏭️ Next Step

➡️ **Phase 5 — execute deployment.** Full plan authored in `reports/checklists/09-deployment.md`
(prerequisites, parameter updates, what-if validation, provisioning, SQL managed-identity setup,
app deploy, post-deployment verification, rollback, sign-off).
When the **CAF Azure subscription** is available:
1. Update `infra/main.parameters.json` (`sqlAadAdminName`, `sqlAadAdminObjectId`).
2. `az account set --subscription <id>`.
3. Run what-if → `az deployment sub create` → deploy app → post-deploy verification.

### Phase 5 Plan Prepared (2026-08-30)

- ✅ Authored `reports/checklists/09-deployment.md` with placeholder values (CAF subscription pending).
- Covers: prerequisites, placeholder-replacement table, what-if validation, subscription-scope provisioning, **Azure SQL contained-user setup for the managed identity**, `dotnet publish` + zip deploy, and full post-deployment verification (endpoints `/`, `/api/brands`, `/api/files`; SQL via managed identity; Key Vault reference; App Insights; security checks), plus rollback and sign-off steps.

### Phase 4 Outcome (2026-08-30) — Templates Generated

- ✅ Authored Bicep under `infra/`: `main.bicep` (sub-scope, creates RG), `resources.bicep` (all resources), `main.parameters.json`.
- ✅ Resources: **Linux App Service (DOTNETCORE|8.0)** + Plan, **Azure SQL Server + DB** (Entra-only auth), **Key Vault** (RBAC), **App Insights** + **Log Analytics**, **User-Assigned Managed Identity**.
- ✅ **Passwordless SQL**: connection string uses `Authentication=Active Directory Default` + UAMI `clientId`; stored in Key Vault and consumed via Key Vault reference.
- ✅ Security hardening: `httpsOnly`, `minTlsVersion 1.2`, `ftpsState Disabled`, Entra-only SQL auth, KV RBAC (`Key Vault Secrets User`).
- ✅ App settings wired: `UseMockData=false`, `AZURE_CLIENT_ID`, `APPLICATIONINSIGHTS_CONNECTION_STRING`, `ConnectionStrings__DefaultConnection` (KV ref).
- ✅ App Service **health check** configured (`healthCheckPath: '/'`) for load-balancer instance probing.
- ✅ Re-validated (2026-08-30): `get_errors` clean on both Bicep files; `az bicep build` succeeds (exit 0).
- ⏳ **Placeholders pending CAF handoff:** subscription ID, Entra SQL admin name + object ID.
- ↪️ Post-deploy manual step: add the managed identity as a **contained DB user** in Azure SQL and grant `db_datareader`/`db_datawriter`/`db_ddladmin` (EF migrations run at startup).

### Phase 3 Outcome (2026-08-28)

- ✅ `eShopPorted/` retargeted `net461` → **net8.0**; `eShopLegacy.Utilities` converted to SDK-style **net8.0**.
- ✅ Legacy packages removed (`Autofac.Mvc5`, `WebGrease`, `Antlr`, `Microsoft.AspNetCore` metapackage, `Newtonsoft.Json`).
- ✅ EF Core `2.2.6` → **8.0.11**; migrations regenerated for Azure SQL (auto-applied on startup when not mock).
- ✅ Minimal hosting (`Program.cs` + `WebApplication`); `Startup.cs` removed; Autofac via `AutofacServiceProviderFactory`.
- ✅ `PicController` ported off `System.Web.Mvc` → `IWebHostEnvironment` + path-traversal guard.
- 🔴→✅ **Security High resolved:** `BinaryFormatter` removed from `Serializing.cs`; `FilesController` now returns JSON.
- ✅ HTTPS redirection + HSTS added; App Insights wired.
- ✅ Build clean (0 errors); app runs on .NET 8; `/`, `/api/brands`, `/api/files` return **200**.
- ✅ Added `Dockerfile`, `.dockerignore`, `build-and-run.ps1/.sh`, `docker-build-run.ps1`.
- ↪️ Deferred to Phase 4: Key Vault secrets, Managed Identity DB auth, full logging modernization.

> This tracker is updated after each completed checklist task.
