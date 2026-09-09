using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RentalApp.Application.DTOs.Properties;
using RentalApp.Application.Interfaces;

namespace RentalApp.Pages.Properties;

[Authorize]
public class CreateModel(IPropertyService propertyService) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await propertyService.CreateAsync(new CreatePropertyRequestDto
        {
            Name = Input.Name,
            Address = Input.Address
        }, cancellationToken);

        return RedirectToPage("/Properties/Index");
    }

    public class InputModel
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string Address { get; set; } = string.Empty;
    }
}
