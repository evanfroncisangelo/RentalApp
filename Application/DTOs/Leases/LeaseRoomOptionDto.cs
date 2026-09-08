namespace RentalApp.Application.DTOs.Leases;

public class LeaseRoomOptionDto
{
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int MaxCapacity { get; set; }
    public int OccupiedCount { get; set; }
    public int AvailableSlots { get; set; }
}
