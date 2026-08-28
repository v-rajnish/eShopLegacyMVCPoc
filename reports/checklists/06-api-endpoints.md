# ✅ Migration Checklist – 6. API Endpoints

> **Baseline:** `eShopPorted/Controllers/Api/` (`BrandsController`, `FilesController`).
> **Target:** ASP.NET Core `[ApiController]` returning `IActionResult`; remove insecure serialization.

---

## Endpoint Surface

| Route | Verb | Handler | Notes |
|-------|------|---------|-------|
| `api/brands` | GET/DELETE | `BrandsController` | DELETE is a demo no-op |
| `api/files` | GET | `FilesController` | 🔴 Returns `BinaryFormatter` payload |
| `items/{catalogItemId:int}/pic` | GET | `PicController` | Image bytes (see checklist 1) |

---

## Tasks

- [ ] **FilesController** – Remove dependency on `eShopLegacy.Utilities/Serializing.cs` (`BinaryFormatter`) (Sec finding #1).
- [ ] **FilesController** – Return payload via `System.Text.Json` (`return Ok(model)` / `JsonResult`).
- [ ] **BrandsController** – Confirm ported to `ControllerBase` + `[ApiController]`; return `Ok()/NotFound()`.
- [ ] Replace any `IHttpActionResult` / `HttpResponseMessage` remnants with `IActionResult`.
- [ ] Confirm attribute routing (`[Route("api/[controller]")]`) resolves under endpoint routing.
- [ ] Add model validation responses (automatic with `[ApiController]`).
- [ ] Decide auth policy for API endpoints (see checklist 4).
- [ ] Remove/retire the trivial `api` stub (`CatalogController2 -> "Hello World"`) if not ported.
- [ ] Delete `eShopLegacy.Utilities` project reference once `BinaryFormatter` usage is gone.

---

## Verification

- [ ] `GET api/brands` returns JSON list; `DELETE` behaves as intended.
- [ ] `GET api/files` returns JSON (no binary serialization).
- [ ] No references to `BinaryFormatter` remain in the solution.

**Status:** ⬜ Not started
