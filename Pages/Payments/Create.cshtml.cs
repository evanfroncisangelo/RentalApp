using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RentalApp.Application.DTOs.Payments;
using RentalApp.Application.Interfaces;
using RentalApp.Domain.Enums;

namespace RentalApp.Pages.Payments;

[Authorize]
public class CreateModel(IPaymentService paymentService, ILeaseService leaseService) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<SelectListItem> LeaseOptions { get; private set; } = [];
    public List<SelectListItem> PaymentMethodOptions { get; private set; } = [];
    public Dictionary<int, decimal> LeaseRentMap { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadOptionsAsync(cancellationToken);

        if (Input.LeaseId > 0 && LeaseRentMap.TryGetValue(Input.LeaseId, out var mappedRent))
        {
            Input.Amount = mappedRent;
        }
        else if (LeaseRentMap.Count > 0)
        {
            var firstLeaseId = LeaseRentMap.Keys.First();
            Input.LeaseId = firstLeaseId;
            Input.Amount = LeaseRentMap[firstLeaseId];
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadOptionsAsync(cancellationToken);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        await paymentService.CreateAsync(new CreatePaymentRequestDto
        {
            LeaseId = Input.LeaseId,
            Amount = Input.Amount,
            PaymentDate = Input.PaymentDate,
            PaymentMethod = Input.PaymentMethod,
            ReferenceNumber = Input.ReferenceNumber,
            Notes = Input.Notes
        }, cancellationToken);

        return RedirectToPage("/Payments/Index");
    }

    private async Task LoadOptionsAsync(CancellationToken cancellationToken)
    {
        var leases = await leaseService.GetAllAsync(cancellationToken);
        LeaseRentMap = leases.ToDictionary(x => x.Id, x => x.MonthlyRent);

        LeaseOptions = leases
            .Select(x => new SelectListItem($"#{x.Id} - {x.TenantName} ({x.UnitNumber}) [{x.Status}]", x.Id.ToString()))
            .ToList();

        PaymentMethodOptions = Enum.GetValues<PaymentMethod>()
            .Select(x => new SelectListItem(x.ToString(), x.ToString()))
            .ToList();
    }

    public class InputModel
    {
        [Required]
        [Display(Name = "Lease")]
        public int LeaseId { get; set; }

        [Range(0.01, 100000000)]
        public decimal Amount { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Payment Date")]
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow.Date;

        [Display(Name = "Payment Method")]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        [MaxLength(100)]
        [Display(Name = "Reference Number")]
        public string? ReferenceNumber { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}
