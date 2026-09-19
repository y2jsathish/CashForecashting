# Roadmap — what's deferred, and where it plugs in

This build prioritized a coherent, working core (real forecasting/replenishment/risk math, a
full data model, a working API + UI, tests, containerization and CI/CD) over shallow coverage of
every item in the original 24-deliverable spec. What follows is deferred, with the seam already
in place for each.

## Phase 2 forecasting models (ML.NET / Prophet / XGBoost / LSTM)

`IForecastingEngine.Forecast` currently throws `NotSupportedException` for
`ForecastMethod.MLNet / Prophet / XGBoost / Lstm` (see `ForecastingEngine.cs`). The intended
shape:

- **ML.NET** — a second implementation of `IForecastingEngine` (or a new
  `IMLForecastProvider` called from `ForecastService` when `Method` is 5–8), trained offline via
  ML.NET's `SsaForecastingEstimator` per-ATM or per-cluster-of-ATMs, model artifacts stored in
  Azure Blob Storage and loaded via `PredictionEnginePool`.
- **Prophet / XGBoost** — genuinely need Python (Prophet has no maintained .NET port; XGBoost's
  .NET bindings are far behind the Python library). The clean seam is a small FastAPI
  microservice (`forecasting-service/`, not created here) that `ForecastService` calls over
  HTTP/gRPC, passing the same `HistoricalPoint` series and getting back the same
  `ForecastPoint` shape — no change needed to `ReplenishmentEngine` or anything downstream.
- **LSTM** — same microservice, via TensorFlow/PyTorch, or via ML.NET's TensorFlow bindings if
  kept in-process.
- Model retraining/versioning, feature engineering (holiday uplift from `HolidayMaster` is
  modeled in the schema but not yet fed into any forecasting method), and A/B comparison of
  methods per ATM are all Phase 2 work.

## Reporting

- RDLC report definitions (the spec explicitly asks for RDLC) — the Daily Forecast Report is
  implemented today via ClosedXML (Excel) and QuestPDF (PDF) instead, which are simpler to keep
  license-clean and don't require Report Viewer control wiring in Razor. Porting to RDLC is
  straightforward if the organization has existing RDLC tooling/standards to align with.
- Replenishment Report, ATM Health Report, Cash-Out Report, Cost Optimization Report — the
  underlying data (`AtmCashLoads`, `AtmStatusHistory`, `ForecastHistory`) already exists; only
  the query + export endpoint is missing. `ReportsController` (API) is the place to add them.
- Scheduled report delivery (email a PDF nightly) — add a `ReportSchedulerJob` alongside
  `ForecastJob`/`AlertScanJob`, reusing `INotificationSender` (Email channel) with an attachment.

## Region/branch-scoped authorization

Regional Manager and Branch Manager roles exist (`Roles.cs`) but every service currently returns
bank-wide data — there's no `RegionId`/`BranchId` filter applied based on the signed-in user's
assigned region/branch (`ApplicationUser.RegionId` / `BranchId` are already on the entity).
Wiring this is a filter added to `AtmService.SearchAsync`, `DashboardService`, etc., keyed off
`ICurrentUserService`, plus corresponding `[Authorize(Policy = "RegionScoped")]` policies.

## Azure Infrastructure-as-Code

`docs/DEPLOYMENT_AZURE.md` documents the target architecture and gives `az cli` commands; it is
not a Bicep/Terraform module. Productionizing this is templating those resources (App Service,
SQL, Redis, Key Vault, Storage, Front Door) as IaC with environment-specific parameter files and
wiring `terraform plan`/`apply` (or `az deployment group create`) into a CI/CD stage gated by
approval.

## MFA, Teams, SMS — providers beyond the abstraction

- MFA: `ApplicationUser.IsMfaEnabled` / `MfaSecretKey` and the TOTP verification call
  (`UserManager.VerifyTwoFactorTokenAsync`) are wired in `AuthController`, but there's no
  enrollment UI (QR code generation/display) yet.
  - `EmailNotificationSender` is a real SMTP client; `SmsNotificationSender` and
  `TeamsNotificationSender` post to a configurable gateway/webhook URL (`NotificationOptions`)
  but need an actual provider chosen and its specific payload/auth format applied (Twilio,
  Azure Communication Services SMS, etc. for SMS; the Teams webhook shape is already correct for
  a basic Incoming Webhook connector, but Adaptive Cards would look better than the current
  plain-markdown payload).

## Scale hardening for 5,000+ ATMs / 1,000+ concurrent users

- `ForecastJob.RunNightlyForecastAsync` processes ATMs sequentially per horizon today; needs
  bounded parallelism (`Parallel.ForEachAsync`) or Hangfire batch/fan-out before running against
  the full estate — flagged rather than guessed-at because the right degree of parallelism
  depends on SQL DTU/vCore headroom that should be measured, not assumed.
- No caching is applied yet to the dashboard KPI queries even though `IDistributedCache` (Redis)
  is wired in `AddInfrastructure` — add it once real load-testing shows which reads are hot.
- Load testing (k6/JMeter/Azure Load Testing) against the 1,000-concurrent-user /
  2-second-response-time targets has not been run in this environment (no such tooling
  available here) and should gate any go-live decision.

## Testing depth

Unit tests cover the three engines (forecasting math, replenishment formula, risk bands) with
real, deterministic assertions. Not yet added: integration tests against a real/test SQL Server
(e.g., via Testcontainers), API contract tests (WebApplicationFactory), UI/E2E tests (Playwright),
and a measured coverage number against the 80% target — the scaffolding
(`ATMCashForecasting.Tests.csproj`) is ready to add all of these as separate xUnit trait
categories.

## Why these were deferred rather than stubbed

Everything above could have been faked with placeholder classes that compile but do nothing.
That would look more "complete" in a file listing while being actively misleading about what
works. Instead, every module in the original spec has either a genuine working implementation or
an honest gap documented here with the exact extension point to close it.
