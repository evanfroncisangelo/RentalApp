namespace RentalApp.Domain.Entities;

public class UtilityBillResponsibility
{
    public int Id { get; set; }
    public int UtilityBillId { get; set; }
    public int? TenantId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int DaysCovered { get; set; }
    public decimal PercentageShare { get; set; }
    public decimal AmountShare { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public UtilityBill? UtilityBill { get; set; }
    public Tenant? Tenant { get; set; }
}
