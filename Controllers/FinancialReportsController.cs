using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalApp.Application.DTOs.Finance;
using RentalApp.Application.Interfaces;

namespace RentalApp.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class FinancialReportsController(IFinancialReportService financialReportService) : ControllerBase
{
    [HttpGet("monthly-summary")]
    public async Task<ActionResult<MonthlyFinancialSummaryDto>> GetMonthlySummary([FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken)
    {
        var summary = await financialReportService.GetMonthlySummaryAsync(year, month, cancellationToken);
        return Ok(summary);
    }

    [HttpGet("monthly-history")]
    public async Task<ActionResult<IReadOnlyList<MonthlyFinancialHistoryItemDto>>> GetMonthlyHistory([FromQuery] int monthsBack = 12, CancellationToken cancellationToken = default)
    {
        var history = await financialReportService.GetMonthlyHistoryAsync(monthsBack, cancellationToken);
        return Ok(history);
    }
}
