using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RentalApp.Application.DTOs.Tenants;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;
using RentalApp.Domain.Enums;

namespace RentalApp.Pages.Tenants;

[Authorize]
public class EditModel(ITenantService tenantService, IUnitService unitService) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<SelectListItem> UnitOptions { get; private set; } = [];
    public string SelectedUnitCapacity { get; private set; } = "-";

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var item = await tenantService.GetByIdAsync(id, cancellationToken);
            Input = new InputModel
            {
                Id = item.Id,
                FirstName = item.FirstName,
                LastName = item.LastName,
                ContactNumber = item.ContactNumber,
                Email = item.Email,
                Address = item.Address,
                UnitId = item.UnitId,
                UnitNumber = item.UnitNumber,
                RoomNumber = item.RoomNumber,
                DateOfBirth = item.DateOfBirth,
                MoveInDate = item.MoveInDate,
                Notes = item.Notes,
                IsActive = item.IsActive
            };

            await LoadOptionsAsync(cancellationToken, null, Input.Id);
            return Page();
        }
        catch (AppNotFoundException)
        {
            return RedirectToPage("/Tenants/Index");
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadOptionsAsync(cancellationToken, null, Input.Id);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!Input.UnitId.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Unit is required.");
            return Page();
        }

        var selectedUnit = await GetSelectedUnitAsync(Input.UnitId.Value, cancellationToken);
        if (selectedUnit is null)
        {
            ModelState.AddModelError(string.Empty, "Selected unit does not exist or is inactive.");
            return Page();
        }

        var canAssign = await IsUnitAvailableAsync(Input.UnitId.Value, Input.Id, cancellationToken);
        if (!canAssign)
        {
            ModelState.AddModelError(string.Empty, "Selected unit is already at max capacity.");
            return Page();
        }

        Input.UnitNumber = selectedUnit.UnitNumber;
        Input.RoomNumber = selectedUnit.RoomStatuses
            .Where(r => r.Status != UnitStatus.Inactive.ToString())
            .OrderBy(r => r.RoomNumber)
            .Select(r => r.RoomNumber)
            .FirstOrDefault() ?? "Room 1";

        await tenantService.UpdateAsync(Input.Id, new UpdateTenantRequestDto
        {
            FirstName = Input.FirstName,
            LastName = Input.LastName,
            ContactNumber = Input.ContactNumber,
            Email = Input.Email,
            Address = Input.Address,
            UnitId = selectedUnit.Id,
            UnitNumber = Input.UnitNumber,
            RoomNumber = Input.RoomNumber,
            DateOfBirth = Input.DateOfBirth,
            MoveInDate = Input.MoveInDate,
            Notes = Input.Notes,
            IsActive = Input.IsActive
        }, cancellationToken);

        return RedirectToPage("/Tenants/Index");
    }

    public class InputModel
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? ContactNumber { get; set; }

        [EmailAddress, MaxLength(256)]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [Required]
        [Display(Name = "Unit")]
        public int? UnitId { get; set; }

        [MaxLength(50)]
        public string? UnitNumber { get; set; }

        [MaxLength(50)]
        public string? RoomNumber { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Move In Date")]
        public DateTime? MoveInDate { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }

        [Display(Name = "Status")]
        public bool IsActive { get; set; }
    }

    private async Task LoadOptionsAsync(CancellationToken cancellationToken, string? _, int? currentTenantId = null)
    {
        var units = await unitService.GetAllAsync(null, cancellationToken);
        var allTenants = await tenantService.GetAllAsync(null, cancellationToken);

        var activeTenantCountByUnit = allTenants
            .Where(t => t.IsActive && t.UnitId.HasValue)
            .GroupBy(t => t.UnitId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var activeTenantCountByUnitExcludingCurrent = allTenants
            .Where(t => t.IsActive
                        && t.UnitId.HasValue
                        && (!currentTenantId.HasValue || t.Id != currentTenantId.Value))
            .GroupBy(t => t.UnitId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var selectedTenantCurrentUnitId = currentTenantId.HasValue
            ? allTenants.FirstOrDefault(x => x.Id == currentTenantId.Value)?.UnitId
            : null;

        var activeUnits = units
            .Where(x => x.IsActive)
            .OrderBy(x => x.UnitNumber)
            .Select(x => new
            {
                Unit = x,
                Capacity = x.RoomMaxCapacity,
                Assigned = activeTenantCountByUnit.TryGetValue(x.Id, out var count) ? count : 0,
                AssignedExcludingCurrent = activeTenantCountByUnitExcludingCurrent.TryGetValue(x.Id, out var countWithoutCurrent) ? countWithoutCurrent : 0
            })
            .Where(x => x.Capacity > 0
                        && (x.AssignedExcludingCurrent < x.Capacity
                            || x.Unit.Id == selectedTenantCurrentUnitId
                            || x.Unit.Id == Input.UnitId))
            .ToList();

        UnitOptions = activeUnits
            .Select(x => new SelectListItem($"{x.Unit.PropertyName} - {x.Unit.UnitNumber} ({x.Assigned}/{x.Capacity})", x.Unit.Id.ToString()))
            .ToList();

        if (!Input.UnitId.HasValue && UnitOptions.Count > 0 && int.TryParse(UnitOptions[0].Value, out var firstUnitId))
        {
            Input.UnitId = firstUnitId;
        }

        var chosen = activeUnits.FirstOrDefault(x => x.Unit.Id == Input.UnitId);
        if (chosen is null)
        {
            SelectedUnitCapacity = "-";
            return;
        }

        SelectedUnitCapacity = $"{chosen.Assigned}/{chosen.Capacity}";
    }

    private async Task<bool> IsUnitAvailableAsync(int unitId, int? currentTenantId, CancellationToken cancellationToken)
    {
        var units = await unitService.GetAllAsync(null, cancellationToken);
        var selectedUnit = units.FirstOrDefault(x => x.IsActive && x.Id == unitId);

        if (selectedUnit is null || selectedUnit.RoomMaxCapacity <= 0)
        {
            return false;
        }

        var allTenants = await tenantService.GetAllAsync(null, cancellationToken);
        var assignedCount = allTenants.Count(t =>
            t.IsActive
            && (!currentTenantId.HasValue || t.Id != currentTenantId.Value)
            && t.UnitId == unitId);

        return assignedCount < selectedUnit.RoomMaxCapacity;
    }

    private async Task<RentalApp.Application.DTOs.Units.UnitDto?> GetSelectedUnitAsync(int unitId, CancellationToken cancellationToken)
    {
        var units = await unitService.GetAllAsync(null, cancellationToken);
        return units.FirstOrDefault(x => x.IsActive && x.Id == unitId);
    }
}
