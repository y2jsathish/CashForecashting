using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATMCashForecasting.Web.Controllers.Mvc;

/// <summary>Module 2 UI shell. Data is loaded client-side from /api/atm via DataTables.</summary>
[Authorize]
public class AtmManagementController : Controller
{
    public IActionResult Index() => View();
}
