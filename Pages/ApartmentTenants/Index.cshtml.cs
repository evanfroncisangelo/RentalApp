using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RentalApp.Application.Common;
using RentalApp.Application.DTOs.Leases;
using RentalApp.Application.DTOs.Payments;
using RentalApp.Application.DTOs.Properties;
using RentalApp.Application.DTOs.Units;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;
using RentalApp.Domain.Enums;

namespace RentalApp.Pages.ApartmentTenants;

[Authorize]
public class IndexModel(
    ILeaseService leaseService,
    IPaymentService paymentService,
    IUnitService unitService,
    IPropertyService propertyService,
    ITenantService tenantService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? PropertySearch { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PropertiesPageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int DepositsPageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int? ExpandedRoomId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? ExpandedDepositUnitId { get; set; }

    [BindProperty]
    public AddPaymentInputModel AddPaymentInput { get; set; } = new();

    [BindProperty]
    public EditPaymentInputModel EditPaymentInput { get; set; } = new();

    [BindProperty]
    public AddDepositInputModel AddDepositInput { get; set; } = new();

    public string? ErrorMessage { get; private set; }
    public bool ShowAddPaymentModal { get; private set; }
    public bool ShowEditPaymentModal { get; private set; }

    public PagedResult<PropertyDto> PagedProperties { get; private set; } = new();
    public Dictionary<int, IReadOnlyList<UnitDto>> UnitsByPropertyId { get; private set; } = [];

    public PagedResult<PaymentRoomRowViewModel> PagedPaymentRows { get; private set; } = new();
    public Dictionary<int, IReadOnlyList<PaymentHistoryItemViewModel>> PaymentHistoryByRoomId { get; private set; } = [];
    public PagedResult<DepositRowViewModel> PagedDepositRows { get; private set; } = new();
    public Dictionary<int, IReadOnlyList<DepositHistoryItemViewModel>> DepositHistoryByUnitId { get; private set; } = [];
    public List<SelectListItem> PaymentMethodOptions { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadPageDataAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAddPaymentAsync(CancellationToken cancellationToken)
    {
        await LoadPageDataAsync(cancellationToken);
        ModelState.Clear();

        if (!TryValidateModel(AddPaymentInput, nameof(AddPaymentInput)))
        {
            ShowAddPaymentModal = true;
            ExpandedRoomId = AddPaymentInput.RoomId;
            ErrorMessage = string.Join(" ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Take(3));
            return Page();
        }

        var leases = await leaseService.GetAllAsync(cancellationToken);
        var selectedLease = leases.FirstOrDefault(x =>
            x.Id == AddPaymentInput.LeaseId &&
            x.UnitId == AddPaymentInput.RoomId &&
            x.Status is LeaseStatus.Active or LeaseStatus.Ended);

        if (selectedLease is null)
        {
            ErrorMessage = "Selected unit does not have a valid lease for payment.";
            ShowAddPaymentModal = true;
            ExpandedRoomId = AddPaymentInput.RoomId;
            return Page();
        }

        try
        {
            await paymentService.CreateAsync(new CreatePaymentRequestDto
            {
                LeaseId = selectedLease.Id,
                Amount = AddPaymentInput.Amount,
                PaymentDate = AddPaymentInput.PaymentDate,
                PaymentType = PaymentType.Rent,
                PaymentMethod = AddPaymentInput.PaymentMethod
            }, cancellationToken);

            return RedirectToPage(new
            {
                PropertySearch,
                PropertiesPageNumber,
                PageNumber,
                DepositsPageNumber,
                ExpandedRoomId = AddPaymentInput.RoomId
            });
        }
        catch (AppValidationException ex)
        {
            ErrorMessage = ex.Message;
            ShowAddPaymentModal = true;
            ExpandedRoomId = AddPaymentInput.RoomId;
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAddDepositAsync(CancellationToken cancellationToken)
    {
        await LoadPageDataAsync(cancellationToken);
        ModelState.Clear();

        if (!TryValidateModel(AddDepositInput, nameof(AddDepositInput)))
        {
            ErrorMessage = string.Join(" ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Take(3));
            return Page();
        }

        var leases = await leaseService.GetAllAsync(cancellationToken);
        var selectedLease = leases.FirstOrDefault(x =>
            x.Id == AddDepositInput.LeaseId &&
            x.UnitId == AddDepositInput.UnitId &&
            x.Status is LeaseStatus.Active or LeaseStatus.Ended);

        if (selectedLease is null)
        {
            ErrorMessage = "Selected unit does not have a valid lease for deposit payment.";
            return Page();
        }

        try
        {
            await paymentService.CreateAsync(new CreatePaymentRequestDto
            {
                LeaseId = selectedLease.Id,
                Amount = AddDepositInput.Amount,
                PaymentDate = AddDepositInput.PaymentDate,
                PaymentType = PaymentType.Deposit,
                PaymentMethod = AddDepositInput.PaymentMethod
            }, cancellationToken);

            return RedirectToPage(new
            {
                PropertySearch,
                PropertiesPageNumber,
                PageNumber,
                DepositsPageNumber,
                ExpandedRoomId,
                ExpandedDepositUnitId = AddDepositInput.UnitId
            });
        }
        catch (AppValidationException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
    }

    public async Task<IActionResult> OnPostEditPaymentAsync(CancellationToken cancellationToken)
    {
        await LoadPageDataAsync(cancellationToken);
        ModelState.Clear();

        if (!TryValidateModel(EditPaymentInput, nameof(EditPaymentInput)))
        {
            ShowEditPaymentModal = true;
            ExpandedRoomId = EditPaymentInput.RoomId;
            ErrorMessage = string.Join(" ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Take(3));
            return Page();
        }

        try
        {
            await paymentService.UpdateAsync(EditPaymentInput.PaymentId, new UpdatePaymentRequestDto
            {
                Amount = EditPaymentInput.Amount,
                PaymentDate = EditPaymentInput.PaymentDate,
                PaymentMethod = EditPaymentInput.PaymentMethod,
                ReferenceNumber = EditPaymentInput.ReferenceNumber,
                Notes = EditPaymentInput.Notes
            }, cancellationToken);

            return RedirectToPage(new
            {
                PropertySearch,
                PropertiesPageNumber,
                PageNumber,
                DepositsPageNumber,
                ExpandedRoomId = EditPaymentInput.RoomId
            });
        }
        catch (AppValidationException ex)
        {
            ErrorMessage = ex.Message;
            ShowEditPaymentModal = true;
            ExpandedRoomId = EditPaymentInput.RoomId;
            return Page();
        }
    }

    private async Task LoadPageDataAsync(CancellationToken cancellationToken)
    {
        var properties = await propertyService.GetAllAsync(PropertySearch, cancellationToken);
        var units = await unitService.GetAllAsync(null, cancellationToken);
        var leases = await leaseService.GetAllAsync(cancellationToken);
        var payments = await paymentService.GetAllAsync(null, cancellationToken);
        var tenants = await tenantService.GetAllAsync(null, cancellationToken);

        PagedProperties = PagedResult<PropertyDto>.Create(properties, PropertiesPageNumber, 10);
        UnitsByPropertyId = units
            .GroupBy(x => x.PropertyId)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<UnitDto>)x.OrderBy(u => u.UnitNumber).ToList());

        PaymentMethodOptions = Enum.GetValues<PaymentMethod>()
            .Select(x => new SelectListItem(x.ToString(), x.ToString()))
            .ToList();

        var leaseById = leases.ToDictionary(x => x.Id, x => x);

        var activeTenantCountsByUnit = tenants
            .Where(x => x.IsActive && x.UnitId.HasValue)
            .GroupBy(x => x.UnitId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var rentPayments = payments.Where(x => x.PaymentType == PaymentType.Rent).ToList();
        var depositPayments = payments.Where(x => x.PaymentType == PaymentType.Deposit).ToList();

        PaymentHistoryByRoomId = rentPayments
            .Where(x => leaseById.ContainsKey(x.LeaseId))
            .GroupBy(x => leaseById[x.LeaseId].UnitId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<PaymentHistoryItemViewModel>)g
                    .OrderByDescending(x => x.PaymentDate)
                    .ThenByDescending(x => x.Id)
                    .Select(x =>
                    {
                        var lease = leaseById[x.LeaseId];
                        return new PaymentHistoryItemViewModel
                        {
                            Id = x.Id,
                            PaymentDate = x.PaymentDate,
                            DueDate = ResolveDueDate(x.PaymentDate, lease.DueDayOfMonth),
                            Amount = x.Amount,
                            PaymentMethod = x.PaymentMethod,
                            ReferenceNumber = x.ReferenceNumber,
                            Notes = x.Notes
                        };
                    })
                    .ToList());

        var rows = units
            .Where(x => x.IsActive)
            .OrderBy(x => x.PropertyName)
            .ThenBy(x => x.UnitNumber)
            .Select(unit =>
            {
                var unitLeases = leases
                    .Where(x => x.UnitId == unit.Id)
                    .OrderByDescending(x => x.Status == LeaseStatus.Active)
                    .ThenByDescending(x => x.StartDate)
                    .ToList();

                var selectedLease = unitLeases
                    .FirstOrDefault(x => x.Status is LeaseStatus.Active or LeaseStatus.Ended);

                var monthlyRent = unit.MonthlyRent;
                var dueDate = selectedLease is null
                    ? (DateTime?)null
                    : ResolveDueDate(DateTime.UtcNow.Date, selectedLease.DueDayOfMonth);
                var unpaidAmount = selectedLease is null
                    ? 0m
                    : CalculateOutstandingAmount(selectedLease, rentPayments.Where(p => p.LeaseId == selectedLease.Id), DateTime.UtcNow.Date);

                var activeTenantCount = activeTenantCountsByUnit.TryGetValue(unit.Id, out var count)
                    ? count
                    : 0;

                return new PaymentRoomRowViewModel
                {
                    RoomId = unit.Id,
                    UnitRoomLabel = unit.UnitNumber,
                    PropertyName = unit.PropertyName,
                    MonthlyRent = monthlyRent,
                    DueDate = dueDate,
                    TenantCountDisplay = $"{activeTenantCount}/{unit.RoomMaxCapacity}",
                    Status = unit.Status.ToString(),
                    LeaseId = selectedLease?.Id,
                    CanAddPayment = selectedLease is not null && unpaidAmount > 0,
                    DefaultAmount = selectedLease is null ? 0m : Math.Min(monthlyRent, unpaidAmount),
                    UnpaidAmount = unpaidAmount
                };
            })
            .ToList();

        PagedPaymentRows = PagedResult<PaymentRoomRowViewModel>.Create(rows, PageNumber, 10);

        DepositHistoryByUnitId = depositPayments
            .GroupBy(x => x.UnitId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<DepositHistoryItemViewModel>)g
                    .GroupBy(x => x.PaymentDate.Date)
                    .Select(dateGroup => dateGroup
                        .OrderByDescending(x => x.Id)
                        .First())
                    .OrderByDescending(x => x.PaymentDate)
                    .ThenByDescending(x => x.Id)
                    .Select(x => new DepositHistoryItemViewModel
                    {
                        Id = x.Id,
                        Amount = x.Amount,
                        PaymentDate = x.PaymentDate,
                        PaymentMethod = x.PaymentMethod
                    })
                    .ToList());

        var depositsRows = units
            .Where(x => x.IsActive)
            .OrderBy(x => x.PropertyName)
            .ThenBy(x => x.UnitNumber)
            .Select(unit =>
            {
                var activeOrEndedLease = leases
                    .Where(x => x.UnitId == unit.Id && x.Status is LeaseStatus.Active or LeaseStatus.Ended)
                    .OrderByDescending(x => x.Status == LeaseStatus.Active)
                    .ThenByDescending(x => x.StartDate)
                    .FirstOrDefault();

                var latestDeposit = DepositHistoryByUnitId.TryGetValue(unit.Id, out var history)
                    ? history.FirstOrDefault()
                    : null;

                return new DepositRowViewModel
                {
                    UnitId = unit.Id,
                    UnitLabel = unit.UnitNumber,
                    PropertyName = unit.PropertyName,
                    LeaseId = activeOrEndedLease?.Id,
                    DepositAmount = latestDeposit?.Amount,
                    DatePaid = latestDeposit?.PaymentDate,
                    CanAddDeposit = activeOrEndedLease is not null,
                    DepositCount = history?.Count ?? 0
                };
            })
            .ToList();

        PagedDepositRows = PagedResult<DepositRowViewModel>.Create(depositsRows, DepositsPageNumber, 10);

        if (AddPaymentInput.PaymentDate == default)
        {
            AddPaymentInput.PaymentDate = DateTime.UtcNow.Date;
        }

        if (AddDepositInput.PaymentDate == default)
        {
            AddDepositInput.PaymentDate = DateTime.UtcNow.Date;
        }

        if (EditPaymentInput.PaymentDate == default)
        {
            EditPaymentInput.PaymentDate = DateTime.UtcNow.Date;
        }
    }

    public class PaymentRoomRowViewModel
    {
        public int RoomId { get; set; }
        public string UnitRoomLabel { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public decimal MonthlyRent { get; set; }
        public DateTime? DueDate { get; set; }
        public string TenantCountDisplay { get; set; } = "0/0";
        public string Status { get; set; } = string.Empty;
        public int? LeaseId { get; set; }
        public bool CanAddPayment { get; set; }
        public decimal DefaultAmount { get; set; }
        public decimal UnpaidAmount { get; set; }
    }

    public class PaymentHistoryItemViewModel
    {
        public int Id { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Notes { get; set; }
    }

    public class DepositRowViewModel
    {
        public int UnitId { get; set; }
        public string UnitLabel { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public int? LeaseId { get; set; }
        public decimal? DepositAmount { get; set; }
        public DateTime? DatePaid { get; set; }
        public bool CanAddDeposit { get; set; }
        public int DepositCount { get; set; }
    }

    public class DepositHistoryItemViewModel
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
    }

    public class AddPaymentInputModel
    {
        [Required(ErrorMessage = "Unit is required.")]
        public int RoomId { get; set; }

        [Required(ErrorMessage = "Lease is required.")]
        public int LeaseId { get; set; }

        [Range(0.01, 100000000, ErrorMessage = "Amount must be between 0.01 and 100000000.")]
        public decimal Amount { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Payment Date")]
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow.Date;

        [Display(Name = "Payment Method")]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    }

    public class AddDepositInputModel
    {
        [Required(ErrorMessage = "Unit is required.")]
        public int UnitId { get; set; }

        [Required(ErrorMessage = "Lease is required.")]
        public int LeaseId { get; set; }

        [Range(0.01, 100000000, ErrorMessage = "Amount must be between 0.01 and 100000000.")]
        public decimal Amount { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Payment Date")]
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow.Date;

        [Display(Name = "Payment Method")]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    }

    public class EditPaymentInputModel
    {
        [Required]
        public int PaymentId { get; set; }

        [Required]
        public int RoomId { get; set; }

        [Range(0.01, 100000000, ErrorMessage = "Amount must be between 0.01 and 100000000.")]
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

    private static DateTime ResolveDueDate(DateTime date, int dueDayOfMonth)
    {
        var day = Math.Clamp(dueDayOfMonth, 1, 28);
        return new DateTime(date.Year, date.Month, day);
    }

    private static decimal CalculateOutstandingAmount(LeaseDto lease, IEnumerable<PaymentDto> leasePayments, DateTime asOfDate)
    {
        var leaseEnd = lease.EndDate?.Date ?? asOfDate;
        var rangeEnd = leaseEnd > asOfDate ? asOfDate : leaseEnd;
        if (rangeEnd < lease.StartDate.Date)
        {
            return 0m;
        }

        var dueMonths = 0;
        var current = new DateTime(lease.StartDate.Year, lease.StartDate.Month, 1);
        var last = new DateTime(rangeEnd.Year, rangeEnd.Month, 1);

        while (current <= last)
        {
            var dueDate = ResolveDueDate(current, lease.DueDayOfMonth);
            if (dueDate >= lease.StartDate.Date && dueDate <= rangeEnd)
            {
                dueMonths++;
            }

            current = current.AddMonths(1);
        }

        var totalDue = dueMonths * lease.MonthlyRent;
        var totalPaid = leasePayments.Sum(x => x.Amount);
        return Math.Max(0m, totalDue - totalPaid);
    }


}
