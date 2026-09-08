using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RentalApp.Application.DTOs.Expenses;
using RentalApp.Application.Interfaces;

namespace RentalApp.Pages.Expenses;

[Authorize]
public class CreateModel(
    IExpenseService expenseService,
    IPropertyService propertyService,
    IUnitService unitService,
    IExpenseCategoryService expenseCategoryService) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<SelectListItem> PropertyOptions { get; private set; } = [];
    public List<SelectListItem> UnitOptions { get; private set; } = [];
    public List<SelectListItem> CategoryOptions { get; private set; } = [];

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

        await expenseService.CreateAsync(new CreateExpenseRequestDto
        {
            PropertyId = Input.PropertyId,
            UnitId = Input.UnitId,
            CategoryId = Input.CategoryId,
            Description = Input.Description,
            Amount = Input.Amount,
            ExpenseDate = Input.ExpenseDate,
            Notes = Input.Notes
        }, cancellationToken);

        return RedirectToPage("/Expenses/Index");
    }

    private async Task LoadOptionsAsync(CancellationToken cancellationToken)
    {
        var properties = await propertyService.GetAllAsync(null, cancellationToken);
        PropertyOptions = properties
            .Where(x => x.IsActive)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();

        var units = await unitService.GetAllAsync(null, cancellationToken);
        UnitOptions = [new SelectListItem("(None)", "")];
        UnitOptions.AddRange(units
            .Where(x => x.IsActive)
            .Select(x => new SelectListItem($"{x.PropertyName} - {x.UnitNumber}", x.Id.ToString())));

        var categories = await expenseCategoryService.GetAllAsync(null, cancellationToken);
        CategoryOptions = categories
            .Where(x => x.IsActive)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();
    }

    public class InputModel
    {
        [Required]
        [Display(Name = "Property")]
        public int PropertyId { get; set; }

        [Display(Name = "Unit (Optional)")]
        public int? UnitId { get; set; }

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Required, MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Range(0.01, 100000000)]
        public decimal Amount { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Expense Date")]
        public DateTime ExpenseDate { get; set; } = DateTime.UtcNow.Date;

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}
