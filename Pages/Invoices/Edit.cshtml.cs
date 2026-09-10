using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RentalApp.Application.DTOs.Invoices;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;
using RentalApp.Domain.Enums;

namespace RentalApp.Pages.Invoices;

[Authorize]
public class EditModel(IInvoiceService invoiceService) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string InvoiceNumber { get; private set; } = string.Empty;
    public List<SelectListItem> StatusOptions { get; private set; } = [];
    public IReadOnlyList<InvoiceItemDto> Items { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        StatusOptions = Enum.GetValues<InvoiceStatus>().Select(x => new SelectListItem(x.ToString(), x.ToString())).ToList();

        try
        {
            var invoice = await invoiceService.GetByIdAsync(id, cancellationToken);
            InvoiceNumber = invoice.InvoiceNumber;
            Items = invoice.Items;
            Input = new InputModel
            {
                Id = invoice.Id,
                InvoiceDate = invoice.InvoiceDate,
                DueDate = invoice.DueDate,
                Status = invoice.Status
            };

            return Page();
        }
        catch (AppNotFoundException)
        {
            return RedirectToPage("/Invoices/Index");
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        StatusOptions = Enum.GetValues<InvoiceStatus>().Select(x => new SelectListItem(x.ToString(), x.ToString())).ToList();

        if (!ModelState.IsValid)
        {
            var invoice = await invoiceService.GetByIdAsync(Input.Id, cancellationToken);
            InvoiceNumber = invoice.InvoiceNumber;
            Items = invoice.Items;
            return Page();
        }

        await invoiceService.UpdateAsync(Input.Id, new UpdateInvoiceRequestDto
        {
            InvoiceDate = Input.InvoiceDate,
            DueDate = Input.DueDate,
            Status = Input.Status,
            Notes = null,
            Items = []
        }, cancellationToken);

        return RedirectToPage("/Invoices/Index");
    }

    public class InputModel
    {
        public int Id { get; set; }

        [DataType(DataType.Date)]
        public DateTime InvoiceDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        public InvoiceStatus Status { get; set; }
    }
}
