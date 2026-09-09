using RentalApp.Domain.Enums;

namespace RentalApp.Application.DTOs.Units;

public class CreateUnitRequestDto
{
    public int PropertyId { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public decimal MonthlyRent { get; set; }
    public UnitStatus Status { get; set; } = UnitStatus.Available;
}
