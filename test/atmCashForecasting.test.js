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

test('forecastAtmCash honors custom planning options', () => {
  const forecast = forecastAtmCash(
    {
      atmId: 'ATM-250',
      location: 'Corporate Park',
      currentCash: 15000,
      maxCapacity: 50000,
    },
    [
      { date: '2026-09-13', withdrawalAmount: 2000 },
      { date: '2026-09-14', withdrawalAmount: 3000 },
      { date: '2026-09-15', withdrawalAmount: 6000 },
      { date: '2026-09-16', withdrawalAmount: 7000 },
    ],
    {
      forecastDays: 2,
      lookbackDays: 2,
      minimumCashRatio: 0.1,
      targetCashRatio: 0.5,
      safetyDays: 2,
    },
  );

  assert.equal(forecast.averageDailyWithdrawal, 6500);
  assert.equal(forecast.forecastedDemand, 13000);
  assert.equal(forecast.minimumCashLevel, 5000);
  assert.equal(forecast.targetCashLevel, 25000);
  assert.equal(forecast.safetyStock, 13000);
  assert.equal(forecast.recommendedReplenishment, 36000);
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

test('forecastAtmCash rejects invalid option values', () => {
  assert.throws(
    () =>
      forecastAtmCash(
        {
          atmId: 'ATM-401',
          location: 'Metro Station',
          currentCash: 10000,
          maxCapacity: 30000,
        },
        [],
        { lookbackDays: 0 },
      ),
    /lookbackDays must be a positive integer/,
  );

  assert.throws(
    () =>
      forecastAtmCash(
        {
          atmId: 'ATM-402',
          location: 'Metro Station',
          currentCash: 10000,
          maxCapacity: 30000,
        },
        [],
        { minimumCashRatio: 1.2 },
      ),
    /minimumCashRatio must be between 0 and 1/,
  );
});

test('forecastAtmCash replenishment considers projected service-time balance', () => {
  const forecast = forecastAtmCash(
    {
      atmId: 'ATM-501',
      location: 'Hospital Lobby',
      currentCash: 49000,
      maxCapacity: 50000,
    },
    [
      { date: '2026-09-15', withdrawalAmount: 15000 },
      { date: '2026-09-16', withdrawalAmount: 15000 },
      { date: '2026-09-17', withdrawalAmount: 15000 },
    ],
    {
      forecastDays: 3,
      targetCashRatio: 0.8,
      safetyDays: 1,
    },
  );

  assert.equal(forecast.projectedCash, 4000);
  assert.equal(forecast.replenish, true);
  assert.equal(forecast.recommendedReplenishment, 46000);
});

test('buildReplenishmentPlan accepts omitted history and rejects invalid history containers', () => {
  const plan = buildReplenishmentPlan([
    {
      atmId: 'ATM-601',
      location: 'Retail Plaza',
      currentCash: 10000,
      maxCapacity: 20000,
    },
  ]);

  assert.equal(plan[0].averageDailyWithdrawal, 0);
  assert.equal(plan[0].recommendedReplenishment, 0);

  assert.throws(
    () =>
      buildReplenishmentPlan(
        [
          {
            atmId: 'ATM-602',
            location: 'Retail Plaza',
            currentCash: 10000,
            maxCapacity: 20000,
          },
        ],
        [],
      ),
    /Withdrawal history must be provided as an object keyed by ATM ID/,
  );
});
