using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RentalApp.Application.Common;
using RentalApp.Application.DTOs.Dashboard;
using RentalApp.Application.Interfaces;

namespace RentalApp.Pages.Dashboard;

[Authorize]
public class IndexModel(IDashboardService dashboardService) : PageModel
{
    private static readonly int[] AllowedPageSizes = [5, 10, 15, 20];

    [BindProperty(SupportsGet = true)]
    [Range(2026, 3000)]
    public int Year { get; set; } = DateTime.UtcNow.Year < 2026 ? 2026 : DateTime.UtcNow.Year;

    [BindProperty(SupportsGet = true)]
    public int ApartmentPageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int UtilityPageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int ExpensePageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 10;

    public string DisplayName { get; private set; } = "User";
    public DashboardSummaryDto Summary { get; private set; } = new();
    public PagedResult<ApartmentTenantDashboardRowDto> ApartmentTenantGrid { get; private set; } = new();
    public PagedResult<UtilityCustomerDashboardRowDto> UtilityCustomerGrid { get; private set; } = new();
    public PagedResult<ExpenseDashboardRowDto> ExpenseGrid { get; private set; } = new();

    public decimal OverallSummaryAmount => (Summary.CollectedRent + Summary.UtilityIncome) - Summary.TotalExpenses;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            Year = 2026;
        }

        if (!AllowedPageSizes.Contains(PageSize))
        {
            PageSize = 10;
        }

        // Disable caching to ensure fresh data
        Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, proxy-revalidate";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";

        DisplayName = User.Identity?.Name ?? "User";
        Summary = await dashboardService.GetSummaryAsync(Year, cancellationToken);

        ApartmentTenantGrid = PagedResult<ApartmentTenantDashboardRowDto>.Create(Summary.ApartmentTenantGrid, ApartmentPageNumber, PageSize);
        UtilityCustomerGrid = PagedResult<UtilityCustomerDashboardRowDto>.Create(Summary.UtilityCustomerGrid, UtilityPageNumber, PageSize);
        ExpenseGrid = PagedResult<ExpenseDashboardRowDto>.Create(Summary.ExpenseGrid, ExpensePageNumber, PageSize);

        return Page();
    }
}
