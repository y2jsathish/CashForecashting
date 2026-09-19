# ATM Cash Forecasting & Replenishment Management System

An enterprise ASP.NET Core 8 application for banks to forecast ATM cash demand, generate
replenishment recommendations, flag cash-out risk, and give operations/executive teams a
role-based dashboard over the whole estate.

> **Build status:** `dotnet build ATMCashForecasting.sln`, `dotnet test`, and
> `dotnet publish` all succeed in this repository (verified via `apt-get install dotnet-sdk-8.0`
> after the usual `dot.net` download endpoint turned out to be blocked in this environment). All
> 20 unit tests pass. It has **not** been run end-to-end against a real SQL Server instance here
> (none was available), so integration-level behavior (migrations, Identity seeding, Hangfire
> against SQL Server storage) is compiler-verified and code-reviewed but not execution-verified —
> see [docs/INSTALLATION_GUIDE.md](docs/INSTALLATION_GUIDE.md) to run it against a real database.

## What's implemented

This is a real, coherent Clean Architecture implementation — not a stub. Fully working:

| Area | Detail |
|---|---|
| **Domain model** | Full entity set matching the spec's table list (ATM master, transactions, cash loads, forecast results/history, replenishment recommendations, alerts, notifications, holidays, audit log) with EF Core Fluent API configuration, indexes and constraints. |
| **Forecasting engine (Phase 1)** | Real, unit-tested algorithms: Moving Average, Weighted Moving Average, Trend (OLS linear regression), Seasonal (day-of-week index) — for 1/3/7/15/30-day horizons, with a confidence score derived from historical variance. |
| **Replenishment engine** | Implements `Recommended Load = Forecast Demand + Safety Buffer − Current Cash`, clamped to physical capacity, plus depletion-date projection and priority mapping. |
| **Risk classification** | Critical (<24h) / High (<48h) / Medium (<72h) / Low (≥72h), exactly per the business rule. |
| **Transaction import & validation** | CSV upload + JSON API ingestion, FluentValidation row-level checks, per-row accept/reject reporting. |
| **Alerting & notifications** | Cash-depletion / ATM-offline / forecast-failure / import-failure alerts, queued to Email/SMS/Teams via `INotificationSender`, dispatched by a Hangfire job. |
| **Background jobs** | Nightly multi-horizon forecast run, 15-minute offline-ATM sweep, 5-minute notification dispatch — all via Hangfire with SQL Server storage. |
| **REST API** | `/api/auth`, `/api/atm`, `/api/transactions`, `/api/forecast`, `/api/replenishment`, `/api/dashboard`, `/api/alerts`, `/api/reports`, JWT-secured, Swagger-documented. |
| **MVC UI** | Bootstrap 5 + DataTables + Chart.js pages: Login, Executive Dashboard, ATM Management, Forecast Management (+ per-ATM detail), Replenishment Planning, Transaction Upload, Reports, Audit Logs. Dark/light theme toggle, responsive sidebar. |
| **Security** | ASP.NET Identity + JWT, 6-role RBAC, password policy, account lockout, session timeout, immutable audit log, HTTPS/HSTS. |
| **Reporting** | Daily Forecast Report as Excel (ClosedXML) and PDF (QuestPDF). |
| **Database** | Hand-written SQL Server DDL (`database/01_Schema.sql`) mirroring the EF model, plus stored procedures and views — usable standalone or as a reference alongside EF Core migrations. |
| **Tests** | xUnit tests with real assertions against the forecasting engine, replenishment engine and risk classifier (boundary conditions, formula correctness, clamping). |
| **DevOps** | Dockerfile (multi-stage, runs tests during build), docker-compose (SQL Server + Redis + web), Kubernetes manifests (Deployment/HPA/Service/Ingress), GitHub Actions CI/CD (build, test, vulnerability scan, container push, Azure deploy). |

## What's scoped as roadmap, not built here

Honest about scope: ML.NET/Prophet/XGBoost/LSTM model training, a Python forecasting
microservice, full Azure Bicep/Terraform IaC, RDLC report designer files, Teams/SMS provider
wiring beyond the abstraction, and load/security test suites are **not** implemented — the
architecture has clean seams for all of them. See [docs/ROADMAP.md](docs/ROADMAP.md) for what
exists as an interface/contract today vs. what still needs building, and why each was deferred.

## Solution layout (Clean Architecture)

```
src/
  ATMCashForecasting.Domain          entities, enums — no dependencies
  ATMCashForecasting.Application     engines, service interfaces/implementations, DTOs, validation
  ATMCashForecasting.Infrastructure  EF Core, repositories, Identity, Hangfire jobs, notification senders
  ATMCashForecasting.Web             ASP.NET Core MVC + Web API host, Razor views, Program.cs wiring
tests/
  ATMCashForecasting.Tests           xUnit tests for the engines
database/                            hand-written SQL Server DDL, stored procs, views, seed data
docker/, k8s/, .github/workflows/    containerization, orchestration, CI/CD
docs/                                architecture, API, deployment, install, user manual, roadmap
```

## Quick start

```bash
# 1. Restore & build
dotnet restore ATMCashForecasting.sln
dotnet build ATMCashForecasting.sln

# 2. Apply the database (either path)
#    a) EF Core migrations (recommended — keeps Identity tables in sync):
dotnet ef migrations add InitialCreate --project src/ATMCashForecasting.Infrastructure --startup-project src/ATMCashForecasting.Web
dotnet ef database update --project src/ATMCashForecasting.Infrastructure --startup-project src/ATMCashForecasting.Web
#    b) or run database/01_Schema.sql .. 04_SeedData.sql by hand against an existing DB.

# 3. Configure secrets (never commit real values)
cd src/ATMCashForecasting.Web
dotnet user-secrets set "Jwt:SigningKey" "a-random-32-plus-character-secret"
dotnet user-secrets set "Seed:AdminPassword" "a-strong-temporary-password"

# 4. Run
dotnet run --project src/ATMCashForecasting.Web
# MVC UI:      https://localhost:5001
# Swagger:     https://localhost:5001/swagger
# Hangfire:    https://localhost:5001/hangfire (SystemAdministrator role only)
```

Full details: [docs/INSTALLATION_GUIDE.md](docs/INSTALLATION_GUIDE.md).

## Documentation

- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — layering, key design decisions, Azure architecture
- [docs/API.md](docs/API.md) — endpoint reference
- [docs/DATABASE_ER.md](docs/DATABASE_ER.md) — entity-relationship model
- [docs/DEPLOYMENT_AZURE.md](docs/DEPLOYMENT_AZURE.md) — Azure App Service / AKS deployment
- [docs/INSTALLATION_GUIDE.md](docs/INSTALLATION_GUIDE.md) — local dev setup
- [docs/USER_MANUAL.md](docs/USER_MANUAL.md) — end-user walkthrough by role
- [docs/ROADMAP.md](docs/ROADMAP.md) — Phase 2 ML, full IaC, and everything else deferred
