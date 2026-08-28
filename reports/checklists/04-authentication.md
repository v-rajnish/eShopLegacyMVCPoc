# ✅ Migration Checklist – 4. Authentication

> **Baseline:** `eShopPorted/` has **no application authentication** (anonymous catalog app).
> **Target:** Entra ID for DB access (Managed Identity) + optional app auth on Azure App Service.

---

## Scope

| Concern | Baseline state | Action |
|---------|----------------|--------|
| App user auth | None (anonymous) | Confirm requirement with stakeholders |
| DB auth | `Trusted_Connection=True` (Windows Integrated) | Switch to **Entra ID + Managed Identity** |
| Secrets | Connection string in config | Move to Key Vault |
| Web API auth | `api/brands`, `api/files` open | Decide auth policy |

---

## Tasks

- [ ] Confirm whether app-level sign-in is required (catalog demo is currently anonymous).
- [ ] Replace `Trusted_Connection=True` with Azure SQL **Entra ID authentication** (`Authentication=Active Directory Default`).
- [ ] Enable **System-Assigned Managed Identity** on the App Service (see checklist 8).
- [ ] Grant the Managed Identity a DB user + roles (`db_datareader`, `db_datawriter`) on Azure SQL.
- [ ] Store any fallback secrets in **Key Vault**; grant `get`/`list` secret access to the identity.
- [ ] (If app auth needed) Add `Microsoft.Identity.Web` + Entra ID app registration.
- [ ] (If app auth needed) Protect controllers/APIs with `[Authorize]` and configure sign-in.
- [ ] Enforce HTTPS + HSTS (`UseHttpsRedirection`, `UseHsts`) (Sec finding #5).

---

## Verification

- [ ] App connects to Azure SQL using Managed Identity (no password in config).
- [ ] Key Vault access works via Managed Identity.
- [ ] HTTPS enforced; HTTP redirects to HTTPS.

**Status:** ⬜ Not started
