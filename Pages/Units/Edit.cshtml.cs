using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RentalApp.Application.DTOs.Units;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;
using RentalApp.Domain.Enums;

namespace RentalApp.Pages.Units;

[Authorize]
public class EditModel(IUnitService unitService, IPropertyService propertyService) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<SelectListItem> PropertyOptions { get; private set; } = [];
    public List<SelectListItem> StatusOptions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        await LoadOptionsAsync(cancellationToken);

        try
        {
            var item = await unitService.GetByIdAsync(id, cancellationToken);
            Input = new InputModel
            {
                Id = item.Id,
                PropertyId = item.PropertyId,
                UnitNumber = item.UnitNumber,
                Description = item.Description,
                MonthlyRent = item.MonthlyRent,
                RoomCount = item.RoomCount,
                RoomMaxCapacity = item.RoomMaxCapacity == 0 ? 3 : item.RoomMaxCapacity,
                Status = item.Status
            };

            return Page();
        }
        catch (AppNotFoundException)
        {
            return RedirectToPage("/Units/Index");
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadOptionsAsync(cancellationToken);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        await unitService.UpdateAsync(Input.Id, new UpdateUnitRequestDto
        {
            PropertyId = Input.PropertyId,
            UnitNumber = Input.UnitNumber,
            Description = Input.Description,
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
        public int Id { get; set; }

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

        [MaxLength(1000)]
        public string? Description { get; set; }
    }
}
