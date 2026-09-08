using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RentalApp.Application.Common;
using RentalApp.Application.DTOs.Finance;
using RentalApp.Application.Interfaces;

namespace RentalApp.Pages.Finance;

[Authorize]
public class IndexModel(IFinancialReportService financialReportService) : PageModel
{
    [BindProperty(SupportsGet = true), Range(2000, 3000)]
    public int Year { get; set; } = DateTime.UtcNow.Year;

    [BindProperty(SupportsGet = true), Range(1, 12)]
    public int Month { get; set; } = DateTime.UtcNow.Month;

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public MonthlyFinancialSummaryDto Summary { get; private set; } = new();
    public PagedResult<MonthlyFinancialHistoryItemDto> PagedHistory { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Summary = await financialReportService.GetMonthlySummaryAsync(Year, Month, cancellationToken);
        var history = await financialReportService.GetMonthlyHistoryAsync(60, cancellationToken);
        PagedHistory = PagedResult<MonthlyFinancialHistoryItemDto>.Create(history, PageNumber, 10);
    }
}
