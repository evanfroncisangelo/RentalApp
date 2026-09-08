using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RentalApp.Application.Common;
using RentalApp.Application.DTOs.Tenants;
using RentalApp.Application.Interfaces;

namespace RentalApp.Pages.Tenants;

[Authorize]
public class IndexModel(ITenantService tenantService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public PagedResult<TenantDto> PagedItems { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var items = await tenantService.GetAllAsync(Search, cancellationToken);
        PagedItems = PagedResult<TenantDto>.Create(items, PageNumber, 10);
    }

    public async Task<IActionResult> OnPostDeactivateAsync(int id, CancellationToken cancellationToken)
    {
        await tenantService.DeactivateAsync(id, cancellationToken);
        return RedirectToPage(new { Search, PageNumber });
    }
}
