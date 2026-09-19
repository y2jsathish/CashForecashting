using ATMCashForecasting.Domain.Enums;

namespace ATMCashForecasting.Application.Risk;

public interface IRiskClassifier
{
    /// <summary>
    /// Classifies risk purely from hours-to-depletion, per the business rules:
    /// Critical &lt; 24h, High &lt; 48h, Medium &lt; 72h, Low &gt;= 72h.
    /// </summary>
    RiskLevel Classify(double hoursToDepletion);
}
