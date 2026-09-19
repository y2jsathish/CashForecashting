using ATMCashForecasting.Domain.Entities;

namespace ATMCashForecasting.Application.Common.Interfaces;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) GenerateToken(ApplicationUser user, IReadOnlyList<string> roles);
}
