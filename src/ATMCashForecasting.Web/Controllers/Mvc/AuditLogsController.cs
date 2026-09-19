using ATMCashForecasting.Application.Common;
using ATMCashForecasting.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ATMCashForecasting.Web.Controllers.Mvc;

/// <summary>Module 10: read-only view over the immutable audit trail, restricted to administrators.</summary>
[Authorize(Roles = Roles.SystemAdministrator)]
public class AuditLogsController : Controller
{
    private readonly IUnitOfWork _uow;

    public AuditLogsController(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var logs = await _uow.AuditLogs.Query()
            .OrderByDescending(a => a.TimestampUtc)
            .Take(500)
            .ToListAsync(ct);

        return View(logs);
    }
}
