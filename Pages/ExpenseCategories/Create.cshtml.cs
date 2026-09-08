using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RentalApp.Application.DTOs.ExpenseCategories;
using RentalApp.Application.Interfaces;

namespace RentalApp.Pages.ExpenseCategories;

[Authorize]
public class CreateModel(IExpenseCategoryService expenseCategoryService) : PageModel
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

        await expenseCategoryService.CreateAsync(new CreateExpenseCategoryRequestDto
        {
            Name = Input.Name,
            Description = Input.Description
        }, cancellationToken);

        return RedirectToPage("/ExpenseCategories/Index");
    }

    public class InputModel
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}
