using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RentalApp.Application.Common;
using RentalApp.Application.DTOs.Leases;
using RentalApp.Application.Interfaces;

namespace RentalApp.Pages.Leases;

[Authorize]
public class IndexModel(ILeaseService leaseService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public PagedResult<LeaseDto> PagedItems { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var items = await leaseService.GetAllAsync(cancellationToken);
        PagedItems = PagedResult<LeaseDto>.Create(items, PageNumber, 10);
    }
}
