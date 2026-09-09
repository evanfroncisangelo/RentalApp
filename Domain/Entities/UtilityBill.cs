using RentalApp.Domain.Enums;

namespace RentalApp.Domain.Entities;

public class UtilityBill
{
    public int Id { get; set; }
    public int UtilityCustomerId { get; set; }
    public int UtilityTypeId { get; set; }
    public string BillingPeriod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime? DueDate { get; set; }
    public UtilityBillStatus Status { get; set; } = UtilityBillStatus.Unpaid;
    public byte[] Version { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public UtilityCustomer? UtilityCustomer { get; set; }
    public UtilityType? UtilityType { get; set; }
    public ICollection<UtilityBillPayment> Payments { get; set; } = new List<UtilityBillPayment>();
}
