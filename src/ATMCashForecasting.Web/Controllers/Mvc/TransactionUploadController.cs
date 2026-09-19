using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATMCashForecasting.Web.Controllers.Mvc;

/// <summary>Module 3 UI shell: CSV/Excel upload form posting to /api/transactions/upload-csv.</summary>
[Authorize]
public class TransactionUploadController : Controller
{
    public IActionResult Index() => View();
}
