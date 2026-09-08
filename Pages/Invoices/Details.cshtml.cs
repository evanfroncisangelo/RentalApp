using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RentalApp.Application.DTOs.Invoices;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;

namespace RentalApp.Pages.Invoices;

[Authorize]
public class DetailsModel(IInvoiceService invoiceService) : PageModel
{
    public InvoiceDto Invoice { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            Invoice = await invoiceService.GetByIdAsync(id, cancellationToken);
            return Page();
        }
        catch (AppNotFoundException)
        {
            return RedirectToPage("/Invoices/Index");
        }
    }
}
