using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RentalApp.Application.DTOs.Properties;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;

namespace RentalApp.Pages.Properties;

[Authorize]
public class EditModel(IPropertyService propertyService) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var item = await propertyService.GetByIdAsync(id, cancellationToken);
            Input = new InputModel
            {
                Id = item.Id,
                Name = item.Name,
                Address = item.Address,
                Description = item.Description
            };

            return Page();
        }
        catch (AppNotFoundException)
        {
            return RedirectToPage("/Properties/Index");
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await propertyService.UpdateAsync(Input.Id, new UpdatePropertyRequestDto
        {
            Name = Input.Name,
            Address = Input.Address,
            Description = Input.Description
        }, cancellationToken);

        return RedirectToPage("/Properties/Index");
    }

    public class InputModel
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }
    }
}
