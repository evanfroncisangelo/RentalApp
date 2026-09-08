using RentalApp.Domain.Enums;

namespace RentalApp.Application.DTOs.Units;

public class UpdateUnitRequestDto
{
    public int PropertyId { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MonthlyRent { get; set; }
    public int RoomCount { get; set; }
    public int RoomMaxCapacity { get; set; } = 3;
    public UnitStatus Status { get; set; } = UnitStatus.Available;
}
