using ATMCashForecasting.Application.Services;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ATMCashForecasting.Web.Controllers.Api;

/// <summary>
/// Module 9: Daily Forecast Report (ATM / Current Cash / Forecast / Recommended Load) with
/// PDF and Excel export. Scheduled/emailed delivery of the same report is wired through
/// ForecastJob + a future ReportSchedulerJob (see docs/ROADMAP.md).
/// </summary>
[ApiController]
[Route("api/reports")]
[Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
public class ReportsController : ControllerBase
{
    private readonly IReplenishmentService _replenishmentService;

    static ReportsController()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public ReportsController(IReplenishmentService replenishmentService)
    {
        _replenishmentService = replenishmentService;
    }

    [HttpGet("daily-forecast/excel")]
    public async Task<IActionResult> DailyForecastExcel(CancellationToken ct)
    {
        var rows = await _replenishmentService.GetActiveRecommendationsAsync(ct);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Daily Forecast Report");
        sheet.Cell(1, 1).Value = "ATM Code";
        sheet.Cell(1, 2).Value = "ATM Name";
        sheet.Cell(1, 3).Value = "Current Cash";
        sheet.Cell(1, 4).Value = "Forecast Demand";
        sheet.Cell(1, 5).Value = "Recommended Load";
        sheet.Cell(1, 6).Value = "Priority";
        sheet.Cell(1, 7).Value = "Risk Level";
        sheet.Cell(1, 8).Value = "Projected Depletion Date";
        sheet.Row(1).Style.Font.Bold = true;

        var r = 2;
        foreach (var row in rows)
        {
            sheet.Cell(r, 1).Value = row.AtmCode;
            sheet.Cell(r, 2).Value = row.AtmName;
            sheet.Cell(r, 3).Value = row.CurrentCash;
            sheet.Cell(r, 4).Value = row.ForecastDemand;
            sheet.Cell(r, 5).Value = row.RecommendedLoadAmount;
            sheet.Cell(r, 6).Value = row.Priority.ToString();
            sheet.Cell(r, 7).Value = row.RiskLevel.ToString();
            sheet.Cell(r, 8).Value = row.ProjectedDepletionDate.ToString("yyyy-MM-dd");
            r++;
        }
        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"DailyForecastReport-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    [HttpGet("daily-forecast/pdf")]
    public async Task<IActionResult> DailyForecastPdf(CancellationToken ct)
    {
        var rows = await _replenishmentService.GetActiveRecommendationsAsync(ct);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.Header().Text("Daily Forecast Report").FontSize(18).Bold();
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn(2);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    foreach (var header in new[] { "ATM Code", "ATM Name", "Current Cash", "Forecast", "Recommended Load", "Risk" })
                    {
                        table.Cell().Element(c => c.Border(1).Padding(4)).Text(header).Bold();
                    }

                    foreach (var row in rows)
                    {
                        table.Cell().Element(c => c.Border(1).Padding(4)).Text(row.AtmCode);
                        table.Cell().Element(c => c.Border(1).Padding(4)).Text(row.AtmName);
                        table.Cell().Element(c => c.Border(1).Padding(4)).Text(row.CurrentCash.ToString("C"));
                        table.Cell().Element(c => c.Border(1).Padding(4)).Text(row.ForecastDemand.ToString("C"));
                        table.Cell().Element(c => c.Border(1).Padding(4)).Text(row.RecommendedLoadAmount.ToString("C"));
                        table.Cell().Element(c => c.Border(1).Padding(4)).Text(row.RiskLevel.ToString());
                    }
                });
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Generated ").FontSize(9);
                    x.Span(DateTime.UtcNow.ToString("u")).FontSize(9);
                });
            });
        });

        var bytes = document.GeneratePdf();
        return File(bytes, "application/pdf", $"DailyForecastReport-{DateTime.UtcNow:yyyyMMdd}.pdf");
    }
}
