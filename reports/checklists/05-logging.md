# ✅ Migration Checklist – 5. Logging

> **Baseline:** `eShopPorted/` uses **log4net** directly (`LogManager.GetLogger`) in controllers.
> **Target:** `Microsoft.Extensions.Logging` (`ILogger<T>`) + Application Insights for ASP.NET Core.

---

## Scope

| Item | Baseline state | Action |
|------|----------------|--------|
| Controllers | `private static readonly ILog _log = LogManager.GetLogger(...)` | Replace with injected `ILogger<T>` |
| log4net package | `log4net 2.0.10` referenced | Remove after migration |
| Telemetry | App Insights (full-framework config in legacy) | Adopt `Microsoft.ApplicationInsights.AspNetCore` |

---

## Tasks

- [ ] Add `Microsoft.Extensions.Logging` usage; inject `ILogger<CatalogController>` / `ILogger<PicController>`.
- [ ] Replace each `_log.Info/Debug` call with `_logger.LogInformation/LogDebug` (preserve messages).
- [ ] Remove `log4net` package reference and `using log4net;` statements.
- [ ] Remove legacy `log4Net.xml` config (no longer used).
- [ ] Add `builder.Services.AddApplicationInsightsTelemetry()` wired to App Insights connection string.
- [ ] Configure log levels via `appsettings.json` `Logging:LogLevel` (already present).
- [ ] Ensure request/dependency correlation flows to App Insights (replaces log4net `LogicalThreadContext`).
- [ ] Confirm no `System.Web`-based logging modules remain.

---

## Verification

- [ ] Log output appears in console + App Insights.
- [ ] No `log4net` references remain in the project.
- [ ] Log levels respect `appsettings.json` configuration.

**Status:** ⬜ Not started
