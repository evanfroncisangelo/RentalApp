using RentalApp.Domain.Enums;

namespace RentalApp.Domain.Entities;

public class UtilityBill
{
    public int Id { get; set; }
    public int UtilityCustomerId { get; set; }
    public int UtilityTypeId { get; set; }
    public string BillingPeriod { get; set; } = string.Empty;
    public decimal PreviousReading { get; set; }
    public decimal CurrentReading { get; set; }
    public decimal Consumption { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public UtilityBillStatus Status { get; set; } = UtilityBillStatus.Unpaid;
    public int? CreatedFromReadingId { get; set; }
    public bool IsRecalculated { get; set; }
    public Guid? RecalculationBatchId { get; set; }
    public byte[] Version { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public UtilityCustomer? UtilityCustomer { get; set; }
    public UtilityType? UtilityType { get; set; }
    public UtilityMeterReading? CreatedFromReading { get; set; }
    public UtilityRecalculationBatch? RecalculationBatch { get; set; }
    public ICollection<UtilityBillPayment> Payments { get; set; } = new List<UtilityBillPayment>();
    public ICollection<UtilityBillResponsibility> Responsibilities { get; set; } = new List<UtilityBillResponsibility>();
}
