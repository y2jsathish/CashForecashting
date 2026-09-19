using ATMCashForecasting.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ATMCashForecasting.Web.Controllers.Mvc;

public class HomeController : Controller
{
    private readonly IDashboardService _dashboardService;

    public HomeController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>Module 7: role-aware executive dashboard landing page.</summary>
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var kpis = await _dashboardService.GetKpisAsync(ct);
        var regional = await _dashboardService.GetRegionalSummaryAsync(ct);
        var riskDistribution = await _dashboardService.GetRiskDistributionAsync(ct);

        ViewBag.Kpis = kpis;
        ViewBag.Regional = regional;
        ViewBag.RiskDistribution = riskDistribution;

        return View();
    }

    public IActionResult Error() => View();
}
