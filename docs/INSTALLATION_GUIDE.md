# Installation Guide (Local Development)

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (LocalDB, a Docker container, or a real instance)
- Optional: Redis (falls back to in-memory distributed cache if `ConnectionStrings:Redis` is empty)
- Optional: Docker Desktop, if you want to run via `docker-compose` instead

## Option A — run natively

```bash
git clone <repo-url>
cd CashForecashting

dotnet restore ATMCashForecasting.sln
dotnet build ATMCashForecasting.sln

# Secrets (never commit these — appsettings.json ships a placeholder signing key on purpose)
cd src/ATMCashForecasting.Web
dotnet user-secrets init
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 48)"
dotnet user-secrets set "Seed:AdminUserName" "admin@yourbank.example"
dotnet user-secrets set "Seed:AdminPassword" "ChangeMe!2026Strong"
cd ../..

# Database — pick ONE:
# (a) EF Core migrations (recommended: creates Identity + domain tables together)
dotnet tool install --global dotnet-ef   # if not already installed
dotnet ef migrations add InitialCreate --project src/ATMCashForecasting.Infrastructure --startup-project src/ATMCashForecasting.Web
dotnet ef database update --project src/ATMCashForecasting.Infrastructure --startup-project src/ATMCashForecasting.Web

# (b) or hand-run the SQL scripts against an existing empty database:
#     sqlcmd -S localhost -d AtmCashForecasting -i database/01_Schema.sql
#     sqlcmd -S localhost -d AtmCashForecasting -i database/02_StoredProcedures.sql
#     sqlcmd -S localhost -d AtmCashForecasting -i database/03_Views.sql
#     sqlcmd -S localhost -d AtmCashForecasting -i database/04_SeedData.sql
#     (Identity tables still need to come from EF Core migrations in this path.)

dotnet run --project src/ATMCashForecasting.Web
```

Then browse to `https://localhost:5001`, sign in with the seeded admin account, and:

1. **ATM Management** → Add ATM (or POST `/api/atm/import` with a bulk list).
2. **Transaction Upload** → upload a CSV of history (need ≥ 3 days per ATM before forecasting
   will produce output).
3. **Forecast Management** → Run Forecast.
4. **Replenishment Planning** → Generate Recommendations.
5. **Dashboard** → KPIs and charts populate from the above.

In development the app also runs `EnsureCreated()` if EF migrations haven't been applied yet
(`Program.cs`), purely as a convenience for a from-scratch local demo — do not rely on this in
any shared or production environment; use migrations there.

## Option B — run via Docker Compose

```bash
cp .env.example .env   # create this yourself: SQL_SA_PASSWORD, JWT_SIGNING_KEY, SEED_ADMIN_PASSWORD
docker compose up --build
```

The `web` service builds the multi-stage `docker/Dockerfile`, which runs `dotnet test` during
the build — the image will not build if the unit tests fail. App listens on `http://localhost:8080`.

## Running tests only

```bash
dotnet test tests/ATMCashForecasting.Tests/ATMCashForecasting.Tests.csproj
```

## Troubleshooting

- **"Jwt:SigningKey is not configured"** — set it via `dotnet user-secrets` (dev) or
  `Jwt__SigningKey` environment variable / Key Vault reference (prod). The placeholder in
  `appsettings.json` is intentionally not a usable secret.
- **Login succeeds but `/api/*` calls return 401 from the browser UI** — the MVC login mints a
  JWT and stashes it in `sessionStorage` via `TempData` on the *next* page load only; if you
  navigate directly to a deep link right after login in a new tab, refresh once.
- **Hangfire dashboard at `/hangfire` returns 403** — only users in the `SystemAdministrator`
  role can view it (`HangfireAdminAuthorizationFilter` in `Program.cs`).
