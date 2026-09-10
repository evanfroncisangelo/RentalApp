using RentalApp.Domain.Enums;

namespace RentalApp.Application.DTOs.Units;

public class UpdateUnitRequestDto
{
    public int PropertyId { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public decimal MonthlyRent { get; set; }
    public int MaxCapacity { get; set; } = 0;
    public UnitStatus Status { get; set; } = UnitStatus.Available;
}
