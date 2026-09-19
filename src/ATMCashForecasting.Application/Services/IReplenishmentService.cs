using ATMCashForecasting.Application.Common.Models;
using ATMCashForecasting.Application.DTOs;

namespace ATMCashForecasting.Application.Services;

public interface IReplenishmentService
{
    Task<int> GenerateRecommendationsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ReplenishmentRecommendationDto>> GetActiveRecommendationsAsync(CancellationToken ct = default);
    Task<Result<int>> RecordCashLoadAsync(RecordCashLoadRequest request, string? userId, CancellationToken ct = default);
}
