using RentalApp.Domain.Enums;

namespace RentalApp.Application.DTOs.Units;

public class UnitDto
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MonthlyRent { get; set; }
    public int RoomCount { get; set; }
    public int RoomMaxCapacity { get; set; }
    public UnitStatus Status { get; set; }
    public bool IsActive { get; set; }
    public IReadOnlyList<UnitRoomStatusDto> RoomStatuses { get; set; } = [];
}

public class UnitRoomStatusDto
{
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int MaxCapacity { get; set; }
    public int OccupiedCount { get; set; }
    public int AvailableSlots { get; set; }
    public string Status { get; set; } = "Available";
}
