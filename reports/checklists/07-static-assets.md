# ✅ Migration Checklist – 7. Static Assets

> **Baseline:** `eShopPorted/wwwroot/` (Content, Scripts, Pics, Images, fonts) + Razor Views.
> **Target:** ASP.NET Core static files middleware; replace `System.Web.Optimization` bundling.

---

## Scope

| Item | Baseline state | Action |
|------|----------------|--------|
| `wwwroot/Content/*.css` | Bootstrap + Site + custom CSS | Serve via static files |
| `wwwroot/Scripts/*.js` | jQuery + Bootstrap bundles | Serve via static files |
| `wwwroot/Pics/`, `Images/`, `fonts/` | Catalog images/assets | Serve via static files |
| `Views/**/*.cshtml` | Razor views | Reuse; update bundling helpers |
| Bundling | Legacy `System.Web.Optimization` / `WebGrease` | Replace with static refs or bundler |

---

## Tasks

- [ ] Confirm `app.UseStaticFiles()` is enabled in the .NET 8 pipeline (present in baseline `Startup`).
- [ ] Remove `WebGrease` package reference (`1.6.0`) — legacy bundling dependency.
- [ ] Replace `@Scripts.Render` / `@Styles.Render` helpers in Views with direct `<link>` / `<script>` tags or a modern bundler.
- [ ] Verify `_Layout.cshtml` references resolve to `wwwroot/` paths (`~/Content/...`, `~/Scripts/...`).
- [ ] Ensure `Pics/` images resolve for `PicController` and `PictureUri` (`/Pics/{id}.png`).
- [ ] Confirm font/image assets copy to output and serve correctly.
- [ ] Set cache headers / static file options for production if needed.
- [ ] (Optional) Add minification via `BundlerMinifier` / build-time bundler.

---

## Verification

- [ ] CSS/JS load with HTTP 200 from `wwwroot/`.
- [ ] Catalog pages render with correct styling and scripts.
- [ ] Catalog images display via `/Pics/{id}.png`.

**Status:** ⬜ Not started
