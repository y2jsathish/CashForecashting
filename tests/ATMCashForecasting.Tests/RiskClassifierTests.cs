using ATMCashForecasting.Application.Risk;
using ATMCashForecasting.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace ATMCashForecasting.Tests;

public class RiskClassifierTests
{
    private readonly RiskClassifier _classifier = new();

    [Theory]
    [InlineData(0, RiskLevel.Critical)]
    [InlineData(23.9, RiskLevel.Critical)]
    [InlineData(24, RiskLevel.High)]
    [InlineData(47.9, RiskLevel.High)]
    [InlineData(48, RiskLevel.Medium)]
    [InlineData(71.9, RiskLevel.Medium)]
    [InlineData(72, RiskLevel.Low)]
    [InlineData(200, RiskLevel.Low)]
    public void Classify_MatchesTheBusinessRuleBandsExactlyAtTheBoundaries(double hours, RiskLevel expected)
    {
        _classifier.Classify(hours).Should().Be(expected);
    }
}
