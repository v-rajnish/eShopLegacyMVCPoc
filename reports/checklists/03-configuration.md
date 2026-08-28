# ✅ Migration Checklist – 3. Configuration

> **Baseline:** `eShopPorted/` uses `appsettings.json` + `app.config`. **Target:** `appsettings.json` + App Service config + Key Vault.

---

## Scope

| Item | Baseline state | Action |
|------|----------------|--------|
| `appsettings.json` | Has `DefaultConnection` (LocalDB), `UseMockData`, `UseCustomizationData` | Externalize secrets; retarget DB |
| `appsettings.Development.json` | Dev overrides | Keep for local only |
| `app.config` | Legacy leftover | Remove if unused |
| `Startup.cs` config reads | `Configuration.GetValue<bool>("UseMockData")`, `GetConnectionString` | Keep; layer env vars/Key Vault |

---

## Tasks

- [ ] Remove hardcoded LocalDB connection string from `appsettings.json` (keep placeholder only).
- [ ] Move `DefaultConnection` to App Service Configuration / Key Vault reference (no secrets in repo).
- [ ] Set `UseMockData = false` for cloud/prod; keep `true` only in `appsettings.Development.json`.
- [ ] Add environment-based config layering (`appsettings.{Environment}.json` + env vars).
- [ ] Add `Azure.Extensions.AspNetCore.Configuration.Secrets` + Key Vault provider (Managed Identity).
- [ ] Remove obsolete `app.config` if not referenced by the .NET 8 host.
- [ ] Validate config binding on startup (fail fast if connection string missing when `UseMockData=false`).
- [ ] Ensure `compilation debug` / verbose diagnostics disabled for production (Sec finding #3).

---

## Verification

- [ ] App reads connection string from Key Vault/App Service config in cloud.
- [ ] Local dev still runs with `appsettings.Development.json`.
- [ ] No secrets committed to source control.

**Status:** ⬜ Not started
