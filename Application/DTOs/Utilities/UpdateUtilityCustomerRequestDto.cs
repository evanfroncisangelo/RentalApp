using RentalApp.Domain.Enums;

namespace RentalApp.Application.DTOs.Utilities;

public class UpdateUtilityCustomerRequestDto
{
    public int? RoomId { get; set; }
    public int? TenantId { get; set; }
    public int? UtilityCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public UtilityCustomerType CustomerType { get; set; }
    public DateTime? UtilityStartDate { get; set; }
    public decimal? AmountToPay { get; set; }
    public int? DueDayOfMonth { get; set; }
}
