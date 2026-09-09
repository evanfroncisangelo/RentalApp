using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RentalApp.Application.DTOs.Units;
using RentalApp.Application.Interfaces;
using RentalApp.Domain.Enums;

namespace RentalApp.Pages.Units;

[Authorize]
public class CreateModel(IUnitService unitService, IPropertyService propertyService) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<SelectListItem> PropertyOptions { get; private set; } = [];
    public List<SelectListItem> StatusOptions { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadOptionsAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadOptionsAsync(cancellationToken);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        await unitService.CreateAsync(new CreateUnitRequestDto
        {
            PropertyId = Input.PropertyId,
            UnitNumber = Input.UnitNumber,
            MonthlyRent = Input.MonthlyRent,
            RoomCount = Input.RoomCount,
            RoomMaxCapacity = Input.RoomMaxCapacity,
            Status = Input.Status
        }, cancellationToken);

        return RedirectToPage("/Units/Index");
    }

    private async Task LoadOptionsAsync(CancellationToken cancellationToken)
    {
        var properties = await propertyService.GetAllAsync(null, cancellationToken);
        PropertyOptions = properties
            .Where(x => x.IsActive)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();

        StatusOptions = Enum.GetValues<UnitStatus>()
            .Select(x => new SelectListItem(x.ToString(), x.ToString()))
            .ToList();
    }

    public class InputModel
    {
        [Required]
        [Display(Name = "Property")]
        public int PropertyId { get; set; }

        [Required, MaxLength(50)]
        [Display(Name = "Unit Number")]
        public string UnitNumber { get; set; } = string.Empty;

        [Range(0, 100000000)]
        [Display(Name = "Monthly Rent")]
        public decimal MonthlyRent { get; set; }

        [Range(1, 500)]
        [Display(Name = "Room Count")]
        public int RoomCount { get; set; } = 1;

        [Range(1, 3)]
        [Display(Name = "Max Tenants Per Room")]
        public int RoomMaxCapacity { get; set; } = 3;

        public UnitStatus Status { get; set; } = UnitStatus.Available;
    }
}
