using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RentalApp.Application.Common;
using RentalApp.Application.DTOs.ExpenseCategories;
using RentalApp.Application.DTOs.Expenses;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;

namespace RentalApp.Pages.Expenses;

[Authorize]
public class IndexModel(
    IExpenseService expenseService,
    IExpenseCategoryService expenseCategoryService,
    IPropertyService propertyService,
    IUnitService unitService) : PageModel
{
    [BindProperty(SupportsGet = true), DataType(DataType.Date)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true), DataType(DataType.Date)]
    public DateTime? ToDate { get; set; }

    [BindProperty(SupportsGet = true)]
    [Display(Name = "Category")]
    public int? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int ExpensesPageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int CategoriesPageNumber { get; set; } = 1;

    [BindProperty]
    public CategoryInputModel CategoryInput { get; set; } = new();

    [BindProperty]
    public ExpenseInputModel ExpenseInput { get; set; } = new();

    public PagedResult<ExpenseDto> PagedItems { get; private set; } = new();
    public PagedResult<ExpenseCategoryDto> PagedCategories { get; private set; } = new();
    public List<SelectListItem> CategoryFilterOptions { get; private set; } = [];
    public List<SelectListItem> CategoryOptions { get; private set; } = [];
    public List<SelectListItem> PropertyOptions { get; private set; } = [];
    public List<SelectListItem> UnitOptions { get; private set; } = [];
    public Dictionary<int, int> UnitPropertyMap { get; private set; } = [];
    public bool ShowCategoryModal { get; private set; }
    public bool ShowExpenseModal { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var items = await expenseService.GetAllAsync(FromDate, ToDate, CategoryId, cancellationToken);
        var categories = await expenseCategoryService.GetAllAsync(null, cancellationToken);
        var properties = await propertyService.GetAllAsync(null, cancellationToken);
        var units = await unitService.GetAllAsync(null, cancellationToken);

        PagedItems = PagedResult<ExpenseDto>.Create(items, ExpensesPageNumber, 10);
        PagedCategories = PagedResult<ExpenseCategoryDto>.Create(
            categories.OrderByDescending(x => x.Id).ToList(),
            CategoriesPageNumber,
            10);

        CategoryFilterOptions = [new SelectListItem("All", "")];
        CategoryFilterOptions.AddRange(categories
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString(), CategoryId == x.Id)));

        CategoryOptions = categories
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();

        PropertyOptions = properties
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();

        var activeUnits = units
            .Where(x => x.IsActive)
            .OrderBy(x => x.PropertyName)
            .ThenBy(x => x.UnitNumber)
            .ToList();

        UnitOptions = [new SelectListItem("(None)", "")];
        UnitOptions.AddRange(activeUnits
            .Select(x => new SelectListItem($"{x.PropertyName} - {x.UnitNumber}", x.Id.ToString())));

        UnitPropertyMap = activeUnits.ToDictionary(x => x.Id, x => x.PropertyId);

        if (ExpenseInput.ExpenseDate == default)
        {
            ExpenseInput.ExpenseDate = DateTime.UtcNow.Date;
        }
    }

    public async Task<IActionResult> OnPostSaveCategoryAsync(CancellationToken cancellationToken)
    {
        ModelState.Clear();

        if (!TryValidateModel(CategoryInput, nameof(CategoryInput)))
        {
            ShowCategoryModal = true;
            await LoadAsync(cancellationToken);
            return Page();
        }

        try
        {
            if (CategoryInput.Id > 0)
            {
                await expenseCategoryService.UpdateAsync(CategoryInput.Id, new()
                {
                    Name = CategoryInput.Name,
                    Description = CategoryInput.Description
                }, cancellationToken);
            }
            else
            {
                await expenseCategoryService.CreateAsync(new()
                {
                    Name = CategoryInput.Name,
                    Description = CategoryInput.Description
                }, cancellationToken);
            }
        }
        catch (AppValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ShowCategoryModal = true;
            await LoadAsync(cancellationToken);
            return Page();
        }

        return RedirectToPage(new
        {
            FromDate,
            ToDate,
            CategoryId,
            ExpensesPageNumber = 1,
            CategoriesPageNumber = 1
        });
    }

    public async Task<IActionResult> OnPostSaveExpenseAsync(CancellationToken cancellationToken)
    {
        ModelState.Clear();

        if (!TryValidateModel(ExpenseInput, nameof(ExpenseInput)))
        {
            ShowExpenseModal = true;
            await LoadAsync(cancellationToken);
            return Page();
        }

        try
        {
            if (ExpenseInput.Id > 0)
            {
                await expenseService.UpdateAsync(ExpenseInput.Id, new UpdateExpenseRequestDto
                {
                    PropertyId = ExpenseInput.PropertyId,
                    UnitId = ExpenseInput.UnitId,
                    CategoryId = ExpenseInput.CategoryId,
                    Amount = ExpenseInput.Amount,
                    ExpenseDate = ExpenseInput.ExpenseDate,
                    Notes = ExpenseInput.Notes
                }, cancellationToken);
            }
            else
            {
                await expenseService.CreateAsync(new CreateExpenseRequestDto
                {
                    PropertyId = ExpenseInput.PropertyId,
                    UnitId = ExpenseInput.UnitId,
                    CategoryId = ExpenseInput.CategoryId,
                    Amount = ExpenseInput.Amount,
                    ExpenseDate = ExpenseInput.ExpenseDate,
                    Notes = ExpenseInput.Notes
                }, cancellationToken);
            }
        }
        catch (AppValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ShowExpenseModal = true;
            await LoadAsync(cancellationToken);
            return Page();
        }

        return RedirectToPage(new
        {
            FromDate,
            ToDate,
            CategoryId,
            ExpensesPageNumber = 1,
            CategoriesPageNumber = 1
        });
    }

    public class CategoryInputModel
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }
    }

    public class ExpenseInputModel
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
        public DateTime ExpenseDate { get; set; } = DateTime.UtcNow.Date;

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}
