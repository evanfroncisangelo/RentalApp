using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RentalApp.Application.DTOs.ExpenseCategories;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;

namespace RentalApp.Pages.ExpenseCategories;

[Authorize]
public class EditModel(IExpenseCategoryService expenseCategoryService) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var item = await expenseCategoryService.GetByIdAsync(id, cancellationToken);
            Input = new InputModel
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description
            };

            return Page();
        }
        catch (AppNotFoundException)
        {
            return RedirectToPage("/ExpenseCategories/Index");
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await expenseCategoryService.UpdateAsync(Input.Id, new UpdateExpenseCategoryRequestDto
        {
            Name = Input.Name,
            Description = Input.Description
        }, cancellationToken);

        return RedirectToPage("/ExpenseCategories/Index");
    }

    public class InputModel
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}
