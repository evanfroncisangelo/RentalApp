namespace RentalApp.Application.DTOs.Leases;

public class TransferLeaseRequestDto
{
    public int CurrentLeaseId { get; set; }
    public int NewUnitId { get; set; }
    public int NewRoomId { get; set; }
    public DateTime TransferDate { get; set; }
    public string? Notes { get; set; }
}
