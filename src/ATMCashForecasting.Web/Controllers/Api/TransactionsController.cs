using System.Globalization;
using ATMCashForecasting.Application.Common;
using ATMCashForecasting.Application.DTOs;
using ATMCashForecasting.Application.Services;
using ATMCashForecasting.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATMCashForecasting.Web.Controllers.Api;

[ApiController]
[Route("api/transactions")]
[Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme,
    Roles = $"{Roles.SystemAdministrator},{Roles.AtmOperationsUser},{Roles.CashManagementUser}")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionImportService _importService;

    public TransactionsController(ITransactionImportService importService)
    {
        _importService = importService;
    }

    /// <summary>Programmatic/API integration ingestion (Module 3) — accepts already-parsed rows as JSON.</summary>
    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] IReadOnlyList<TransactionImportRowDto> rows, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var result = await _importService.ImportAsync(rows, TransactionImportSource.ApiIntegration, userId, ct);
        return Ok(result);
    }

    /// <summary>CSV upload endpoint. Expected header: AtmCode,TransactionDate,WithdrawalCount,WithdrawalAmount,DepositAmount,RemainingCash,CashLoaded</summary>
    [HttpPost("upload-csv")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> UploadCsv(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "No file was uploaded." });
        }

        var rows = new List<TransactionImportRowDto>();
        using var reader = new StreamReader(file.OpenReadStream());

        var headerLine = await reader.ReadLineAsync(ct);
        if (headerLine is null)
        {
            return BadRequest(new { message = "File is empty." });
        }

        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split(',');
            if (parts.Length < 7) continue;

            if (!DateOnly.TryParse(parts[1], CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) continue;

            rows.Add(new TransactionImportRowDto(
                parts[0].Trim(),
                date,
                int.TryParse(parts[2], out var wc) ? wc : 0,
                decimal.TryParse(parts[3], NumberStyles.Number, CultureInfo.InvariantCulture, out var wa) ? wa : 0,
                decimal.TryParse(parts[4], NumberStyles.Number, CultureInfo.InvariantCulture, out var da) ? da : 0,
                decimal.TryParse(parts[5], NumberStyles.Number, CultureInfo.InvariantCulture, out var rc) ? rc : 0,
                decimal.TryParse(parts[6], NumberStyles.Number, CultureInfo.InvariantCulture, out var cl) ? cl : 0));
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var result = await _importService.ImportAsync(rows, TransactionImportSource.CsvUpload, userId, ct);
        return Ok(result);
    }
}
