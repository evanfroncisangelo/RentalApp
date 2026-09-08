namespace RentalApp.Domain.Entities;

public class UtilityMeterReading
{
    public int Id { get; set; }
    public int UtilityCustomerId { get; set; }
    public int UtilityTypeId { get; set; }
    public DateTime ReadingDate { get; set; }
    public decimal ReadingValue { get; set; }
    public decimal PreviousReadingValue { get; set; }
    public decimal Consumption { get; set; }
    public bool IsBackdated { get; set; }
    public Guid? RecalculationBatchId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public UtilityCustomer? UtilityCustomer { get; set; }
    public UtilityType? UtilityType { get; set; }
    public UtilityRecalculationBatch? RecalculationBatch { get; set; }
    public ICollection<UtilityBill> UtilityBills { get; set; } = new List<UtilityBill>();
}
