namespace RentalApp.Domain.Entities;

public class UtilityBillingPeriodLock
{
    public int Id { get; set; }
    public int UtilityTypeId { get; set; }
    public int? UtilityCustomerId { get; set; }
    public string BillingPeriod { get; set; } = string.Empty;
    public bool IsLocked { get; set; } = true;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public UtilityType? UtilityType { get; set; }
    public UtilityCustomer? UtilityCustomer { get; set; }
}
