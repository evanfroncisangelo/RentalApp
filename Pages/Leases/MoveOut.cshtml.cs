using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RentalApp.Application.DTOs.Leases;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;

namespace RentalApp.Pages.Leases;

[Authorize]
public class MoveOutModel(ILeaseService leaseService) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            await leaseService.GetByIdAsync(id, cancellationToken);
            Input = new InputModel
            {
                LeaseId = id,
                MoveOutDate = DateTime.UtcNow.Date
            };
            return Page();
        }
        catch (AppNotFoundException)
        {
            return RedirectToPage("/Leases/Index");
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await leaseService.MoveOutAsync(Input.LeaseId, new MoveOutRequestDto
        {
            MoveOutDate = Input.MoveOutDate,
            Notes = Input.Notes
        }, cancellationToken);

        return RedirectToPage("/Leases/Index");
    }

    public class InputModel
    {
        public int LeaseId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Move Out Date")]
        public DateTime MoveOutDate { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}
