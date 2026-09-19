using ATMCashForecasting.Domain.Enums;

namespace ATMCashForecasting.Application.DTOs;

public record AtmListItemDto(
    int Id,
    string AtmCode,
    string AtmName,
    string TerminalId,
    string RegionName,
    string BranchName,
    AtmType AtmType,
    string Currency,
    decimal Capacity,
    AtmOperationalStatus Status);

public record AtmDetailDto(
    int Id,
    string AtmCode,
    string AtmName,
    string TerminalId,
    int RegionId,
    int BranchId,
    decimal Latitude,
    decimal Longitude,
    AtmType AtmType,
    string Currency,
    decimal Capacity,
    decimal SafetyBufferAmount,
    AtmOperationalStatus Status);

public record CreateAtmRequest(
    string AtmCode,
    string AtmName,
    string TerminalId,
    int RegionId,
    int BranchId,
    decimal Latitude,
    decimal Longitude,
    AtmType AtmType,
    string Currency,
    decimal Capacity,
    decimal SafetyBufferAmount);

public record UpdateAtmRequest(
    string AtmName,
    int RegionId,
    int BranchId,
    decimal Latitude,
    decimal Longitude,
    AtmType AtmType,
    string Currency,
    decimal Capacity,
    decimal SafetyBufferAmount,
    AtmOperationalStatus Status);
