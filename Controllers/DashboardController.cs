using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalApp.Application.DTOs.Dashboard;
using RentalApp.Application.Interfaces;

namespace RentalApp.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary([FromQuery] int? year, CancellationToken cancellationToken)
    {
        var selectedYear = year.GetValueOrDefault(DateTime.UtcNow.Year);
        if (selectedYear < 2026)
        {
            selectedYear = 2026;
        }

        var summary = await dashboardService.GetSummaryAsync(selectedYear, cancellationToken);
        return Ok(summary);
    }
}
