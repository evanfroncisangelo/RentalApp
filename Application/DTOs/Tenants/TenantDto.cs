namespace RentalApp.Application.DTOs.Tenants;

public class TenantDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? ContactNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public int? UnitId { get; set; }
    public string? UnitNumber { get; set; }
    public string? RoomNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime? MoveInDate { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}
