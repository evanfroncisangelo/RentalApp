namespace RentalApp.Domain.Entities;

public class Room
{
    public int Id { get; set; }
    public int UnitId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int MaxCapacity { get; set; } = 3;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Unit? Unit { get; set; }
    public ICollection<Lease> Leases { get; set; } = new List<Lease>();
}
