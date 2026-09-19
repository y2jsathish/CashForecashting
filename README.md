# CashForecasting

ATM Cash Forecasting & Replenishment Management System.

## What is included

This repository now contains a small Node.js forecasting module that:

- calculates average daily ATM withdrawals from transaction history
- forecasts short-term cash demand
- flags ATMs that need replenishment
- produces a prioritized replenishment plan

## Project structure

- `src/atmCashForecasting.js` - core forecasting and replenishment logic
- `index.js` - runnable sample that prints a replenishment plan
- `test/atmCashForecasting.test.js` - targeted unit tests

## Usage

```bash
npm install
npm test
npm start
```

`npm start` prints a sample replenishment plan for two ATMs.

## Data model

Each ATM record requires:

```js
{
  atmId: 'ATM-101',
  location: 'Airport Terminal',
  currentCash: 18000,
  maxCapacity: 50000
}
```

Each withdrawal history record requires:

```js
{
  date: '2026-09-17',
  withdrawalAmount: 5900
}
```

## Core API

```js
const {
  calculateAverageDailyWithdrawal,
  forecastAtmCash,
  buildReplenishmentPlan,
} = require('./src/atmCashForecasting');
```

- `calculateAverageDailyWithdrawal(history, lookbackDays)`
- `forecastAtmCash(atm, history, options)`
- `buildReplenishmentPlan(atms, historyByAtm, options)`
