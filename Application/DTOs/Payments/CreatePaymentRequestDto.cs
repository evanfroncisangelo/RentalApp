using RentalApp.Domain.Enums;

namespace RentalApp.Application.DTOs.Payments;

public class CreatePaymentRequestDto
{
    public int LeaseId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public PaymentType PaymentType { get; set; } = PaymentType.Rent;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
}
