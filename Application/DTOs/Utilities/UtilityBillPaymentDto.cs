namespace RentalApp.Application.DTOs.Utilities;

public class UtilityBillPaymentDto
{
    public int Id { get; set; }
    public int UtilityBillId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public bool IsVoided { get; set; }
    public decimal BillTotalPaidAfterPayment { get; set; }
    public decimal BillBalanceAfterPayment { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public bool IsCreditApplication { get; set; }
}
