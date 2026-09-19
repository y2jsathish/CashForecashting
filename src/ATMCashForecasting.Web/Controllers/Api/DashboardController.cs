using ATMCashForecasting.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATMCashForecasting.Web.Controllers.Api;

[ApiController]
[Route("api/dashboard")]
[Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("kpis")]
    public async Task<IActionResult> GetKpis(CancellationToken ct)
        => Ok(await _dashboardService.GetKpisAsync(ct));

    [HttpGet("regional-summary")]
    public async Task<IActionResult> GetRegionalSummary(CancellationToken ct)
        => Ok(await _dashboardService.GetRegionalSummaryAsync(ct));

    [HttpGet("risk-distribution")]
    public async Task<IActionResult> GetRiskDistribution(CancellationToken ct)
        => Ok(await _dashboardService.GetRiskDistributionAsync(ct));

    [HttpGet("forecast-vs-actual/{atmId:int}")]
    public async Task<IActionResult> GetForecastVsActual(int atmId, [FromQuery] int days = 30, CancellationToken ct = default)
        => Ok(await _dashboardService.GetForecastVsActualAsync(atmId, days, ct));
}
