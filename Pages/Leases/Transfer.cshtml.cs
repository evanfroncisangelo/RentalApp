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
public class TransferModel(ILeaseService leaseService, IUnitService unitService) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<SelectListItem> UnitOptions { get; private set; } = [];
    public List<SelectListItem> RoomOptions { get; private set; } = [];
    public string CurrentSummary { get; private set; } = string.Empty;
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var lease = await leaseService.GetByIdAsync(id, cancellationToken);
            CurrentSummary = $"{lease.TenantName} - {lease.UnitNumber} / {lease.RoomNumber}";
            Input.CurrentLeaseId = id;
            Input.TransferDate = DateTime.UtcNow.Date;

            await LoadUnitsAsync(cancellationToken);
            return Page();
        }
        catch (AppNotFoundException)
        {
            return RedirectToPage("/Leases/Index");
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadUnitsAsync(cancellationToken, Input.NewUnitId);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            await leaseService.TransferAsync(new TransferLeaseRequestDto
            {
                CurrentLeaseId = Input.CurrentLeaseId,
                NewUnitId = Input.NewUnitId,
                NewRoomId = Input.NewRoomId,
                TransferDate = Input.TransferDate,
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

    private async Task LoadUnitsAsync(CancellationToken cancellationToken, int? selectedUnitId = null)
    {
        var units = await unitService.GetAllAsync(null, cancellationToken);
        UnitOptions = units.Where(x => x.IsActive)
            .Select(x => new SelectListItem($"{x.PropertyName} - {x.UnitNumber}", x.Id.ToString()))
            .ToList();

        if (selectedUnitId.GetValueOrDefault() > 0)
        {
            try
            {
                var selectedUnit = selectedUnitId.GetValueOrDefault();
                var info = await leaseService.GetUnitLeaseInfoAsync(selectedUnit, cancellationToken);
                RoomOptions = info.Rooms.Where(x => x.AvailableSlots > 0)
                    .Select(x => new SelectListItem($"{x.RoomNumber} ({x.OccupiedCount}/{x.MaxCapacity})", x.RoomId.ToString()))
                    .ToList();
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
        public int CurrentLeaseId { get; set; }

        [Required]
        [Display(Name = "New Unit")]
        public int NewUnitId { get; set; }

        [Required]
        [Display(Name = "New Room")]
        public int NewRoomId { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Transfer Date")]
        public DateTime TransferDate { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}
