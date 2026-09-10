using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RentalApp.Application.DTOs.Tenants;
using RentalApp.Application.Interfaces;
using RentalApp.Domain.Enums;

namespace RentalApp.Pages.Tenants;

[Authorize]
public class CreateModel(ITenantService tenantService, IUnitService unitService) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<SelectListItem> UnitOptions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadOptionsAsync(cancellationToken, null);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadOptionsAsync(cancellationToken, null);

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

        var canAssign = await IsUnitAvailableAsync(Input.UnitId.Value, cancellationToken);
        if (!canAssign)
        {
            ModelState.AddModelError(string.Empty, "Selected unit is not available.");
            return Page();
        }

        Input.UnitNumber = selectedUnit.UnitNumber;
        Input.RoomNumber = string.Empty; // No longer tracking rooms

        try
        {
            await tenantService.CreateAsync(new CreateTenantRequestDto
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
        }
        catch (RentalApp.Application.Exceptions.AppValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }

        return RedirectToPage("/Tenants/Index");
    }

    public class InputModel
    {
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
        public bool IsActive { get; set; } = true;
    }

    private async Task LoadOptionsAsync(CancellationToken cancellationToken, string? _)
    {
        var units = await unitService.GetAllAsync(null, cancellationToken);
        var allTenants = await tenantService.GetAllAsync(null, cancellationToken);

        var activeTenantCountByUnit = allTenants
            .Where(t => t.IsActive && t.UnitId.HasValue)
            .GroupBy(t => t.UnitId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

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
            .Where(x => x.Assigned < x.Unit.MaxCapacity)
            .Select(x => new SelectListItem($"{x.Unit.PropertyName} - {x.Unit.UnitNumber} ({x.Assigned}/{x.Unit.MaxCapacity})", x.Unit.Id.ToString()))
            .ToList();

        if (!Input.UnitId.HasValue && UnitOptions.Count > 0 && int.TryParse(UnitOptions[0].Value, out var firstUnitId))
        {
            Input.UnitId = firstUnitId;
        }
    }

    private async Task<bool> IsUnitAvailableAsync(int unitId, CancellationToken cancellationToken)
    {
        var units = await unitService.GetAllAsync(null, cancellationToken);
        var selectedUnit = units.FirstOrDefault(x => x.IsActive && x.Id == unitId);
        if (selectedUnit is null)
        {
            return false;
        }

        var allTenants = await tenantService.GetAllAsync(null, cancellationToken);
        var assignedCount = allTenants.Count(x => x.IsActive && x.UnitId == unitId);
        return assignedCount < selectedUnit.MaxCapacity;
    }

    private async Task<RentalApp.Application.DTOs.Units.UnitDto?> GetSelectedUnitAsync(int unitId, CancellationToken cancellationToken)
    {
        var units = await unitService.GetAllAsync(null, cancellationToken);
        return units.FirstOrDefault(x => x.IsActive && x.Id == unitId);
    }
}
