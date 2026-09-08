using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RentalApp.Application.Common;
using RentalApp.Application.DTOs.Invoices;
using RentalApp.Application.Interfaces;

namespace RentalApp.Pages.Invoices;

[Authorize]
public class IndexModel(IInvoiceService invoiceService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public PagedResult<InvoiceDto> PagedItems { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var items = await invoiceService.GetAllAsync(cancellationToken);
        PagedItems = PagedResult<InvoiceDto>.Create(items, PageNumber, 10);
    }
}
