using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATMCashForecasting.Web.Controllers.Mvc;

/// <summary>Module 5 UI shell: replenishment recommendation queue fed from /api/replenishment.</summary>
[Authorize]
public class ReplenishmentPlanningController : Controller
{
    public IActionResult Index() => View();
}
