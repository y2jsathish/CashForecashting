using ATMCashForecasting.Application.Common;
using ATMCashForecasting.Application.DTOs;
using ATMCashForecasting.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATMCashForecasting.Web.Controllers.Api;

[ApiController]
[Route("api/replenishment")]
[Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
public class ReplenishmentController : ControllerBase
{
    private readonly IReplenishmentService _replenishmentService;

    public ReplenishmentController(IReplenishmentService replenishmentService)
    {
        _replenishmentService = replenishmentService;
    }

    [HttpPost("generate")]
    [Authorize(Roles = $"{Roles.SystemAdministrator},{Roles.CashManagementUser}")]
    public async Task<IActionResult> Generate(CancellationToken ct)
    {
        var count = await _replenishmentService.GenerateRecommendationsAsync(ct);
        return Ok(new { recommendationsGenerated = count });
    }

    [HttpGet]
    public async Task<IActionResult> GetActive(CancellationToken ct)
    {
        var recommendations = await _replenishmentService.GetActiveRecommendationsAsync(ct);
        return Ok(recommendations);
    }

    /// <summary>Records that cash was physically loaded into an ATM, fulfilling a recommendation.</summary>
    [HttpPost("cash-loads")]
    [Authorize(Roles = $"{Roles.SystemAdministrator},{Roles.CashManagementUser}")]
    public async Task<IActionResult> RecordCashLoad([FromBody] RecordCashLoadRequest request, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var result = await _replenishmentService.RecordCashLoadAsync(request, userId, ct);
        if (!result.Succeeded) return BadRequest(new { errors = result.Errors });
        return Ok(new { cashLoadId = result.Value });
    }
}
