# Architecture

## Layering (Clean Architecture)

```
┌─────────────────────────────────────────────────────────┐
│  ATMCashForecasting.Web                                  │
│  MVC controllers + views, Web API controllers, Program.cs │
│  depends on ↓                                             │
├─────────────────────────────────────────────────────────┤
│  ATMCashForecasting.Infrastructure                        │
│  EF Core DbContext, Repository/UnitOfWork, ASP.NET        │
│  Identity, Hangfire jobs, notification senders, JWT       │
│  depends on ↓                                             │
├─────────────────────────────────────────────────────────┤
│  ATMCashForecasting.Application                           │
│  Forecasting/Replenishment/Risk engines, service          │
│  interfaces + implementations, DTOs, FluentValidation     │
│  depends on ↓                                             │
├─────────────────────────────────────────────────────────┤
│  ATMCashForecasting.Domain                                │
│  Entities, enums — no outward dependencies                │
└─────────────────────────────────────────────────────────┘
```

Dependencies point inward only. `Domain` has zero package dependencies beyond
`Microsoft.AspNetCore.Identity` (for the `ApplicationUser`/`ApplicationRole` base classes — a
pragmatic exception many Clean Architecture templates make rather than hand-rolling Identity).
`Application` references EF Core **only** for `IQueryable` async LINQ extensions
(`ToListAsync`, `Include`, etc.) against the `IRepository<T>.Query()` abstraction — it does not
reference `DbContext` directly. This is the same trade-off used by the Jason Taylor / Microsoft
"Clean Architecture" ASP.NET Core template.

## Why a single Web project hosts both MVC and the API

Rather than splitting into `ATMCashForecasting.Api` and `ATMCashForecasting.Mvc` projects, both
live in `ATMCashForecasting.Web`: MVC controllers under `Controllers/Mvc`, API controllers under
`Controllers/Api`. This mirrors how most real banking intranet apps are actually deployed (one
app registration, one deployment unit, one Swagger surface) and avoids duplicating
cross-cutting concerns (auth, Serilog, Hangfire dashboard) across two hosts. The trade-off is
that both authentication schemes (Identity cookie for the browser UI, JWT bearer for
`/api/*`) are registered in the same `Program.cs` — documented inline there.

## Request flow (forecast → replenishment → alert)

1. `ForecastJob` (Hangfire, nightly at 02:00 UTC) calls `IForecastService.RunForecastAsync` for
   every active ATM across all five horizons.
2. `ForecastService` pulls up to 120 days of `AtmTransaction` history per ATM, runs it through
   `IForecastingEngine` (Moving Average / WMA / Trend / Seasonal), archives the ATM's previous
   `ForecastResult` rows into `ForecastHistory`, and persists the new results. It also
   back-fills `ActualWithdrawalAmount` / `AbsolutePercentageError` on past forecasts whose
   target date has now occurred, which is what powers the accuracy KPI.
3. `ForecastJob` then calls `IReplenishmentService.GenerateRecommendationsAsync`, which for each
   ATM with a live forecast pulls the latest known cash balance and delegates to
   `IReplenishmentEngine.Recommend` — applying the `Demand + Buffer − CurrentCash` formula,
   clamping to capacity, and projecting a depletion date by walking the forecast curve forward.
4. `IRiskClassifier` buckets the projected depletion into Critical/High/Medium/Low purely from
   hours-to-depletion (the business rule is deliberately isolated in its own single-method
   interface so it can be unit-tested and swapped without touching the replenishment math).
5. Critical/High recommendations raise an `Alert` via `IAlertService`, which queues
   `NotificationLog` rows (Email + Teams always; SMS reserved for Critical to control cost).
   `AlertScanJob` (every 5 minutes) dispatches pending notifications through whichever
   `INotificationSender` implementations are registered for that channel.

## Azure target architecture

```
                         ┌────────────────────┐
  Users ──HTTPS──▶ Azure Front Door / App GW ──▶  Azure App Service (Linux, .NET 8)
                         └────────────────────┘         │  ATMCashForecasting.Web
                                                          │  (Hangfire server co-hosted)
                            ┌─────────────────────────────┼─────────────────────────┐
                            ▼                              ▼                         ▼
                   Azure SQL Database              Azure Cache for Redis     Azure Key Vault
                   (AtmCashForecasting)             (distributed cache,      (JWT signing key,
                   + read replica for               Hangfire lock backing)   SQL/SMTP/Teams
                     BI/reporting workloads                                  secrets via
                                                                              Key Vault references)
                            │
                            ▼
                   Azure Storage (Blob)
                   (CSV/Excel import staging,
                    exported PDF/Excel reports,
                    Serilog log archive)
```

Deployment detail (App Service slots, AKS alternative, scaling) is in
[DEPLOYMENT_AZURE.md](DEPLOYMENT_AZURE.md).

## Key design decisions

- **Soft delete + audit, not hard delete.** `BaseEntity.IsDeleted` plus EF Core global query
  filters on master data (Region/Branch/AtmMaster). Transactional and audit tables are never
  deleted at all — `AuditLog` has no update/delete path by design (Module 10 requirement).
- **One `AtmTransaction` row per ATM per day.** Raw switch/EJ feed granularity is aggregated at
  import time (`UQ_AtmTransactions_Atm_Date` unique constraint) so the forecasting engine always
  sees a dense daily series — this is what makes Moving Average/Trend/Seasonal well-defined.
- **`ForecastResult` (current) vs. `ForecastHistory` (append-only archive).** Every forecast run
  supersedes the previous "live" numbers but nothing is thrown away, which is what makes MAPE /
  forecast-accuracy trend reporting possible.
- **Risk classification isolated from the replenishment formula.** `IRiskClassifier` is a
  single pure function (hours → `RiskLevel`) precisely so the 24/48/72-hour bands can change
  (or become configurable) without touching `ReplenishmentEngine`'s cash-flow math.
- **Repository/UnitOfWork over bare `DbContext` injection.** Chosen for testability of the
  Application layer's services against the `IUnitOfWork` abstraction without booting EF Core.
