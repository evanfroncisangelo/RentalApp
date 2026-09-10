using RentalApp.Domain.Enums;

namespace RentalApp.Domain.Entities;

public class Unit
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public decimal MonthlyRent { get; set; }
    public int MaxCapacity { get; set; } = 0;
    public UnitStatus Status { get; set; } = UnitStatus.Available;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Property? Property { get; set; }
    public ICollection<Room> Rooms { get; set; } = new List<Room>();
    public ICollection<Lease> Leases { get; set; } = new List<Lease>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
