namespace RentalApp.Application.DTOs.Utilities;

public class UtilityCustomerRateDto
{
    public int Id { get; set; }
    public int UtilityCustomerId { get; set; }
    public int UtilityTypeId { get; set; }
    public string BillingPeriod { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public bool IsLocked { get; set; }
}
