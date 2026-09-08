using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RentalApp.Application.Common;
using RentalApp.Application.DTOs.Properties;
using RentalApp.Application.Interfaces;

namespace RentalApp.Pages.Properties;

[Authorize]
public class IndexModel(IPropertyService propertyService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public PagedResult<PropertyDto> PagedItems { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var items = await propertyService.GetAllAsync(Search, cancellationToken);
        PagedItems = PagedResult<PropertyDto>.Create(items, PageNumber, 10);
    }

    public async Task<IActionResult> OnPostDeactivateAsync(int id, CancellationToken cancellationToken)
    {
        await propertyService.DeactivateAsync(id, cancellationToken);
        return RedirectToPage(new { Search, PageNumber });
    }
}
