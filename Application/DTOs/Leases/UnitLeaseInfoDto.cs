namespace RentalApp.Application.DTOs.Leases;

public class UnitLeaseInfoDto
{
    public int UnitId { get; set; }
    public decimal MonthlyRent { get; set; }
    public List<LeaseRoomOptionDto> Rooms { get; set; } = [];
}
