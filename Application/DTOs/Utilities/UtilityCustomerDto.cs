using RentalApp.Domain.Enums;

namespace RentalApp.Application.DTOs.Utilities;

public class UtilityCustomerDto
{
    public int Id { get; set; }
    public int? RoomId { get; set; }
    public int? TenantId { get; set; }
    public int? UtilityCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public UtilityCustomerType CustomerType { get; set; }
    public UtilityDueDateRuleType DueDateRuleType { get; set; }
    public int? DueDayOfMonth { get; set; }
    public int? DueInDays { get; set; }
    public decimal? DefaultRate { get; set; }
    public bool IsActive { get; set; }
}
