using RentalApp.Domain.Enums;

namespace RentalApp.Domain.Entities;

public class UtilityCustomer
{
    public int Id { get; set; }
    public int? RoomId { get; set; }
    public int? TenantId { get; set; }
    public int? UtilityCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public UtilityCustomerType CustomerType { get; set; } = UtilityCustomerType.Other;
    public UtilityDueDateRuleType DueDateRuleType { get; set; } = UtilityDueDateRuleType.DaysAfterBill;
    public DateTime? UtilityStartDate { get; set; }
    public int? DueDayOfMonth { get; set; }
    public int? DueInDays { get; set; }
    public decimal? DefaultRate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Room? Room { get; set; }
    public Tenant? Tenant { get; set; }
    public UtilityType? UtilityCategory { get; set; }
    public ICollection<UtilityBill> Bills { get; set; } = new List<UtilityBill>();
    public ICollection<UtilityCustomerCredit> Credits { get; set; } = new List<UtilityCustomerCredit>();
}
