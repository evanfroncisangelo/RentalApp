using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RentalApp.Application.DTOs.Leases;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;

namespace RentalApp.Pages.Leases;

[Authorize]
public class CreateModel(ILeaseService leaseService, IUnitService unitService, ITenantService tenantService) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<SelectListItem> UnitOptions { get; private set; } = [];
    public List<SelectListItem> TenantOptions { get; private set; } = [];
    public List<SelectListItem> RoomOptions { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadOptionsAsync(cancellationToken);

        if (Input.UnitId <= 0 && UnitOptions.Count > 0 && int.TryParse(UnitOptions[0].Value, out var defaultUnitId))
        {
            Input.UnitId = defaultUnitId;
            await LoadOptionsAsync(cancellationToken, Input.UnitId);
        }

        if (Input.RoomId <= 0 && RoomOptions.Count > 0 && int.TryParse(RoomOptions[0].Value, out var defaultRoomId))
        {
            Input.RoomId = defaultRoomId;
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadOptionsAsync(cancellationToken, Input.UnitId);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            await leaseService.CreateAsync(new CreateLeaseRequestDto
            {
                UnitId = Input.UnitId,
                RoomId = Input.RoomId,
                TenantId = Input.TenantId,
                StartDate = Input.StartDate,
                EndDate = Input.EndDate,
                MonthlyRent = Input.MonthlyRent,
                SecurityDeposit = Input.SecurityDeposit,
                DueDayOfMonth = Input.DueDayOfMonth,
                Notes = Input.Notes
            }, cancellationToken);

            return RedirectToPage("/Leases/Index");
        }
        catch (AppValidationException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
    }

    private async Task LoadOptionsAsync(CancellationToken cancellationToken, int? selectedUnitId = null)
    {
        var units = await unitService.GetAllAsync(null, cancellationToken);
        UnitOptions = units
            .Where(x => x.IsActive)
            .Select(x => new SelectListItem($"{x.PropertyName} - {x.UnitNumber}", x.Id.ToString()))
            .ToList();

        var tenants = await tenantService.GetAllAsync(null, cancellationToken);
        TenantOptions = tenants
            .Where(x => x.IsActive)
            .Select(x => new SelectListItem($"{x.FirstName} {x.LastName}", x.Id.ToString()))
            .ToList();

        var unitId = selectedUnitId.GetValueOrDefault();
        if (unitId <= 0 && UnitOptions.Count > 0)
        {
            int.TryParse(UnitOptions[0].Value, out unitId);
            Input.UnitId = unitId;
        }

        if (unitId > 0)
        {
            try
            {
                var info = await leaseService.GetUnitLeaseInfoAsync(unitId, cancellationToken);
                Input.MonthlyRent = info.MonthlyRent;
                RoomOptions = info.Rooms
                    .Where(x => x.AvailableSlots > 0)
                    .Select(x => new SelectListItem($"{x.RoomNumber} ({x.OccupiedCount}/{x.MaxCapacity})", x.RoomId.ToString()))
                    .ToList();

                if (Input.RoomId <= 0 && RoomOptions.Count > 0 && int.TryParse(RoomOptions[0].Value, out var defaultRoomId))
                {
                    Input.RoomId = defaultRoomId;
                }
            }
            catch (AppValidationException ex)
            {
                ErrorMessage = ex.Message;
                RoomOptions = [];
            }
        }
    }

    public class InputModel
    {
        [Required]
        [Display(Name = "Unit")]
        public int UnitId { get; set; }

        [Required]
        [Display(Name = "Room")]
        public int RoomId { get; set; }

        [Required]
        [Display(Name = "Tenant")]
        public int TenantId { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;

        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }

        [Range(0, 100000000)]
        [Display(Name = "Monthly Rent")]
        public decimal MonthlyRent { get; set; }

        [Range(0, 100000000)]
        [Display(Name = "Security Deposit")]
        public decimal SecurityDeposit { get; set; }

        [Range(1, 31)]
        [Display(Name = "Due Day")]
        public int DueDayOfMonth { get; set; } = 1;

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}
