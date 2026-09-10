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
public class EditModel(ILeaseService leaseService, IUnitService unitService) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<SelectListItem> UnitOptions { get; private set; } = [];
    public List<SelectListItem> RoomOptions { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var lease = await leaseService.GetByIdAsync(id, cancellationToken);
            Input = new InputModel
            {
                Id = lease.Id,
                UnitId = lease.UnitId,
                RoomId = lease.RoomId,
                StartDate = lease.StartDate,
                EndDate = lease.EndDate,
                MonthlyRent = lease.MonthlyRent,
                SecurityDeposit = lease.SecurityDeposit,
                DueDayOfMonth = lease.DueDayOfMonth,
                Notes = lease.Notes
            };

            await LoadOptionsAsync(cancellationToken, lease.UnitId);
            return Page();
        }
        catch (AppNotFoundException)
        {
            return RedirectToPage("/Leases/Index");
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
            await leaseService.UpdateAsync(Input.Id, new UpdateLeaseRequestDto
            {
                UnitId = Input.UnitId,
                RoomId = Input.RoomId,
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
        UnitOptions = units.Where(x => x.IsActive)
            .Select(x => new SelectListItem($"{x.PropertyName} - {x.UnitNumber}", x.Id.ToString()))
            .ToList();

        var unitId = selectedUnitId.GetValueOrDefault();
        if (unitId > 0)
        {
            try
            {
                var info = await leaseService.GetUnitLeaseInfoAsync(unitId, cancellationToken);
                RoomOptions = info.Rooms
                    .Select(x => new SelectListItem($"{x.RoomNumber} ({x.OccupiedCount}/{x.MaxCapacity})", x.RoomId.ToString()))
                    .ToList();
                Input.MonthlyRent = info.MonthlyRent;
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
        public int Id { get; set; }

        [Required]
        [Display(Name = "Unit")]
        public int UnitId { get; set; }

        [Required]
        [Display(Name = "Room")]
        public int RoomId { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

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
        public int DueDayOfMonth { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}
