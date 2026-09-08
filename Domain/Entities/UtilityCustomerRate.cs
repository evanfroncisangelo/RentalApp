namespace RentalApp.Domain.Entities;

public class UtilityCustomerRate
{
    public int Id { get; set; }
    public int UtilityCustomerId { get; set; }
    public int UtilityTypeId { get; set; }
    public string BillingPeriod { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public bool IsLocked { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public UtilityCustomer? UtilityCustomer { get; set; }
    public UtilityType? UtilityType { get; set; }
}
