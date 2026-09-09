using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RentalApp.Application.DTOs.Expenses;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;

namespace RentalApp.Pages.Expenses;

[Authorize]
public class EditModel(
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

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        await LoadOptionsAsync(cancellationToken);

        try
        {
            var expense = await expenseService.GetByIdAsync(id, cancellationToken);
            Input = new InputModel
            {
                Id = expense.Id,
                PropertyId = expense.PropertyId,
                UnitId = expense.UnitId,
                CategoryId = expense.CategoryId,
                Amount = expense.Amount,
                ExpenseDate = expense.ExpenseDate,
                Notes = expense.Notes
            };
            return Page();
        }
        catch (AppNotFoundException)
        {
            return RedirectToPage("/Expenses/Index");
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadOptionsAsync(cancellationToken);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        await expenseService.UpdateAsync(Input.Id, new UpdateExpenseRequestDto
        {
            PropertyId = Input.PropertyId,
            UnitId = Input.UnitId,
            CategoryId = Input.CategoryId,
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
        public int Id { get; set; }

        [Required]
        [Display(Name = "Property")]
        public int PropertyId { get; set; }

        [Display(Name = "Unit (Optional)")]
        public int? UnitId { get; set; }

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Range(0.01, 100000000)]
        public decimal Amount { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Expense Date")]
        public DateTime ExpenseDate { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}
