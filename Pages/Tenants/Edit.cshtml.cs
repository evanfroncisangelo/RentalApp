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
                Address = item.Address,
                UnitId = item.UnitId,
                UnitNumber = item.UnitNumber,
                RoomNumber = item.RoomNumber,
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
            ModelState.AddModelError(string.Empty, "Selected unit is not available.");
            return Page();
        }

        Input.UnitNumber = selectedUnit.UnitNumber;
        Input.RoomNumber = string.Empty; // No longer tracking rooms

        await tenantService.UpdateAsync(Input.Id, new UpdateTenantRequestDto
        {
            FirstName = Input.FirstName,
            LastName = Input.LastName,
            ContactNumber = Input.ContactNumber,
            Address = Input.Address,
            UnitId = selectedUnit.Id,
            UnitNumber = Input.UnitNumber,
            RoomNumber = Input.RoomNumber,
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

        var currentTenant = currentTenantId.HasValue
            ? allTenants.FirstOrDefault(x => x.Id == currentTenantId.Value)
            : null;

        if (currentTenant is not null && currentTenant.IsActive && currentTenant.UnitId.HasValue)
        {
            var currentUnitId = currentTenant.UnitId.Value;
            if (activeTenantCountByUnit.TryGetValue(currentUnitId, out var count) && count > 0)
            {
                activeTenantCountByUnit[currentUnitId] = count - 1;
            }
        }

        var activeUnits = units
            .Where(x => x.IsActive)
            .OrderBy(x => x.UnitNumber)
            .Select(x => new
            {
                Unit = x,
                Assigned = activeTenantCountByUnit.TryGetValue(x.Id, out var count) ? count : 0
            })
            .ToList();

        UnitOptions = activeUnits
            .Where(x => x.Assigned < x.Unit.MaxCapacity || (Input.UnitId.HasValue && x.Unit.Id == Input.UnitId.Value))
            .Select(x => new SelectListItem($"{x.Unit.PropertyName} - {x.Unit.UnitNumber} ({x.Assigned}/{x.Unit.MaxCapacity})", x.Unit.Id.ToString()))
            .ToList();

        if (!Input.UnitId.HasValue && UnitOptions.Count > 0 && int.TryParse(UnitOptions[0].Value, out var firstUnitId))
        {
            Input.UnitId = firstUnitId;
        }
    }

    private async Task<bool> IsUnitAvailableAsync(int unitId, int? currentTenantId, CancellationToken cancellationToken)
    {
        var units = await unitService.GetAllAsync(null, cancellationToken);
        var selectedUnit = units.FirstOrDefault(x => x.IsActive && x.Id == unitId);
        if (selectedUnit is null)
        {
            return false;
        }

        var allTenants = await tenantService.GetAllAsync(null, cancellationToken);
        var assignedCount = allTenants.Count(x =>
            x.IsActive &&
            x.UnitId == unitId &&
            (!currentTenantId.HasValue || x.Id != currentTenantId.Value));

        return assignedCount < selectedUnit.MaxCapacity;
    }

    private async Task<RentalApp.Application.DTOs.Units.UnitDto?> GetSelectedUnitAsync(int unitId, CancellationToken cancellationToken)
    {
        var units = await unitService.GetAllAsync(null, cancellationToken);
        return units.FirstOrDefault(x => x.IsActive && x.Id == unitId);
    }
}
