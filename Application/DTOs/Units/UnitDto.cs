using RentalApp.Domain.Enums;

namespace RentalApp.Application.DTOs.Units;

public class UnitDto
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public decimal MonthlyRent { get; set; }
    public int MaxCapacity { get; set; }
    public UnitStatus Status { get; set; }
    public bool IsActive { get; set; }
}
