using RentalApp.Domain.Enums;

namespace RentalApp.Application.DTOs.Leases;

public class LeaseDto
{
    public int Id { get; set; }
    public int UnitId { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal SecurityDeposit { get; set; }
    public int DueDayOfMonth { get; set; }
    public LeaseStatus Status { get; set; }
    public string? Notes { get; set; }
}
