using RentalApp.Domain.Enums;

namespace RentalApp.Application.DTOs.Utilities;

public class UtilityBillDto
{
    public int Id { get; set; }
    public int UtilityCustomerId { get; set; }
    public string UtilityCustomerName { get; set; } = string.Empty;
    public int UtilityTypeId { get; set; }
    public string UtilityTypeName { get; set; } = string.Empty;
    public string BillingPeriod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Balance { get; set; }
    public DateTime? DueDate { get; set; }
    public UtilityBillStatus Status { get; set; }
    public int? LastPaymentId { get; set; }
}
