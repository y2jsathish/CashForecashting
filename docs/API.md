# API Reference

Base URL: `https://<host>/api`. All endpoints except `/api/auth/login` require
`Authorization: Bearer <token>` obtained from login. Interactive docs: `/swagger` (Development).

## Auth

| Method | Route | Roles | Description |
|---|---|---|---|
| POST | `/api/auth/login` | anonymous | `{ userName, password, mfaCode? }` → `{ accessToken, expiresAtUtc, userName, roles[], mfaRequired }`. If `mfaRequired` is true and no code was supplied, re-submit with `mfaCode`. |
| POST | `/api/auth/logout` | any authenticated | Audit-logs the logout event. |

## ATM Master (`/api/atm`) — Module 2

| Method | Route | Roles | Description |
|---|---|---|---|
| GET | `/api/atm?searchTerm=&regionId=&branchId=&pageNumber=&pageSize=` | any | Paged ATM search. |
| GET | `/api/atm/{id}` | any | ATM detail. |
| POST | `/api/atm` | SystemAdministrator, AtmOperationsUser | Create ATM. `409`-style validation error if `AtmCode` already exists. |
| PUT | `/api/atm/{id}` | SystemAdministrator, AtmOperationsUser | Update ATM; status changes are recorded to `AtmStatusHistory`. |
| DELETE | `/api/atm/{id}` | SystemAdministrator | Soft-deletes the ATM. |
| POST | `/api/atm/import` | SystemAdministrator, AtmOperationsUser | Bulk import (array of create requests), returns per-row success/failure. |

## Transactions (`/api/transactions`) — Module 3

| Method | Route | Roles | Description |
|---|---|---|---|
| POST | `/api/transactions/import` | SystemAdministrator, AtmOperationsUser, CashManagementUser | JSON array of rows (API integration path). Validated row-by-row; invalid rows are rejected, not the whole batch. |
| POST | `/api/transactions/upload-csv` | same | `multipart/form-data` CSV upload. Header: `AtmCode,TransactionDate,WithdrawalCount,WithdrawalAmount,DepositAmount,RemainingCash,CashLoaded`. |

Response shape (`TransactionImportResultDto`): `{ batchId, totalRows, acceptedRows, rejectedRows, rowResults: [{ rowNumber, atmCode, isValid, errors[] }] }`.

## Forecast (`/api/forecast`) — Module 4

| Method | Route | Roles | Description |
|---|---|---|---|
| POST | `/api/forecast/run` | SystemAdministrator, CashManagementUser | `{ atmIds: int[]|null, method, horizon }`. `atmIds: null` runs every active ATM. Returns run summary (processed/failed counts, duration). |
| GET | `/api/forecast/{atmId}` | any | Current forward-looking forecast points for the ATM. |
| GET | `/api/forecast/{atmId}/accuracy?days=90` | any | Historical forecast-vs-actual + MAPE for the ATM. |
| GET | `/api/forecast/accuracy/overall` | any | Bank-wide forecast accuracy percentage (used by the dashboard KPI). |

`method`: `1`=MovingAverage, `2`=WeightedMovingAverage, `3`=TrendForecasting, `4`=SeasonalForecasting
(`5`-`8` = ML.NET/Prophet/XGBoost/LSTM, reserved for Phase 2 — see [ROADMAP.md](ROADMAP.md)).
`horizon`: `1`, `3`, `7`, `15`, `30` (days).

## Replenishment (`/api/replenishment`) — Module 5

| Method | Route | Roles | Description |
|---|---|---|---|
| POST | `/api/replenishment/generate` | SystemAdministrator, CashManagementUser | Recomputes recommendations for every ATM with a live forecast. |
| GET | `/api/replenishment` | any | Active (unfulfilled) recommendations, highest risk first. |
| POST | `/api/replenishment/cash-loads` | SystemAdministrator, CashManagementUser | Records a physical cash load; if `recommendationId` is supplied, marks it fulfilled. |

## Dashboard (`/api/dashboard`) — Module 7

| Method | Route | Description |
|---|---|---|
| GET | `/api/dashboard/kpis` | Total/active ATMs, cash-out risk count, replenishment due, forecast accuracy %, total cash position. |
| GET | `/api/dashboard/regional-summary` | Cash + at-risk count per region. |
| GET | `/api/dashboard/risk-distribution` | Count of open recommendations per `RiskLevel`. |
| GET | `/api/dashboard/forecast-vs-actual/{atmId}?days=30` | Time series for charting. |

## Alerts (`/api/alerts`) — Module 8

| Method | Route | Description |
|---|---|---|
| GET | `/api/alerts` | Open + acknowledged alerts, highest severity first. |
| POST | `/api/alerts/{id}/acknowledge` | Marks acknowledged by the current user. |
| POST | `/api/alerts/{id}/resolve` | Marks resolved. |

## Reports (`/api/reports`) — Module 9

| Method | Route | Description |
|---|---|---|
| GET | `/api/reports/daily-forecast/excel` | Daily Forecast Report (ATM, Current Cash, Forecast, Recommended Load) as `.xlsx`. |
| GET | `/api/reports/daily-forecast/pdf` | Same report as PDF. |

## Error handling conventions

- `400 Bad Request` — validation failure; body is `{ errors: string[] }`.
- `401 Unauthorized` — missing/expired/invalid JWT, or bad login credentials.
- `403 Forbidden` — authenticated but missing the required role.
- `404 Not Found` — entity id does not exist (or belongs to a soft-deleted record).
- All other unhandled exceptions return `500` and are logged via Serilog with a correlation id
  (`Activity.Current.Id`), surfaced to the client on the MVC error page.
