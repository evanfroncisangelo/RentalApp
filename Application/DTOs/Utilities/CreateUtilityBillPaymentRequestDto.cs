namespace RentalApp.Application.DTOs.Utilities;

public class CreateUtilityBillPaymentRequestDto
{
    public int UtilityBillId { get; set; }
    public int? PaymentMethodId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
}
