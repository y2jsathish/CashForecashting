using ATMCashForecasting.Application.Common;
using ATMCashForecasting.Application.DTOs;
using ATMCashForecasting.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATMCashForecasting.Web.Controllers.Api;

[ApiController]
[Route("api/atm")]
[Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
public class AtmController : ControllerBase
{
    private readonly IAtmService _atmService;

    public AtmController(IAtmService atmService)
    {
        _atmService = atmService;
    }

    /// <summary>Search/paginate the ATM master (Module 2).</summary>
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? searchTerm, [FromQuery] int? regionId, [FromQuery] int? branchId,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        var result = await _atmService.SearchAsync(searchTerm, regionId, branchId, pageNumber, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var atm = await _atmService.GetByIdAsync(id, ct);
        return atm is null ? NotFound() : Ok(atm);
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.SystemAdministrator},{Roles.AtmOperationsUser}")]
    public async Task<IActionResult> Create([FromBody] CreateAtmRequest request, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var result = await _atmService.CreateAsync(request, userId, ct);
        if (!result.Succeeded) return BadRequest(new { errors = result.Errors });
        return CreatedAtAction(nameof(GetById), new { id = result.Value }, new { id = result.Value });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.SystemAdministrator},{Roles.AtmOperationsUser}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAtmRequest request, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var result = await _atmService.UpdateAsync(id, request, userId, ct);
        if (!result.Succeeded) return NotFound(new { errors = result.Errors });
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.SystemAdministrator)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var result = await _atmService.DeleteAsync(id, userId, ct);
        if (!result.Succeeded) return NotFound(new { errors = result.Errors });
        return NoContent();
    }

    /// <summary>Bulk import of the ATM master from a previously parsed CSV/Excel file (Module 2).</summary>
    [HttpPost("import")]
    [Authorize(Roles = $"{Roles.SystemAdministrator},{Roles.AtmOperationsUser}")]
    public async Task<IActionResult> Import([FromBody] IReadOnlyList<CreateAtmRequest> rows, CancellationToken ct)
    {
        var succeeded = 0;
        var errors = new List<string>();

        foreach (var row in rows)
        {
            var result = await _atmService.ImportRowAsync(row, ct);
            if (result.Succeeded) succeeded++;
            else errors.AddRange(result.Errors.Select(e => $"{row.AtmCode}: {e}"));
        }

        return Ok(new { totalRows = rows.Count, succeeded, failed = rows.Count - succeeded, errors });
    }
}
