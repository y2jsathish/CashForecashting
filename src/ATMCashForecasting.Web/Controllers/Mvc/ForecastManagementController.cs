using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATMCashForecasting.Web.Controllers.Mvc;

/// <summary>Module 4 UI shell: forecast run trigger + per-ATM detail (Chart.js fed from /api/forecast).</summary>
[Authorize]
public class ForecastManagementController : Controller
{
    public IActionResult Index() => View();

    public IActionResult Detail(int atmId)
    {
        ViewBag.AtmId = atmId;
        return View();
    }
}
