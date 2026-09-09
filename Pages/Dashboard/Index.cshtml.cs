using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RentalApp.Application.DTOs.Dashboard;
using RentalApp.Application.Interfaces;

namespace RentalApp.Pages.Dashboard;

[Authorize]
public class IndexModel(IDashboardService dashboardService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    [Range(2026, 3000)]
    public int Year { get; set; } = DateTime.UtcNow.Year < 2026 ? 2026 : DateTime.UtcNow.Year;

    public string DisplayName { get; private set; } = "User";
    public DashboardSummaryDto Summary { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            Year = 2026;
        }

        // Disable caching to ensure fresh data
        Response.Headers.Add("Cache-Control", "no-store, no-cache, must-revalidate, proxy-revalidate");
        Response.Headers.Add("Pragma", "no-cache");
        Response.Headers.Add("Expires", "0");

        DisplayName = User.Identity?.Name ?? "User";
        Summary = await dashboardService.GetSummaryAsync(Year, cancellationToken);
        return Page();
    }
}
