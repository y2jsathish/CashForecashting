using ATMCashForecasting.Domain.Enums;

namespace ATMCashForecasting.Application.Risk;

public class RiskClassifier : IRiskClassifier
{
    public RiskLevel Classify(double hoursToDepletion)
    {
        return hoursToDepletion switch
        {
            < 24 => RiskLevel.Critical,
            < 48 => RiskLevel.High,
            < 72 => RiskLevel.Medium,
            _ => RiskLevel.Low
        };
    }
}
