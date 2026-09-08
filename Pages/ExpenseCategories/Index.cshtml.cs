using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RentalApp.Application.Common;
using RentalApp.Application.DTOs.ExpenseCategories;
using RentalApp.Application.Interfaces;

namespace RentalApp.Pages.ExpenseCategories;

[Authorize]
public class IndexModel(IExpenseCategoryService expenseCategoryService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public PagedResult<ExpenseCategoryDto> PagedItems { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var items = await expenseCategoryService.GetAllAsync(Search, cancellationToken);
        PagedItems = PagedResult<ExpenseCategoryDto>.Create(items, PageNumber, 10);
    }

    public async Task<IActionResult> OnPostDeactivateAsync(int id, CancellationToken cancellationToken)
    {
        await expenseCategoryService.DeactivateAsync(id, cancellationToken);
        return RedirectToPage(new { Search, PageNumber });
    }
}
