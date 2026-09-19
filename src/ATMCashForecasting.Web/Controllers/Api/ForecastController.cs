using ATMCashForecasting.Application.Common;
using ATMCashForecasting.Application.DTOs;
using ATMCashForecasting.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATMCashForecasting.Web.Controllers.Api;

[ApiController]
[Route("api/forecast")]
[Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
public class ForecastController : ControllerBase
{
    private readonly IForecastService _forecastService;

    public ForecastController(IForecastService forecastService)
    {
        _forecastService = forecastService;
    }

    /// <summary>Manually triggers a forecast run (Module 4). Normally driven by the nightly Hangfire job.</summary>
    [HttpPost("run")]
    [Authorize(Roles = $"{Roles.SystemAdministrator},{Roles.CashManagementUser}")]
    public async Task<IActionResult> Run([FromBody] RunForecastRequest request, CancellationToken ct)
    {
        var summary = await _forecastService.RunForecastAsync(request, ct);
        return Ok(summary);
    }

    [HttpGet("{atmId:int}")]
    public async Task<IActionResult> GetLatest(int atmId, CancellationToken ct)
    {
        var results = await _forecastService.GetLatestForecastAsync(atmId, ct);
        return Ok(results);
    }

    [HttpGet("{atmId:int}/accuracy")]
    public async Task<IActionResult> GetAccuracy(int atmId, [FromQuery] int days = 90, CancellationToken ct = default)
    {
        var history = await _forecastService.GetAccuracyHistoryAsync(atmId, days, ct);
        return Ok(history);
    }

    [HttpGet("accuracy/overall")]
    public async Task<IActionResult> GetOverallAccuracy(CancellationToken ct)
    {
        var accuracy = await _forecastService.GetOverallForecastAccuracyAsync(ct);
        return Ok(new { forecastAccuracyPercent = accuracy });
    }
}
