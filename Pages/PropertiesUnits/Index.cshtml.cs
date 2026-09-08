using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RentalApp.Application.Common;
using RentalApp.Application.DTOs.Properties;
using RentalApp.Application.DTOs.Units;
using RentalApp.Application.Interfaces;

namespace RentalApp.Pages.PropertiesUnits;

[Authorize]
public class IndexModel(IPropertyService propertyService, IUnitService unitService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public PagedResult<PropertyDto> PagedProperties { get; private set; } = new();
    public Dictionary<int, IReadOnlyList<UnitDto>> UnitsByPropertyId { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var properties = await propertyService.GetAllAsync(Search, cancellationToken);
        var units = await unitService.GetAllAsync(null, cancellationToken);

        PagedProperties = PagedResult<PropertyDto>.Create(properties, PageNumber, 10);
        UnitsByPropertyId = units
            .GroupBy(x => x.PropertyId)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<UnitDto>)x.OrderBy(u => u.UnitNumber).ToList());
    }

    public async Task<IActionResult> OnPostDeactivatePropertyAsync(int id, CancellationToken cancellationToken)
    {
        await propertyService.DeactivateAsync(id, cancellationToken);
        return RedirectToPage(new { Search, PageNumber });
    }

    public async Task<IActionResult> OnPostDeactivateUnitAsync(int id, CancellationToken cancellationToken)
    {
        await unitService.DeactivateAsync(id, cancellationToken);
        return RedirectToPage(new { Search, PageNumber });
    }
}
