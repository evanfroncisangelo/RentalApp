using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RentalApp.Application.DTOs.Tenants;
using RentalApp.Application.Interfaces;

namespace RentalApp.Pages.Tenants;

[Authorize]
public class CreateModel(ITenantService tenantService) : PageModel
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

        await tenantService.CreateAsync(new CreateTenantRequestDto
        {
            FirstName = Input.FirstName,
            LastName = Input.LastName,
            ContactNumber = Input.ContactNumber,
            Email = Input.Email,
            Address = Input.Address,
            DateOfBirth = Input.DateOfBirth,
            Notes = Input.Notes
        }, cancellationToken);

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

        [EmailAddress, MaxLength(256)]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }
    }
}
