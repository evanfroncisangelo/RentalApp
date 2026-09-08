namespace RentalApp.Application.DTOs.ExpenseCategories;

public class CreateExpenseCategoryRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
