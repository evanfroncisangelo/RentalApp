namespace RentalApp.Application.DTOs.Leases;

public class CreateLeaseRequestDto
{
    public int UnitId { get; set; }
    public int RoomId { get; set; }
    public int TenantId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal SecurityDeposit { get; set; }
    public int DueDayOfMonth { get; set; } = 1;
    public string? Notes { get; set; }
}
