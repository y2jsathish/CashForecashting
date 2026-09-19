function roundUp(value) {
  return Math.ceil(value);
}

function validatePositiveInteger(value, fieldName) {
  if (!Number.isInteger(value) || value <= 0) {
    throw new RangeError(`${fieldName} must be a positive integer.`);
  }
}

function validateRatio(value, fieldName) {
  if (typeof value !== 'number' || Number.isNaN(value) || value < 0 || value > 1) {
    throw new RangeError(`${fieldName} must be between 0 and 1.`);
  }
}

function validateAtm(atm) {
  if (!atm || typeof atm !== 'object') {
    throw new TypeError('ATM details are required.');
  }

  const requiredFields = ['atmId', 'location', 'currentCash', 'maxCapacity'];
  for (const field of requiredFields) {
    if (atm[field] === undefined || atm[field] === null || atm[field] === '') {
      throw new TypeError(`ATM field ${field} is required.`);
    }
  }

  if (atm.currentCash < 0) {
    throw new RangeError('Current cash cannot be negative.');
  }

  if (atm.maxCapacity <= 0) {
    throw new RangeError('ATM max capacity must be greater than zero.');
  }

  if (atm.currentCash > atm.maxCapacity) {
    throw new RangeError('Current cash cannot exceed ATM max capacity.');
  }
}

function normalizeWithdrawalHistory(withdrawalHistory) {
  if (!Array.isArray(withdrawalHistory)) {
    throw new TypeError('Withdrawal history must be an array.');
  }

  const totalsByDate = new Map();

  for (const record of withdrawalHistory) {
    if (!record || typeof record !== 'object') {
      throw new TypeError('Withdrawal history records must be objects.');
    }

    const { date, withdrawalAmount } = record;
    if (!date) {
      throw new TypeError('Withdrawal history records require a date.');
    }

    if (typeof withdrawalAmount !== 'number' || Number.isNaN(withdrawalAmount) || withdrawalAmount < 0) {
      throw new TypeError('Withdrawal amount must be a non-negative number.');
    }

    totalsByDate.set(date, (totalsByDate.get(date) || 0) + withdrawalAmount);
  }

  return Array.from(totalsByDate.entries())
    .sort(([left], [right]) => left.localeCompare(right))
    .map(([date, total]) => ({ date, withdrawalAmount: total }));
}

function calculateAverageDailyWithdrawal(withdrawalHistory, lookbackDays = 7) {
  validatePositiveInteger(lookbackDays, 'lookbackDays');
  const dailyHistory = normalizeWithdrawalHistory(withdrawalHistory);
  if (dailyHistory.length === 0) {
    return 0;
  }

  const relevantHistory = dailyHistory.slice(-lookbackDays);
  const total = relevantHistory.reduce((sum, record) => sum + record.withdrawalAmount, 0);
  return total / relevantHistory.length;
}

function forecastAtmCash(atm, withdrawalHistory, options = {}) {
  validateAtm(atm);

  const {
    forecastDays = 3,
    lookbackDays = 7,
    minimumCashRatio = 0.2,
    targetCashRatio = 0.8,
    safetyDays = 1,
  } = options;

  validatePositiveInteger(forecastDays, 'forecastDays');
  validatePositiveInteger(lookbackDays, 'lookbackDays');
  validatePositiveInteger(safetyDays, 'safetyDays');
  validateRatio(minimumCashRatio, 'minimumCashRatio');
  validateRatio(targetCashRatio, 'targetCashRatio');

  if (targetCashRatio < minimumCashRatio) {
    throw new RangeError('targetCashRatio must be greater than or equal to minimumCashRatio.');
  }

  const averageDailyWithdrawal = calculateAverageDailyWithdrawal(withdrawalHistory, lookbackDays);
  const forecastedDemand = roundUp(averageDailyWithdrawal * forecastDays);
  const safetyStock = roundUp(averageDailyWithdrawal * safetyDays);
  const projectedCash = atm.currentCash - forecastedDemand;
  const minimumCashLevel = roundUp(atm.maxCapacity * minimumCashRatio);
  const targetCashLevel = roundUp(atm.maxCapacity * targetCashRatio);
  const replenish = projectedCash <= minimumCashLevel;
  const availableCapacityAtServiceTime = atm.maxCapacity - Math.max(projectedCash, 0);
  const recommendedReplenishment = replenish
    ? Math.min(availableCapacityAtServiceTime, Math.max(targetCashLevel + safetyStock - projectedCash, 0))
    : 0;

  return {
    atmId: atm.atmId,
    location: atm.location,
    averageDailyWithdrawal: roundUp(averageDailyWithdrawal),
    forecastDays,
    forecastedDemand,
    projectedCash,
    minimumCashLevel,
    targetCashLevel,
    safetyStock,
    replenish,
    recommendedReplenishment,
    urgency: projectedCash <= 0 ? 'critical' : replenish ? 'high' : 'normal',
  };
}

function buildReplenishmentPlan(atms, withdrawalHistoryByAtm, options = {}) {
  if (!Array.isArray(atms)) {
    throw new TypeError('ATMs must be provided as an array.');
  }

  const plan = atms.map((atm) => {
    const history = withdrawalHistoryByAtm[atm.atmId] || [];
    return forecastAtmCash(atm, history, options);
  });

  return plan.sort((left, right) => {
    const urgencyScore = { critical: 0, high: 1, normal: 2 };
    return urgencyScore[left.urgency] - urgencyScore[right.urgency] || left.projectedCash - right.projectedCash;
  });
}

module.exports = {
  buildReplenishmentPlan,
  calculateAverageDailyWithdrawal,
  forecastAtmCash,
};
