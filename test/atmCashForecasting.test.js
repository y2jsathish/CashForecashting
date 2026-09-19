const test = require('node:test');
const assert = require('node:assert/strict');

const {
  buildReplenishmentPlan,
  calculateAverageDailyWithdrawal,
  forecastAtmCash,
} = require('../src/atmCashForecasting');

test('calculateAverageDailyWithdrawal aggregates multiple transactions for the same day', () => {
  const average = calculateAverageDailyWithdrawal([
    { date: '2026-09-15', withdrawalAmount: 3000 },
    { date: '2026-09-15', withdrawalAmount: 2000 },
    { date: '2026-09-16', withdrawalAmount: 4000 },
  ]);

  assert.equal(average, 4500);
});

test('forecastAtmCash recommends replenishment for low projected cash', () => {
  const forecast = forecastAtmCash(
    {
      atmId: 'ATM-201',
      location: 'Mall Entrance',
      currentCash: 10000,
      maxCapacity: 50000,
    },
    [
      { date: '2026-09-15', withdrawalAmount: 4000 },
      { date: '2026-09-16', withdrawalAmount: 4500 },
      { date: '2026-09-17', withdrawalAmount: 4200 },
    ],
  );

  assert.equal(forecast.replenish, true);
  assert.equal(forecast.urgency, 'critical');
  assert.ok(forecast.recommendedReplenishment > 0);
});

test('buildReplenishmentPlan orders urgent ATMs first and leaves healthy ATMs untouched', () => {
  const plan = buildReplenishmentPlan(
    [
      {
        atmId: 'ATM-301',
        location: 'Railway Station',
        currentCash: 7000,
        maxCapacity: 40000,
      },
      {
        atmId: 'ATM-302',
        location: 'University Campus',
        currentCash: 35000,
        maxCapacity: 40000,
      },
    ],
    {
      'ATM-301': [
        { date: '2026-09-15', withdrawalAmount: 5000 },
        { date: '2026-09-16', withdrawalAmount: 4500 },
      ],
      'ATM-302': [
        { date: '2026-09-15', withdrawalAmount: 1200 },
        { date: '2026-09-16', withdrawalAmount: 1300 },
      ],
    },
  );

  assert.equal(plan[0].atmId, 'ATM-301');
  assert.equal(plan[0].replenish, true);
  assert.equal(plan[1].atmId, 'ATM-302');
  assert.equal(plan[1].replenish, false);
  assert.equal(plan[1].recommendedReplenishment, 0);
});
