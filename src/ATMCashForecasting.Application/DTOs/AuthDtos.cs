namespace ATMCashForecasting.Application.DTOs;

public record LoginRequest(string UserName, string Password, string? MfaCode);
public record LoginResponse(string AccessToken, DateTime ExpiresAtUtc, string UserName, IReadOnlyList<string> Roles, bool MfaRequired);
public record RefreshTokenRequest(string RefreshToken);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
