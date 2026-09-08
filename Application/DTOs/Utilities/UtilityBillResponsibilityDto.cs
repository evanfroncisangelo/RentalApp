namespace RentalApp.Application.DTOs.Utilities;

public class UtilityBillResponsibilityDto
{
    public int Id { get; set; }
    public int UtilityBillId { get; set; }
    public int? TenantId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int DaysCovered { get; set; }
    public decimal PercentageShare { get; set; }
    public decimal AmountShare { get; set; }
}
