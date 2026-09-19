using ATMCashForecasting.Application.Common.Models;
using ATMCashForecasting.Application.DTOs;

namespace ATMCashForecasting.Application.Services;

public interface IAtmService
{
    Task<PagedList<AtmListItemDto>> SearchAsync(string? searchTerm, int? regionId, int? branchId, int pageNumber, int pageSize, CancellationToken ct = default);
    Task<AtmDetailDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<int>> CreateAsync(CreateAtmRequest request, string? userId, CancellationToken ct = default);
    Task<Result> UpdateAsync(int id, UpdateAtmRequest request, string? userId, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, string? userId, CancellationToken ct = default);
    Task<Result<int>> ImportRowAsync(CreateAtmRequest request, CancellationToken ct = default);
}
