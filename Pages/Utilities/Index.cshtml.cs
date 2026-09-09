using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RentalApp.Application.DTOs.Utilities;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;
using RentalApp.Data;
using RentalApp.Domain.Enums;

namespace RentalApp.Pages.Utilities;

[Authorize]
public class IndexModel(
    IUtilityCustomerService utilityCustomerService,
    ITenantService tenantService,
    IUtilityCategoryService utilityCategoryService,
    IUtilityBillService utilityBillService,
    IUtilityPaymentService utilityPaymentService,
    RentalDbContext dbContext) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int? ExpandedCustomerId { get; set; }

    [BindProperty]
    public UtilityCustomerInputModel Input { get; set; } = new();

    [BindProperty]
    public CategoryInputModel CategoryInput { get; set; } = new();

    [BindProperty]
    public AddUtilityPaymentInputModel AddUtilityPaymentInput { get; set; } = new();

    public IReadOnlyList<UtilityCustomerListItem> Customers { get; private set; } = [];
    public IReadOnlyList<UtilityCategoryDto> Categories { get; private set; } = [];
    public IReadOnlyDictionary<int, IReadOnlyList<UtilityPaymentHistoryItem>> PaymentHistoryByCustomerId { get; private set; } = new Dictionary<int, IReadOnlyList<UtilityPaymentHistoryItem>>();
    public IReadOnlyDictionary<int, IReadOnlyList<UtilityUnpaidBillItem>> UnpaidBillsByCustomerId { get; private set; } = new Dictionary<int, IReadOnlyList<UtilityUnpaidBillItem>>();
    public List<SelectListItem> CustomerTypeOptions { get; private set; } = [];
    public List<SelectListItem> TenantOptions { get; private set; } = [];
    public List<SelectListItem> RoomOptions { get; private set; } = [];
    public List<SelectListItem> CategoryOptions { get; private set; } = [];
    public List<SelectListItem> UtilityTypeOptions { get; private set; } = [];
    public IReadOnlyDictionary<int, int?> TenantDefaultRoomMap { get; private set; } = new Dictionary<int, int?>();
    public bool ShowModal { get; private set; }
    public bool ShowCategoryModal { get; private set; }
    public bool ShowAddPaymentModal { get; private set; }
    public int PaymentModalCustomerId { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostSaveAsync(CancellationToken cancellationToken)
    {
        ModelState.Clear();
        if (!TryValidateModel(Input, nameof(Input)))
        {
            ShowModal = true;
            await LoadAsync(cancellationToken);
            return Page();
        }

        try
        {
            if (Input.Id > 0)
            {
                await utilityCustomerService.UpdateAsync(Input.Id, new UpdateUtilityCustomerRequestDto
                {
                    Name = Input.Name,
                    CustomerType = Input.CustomerType,
                    TenantId = Input.TenantId,
                    RoomId = Input.RoomId,
                    UtilityCategoryId = Input.UtilityCategoryId,
                    UtilityStartDate = Input.UtilityStartDate,
                    AmountToPay = Input.AmountToPay,
                    DueDayOfMonth = Input.DueDayOfMonth
                }, cancellationToken);
            }
            else
            {
                await utilityCustomerService.CreateAsync(new CreateUtilityCustomerRequestDto
                {
                    Name = Input.Name,
                    CustomerType = Input.CustomerType,
                    TenantId = Input.TenantId,
                    RoomId = Input.RoomId,
                    UtilityCategoryId = Input.UtilityCategoryId,
                    UtilityStartDate = Input.UtilityStartDate,
                    AmountToPay = Input.AmountToPay,
                    DueDayOfMonth = Input.DueDayOfMonth
                }, cancellationToken);
            }
        }
        catch (AppValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ShowModal = true;
            await LoadAsync(cancellationToken);
            return Page();
        }

        return RedirectToPage(new { ExpandedCustomerId });
    }

    public async Task<IActionResult> OnPostAddCategoryAsync(CancellationToken cancellationToken)
    {
        ModelState.Clear();
        if (!TryValidateModel(CategoryInput, nameof(CategoryInput)))
        {
            ShowCategoryModal = true;
            await LoadAsync(cancellationToken);
            return Page();
        }

        try
        {
            await utilityCategoryService.CreateAsync(new CreateUtilityCategoryRequestDto
            {
                Name = CategoryInput.Name
            }, cancellationToken);
        }
        catch (AppValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ShowCategoryModal = true;
            await LoadAsync(cancellationToken);
            return Page();
        }

        return RedirectToPage(new { ExpandedCustomerId });
    }

    public async Task<IActionResult> OnPostDeactivateCategoryAsync(int id, CancellationToken cancellationToken)
    {
        await utilityCategoryService.DeactivateAsync(id, cancellationToken);
        return RedirectToPage(new { ExpandedCustomerId });
    }

    public async Task<IActionResult> OnPostAddUtilityPaymentAsync(CancellationToken cancellationToken)
    {
        ModelState.Clear();
        if (!TryValidateModel(AddUtilityPaymentInput, nameof(AddUtilityPaymentInput)))
        {
            ShowAddPaymentModal = true;
            PaymentModalCustomerId = AddUtilityPaymentInput.UtilityCustomerId;
            ExpandedCustomerId = AddUtilityPaymentInput.UtilityCustomerId;
            await LoadAsync(cancellationToken);
            return Page();
        }

        var bill = await utilityBillService.GetByIdAsync(AddUtilityPaymentInput.UtilityBillId, cancellationToken);
        if (bill.UtilityCustomerId != AddUtilityPaymentInput.UtilityCustomerId)
        {
            ModelState.AddModelError(string.Empty, "Selected bill does not belong to this customer.");
            ShowAddPaymentModal = true;
            PaymentModalCustomerId = AddUtilityPaymentInput.UtilityCustomerId;
            ExpandedCustomerId = AddUtilityPaymentInput.UtilityCustomerId;
            await LoadAsync(cancellationToken);
            return Page();
        }

        try
        {
            await utilityPaymentService.CreateAsync(new CreateUtilityBillPaymentRequestDto
            {
                UtilityBillId = AddUtilityPaymentInput.UtilityBillId,
                Amount = AddUtilityPaymentInput.Amount,
                PaymentDate = AddUtilityPaymentInput.PaymentDate,
                ReferenceNumber = AddUtilityPaymentInput.ReferenceNumber,
                Notes = AddUtilityPaymentInput.Notes
            }, cancellationToken);
        }
        catch (AppValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ShowAddPaymentModal = true;
            PaymentModalCustomerId = AddUtilityPaymentInput.UtilityCustomerId;
            ExpandedCustomerId = AddUtilityPaymentInput.UtilityCustomerId;
            await LoadAsync(cancellationToken);
            return Page();
        }

        return RedirectToPage(new { ExpandedCustomerId = AddUtilityPaymentInput.UtilityCustomerId });
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        await utilityBillService.EnsureCurrentDueBillsAsync(cancellationToken);

        var rawCustomers = await utilityCustomerService.GetAllAsync(null, cancellationToken);

        var tenantIds = rawCustomers
            .Where(x => x.TenantId.HasValue)
            .Select(x => x.TenantId!.Value)
            .Distinct()
            .ToList();

        var tenants = await dbContext.Tenants
            .AsNoTracking()
            .Where(x => tenantIds.Contains(x.Id))
            .Select(x => new { x.Id, Name = (x.FirstName + " " + x.LastName).Trim() })
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var roomIds = rawCustomers
            .Where(x => x.RoomId.HasValue)
            .Select(x => x.RoomId!.Value)
            .Distinct()
            .ToList();

        var rooms = await dbContext.Rooms
            .AsNoTracking()
            .Include(x => x.Unit)
            .ThenInclude(x => x!.Property)
            .Where(x => roomIds.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                Label = $"{x.Unit!.Property!.Name} - {x.Unit.UnitNumber} / {x.RoomNumber}"
            })
            .ToDictionaryAsync(x => x.Id, x => x.Label, cancellationToken);

        var categoryIds = rawCustomers
            .Where(x => x.UtilityCategoryId.HasValue)
            .Select(x => x.UtilityCategoryId!.Value)
            .Distinct()
            .ToList();

        var categories = await dbContext.UtilityTypes
            .AsNoTracking()
            .Where(x => categoryIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Name })
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        Customers = rawCustomers
            .OrderByDescending(x => x.Id)
            .Select(x => new UtilityCustomerListItem
            {
                Id = x.Id,
                Name = x.Name,
                CustomerType = x.CustomerType,
                TenantId = x.TenantId,
                RoomId = x.RoomId,
                UtilityCategoryId = x.UtilityCategoryId,
                UtilityStartDate = x.UtilityStartDate,
                AmountToPay = x.DefaultRate,
                DueDayOfMonth = x.DueDayOfMonth,
                TenantName = x.TenantId.HasValue && tenants.TryGetValue(x.TenantId.Value, out var tenantName) ? tenantName : null,
                RoomLabel = x.RoomId.HasValue && rooms.TryGetValue(x.RoomId.Value, out var roomLabel) ? roomLabel : null,
                CategoryName = x.UtilityCategoryId.HasValue && categories.TryGetValue(x.UtilityCategoryId.Value, out var categoryName) ? categoryName : null
            })
            .ToList();

        var openBills = await dbContext.UtilityBills
            .AsNoTracking()
            .Include(x => x.UtilityType)
            .Include(x => x.Payments)
            .Where(x => x.Status != UtilityBillStatus.Cancelled)
            .Select(x => new
            {
                x.Id,
                x.UtilityCustomerId,
                x.BillingPeriod,
                UtilityTypeName = x.UtilityType != null ? x.UtilityType.Name : string.Empty,
                x.Amount,
                Paid = x.Payments.Where(p => !p.IsVoided).Sum(p => (decimal?)p.Amount) ?? 0m
            })
            .ToListAsync(cancellationToken);

        UnpaidBillsByCustomerId = openBills
            .Select(x => new
            {
                x.Id,
                x.UtilityCustomerId,
                x.BillingPeriod,
                x.UtilityTypeName,
                x.Amount,
                x.Paid,
                Balance = x.Amount - x.Paid
            })
            .Where(x => x.Balance > 0)
            .GroupBy(x => x.UtilityCustomerId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<UtilityUnpaidBillItem>)g
                    .OrderByDescending(x => x.BillingPeriod)
                    .ThenByDescending(x => x.Id)
                    .Select(x => new UtilityUnpaidBillItem
                    {
                        UtilityBillId = x.Id,
                        BillingPeriod = x.BillingPeriod,
                        UtilityTypeName = x.UtilityTypeName,
                        Amount = x.Amount,
                        Paid = x.Paid,
                        Balance = x.Balance
                    })
                    .ToList());

        var paymentRows = await dbContext.UtilityBillPayments
            .AsNoTracking()
            .Include(x => x.UtilityBill)
            .ThenInclude(x => x!.UtilityType)
            .Where(x => x.UtilityBill != null)
            .OrderByDescending(x => x.PaymentDate)
            .ThenByDescending(x => x.Id)
            .Select(x => new
            {
                x.UtilityBill!.UtilityCustomerId,
                x.PaymentDate,
                x.Amount,
                x.IsVoided,
                x.UtilityBill.BillingPeriod,
                UtilityTypeName = x.UtilityBill.UtilityType != null ? x.UtilityBill.UtilityType.Name : string.Empty
            })
            .ToListAsync(cancellationToken);

        PaymentHistoryByCustomerId = paymentRows
            .GroupBy(x => x.UtilityCustomerId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<UtilityPaymentHistoryItem>)g
                    .Select(x => new UtilityPaymentHistoryItem
                    {
                        PaymentDate = x.PaymentDate,
                        Amount = x.Amount,
                        BillingPeriod = x.BillingPeriod,
                        UtilityTypeName = x.UtilityTypeName,
                        IsVoided = x.IsVoided
                    })
                    .ToList());

        CustomerTypeOptions = Enum.GetValues<UtilityCustomerType>()
            .Select(x => new SelectListItem(x.ToString(), ((int)x).ToString()))
            .ToList();

        var tenantOptions = await tenantService.GetAllAsync(null, cancellationToken);
        TenantOptions = [new SelectListItem("(None)", "")];
        TenantOptions.AddRange(tenantOptions
            .Where(x => x.IsActive)
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .Select(x => new SelectListItem($"{x.FirstName} {x.LastName}".Trim(), x.Id.ToString())));

        var activeLeases = await dbContext.Leases
            .AsNoTracking()
            .Where(x => x.Status == LeaseStatus.Active)
            .OrderByDescending(x => x.StartDate)
            .ToListAsync(cancellationToken);

        TenantDefaultRoomMap = activeLeases
            .GroupBy(x => x.TenantId)
            .ToDictionary(g => g.Key, g => (int?)g.First().RoomId);

        var roomOptions = await dbContext.Rooms
            .AsNoTracking()
            .Include(x => x.Unit)
            .ThenInclude(x => x!.Property)
            .Where(x => x.IsActive)
            .OrderBy(x => x.Unit!.Property!.Name)
            .ThenBy(x => x.Unit!.UnitNumber)
            .ThenBy(x => x.RoomNumber)
            .Select(x => new
            {
                x.Id,
                Label = $"{x.Unit!.Property!.Name} - {x.Unit.UnitNumber} / {x.RoomNumber}"
            })
            .ToListAsync(cancellationToken);

        RoomOptions = [new SelectListItem("(None)", "")];
        RoomOptions.AddRange(roomOptions.Select(x => new SelectListItem(x.Label, x.Id.ToString())));

        Categories = await utilityCategoryService.GetAllAsync(null, cancellationToken);

        CategoryOptions = [new SelectListItem("(None)", "")];
        CategoryOptions.AddRange(Categories
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString())));

        UtilityTypeOptions = [new SelectListItem("Select Utility", "")];
        UtilityTypeOptions.AddRange(await dbContext.UtilityTypes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToListAsync(cancellationToken));

        if (AddUtilityPaymentInput.PaymentDate == default)
        {
            AddUtilityPaymentInput.PaymentDate = DateTime.UtcNow.Date;
        }
    }

    public class UtilityCustomerInputModel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public UtilityCustomerType CustomerType { get; set; } = UtilityCustomerType.Other;
        public int? TenantId { get; set; }
        public int? RoomId { get; set; }
        public int? UtilityCategoryId { get; set; }

        [DataType(DataType.Date)]
        public DateTime? UtilityStartDate { get; set; }

        [Range(0.01, 1000000000)]
        public decimal? AmountToPay { get; set; }

        [Range(1, 28)]
        public int? DueDayOfMonth { get; set; }
    }

    public class UtilityCustomerListItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public UtilityCustomerType CustomerType { get; set; }
        public int? TenantId { get; set; }
        public int? RoomId { get; set; }
        public int? UtilityCategoryId { get; set; }
        public DateTime? UtilityStartDate { get; set; }
        public decimal? AmountToPay { get; set; }
        public int? DueDayOfMonth { get; set; }
        public string? TenantName { get; set; }
        public string? RoomLabel { get; set; }
        public string? CategoryName { get; set; }
    }

    public class CategoryInputModel
    {
        [Required]
        public string Name { get; set; } = string.Empty;
    }

    public class AddUtilityPaymentInputModel
    {
        [Required]
        public int UtilityCustomerId { get; set; }

        [Required]
        public int UtilityBillId { get; set; }

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

    public class UtilityPaymentHistoryItem
    {
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string BillingPeriod { get; set; } = string.Empty;
        public string UtilityTypeName { get; set; } = string.Empty;
        public bool IsVoided { get; set; }
    }

    public class UtilityUnpaidBillItem
    {
        public int UtilityBillId { get; set; }
        public string BillingPeriod { get; set; } = string.Empty;
        public string UtilityTypeName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Paid { get; set; }
        public decimal Balance { get; set; }
    }
}
