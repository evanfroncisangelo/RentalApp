using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RentalApp.Application.DTOs.Invoices;
using RentalApp.Application.Interfaces;
using RentalApp.Domain.Enums;

namespace RentalApp.Pages.Invoices;

[Authorize]
public class CreateModel(IInvoiceService invoiceService, ILeaseService leaseService, ITenantService tenantService) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<SelectListItem> LeaseOptions { get; private set; } = [];
    public List<SelectListItem> TenantOptions { get; private set; } = [];
    public List<SelectListItem> StatusOptions { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        SeedItems();
        await LoadOptionsAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (Input.Items.Count == 0)
        {
            SeedItems();
        }

        await LoadOptionsAsync(cancellationToken);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        await invoiceService.CreateAsync(new CreateInvoiceRequestDto
        {
            TenantId = Input.TenantId,
            LeaseId = Input.LeaseId,
            InvoiceDate = Input.InvoiceDate,
            DueDate = Input.DueDate,
            Status = Input.Status,
            Notes = Input.Notes,
            Items = Input.Items.Select(x => new CreateInvoiceItemRequestDto { Description = x.Description, Amount = x.Amount }).ToList()
        }, cancellationToken);

        return RedirectToPage("/Invoices/Index");
    }

    private async Task LoadOptionsAsync(CancellationToken cancellationToken)
    {
        var leases = await leaseService.GetAllAsync(cancellationToken);
        LeaseOptions = leases.Select(x => new SelectListItem($"#{x.Id} - {x.TenantName} ({x.UnitNumber})", x.Id.ToString())).ToList();

        var tenants = await tenantService.GetAllAsync(null, cancellationToken);
        TenantOptions = tenants.Where(x => x.IsActive).Select(x => new SelectListItem($"{x.FirstName} {x.LastName}", x.Id.ToString())).ToList();

        StatusOptions = Enum.GetValues<InvoiceStatus>().Select(x => new SelectListItem(x.ToString(), x.ToString())).ToList();
    }

    private void SeedItems()
    {
        if (Input.Items.Count == 0)
        {
            Input.Items.Add(new ItemInputModel());
            Input.Items.Add(new ItemInputModel());
            Input.Items.Add(new ItemInputModel());
        }
    }

    public class InputModel
    {
        [Required] public int TenantId { get; set; }
        [Required] public int LeaseId { get; set; }
        [DataType(DataType.Date)] public DateTime InvoiceDate { get; set; } = DateTime.UtcNow.Date;
        [DataType(DataType.Date)] public DateTime DueDate { get; set; } = DateTime.UtcNow.Date.AddDays(7);
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
        public string? Notes { get; set; }
        public List<ItemInputModel> Items { get; set; } = [];
    }

    public class ItemInputModel
    {
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
