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
| **Phase 3** | Code Migration | 🔄 **In Progress** | `/phase3-migratecode` |
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

## ✅ Baseline Decision (Confirmed)

- ✅ **Confirmed:** `eShopPorted/` is the **migration baseline / source of truth** for all modernization work (user-confirmed 2026-08-28).
- The partial port targets `net461` + ASP.NET Core 2.2 + EF Core 2.2 and will be modernized to **.NET 8 + EF Core 8**.
- `src/eShopLegacyMVC/` remains reference-only for feature-parity checks.

---

## 🧾 Phase 3 Migration Checklists

Grounded, per-area checklists created under `reports/checklists/`:

| # | Area | File | Status |
|---|------|------|--------|
| 1 | Controllers | `checklists/01-controllers.md` | ⬜ Not started |
| 2 | Data Layer (EF6 → EF Core) | `checklists/02-data-layer-ef.md` | ⬜ Not started |
| 3 | Configuration | `checklists/03-configuration.md` | ⬜ Not started |
| 4 | Authentication | `checklists/04-authentication.md` | ⬜ Not started |
| 5 | Logging | `checklists/05-logging.md` | ⬜ Not started |
| 6 | API Endpoints | `checklists/06-api-endpoints.md` | ⬜ Not started |
| 7 | Static Assets | `checklists/07-static-assets.md` | ⬜ Not started |
| 8 | Infrastructure Preparation | `checklists/08-infrastructure-prep.md` | ⬜ Not started |

---

## 📝 Notes & Open Items

- ✅ **Resolved:** reuse `eShopPorted/` confirmed as baseline (no clean re-port).
- Confirm Azure subscription, resource group naming, and target region before Phase 4.
- Confirm SQL authentication approach for Azure SQL (recommend **Microsoft Entra ID / Managed Identity** over SQL auth).

---

## ⏭️ Next Step

➡️ Begin executing the Phase 3 checklists in `reports/checklists/`. Recommended order:
1. **08 – Infrastructure Prep** (retarget `net461` → `net8.0`, upgrade packages) — unblocks the rest.
2. **06 – API Endpoints** + **01 – Controllers** (remove `BinaryFormatter`, finish `PicController` port).
3. **02 – Data Layer** (EF Core 2.2 → 8, migrations for Azure SQL).
4. **03 – Configuration**, **04 – Authentication**, **05 – Logging**, **07 – Static Assets**.

> This tracker is updated after each completed checklist task.
