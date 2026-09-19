using ATMCashForecasting.Application.Common.Interfaces;
using ATMCashForecasting.Application.Common.Models;
using ATMCashForecasting.Application.DTOs;
using ATMCashForecasting.Domain.Entities;
using ATMCashForecasting.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ATMCashForecasting.Application.Services;

public class AtmService : IAtmService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _auditService;

    public AtmService(IUnitOfWork uow, IAuditService auditService)
    {
        _uow = uow;
        _auditService = auditService;
    }

    public async Task<PagedList<AtmListItemDto>> SearchAsync(string? searchTerm, int? regionId, int? branchId, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var query = _uow.Atms.Query()
            .Include(a => a.Region)
            .Include(a => a.Branch)
            .Where(a => !a.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(a =>
                a.AtmCode.Contains(term) ||
                a.AtmName.Contains(term) ||
                a.TerminalId.Contains(term));
        }

        if (regionId.HasValue) query = query.Where(a => a.RegionId == regionId.Value);
        if (branchId.HasValue) query = query.Where(a => a.BranchId == branchId.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(a => a.AtmCode)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AtmListItemDto(
                a.Id, a.AtmCode, a.AtmName, a.TerminalId,
                a.Region.RegionName, a.Branch.BranchName,
                a.AtmType, a.Currency, a.Capacity, a.Status))
            .ToListAsync(ct);

        return PagedList<AtmListItemDto>.Create(items, totalCount, pageNumber, pageSize);
    }

    public async Task<AtmDetailDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var atm = await _uow.Atms.GetByIdAsync(id, ct);
        if (atm is null || atm.IsDeleted) return null;

        return new AtmDetailDto(
            atm.Id, atm.AtmCode, atm.AtmName, atm.TerminalId,
            atm.RegionId, atm.BranchId, atm.Latitude, atm.Longitude,
            atm.AtmType, atm.Currency, atm.Capacity, atm.SafetyBufferAmount, atm.Status);
    }

    public async Task<Result<int>> CreateAsync(CreateAtmRequest request, string? userId, CancellationToken ct = default)
    {
        var duplicate = await _uow.Atms.Query().AnyAsync(a => a.AtmCode == request.AtmCode && !a.IsDeleted, ct);
        if (duplicate)
        {
            return Result<int>.Failure($"An ATM with code '{request.AtmCode}' already exists.");
        }

        var atm = new AtmMaster
        {
            AtmCode = request.AtmCode,
            AtmName = request.AtmName,
            TerminalId = request.TerminalId,
            RegionId = request.RegionId,
            BranchId = request.BranchId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            AtmType = request.AtmType,
            Currency = request.Currency,
            Capacity = request.Capacity,
            SafetyBufferAmount = request.SafetyBufferAmount,
            Status = AtmOperationalStatus.Active,
            CreatedBy = userId
        };

        await _uow.Atms.AddAsync(atm, ct);
        await _uow.SaveChangesAsync(ct);

        await _auditService.LogAsync(AuditAction.Create, nameof(AtmMaster), atm.Id.ToString(), null, atm.AtmCode, userId, ct);

        return Result<int>.Success(atm.Id);
    }

    public async Task<Result<int>> ImportRowAsync(CreateAtmRequest request, CancellationToken ct = default)
        => await CreateAsync(request, "system-import", ct);

    public async Task<Result> UpdateAsync(int id, UpdateAtmRequest request, string? userId, CancellationToken ct = default)
    {
        var atm = await _uow.Atms.GetByIdAsync(id, ct);
        if (atm is null || atm.IsDeleted)
        {
            return Result.Failure("ATM not found.");
        }

        var previousStatus = atm.Status;

        atm.AtmName = request.AtmName;
        atm.RegionId = request.RegionId;
        atm.BranchId = request.BranchId;
        atm.Latitude = request.Latitude;
        atm.Longitude = request.Longitude;
        atm.AtmType = request.AtmType;
        atm.Currency = request.Currency;
        atm.Capacity = request.Capacity;
        atm.SafetyBufferAmount = request.SafetyBufferAmount;
        atm.Status = request.Status;
        atm.ModifiedAtUtc = DateTime.UtcNow;
        atm.ModifiedBy = userId;

        if (previousStatus != request.Status)
        {
            await _uow.AtmStatusHistories.AddAsync(new AtmStatusHistory
            {
                AtmId = atm.Id,
                PreviousStatus = previousStatus,
                NewStatus = request.Status,
                Reason = "Updated via ATM Management"
            }, ct);
        }

        _uow.Atms.Update(atm);
        await _uow.SaveChangesAsync(ct);

        await _auditService.LogAsync(AuditAction.Update, nameof(AtmMaster), atm.Id.ToString(), previousStatus.ToString(), request.Status.ToString(), userId, ct);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, string? userId, CancellationToken ct = default)
    {
        var atm = await _uow.Atms.GetByIdAsync(id, ct);
        if (atm is null || atm.IsDeleted)
        {
            return Result.Failure("ATM not found.");
        }

        atm.IsDeleted = true;
        atm.ModifiedAtUtc = DateTime.UtcNow;
        atm.ModifiedBy = userId;
        _uow.Atms.Update(atm);
        await _uow.SaveChangesAsync(ct);

        await _auditService.LogAsync(AuditAction.Delete, nameof(AtmMaster), atm.Id.ToString(), atm.AtmCode, null, userId, ct);

        return Result.Success();
    }
}
