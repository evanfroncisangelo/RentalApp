namespace RentalApp.Domain.Entities;

public class UtilityBillPayment
{
    public int Id { get; set; }
    public int UtilityBillId { get; set; }
    public int? PaymentMethodId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public bool IsVoided { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public UtilityBill? UtilityBill { get; set; }
    public ICollection<UtilityCustomerCredit> Credits { get; set; } = new List<UtilityCustomerCredit>();
}
