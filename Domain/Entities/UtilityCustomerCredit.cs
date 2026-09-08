using RentalApp.Domain.Enums;

namespace RentalApp.Domain.Entities;

public class UtilityCustomerCredit
{
    public int Id { get; set; }
    public int UtilityCustomerId { get; set; }
    public int UtilityTypeId { get; set; }
    public int? SourcePaymentId { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public UtilityCreditTransactionType TransactionType { get; set; } = UtilityCreditTransactionType.OverpaymentCreated;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    public UtilityCustomer? UtilityCustomer { get; set; }
    public UtilityType? UtilityType { get; set; }
    public UtilityBillPayment? SourcePayment { get; set; }
}
