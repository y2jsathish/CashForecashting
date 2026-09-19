# Azure Deployment Guide

## Target resources

| Resource | Purpose | Notes |
|---|---|---|
| Azure App Service (Linux, .NET 8) | Hosts `ATMCashForecasting.Web` (MVC + API + Hangfire server) | P1v3+ for production; deployment slots (`staging` → swap to production) |
| Azure SQL Database | Primary datastore | Business Critical or General Purpose tier sized for 5,000+ ATMs × daily transaction rows; enable auto-failover group for DR |
| Azure Cache for Redis | Distributed cache + Hangfire lock coordination across instances | Standard C1+ |
| Azure Key Vault | JWT signing key, SQL connection string, SMTP/SMS/Teams credentials | Referenced from App Service via Key Vault references (`@Microsoft.KeyVault(...)`), not baked into `appsettings.json` |
| Azure Storage (Blob) | CSV/Excel import staging, exported report retention, Serilog archive sink | Private containers, lifecycle policy for retention |
| Azure Front Door / Application Gateway | TLS termination, WAF, global routing | WAF policy tuned to OWASP Core Rule Set |
| Azure Monitor / Application Insights | APM, Serilog sink, alerting | Wire `Serilog.Sinks.ApplicationInsights` in addition to the console sink already configured |
| Microsoft Entra ID | SSO / MFA for staff sign-in (optional, alongside built-in Identity) | See Roadmap — current auth is ASP.NET Identity + JWT, not yet Entra-federated |

## App Service configuration

```bash
az group create -n rg-atmcash-prod -l eastus2

az appservice plan create -g rg-atmcash-prod -n plan-atmcash --sku P1v3 --is-linux

az webapp create -g rg-atmcash-prod -p plan-atmcash -n atmcash-web \
  --runtime "DOTNETCORE:8.0"

az webapp deployment slot create -g rg-atmcash-prod -n atmcash-web --slot staging

# Key Vault references instead of literal secrets:
az webapp config appsettings set -g rg-atmcash-prod -n atmcash-web --settings \
  "ConnectionStrings__DefaultConnection=@Microsoft.KeyVault(SecretUri=https://kv-atmcash.vault.azure.net/secrets/sql-connection-string/)" \
  "Jwt__SigningKey=@Microsoft.KeyVault(SecretUri=https://kv-atmcash.vault.azure.net/secrets/jwt-signing-key/)" \
  "ConnectionStrings__Redis=@Microsoft.KeyVault(SecretUri=https://kv-atmcash.vault.azure.net/secrets/redis-connection-string/)"

az webapp identity assign -g rg-atmcash-prod -n atmcash-web
# then grant that managed identity "Get" on Key Vault secrets, and least-privilege on Azure SQL
# via an Azure AD contained database user instead of SQL auth where possible.
```

## CI/CD

`.github/workflows/ci-cd.yml` builds, tests (failing the pipeline on any red test), scans for
vulnerable NuGet packages, builds/pushes a container image to GHCR, and deploys to the App
Service **staging slot** using `azure/webapps-deploy`. Promotion to production is a manual
slot-swap gated by a GitHub Environment protection rule (`production`), intentionally not
automated — see the roadmap for adding a Bicep/Terraform IaC pipeline and blue/green AKS option.

Required repository secrets/variables:
- `AZURE_CREDENTIALS` — service principal JSON for `azure/login`
- `AZURE_WEBAPP_NAME` — repository variable naming the target App Service

## Scaling for 5,000+ ATMs / 1,000+ concurrent users

- App Service: autoscale rule on CPU (>70%) and HTTP queue length, min 3 instances.
- The nightly forecast job iterates ATMs sequentially per run today (see
  `ATMCashForecasting.Infrastructure/BackgroundJobs/ForecastJob.cs`); at 5,000+ ATMs this should
  be parallelized (e.g., `Parallel.ForEachAsync` with a bounded degree of parallelism, or fanned
  out across multiple Hangfire background workers/queues) before go-live — flagged in
  [ROADMAP.md](ROADMAP.md) rather than done here to avoid over-engineering a path that hasn't
  been load-tested yet.
- Azure SQL: partition/index `AtmTransactions` and `ForecastHistory` by date range if row counts
  grow into the hundreds of millions; consider a read-replica for reporting queries so they
  never contend with the OLTP forecast/replenishment write path.
- Redis-backed `IDistributedCache` is already wired (`AddInfrastructure`) so KPI/dashboard reads
  can be cached with a short TTL once real load-testing identifies the hot paths.

## Kubernetes (AKS) alternative

`k8s/` contains a Deployment (3–12 replica HPA on CPU), Service, Ingress (Application Gateway
Ingress Controller) and a ConfigMap/Secret split — Secret values should come from the Secrets
Store CSI Driver bound to Azure Key Vault in production, not the plain-text
`k8s/secret.example.yaml` placeholder committed here.
