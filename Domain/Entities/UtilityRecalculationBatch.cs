using RentalApp.Domain.Enums;

namespace RentalApp.Domain.Entities;

public class UtilityRecalculationBatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int UtilityCustomerId { get; set; }
    public int UtilityTypeId { get; set; }
    public int TriggerReadingId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public UtilityRecalculationBatchStatus Status { get; set; } = UtilityRecalculationBatchStatus.Pending;
    public int? RequestedByUserId { get; set; }
    public string? Summary { get; set; }

    public UtilityCustomer? UtilityCustomer { get; set; }
    public UtilityType? UtilityType { get; set; }
    public UtilityMeterReading? TriggerReading { get; set; }
    public User? RequestedByUser { get; set; }
    public ICollection<UtilityMeterReading> RecalculatedReadings { get; set; } = new List<UtilityMeterReading>();
    public ICollection<UtilityBill> RecalculatedBills { get; set; } = new List<UtilityBill>();
}
