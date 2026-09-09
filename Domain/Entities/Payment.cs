using RentalApp.Domain.Enums;

namespace RentalApp.Domain.Entities;

public class Payment
{
    public int Id { get; set; }
    public int LeaseId { get; set; }
    public int TenantId { get; set; }
    public int UnitId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public DateTime DueDate { get; set; }
    public PaymentType PaymentType { get; set; } = PaymentType.Rent;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Lease? Lease { get; set; }
    public Tenant? Tenant { get; set; }
    public Unit? Unit { get; set; }
}
