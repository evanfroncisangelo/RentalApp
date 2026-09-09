namespace RentalApp.Domain.Entities;

public class UtilityType
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal? DefaultRate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UtilityBill> Bills { get; set; } = new List<UtilityBill>();
    public ICollection<UtilityCustomerCredit> CustomerCredits { get; set; } = new List<UtilityCustomerCredit>();
}
