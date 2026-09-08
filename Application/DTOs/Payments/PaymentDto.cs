using RentalApp.Domain.Enums;

namespace RentalApp.Application.DTOs.Payments;

public class PaymentDto
{
    public int Id { get; set; }
    public int LeaseId { get; set; }
    public int TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public int UnitId { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string PaymentStatus { get; set; } = "Paid";
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
}
