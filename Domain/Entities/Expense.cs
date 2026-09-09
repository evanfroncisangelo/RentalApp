namespace RentalApp.Domain.Entities;

public class Expense
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public int? UnitId { get; set; }
    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Property? Property { get; set; }
    public Unit? Unit { get; set; }
    public ExpenseCategory? Category { get; set; }
}
