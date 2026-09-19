using ATMCashForecasting.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATMCashForecasting.Web.Controllers.Api;

[ApiController]
[Route("api/alerts")]
[Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
public class AlertsController : ControllerBase
{
    private readonly IAlertService _alertService;

    public AlertsController(IAlertService alertService)
    {
        _alertService = alertService;
    }

    [HttpGet]
    public async Task<IActionResult> GetOpen(CancellationToken ct)
        => Ok(await _alertService.GetOpenAlertsAsync(ct));

    [HttpPost("{id:int}/acknowledge")]
    public async Task<IActionResult> Acknowledge(int id, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        await _alertService.AcknowledgeAsync(id, userId, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/resolve")]
    public async Task<IActionResult> Resolve(int id, CancellationToken ct)
    {
        await _alertService.ResolveAsync(id, ct);
        return NoContent();
    }
}
