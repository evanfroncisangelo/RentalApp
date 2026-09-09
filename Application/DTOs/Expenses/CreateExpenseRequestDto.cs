namespace RentalApp.Application.DTOs.Expenses;

public class CreateExpenseRequestDto
{
    public int PropertyId { get; set; }
    public int? UnitId { get; set; }
    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string? Notes { get; set; }
}
