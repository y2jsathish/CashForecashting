const { buildReplenishmentPlan } = require('./src/atmCashForecasting');

const atms = [
  {
    atmId: 'ATM-101',
    location: 'Airport Terminal',
    currentCash: 18000,
    maxCapacity: 50000,
  },
  {
    atmId: 'ATM-102',
    location: 'Downtown Branch',
    currentCash: 42000,
    maxCapacity: 50000,
  },
];

const withdrawalHistoryByAtm = {
  'ATM-101': [
    { date: '2026-09-13', withdrawalAmount: 5500 },
    { date: '2026-09-14', withdrawalAmount: 6000 },
    { date: '2026-09-15', withdrawalAmount: 5800 },
    { date: '2026-09-16', withdrawalAmount: 6100 },
    { date: '2026-09-17', withdrawalAmount: 5900 },
  ],
  'ATM-102': [
    { date: '2026-09-13', withdrawalAmount: 1800 },
    { date: '2026-09-14', withdrawalAmount: 2200 },
    { date: '2026-09-15', withdrawalAmount: 2100 },
    { date: '2026-09-16', withdrawalAmount: 1900 },
    { date: '2026-09-17', withdrawalAmount: 2000 },
  ],
};

console.log(JSON.stringify(buildReplenishmentPlan(atms, withdrawalHistoryByAtm), null, 2));
