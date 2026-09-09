using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RentalApp.Application.Common;
using RentalApp.Application.DTOs.Utilities;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;

namespace RentalApp.Pages.Utilities.Bills;

[Authorize]
public class IndexModel(
    IUtilityBillService utilityBillService,
    IUtilityCustomerService utilityCustomerService,
    IUtilityPaymentService utilityPaymentService,
    RentalApp.Data.RentalDbContext dbContext) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int? UtilityCustomerId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? UtilityTypeId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? BillingPeriod { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int? ExpandedBillId { get; set; }

    [BindProperty]
    public AddPaymentInputModel AddPaymentInput { get; set; } = new();

    [BindProperty]
    public ApplyCreditInputModel ApplyCreditInput { get; set; } = new();

    [BindProperty]
    public VoidPaymentInputModel VoidPaymentInput { get; set; } = new();

    public PagedResult<UtilityBillDto> PagedItems { get; private set; } = new();
    public IReadOnlyDictionary<int, IReadOnlyList<UtilityBillPaymentDto>> PaidPaymentsByBillId { get; private set; } = new Dictionary<int, IReadOnlyList<UtilityBillPaymentDto>>();
    public List<SelectListItem> CustomerOptions { get; private set; } = [];
    public List<SelectListItem> UtilityTypeOptions { get; private set; } = [];
    public bool ShowPaymentModal { get; private set; }
    public bool ShowApplyCreditModal { get; private set; }
    public bool ShowVoidPaymentModal { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAddPaymentAsync(CancellationToken cancellationToken)
    {
        ModelState.Clear();

        if (!TryValidateModel(AddPaymentInput, nameof(AddPaymentInput)))
        {
            ShowPaymentModal = true;
            ExpandedBillId = AddPaymentInput.UtilityBillId > 0 ? AddPaymentInput.UtilityBillId : ExpandedBillId;
            await LoadAsync(cancellationToken);
            return Page();
        }

        try
        {
            await utilityPaymentService.CreateAsync(new CreateUtilityBillPaymentRequestDto
            {
                UtilityBillId = AddPaymentInput.UtilityBillId,
                Amount = AddPaymentInput.Amount,
                PaymentDate = AddPaymentInput.PaymentDate,
                PaymentMethodId = AddPaymentInput.PaymentMethodId,
                ReferenceNumber = AddPaymentInput.ReferenceNumber,
                Notes = AddPaymentInput.Notes
            }, cancellationToken);
        }
        catch (AppValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ShowPaymentModal = true;
            await LoadAsync(cancellationToken);
            return Page();
        }

        return RedirectToPage(new { UtilityCustomerId, UtilityTypeId, BillingPeriod, PageNumber, ExpandedBillId = AddPaymentInput.UtilityBillId });
    }

    public async Task<IActionResult> OnPostApplyCreditAsync(CancellationToken cancellationToken)
    {
        ModelState.Clear();

        if (!TryValidateModel(ApplyCreditInput, nameof(ApplyCreditInput)))
        {
            ShowApplyCreditModal = true;
            ExpandedBillId = ApplyCreditInput.UtilityBillId > 0 ? ApplyCreditInput.UtilityBillId : ExpandedBillId;
            await LoadAsync(cancellationToken);
            return Page();
        }

        try
        {
            await utilityPaymentService.ApplyCreditAsync(
                ApplyCreditInput.UtilityBillId,
                ApplyCreditInput.Amount,
                ApplyCreditInput.Notes,
                cancellationToken);
        }
        catch (AppValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ShowApplyCreditModal = true;
            await LoadAsync(cancellationToken);
            return Page();
        }

        return RedirectToPage(new { UtilityCustomerId, UtilityTypeId, BillingPeriod, PageNumber, ExpandedBillId = ApplyCreditInput.UtilityBillId });
    }

    public async Task<IActionResult> OnPostVoidPaymentAsync(CancellationToken cancellationToken)
    {
        ModelState.Clear();

        if (!TryValidateModel(VoidPaymentInput, nameof(VoidPaymentInput)))
        {
            ShowVoidPaymentModal = true;
            await LoadAsync(cancellationToken);
            return Page();
        }

        try
        {
            await utilityPaymentService.VoidAsync(VoidPaymentInput.PaymentId, VoidPaymentInput.Reason, cancellationToken);
        }
        catch (AppValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ShowVoidPaymentModal = true;
            await LoadAsync(cancellationToken);
            return Page();
        }

        return RedirectToPage(new { UtilityCustomerId, UtilityTypeId, BillingPeriod, PageNumber, ExpandedBillId });
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var bills = await utilityBillService.GetAllAsync(UtilityCustomerId, UtilityTypeId, BillingPeriod, cancellationToken);
        PagedItems = PagedResult<UtilityBillDto>.Create(bills, PageNumber, 10);

        var paidPayments = await utilityPaymentService.GetAllAsync(null, cancellationToken);
        PaidPaymentsByBillId = paidPayments
            .Where(x => !x.IsVoided)
            .GroupBy(x => x.UtilityBillId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<UtilityBillPaymentDto>)g
                    .OrderByDescending(x => x.PaymentDate)
                    .ThenByDescending(x => x.Id)
                    .ToList());

        var customers = await utilityCustomerService.GetAllAsync(null, cancellationToken);
        CustomerOptions = [new SelectListItem("All", "")];
        CustomerOptions.AddRange(customers
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString(), UtilityCustomerId == x.Id)));

        UtilityTypeOptions = [new SelectListItem("All", "")];
        UtilityTypeOptions.AddRange(await dbContext.UtilityTypes
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString(), UtilityTypeId == x.Id))
            .ToListAsync(cancellationToken));

        if (AddPaymentInput.PaymentDate == default)
        {
            AddPaymentInput.PaymentDate = DateTime.UtcNow.Date;
        }
    }

    public class AddPaymentInputModel
    {
        [Required]
        public int UtilityBillId { get; set; }

        public int? PaymentMethodId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow.Date;

        [Range(0.01, 1000000000)]
        public decimal Amount { get; set; }

        [MaxLength(100)]
        public string? ReferenceNumber { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }

    public class ApplyCreditInputModel
    {
        [Required]
        public int UtilityBillId { get; set; }

        [Range(0.01, 1000000000)]
        public decimal Amount { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }

    public class VoidPaymentInputModel
    {
        [Required]
        public int PaymentId { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }
    }
}
