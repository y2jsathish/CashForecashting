using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATMCashForecasting.Web.Controllers.Mvc;

/// <summary>Module 9 UI shell: links out to the PDF/Excel export endpoints under /api/reports.</summary>
[Authorize]
public class ReportingController : Controller
{
    public IActionResult Index() => View();
}
