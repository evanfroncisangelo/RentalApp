using System.ComponentModel.DataAnnotations;
using System.Text.Json;
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

    public List<SelectListItem> TenantOptions { get; private set; } = [];
    public List<SelectListItem> StatusOptions { get; private set; } = [];
    public string TenantLeaseMapJson { get; private set; } = "[]";
    public string SelectedLeaseLabel { get; private set; } = string.Empty;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadOptionsAsync(cancellationToken);
        ApplyTenantLeaseDefaults();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadOptionsAsync(cancellationToken);
        ApplyTenantLeaseDefaults();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (Input.LeaseId <= 0)
        {
            ModelState.AddModelError(string.Empty, "Selected tenant has no active/ended lease.");
            return Page();
        }

        var created = await invoiceService.CreateAsync(new CreateInvoiceRequestDto
        {
            TenantId = Input.TenantId,
            LeaseId = Input.LeaseId,
            InvoiceDate = Input.InvoiceDate,
            DueDate = Input.DueDate,
            Status = Input.Status,
            Notes = null
        }, cancellationToken);

        var pdfBytes = await invoiceService.GeneratePdfAsync(created.Id, cancellationToken);
        return File(pdfBytes, "application/pdf", $"invoice-{created.InvoiceNumber}.pdf");
    }

    private async Task LoadOptionsAsync(CancellationToken cancellationToken)
    {
        var leases = await leaseService.GetAllAsync(cancellationToken);
        var leaseOptions = leases
            .Where(x => x.Status is LeaseStatus.Active or LeaseStatus.Ended)
            .OrderBy(x => x.TenantName)
            .ThenByDescending(x => x.StartDate)
            .ToList();

        var tenants = await tenantService.GetAllAsync(null, cancellationToken);
        TenantOptions = tenants
            .Where(x => x.IsActive)
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .Select(x => new SelectListItem($"{x.FirstName} {x.LastName}".Trim(), x.Id.ToString()))
            .ToList();

        var tenantLeaseMap = leaseOptions
            .GroupBy(x => x.TenantId)
            .ToDictionary(
                g => g.Key,
                g => g
                    .OrderByDescending(x => x.Status == LeaseStatus.Active)
                    .ThenByDescending(x => x.StartDate)
                    .Select(x => new TenantLeaseOption(
                        x.Id,
                        x.UnitNumber,
                        x.Status.ToString(),
                        x.StartDate.ToString("yyyy-MM-dd")))
                    .ToList());

        TenantLeaseMapJson = JsonSerializer.Serialize(tenantLeaseMap);

        StatusOptions = Enum.GetValues<InvoiceStatus>()
            .Select(x => new SelectListItem(x.ToString(), x.ToString()))
            .ToList();
    }

    private void ApplyTenantLeaseDefaults()
    {
        if (string.IsNullOrWhiteSpace(TenantLeaseMapJson))
        {
            return;
        }

        var map = JsonSerializer.Deserialize<Dictionary<int, List<TenantLeaseOption>>>(TenantLeaseMapJson)
            ?? [];

        if (Input.TenantId <= 0 && TenantOptions.Count > 0 && int.TryParse(TenantOptions[0].Value, out var firstTenantId))
        {
            Input.TenantId = firstTenantId;
        }

        if (Input.TenantId <= 0)
        {
            return;
        }

        if (!map.TryGetValue(Input.TenantId, out var leaseOptions) || leaseOptions.Count == 0)
        {
            Input.LeaseId = 0;
            SelectedLeaseLabel = "No active/ended lease found for selected tenant.";
            return;
        }

        if (!leaseOptions.Any(x => x.LeaseId == Input.LeaseId))
        {
            Input.LeaseId = leaseOptions[0].LeaseId;
        }

        var selected = leaseOptions.FirstOrDefault(x => x.LeaseId == Input.LeaseId) ?? leaseOptions[0];
        SelectedLeaseLabel = $"Lease #{selected.LeaseId} - Unit {selected.UnitNumber} ({selected.LeaseStatus})";
    }

    public class InputModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Tenant is required.")]
        public int TenantId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Lease is required.")]
        public int LeaseId { get; set; }

        [DataType(DataType.Date)]
        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow.Date;

        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; } = DateTime.UtcNow.Date.AddDays(7);

        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    }

    public record TenantLeaseOption(int LeaseId, string UnitNumber, string LeaseStatus, string StartDate);
}
