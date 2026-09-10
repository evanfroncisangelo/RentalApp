using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RentalApp.Application.DTOs.Invoices;
using RentalApp.Application.Interfaces;

namespace RentalApp.Pages.Invoices;

[Authorize]
public class GenerateModel(IInvoiceService invoiceService, ILeaseService leaseService) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<SelectListItem> LeaseOptions { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadOptionsAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadOptionsAsync(cancellationToken);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var invoice = await invoiceService.GenerateFromLeaseAsync(new GenerateInvoiceRequestDto
        {
            LeaseId = Input.LeaseId,
            InvoiceDate = Input.InvoiceDate,
            DueDate = Input.DueDate,
            Notes = null
        }, cancellationToken);

        return RedirectToPage("/Invoices/Details", new { id = invoice.Id });
    }

    private async Task LoadOptionsAsync(CancellationToken cancellationToken)
    {
        var leases = await leaseService.GetAllAsync(cancellationToken);
        LeaseOptions = leases.Select(x => new SelectListItem($"#{x.Id} - {x.TenantName} ({x.UnitNumber})", x.Id.ToString())).ToList();
    }

    public class InputModel
    {
        [Required] public int LeaseId { get; set; }
        [DataType(DataType.Date)] public DateTime? InvoiceDate { get; set; } = DateTime.UtcNow.Date;
        [DataType(DataType.Date)] public DateTime? DueDate { get; set; } = DateTime.UtcNow.Date.AddDays(7);
    }
}
