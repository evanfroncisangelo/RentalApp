using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RentalApp.Application.Common;
using RentalApp.Application.DTOs.Leases;
using RentalApp.Application.DTOs.Payments;
using RentalApp.Application.DTOs.Properties;
using RentalApp.Application.DTOs.Tenants;
using RentalApp.Application.DTOs.Units;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;
using RentalApp.Data;
using RentalApp.Domain.Enums;

namespace RentalApp.Pages.ApartmentTenants;

[Authorize]
public class IndexModel(
    ITenantService tenantService,
    ILeaseService leaseService,
    IPaymentService paymentService,
    IUnitService unitService,
    IPropertyService propertyService,
    RentalDbContext dbContext) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public string? PropertySearch { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PropertiesPageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int? ExpandedTenantId { get; set; }

    [BindProperty]
    public TenantInputModel TenantInput { get; set; } = new();

    [BindProperty]
    public ApartmentInputModel ApartmentInput { get; set; } = new();

    [BindProperty]
    public AddTenantPaymentInputModel AddTenantPaymentInput { get; set; } = new();

    public string? ErrorMessage { get; private set; }
    public bool ShowAddTenantModal { get; private set; }
    public bool ShowAddTenantPaymentModal { get; private set; }
    public int PaymentModalTenantId { get; private set; }
    public int PaymentModalLeaseId { get; private set; }

    public PagedResult<PropertyDto> PagedProperties { get; private set; } = new();
    public Dictionary<int, IReadOnlyList<UnitDto>> UnitsByPropertyId { get; private set; } = [];

    public PagedResult<TenantDto> PagedTenants { get; private set; } = new();
    public Dictionary<int, LeaseDto> ActiveApartmentByTenantId { get; private set; } = [];
    public Dictionary<int, IReadOnlyList<PaymentDto>> PaymentHistoryByTenantId { get; private set; } = [];

    public List<SelectListItem> UnitOptions { get; private set; } = [];
    public Dictionary<int, IReadOnlyList<LeaseDto>> PaymentLeaseOptionsByTenantId { get; private set; } = [];
    public Dictionary<int, Dictionary<int, decimal>> PaymentLeaseRentMapByTenantId { get; private set; } = [];
    public Dictionary<int, IReadOnlyList<UnpaidLeaseItemViewModel>> UnpaidLeasesByTenantId { get; private set; } = [];
    public List<SelectListItem> PaymentMethodOptions { get; private set; } = [];
    public List<SelectListItem> TenantApartmentRoomOptions { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadPageDataAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAddTenantApartmentAsync(CancellationToken cancellationToken)
    {
        await LoadPageDataAsync(cancellationToken, ApartmentInput.UnitId);

        ModelState.Clear();

        var tenantValid = TryValidateModel(TenantInput, nameof(TenantInput));
        var apartmentValid = TryValidateModel(ApartmentInput, nameof(ApartmentInput));

        if (!tenantValid || !apartmentValid)
        {
            ShowAddTenantModal = true;
            ErrorMessage = string.Join(" ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Take(3));
            return Page();
        }

        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var tenant = await tenantService.CreateAsync(new CreateTenantRequestDto
            {
                FirstName = TenantInput.FirstName,
                LastName = TenantInput.LastName,
                ContactNumber = TenantInput.ContactNumber,
                Email = TenantInput.Email,
                Address = TenantInput.Address,
                DateOfBirth = TenantInput.DateOfBirth,
                Notes = TenantInput.Notes
            }, cancellationToken);

            await leaseService.CreateAsync(new CreateLeaseRequestDto
            {
                TenantId = tenant.Id,
                UnitId = ApartmentInput.UnitId,
                RoomId = ApartmentInput.RoomId,
                StartDate = ApartmentInput.StartDate,
                EndDate = ApartmentInput.EndDate,
                MonthlyRent = ApartmentInput.MonthlyRent,
                SecurityDeposit = ApartmentInput.SecurityDeposit,
                DueDayOfMonth = ApartmentInput.DueDayOfMonth,
                Notes = ApartmentInput.Notes
            }, cancellationToken);

            await tx.CommitAsync(cancellationToken);
            return RedirectToPage(new
            {
                Search,
                PropertySearch,
                PropertiesPageNumber,
                PageNumber = 1,
                ExpandedTenantId = tenant.Id
            });
        }
        catch (AppValidationException ex)
        {
            await tx.RollbackAsync(cancellationToken);
            ErrorMessage = ex.Message;
            ShowAddTenantModal = true;
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAddTenantPaymentAsync(CancellationToken cancellationToken)
    {
        await LoadPageDataAsync(cancellationToken, ApartmentInput.UnitId);

        ModelState.Clear();

        if (!TryValidateModel(AddTenantPaymentInput, nameof(AddTenantPaymentInput)))
        {
            ShowAddTenantPaymentModal = true;
            PaymentModalTenantId = AddTenantPaymentInput.TenantId;
            PaymentModalLeaseId = AddTenantPaymentInput.LeaseId;
            ErrorMessage = string.Join(" ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Take(3));
            return Page();
        }

        var leaseBelongsToTenant = PaymentLeaseOptionsByTenantId.TryGetValue(AddTenantPaymentInput.TenantId, out var tenantLeases)
                                  && tenantLeases.Any(x => x.Id == AddTenantPaymentInput.LeaseId);

        if (!leaseBelongsToTenant)
        {
            ModelState.AddModelError(string.Empty, "Selected apartment does not belong to this tenant.");
            ShowAddTenantPaymentModal = true;
            PaymentModalTenantId = AddTenantPaymentInput.TenantId;
            PaymentModalLeaseId = AddTenantPaymentInput.LeaseId;
            return Page();
        }

        try
        {
            await paymentService.CreateAsync(new CreatePaymentRequestDto
            {
                LeaseId = AddTenantPaymentInput.LeaseId,
                Amount = AddTenantPaymentInput.Amount,
                PaymentDate = AddTenantPaymentInput.PaymentDate,
                PaymentMethod = AddTenantPaymentInput.PaymentMethod,
                ReferenceNumber = AddTenantPaymentInput.ReferenceNumber,
                Notes = AddTenantPaymentInput.Notes
            }, cancellationToken);

            return RedirectToPage(new { Search, PageNumber, PropertySearch, PropertiesPageNumber, ExpandedTenantId = AddTenantPaymentInput.TenantId });
        }
        catch (AppValidationException ex)
        {
            ErrorMessage = ex.Message;
            ShowAddTenantPaymentModal = true;
            PaymentModalTenantId = AddTenantPaymentInput.TenantId;
            PaymentModalLeaseId = AddTenantPaymentInput.LeaseId;
            return Page();
        }
    }

    private async Task LoadPageDataAsync(CancellationToken cancellationToken, int? tenantApartmentUnitId = null)
    {
        var properties = await propertyService.GetAllAsync(PropertySearch, cancellationToken);
        var tenants = await tenantService.GetAllAsync(Search, cancellationToken);
        var leases = await leaseService.GetAllAsync(cancellationToken);
        var payments = await paymentService.GetAllAsync(null, cancellationToken);
        var units = await unitService.GetAllAsync(null, cancellationToken);

        PagedProperties = PagedResult<PropertyDto>.Create(properties, PropertiesPageNumber, 10);
        UnitsByPropertyId = units
            .GroupBy(x => x.PropertyId)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<UnitDto>)x.OrderBy(u => u.UnitNumber).ToList());

        ActiveApartmentByTenantId = leases
            .Where(x => x.Status == LeaseStatus.Active)
            .GroupBy(x => x.TenantId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.StartDate).First());

        var orderedTenants = tenants
            .OrderBy(t => ActiveApartmentByTenantId.ContainsKey(t.Id) ? 0 : 1)
            .ThenBy(t => ActiveApartmentByTenantId.TryGetValue(t.Id, out var apartment) ? apartment.UnitNumber : "ZZZ")
            .ThenBy(t => ActiveApartmentByTenantId.TryGetValue(t.Id, out var apartment) ? apartment.RoomNumber : "ZZZ")
            .ThenBy(t => t.LastName)
            .ThenBy(t => t.FirstName)
            .ToList();

        PagedTenants = PagedResult<TenantDto>.Create(orderedTenants, PageNumber, 10);

        PaymentHistoryByTenantId = payments
            .GroupBy(x => x.TenantId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<PaymentDto>)g.OrderByDescending(x => x.PaymentDate).ThenByDescending(x => x.Id).ToList());

        UnitOptions = units
            .Where(x => x.IsActive && x.RoomStatuses.Any(r => r.AvailableSlots > 0))
            .Select(x => new SelectListItem($"{x.PropertyName} - {x.UnitNumber}", x.Id.ToString()))
            .ToList();

        var payableLeases = leases
            .Where(x => x.Status is LeaseStatus.Active or LeaseStatus.Ended)
            .ToList();

        PaymentLeaseOptionsByTenantId = payableLeases
            .GroupBy(x => x.TenantId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<LeaseDto>)g.OrderByDescending(x => x.StartDate).ToList());

        PaymentLeaseRentMapByTenantId = payableLeases
            .GroupBy(x => x.TenantId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(x => x.Id, x => x.MonthlyRent));

        PaymentMethodOptions = Enum.GetValues<PaymentMethod>()
            .Select(x => new SelectListItem(x.ToString(), x.ToString()))
            .ToList();

        var tenantApartmentSelectedUnit = tenantApartmentUnitId ?? ApartmentInput.UnitId;
        if (tenantApartmentSelectedUnit <= 0 && UnitOptions.Count > 0)
        {
            int.TryParse(UnitOptions[0].Value, out tenantApartmentSelectedUnit);
            ApartmentInput.UnitId = tenantApartmentSelectedUnit;
        }

        if (tenantApartmentSelectedUnit > 0)
        {
            try
            {
                var info = await leaseService.GetUnitLeaseInfoAsync(tenantApartmentSelectedUnit, cancellationToken);
                ApartmentInput.MonthlyRent = info.MonthlyRent;
                TenantApartmentRoomOptions = info.Rooms
                    .Where(x => x.AvailableSlots > 0)
                    .Select(x => new SelectListItem($"{x.RoomNumber} ({x.OccupiedCount}/{x.MaxCapacity})", x.RoomId.ToString()))
                    .ToList();

                if (ApartmentInput.RoomId <= 0 && TenantApartmentRoomOptions.Count > 0)
                {
                    int.TryParse(TenantApartmentRoomOptions[0].Value, out var roomId);
                    ApartmentInput.RoomId = roomId;
                }
            }
            catch (AppValidationException ex)
            {
                ErrorMessage = ex.Message;
                TenantApartmentRoomOptions = [];
            }
        }

        var paidByLeaseId = payments
            .GroupBy(x => x.LeaseId)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

        UnpaidLeasesByTenantId = payableLeases
            .GroupBy(x => x.TenantId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<UnpaidLeaseItemViewModel>)g
                    .Select(lease =>
                    {
                        var paidAmount = paidByLeaseId.TryGetValue(lease.Id, out var totalPaid) ? totalPaid : 0m;
                        var unpaidAmount = Math.Max(0m, lease.MonthlyRent - paidAmount);

                        return new UnpaidLeaseItemViewModel
                        {
                            TenantId = lease.TenantId,
                            LeaseId = lease.Id,
                            ApartmentLabel = $"{lease.UnitNumber} / {lease.RoomNumber}",
                            MonthlyRent = lease.MonthlyRent,
                            PaidAmount = paidAmount,
                            UnpaidAmount = unpaidAmount,
                            DueDayOfMonth = lease.DueDayOfMonth
                        };
                    })
                    .Where(x => x.UnpaidAmount > 0)
                    .OrderByDescending(x => x.UnpaidAmount)
                    .ToList());

        if (AddTenantPaymentInput.PaymentDate == default)
        {
            AddTenantPaymentInput.PaymentDate = DateTime.UtcNow.Date;
        }
    }

    public class TenantInputModel
    {
        [Required(ErrorMessage = "First Name is required."), MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last Name is required."), MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact Number is required."), MaxLength(30)]
        public string? ContactNumber { get; set; }

        [EmailAddress, MaxLength(256)]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }
    }

    public class ApartmentInputModel
    {
        [Required(ErrorMessage = "Unit is required.")]
        [Display(Name = "Unit")]
        public int UnitId { get; set; }

        [Required(ErrorMessage = "Room is required.")]
        [Display(Name = "Room")]
        public int RoomId { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;

        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }

        [Range(0, 100000000, ErrorMessage = "Monthly Rent must be between 0 and 100000000.")]
        [Display(Name = "Monthly Rent")]
        public decimal MonthlyRent { get; set; }

        [Required(ErrorMessage = "Security Deposit is Required")]
        [Range(0.01, 100000000, ErrorMessage = "Security Deposit must be between 0.01 and 100000000.")]
        [Display(Name = "Security Deposit")]
        public decimal SecurityDeposit { get; set; }

        [Range(1, 28, ErrorMessage = "Due Day must be between 1 and 28.")]
        [Display(Name = "Due Day")]
        public int DueDayOfMonth { get; set; } = 1;

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }

    public class UnpaidLeaseItemViewModel
    {
        public int TenantId { get; set; }
        public int LeaseId { get; set; }
        public string ApartmentLabel { get; set; } = string.Empty;
        public decimal MonthlyRent { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal UnpaidAmount { get; set; }
        public int DueDayOfMonth { get; set; }
    }

    public class AddTenantPaymentInputModel
    {
        [Required(ErrorMessage = "Tenant is required.")]
        public int TenantId { get; set; }

        [Required(ErrorMessage = "Apartment is required.")]
        [Display(Name = "Apartment")]
        public int LeaseId { get; set; }

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
}
