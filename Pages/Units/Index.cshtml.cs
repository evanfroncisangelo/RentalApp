using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RentalApp.Application.Common;
using RentalApp.Application.DTOs.Units;
using RentalApp.Application.Interfaces;

namespace RentalApp.Pages.Units;

[Authorize]
public class IndexModel(IUnitService unitService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public PagedResult<UnitDto> PagedItems { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var items = await unitService.GetAllAsync(Search, cancellationToken);
        PagedItems = PagedResult<UnitDto>.Create(items, PageNumber, 10);
    }

    public async Task<IActionResult> OnPostDeactivateAsync(int id, CancellationToken cancellationToken)
    {
        await unitService.DeactivateAsync(id, cancellationToken);
        return RedirectToPage(new { Search, PageNumber });
    }
}
