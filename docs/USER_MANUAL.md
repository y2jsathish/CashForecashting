# User Manual

## Roles

| Role | Typical use |
|---|---|
| System Administrator | User/role administration, ATM CRUD, all reports, audit logs, Hangfire dashboard |
| ATM Operations User | ATM master maintenance, transaction upload |
| Cash Management User | Run forecasts, generate/act on replenishment recommendations, record cash loads |
| Regional Manager | Dashboards and reports scoped to their region (region scoping is a roadmap item — see [ROADMAP.md](ROADMAP.md)) |
| Branch Manager | Dashboards and reports scoped to their branch (same scoping note) |
| Executive Management | Executive dashboard, all reports (read-only) |

## Signing in

Go to `/Account/Login`, enter your username/password. If MFA is enabled on your account, you'll
be prompted for a 6-digit authenticator code after a first submit with `mfaRequired: true`
comes back. Sessions time out after 30 minutes of inactivity (sliding).

## Dashboard

Landing page after login. KPI cards: Total ATMs, Active ATMs, Cash-Out Risk count, Replenishment
Due count, Forecast Accuracy %, Total Cash Position. Charts: regional cash/risk bar+line combo,
risk distribution donut. Data refreshes on page load — there is no auto-poll yet (roadmap item).

## ATM Management

Search/filter the ATM master by code, name, terminal id, region, or branch. **Add ATM** opens a
modal for the fields in the spec (code, name, terminal id, region, branch, lat/long, type,
currency, capacity, safety buffer). Bulk import is available via `POST /api/atm/import` (no
dedicated UI file-picker yet for ATM master bulk import — the Transaction Upload page covers
transaction CSVs; see roadmap).

## Transaction Upload

Upload a CSV with header `AtmCode,TransactionDate,WithdrawalCount,WithdrawalAmount,DepositAmount,RemainingCash,CashLoaded`.
The result panel shows a per-row accept/reject breakdown with the specific validation error for
each rejected row (e.g., unknown ATM code, negative amount, future-dated transaction).

## Forecast Management

**Run Forecast** triggers an on-demand run (method + horizon selectable) across every active
ATM — the same operation the nightly Hangfire job performs automatically at 02:00 UTC. **View
Forecast Detail** for a specific ATM id shows a chart of actual vs. forecast for the trailing 30
days plus the forward-looking forecast points, and a table of forecast values with confidence.

## Replenishment Planning

**Generate Recommendations** recomputes the recommendation queue from the latest forecasts and
cash positions. The table is sorted by risk (Critical first) then by projected depletion date.
**Record Load** against a recommendation logs an `AtmCashLoad` and marks the recommendation
fulfilled, removing it from the active queue.

## Reports

Daily Forecast Report is available as Excel or PDF directly from the Reports page (columns: ATM,
Current Cash, Forecast, Recommended Load, Priority, Risk, Projected Depletion Date). Other report
types listed in the spec (Replenishment, ATM Health, Cash-Out, Cost Optimization) are UI
placeholders pointing at [ROADMAP.md](ROADMAP.md) — the underlying data already exists in
`AtmCashLoads` / `AtmStatusHistory` / `ForecastHistory` for a follow-up implementation.

## Audit Logs

System Administrators only. Read-only table of the 500 most recent audit events (login,
create/update/delete, data upload, forecast run). Entries are never edited or deleted by the
application.

## Background jobs (administrators)

`/hangfire` shows the recurring jobs (`nightly-forecast-run`, `atm-offline-scan`,
`notification-dispatch`), their history, and lets an administrator trigger one manually.
