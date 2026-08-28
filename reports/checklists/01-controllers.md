# ✅ Migration Checklist – 1. Controllers

> **Baseline:** `eShopPorted/` · **Target:** ASP.NET Core (.NET 8)
> Ports MVC + Web API controllers from the partial `net461` / ASP.NET Core 2.2 baseline to unified .NET 8 controllers.

---

## Scope

| Controller | Baseline state | Action |
|-----------|----------------|--------|
| `Controllers/CatalogController.cs` | ✅ Ported to `Microsoft.AspNetCore.Mvc` | Clean up legacy `Dispose()` pattern |
| `Controllers/PicController.cs` | ⚠️ Still uses `System.Web.Mvc` | Full port to Core MVC |
| `Controllers/Api/BrandsController.cs` | ⚠️ Verify Core `[ApiController]` | Confirm port |
| `Controllers/Api/FilesController.cs` | 🔴 Uses insecure `BinaryFormatter` payload | Remove serializer; return JSON |

---

## Tasks

- [ ] **PicController** – Remove `using System.Web.Mvc;`; base on `Microsoft.AspNetCore.Mvc.Controller`.
- [ ] **PicController** – Replace `HttpStatusCodeResult(HttpStatusCode.BadRequest)` → `BadRequest()`.
- [ ] **PicController** – Replace `HttpNotFound()` → `NotFound()`.
- [ ] **PicController** – Replace commented `Server.MapPath("~/Pics")` with injected `IWebHostEnvironment.WebRootPath`.
- [ ] **PicController** – Guard image path with `Path.GetFileName(item.PictureFileName)` to prevent path traversal (Sec finding #4).
- [ ] **CatalogController** – Remove manual `Dispose(bool)` override; let DI manage `CatalogDBContext`/service lifetime.
- [ ] **CatalogController** – Keep `[ValidateAntiForgeryToken]` + `[Bind(...)]` on Create/Edit (verify property list).
- [ ] **All controllers** – Replace `log4net.ILog` field with injected `ILogger<T>` (see checklist 5).
- [ ] **Routing** – Confirm attribute route `items/{catalogItemId:int}/pic` resolves under endpoint routing.
- [ ] **Build** – `dotnet build` controllers compile with no `System.Web` references.

---

## Verification

- [ ] `GET /` and `/Catalog/Index` render paginated list.
- [ ] `Create` / `Edit` / `Delete` round-trip with anti-forgery token.
- [ ] `GET items/{id}/pic` returns correct image bytes + MIME type.

**Status:** ⬜ Not started
