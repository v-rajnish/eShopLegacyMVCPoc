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

---

## 🚦 Phase Status

| Phase | Description | Status | Command |
|-------|-------------|--------|---------|
| **Phase 1** | Plan Migration (gather inputs, high-level plan) | ✅ **Complete** | `/Phase1-Plan-Migration` |
| **Phase 2** | Assess Project (deep code & dependency analysis) | ✅ **Complete** | `/phase2-assessproject` |
| **Phase 3** | Code Migration | ⬜ Not started | `/phase3-migratecode` |
| **Phase 4** | Infrastructure (Bicep) | ⬜ Not started | — |
| **Phase 5** | Validation & Deployment | ⬜ Not started | — |

**Legend:** ✅ Complete · 🔄 In Progress · ⬜ Not Started · ⚠️ Blocked

---

## � Phase 2 Key Findings

- **Complexity:** 🟡 Medium — small, clean monolith (3 entities, ~4 controllers, 1 service).
- **Top blockers:** `System.Web` pipeline · EF6 · Autofac MVC5 integration · `System.Web.Optimization` bundling.
- 🔴 **Security (High):** `BinaryFormatter` in `eShopLegacy.Utilities/Serializing.cs` (used by `FilesController`) — insecure deserialization; must be removed.
- 🟠 **Config:** `Integrated Security` connection string won't work on App Service — switch to **Entra ID + Managed Identity**.
- **Recommendation:** Use existing `eShopPorted/` (already on ASP.NET Core + EF Core migrations) as the migration baseline after feature-parity reconciliation.

## 📝 Notes & Open Items

- ⏳ **Decision needed:** reuse `eShopPorted/` vs. clean re-port (assessment recommends reuse).
- Confirm Azure subscription, resource group naming, and target region before Phase 4.
- Confirm SQL authentication approach for Azure SQL (recommend **Microsoft Entra ID / Managed Identity** over SQL auth).

---

## ⏭️ Next Step

➡️ Run **`/phase3-migratecode`** to begin code modernization (start by removing `BinaryFormatter`, then stand up the ASP.NET Core host).
