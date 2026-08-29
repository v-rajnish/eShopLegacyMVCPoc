# ✅ Migration Checklist – 9. Phase 5: Validation & Deployment

> **Status:** ⬜ Not started — ⚠️ **Blocked** pending CAF Azure subscription.
> **Target:** .NET 8 app (`eShopPorted/`) → **Azure App Service (Linux)** + **Azure SQL** (Entra ID + Managed Identity).
> **IaC:** Bicep — `infra/main.bicep` (subscription scope) + `infra/resources.bicep` + `infra/main.parameters.json`.
> All commands assume **PowerShell (pwsh)** from the repo root `C:\Project\eShopLegacyMVC`.

---

## 🔑 Placeholders to Replace (before deploy)

| Placeholder | Where | Current value | Provided by |
|-------------|-------|---------------|-------------|
| Subscription ID | `az account set` | *(not set)* | CAF team |
| `sqlAadAdminName` | `infra/main.parameters.json` | `PLACEHOLDER-entra-admin@contoso.com` | CAF / platform admin |
| `sqlAadAdminObjectId` | `infra/main.parameters.json` | `00000000-0000-0000-0000-000000000000` | CAF / platform admin (Entra object ID GUID) |
| `resourceGroupName` | `infra/main.parameters.json` | `rg-eshop-poc` | Confirm naming standard |
| `location` | `infra/main.parameters.json` | `eastus` | Confirm approved region |

> Resource names (App Service, SQL Server, Key Vault, etc.) are **auto-generated** from a deterministic
> `uniqueString(...)` token in `resources.bicep`; no manual naming required.

---

## 0. Prerequisites (one-time)

- [ ] **Azure CLI** installed and current — `az version` (upgrade with `az upgrade`).
- [ ] **Bicep** available — `az bicep version` (install with `az bicep install`).
- [ ] **.NET 8 SDK** installed — `dotnet --version` (>= 8.0).
- [ ] Signed in — `az login` (use `--tenant <TENANT-ID>` if CAF uses a specific tenant).
- [ ] Subscription selected — `az account set --subscription "<CAF-SUBSCRIPTION-ID>"`.
- [ ] Confirm caller has rights to: create RG, App Service, Azure SQL, Key Vault, **role assignments** (Owner or User Access Administrator on the RG scope).
- [ ] Obtain the **Entra ID SQL admin** UPN + object ID (`az ad user show --id <upn> --query id -o tsv`).
- [ ] Confirm **region quota/availability** for App Service (B1) and Azure SQL (S0) in the target region.

---

## 1. Update Parameters

- [ ] Edit `infra/main.parameters.json`:
  - [ ] Set `sqlAadAdminName` → real Entra UPN / group name.
  - [ ] Set `sqlAadAdminObjectId` → real object ID (GUID).
  - [ ] Confirm `resourceGroupName`, `location`, `environmentName`, `sqlDatabaseName`.
- [ ] Save and re-validate: `az bicep build --file infra/main.bicep --stdout | Out-Null` (expect exit 0).

---

## 2. Pre-Deployment Validation (What-If)

- [ ] Lint/compile: `az bicep build --file infra/main.bicep` (no errors/warnings).
- [ ] Preview changes (subscription-scope what-if):
  ```powershell
  az deployment sub what-if `
    --name eshop-phase5 `
    --location eastus `
    --template-file infra/main.bicep `
    --parameters infra/main.parameters.json
  ```
- [ ] Review what-if output — confirm expected **Create** for RG + all resources; no unexpected deletes.

---

## 3. Provision Infrastructure

- [ ] Deploy:
  ```powershell
  az deployment sub create `
    --name eshop-phase5 `
    --location eastus `
    --template-file infra/main.bicep `
    --parameters infra/main.parameters.json
  ```
- [ ] Capture outputs (needed for later steps):
  ```powershell
  az deployment sub show --name eshop-phase5 --query properties.outputs -o json
  ```
  - [ ] `WEB_APP_NAME`, `WEB_APP_URL`
  - [ ] `SQL_SERVER_FQDN`, `SQL_DATABASE_NAME`
  - [ ] `KEY_VAULT_NAME`
  - [ ] `MANAGED_IDENTITY_NAME`, `MANAGED_IDENTITY_CLIENT_ID`
  - [ ] `APPLICATION_INSIGHTS_NAME`

---

## 4. Configure Azure SQL Managed-Identity Access (manual, required)

> The app authenticates to SQL with the **user-assigned managed identity** (passwordless).
> The identity must exist as a **contained database user** with data + DDL rights (EF migrations run at startup).

- [ ] Connect to the DB **as the Entra SQL admin** (SSMS, Azure Data Studio, or `sqlcmd -G`), targeting the app database (`eShopPorted`).
- [ ] Run (replace `<MANAGED_IDENTITY_NAME>` with the UAMI name from outputs):
  ```sql
  CREATE USER [<MANAGED_IDENTITY_NAME>] FROM EXTERNAL PROVIDER;
  ALTER ROLE db_datareader ADD MEMBER [<MANAGED_IDENTITY_NAME>];
  ALTER ROLE db_datawriter ADD MEMBER [<MANAGED_IDENTITY_NAME>];
  ALTER ROLE db_ddladmin  ADD MEMBER [<MANAGED_IDENTITY_NAME>];
  ```
- [ ] Verify the user exists: `SELECT name, type_desc FROM sys.database_principals WHERE name = '<MANAGED_IDENTITY_NAME>';`

---

## 5. Build & Deploy Application Code

- [ ] Publish the .NET 8 app:
  ```powershell
  dotnet publish eShopPorted/eShopPorted.csproj -c Release -o publish
  ```
- [ ] Zip the publish output:
  ```powershell
  Compress-Archive -Path publish/* -DestinationPath publish.zip -Force
  ```
- [ ] Deploy to App Service (name from Step 3 outputs):
  ```powershell
  az webapp deploy `
    --resource-group rg-eshop-poc `
    --name <WEB_APP_NAME> `
    --src-path publish.zip --type zip
  ```
- [ ] Confirm app settings applied by Bicep: `UseMockData=false`, `AZURE_CLIENT_ID`, `APPLICATIONINSIGHTS_CONNECTION_STRING`, `ConnectionStrings__DefaultConnection` (Key Vault reference).

---

## 6. Post-Deployment Verification

### Health & Endpoints
- [ ] App responds: `Invoke-WebRequest https://<WEB_APP_URL>/ -UseBasicParsing` → **200**.
- [ ] API: `.../api/brands` → **200** + JSON payload.
- [ ] API: `.../api/files` → **200** (BinaryFormatter-free JSON).
- [ ] Catalog page renders with images (static assets under `wwwroot/`).
- [ ] Health check endpoint `/` green in **App Service → Health check**.

### Data / SQL
- [ ] EF Core migrations applied automatically at startup (tables created in `eShopPorted`).
- [ ] Catalog data loads from Azure SQL (not mock) — verify seeded brands/types/items.
- [ ] No SQL auth errors in logs (confirms managed-identity DB user works).

### Key Vault & Identity
- [ ] Key Vault reference resolves — App Service → Configuration shows **green** on `ConnectionStrings__DefaultConnection`.
- [ ] Managed identity has **Key Vault Secrets User** role (assigned by Bicep).

### Observability
- [ ] Application Insights receives requests/telemetry (Live Metrics / Transaction search).
- [ ] Log Analytics workspace ingesting App Service + App Insights logs.
- [ ] Review **Failures** blade — no unexpected exceptions.

### Security
- [ ] HTTPS-only enforced (HTTP redirects to HTTPS).
- [ ] Min TLS 1.2; FTPS disabled.
- [ ] Azure SQL is **Entra-only** auth (no SQL logins).
- [ ] No secrets in app settings (only Key Vault references).

---

## 7. Rollback / Recovery

- [ ] **App rollback:** redeploy previous `publish.zip`, or use App Service deployment slots / `az webapp deployment list`.
- [ ] **Infra rollback:** re-run deployment with prior parameters, or `az group delete --name rg-eshop-poc` (POC only — destroys all resources).
- [ ] Key Vault soft-delete enabled (7-day retention) — secrets recoverable.

---

## 8. Sign-Off

- [ ] All Section 6 verifications pass.
- [ ] `WEB_APP_URL` recorded and shared.
- [ ] Update `reports/Report-Status.md` → **Phase 5 ✅ Complete**.
- [ ] (Optional) Proceed to **Phase 6** — CI/CD pipeline (`/phase6-setupcicd`).

---

**Status:** ⬜ Not started — deploy pending CAF subscription. Checklist ready to execute once placeholders are filled.
