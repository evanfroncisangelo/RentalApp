using RentalApp.Domain.Enums;

namespace RentalApp.Domain.Entities;

public class Lease
{
    public int Id { get; set; }
    public int UnitId { get; set; }
    public int RoomId { get; set; }
    public int TenantId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal SecurityDeposit { get; set; }
    public int DueDayOfMonth { get; set; } = 1;
    public LeaseStatus Status { get; set; } = LeaseStatus.Active;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Unit? Unit { get; set; }
    public Room? Room { get; set; }
    public Tenant? Tenant { get; set; }
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
