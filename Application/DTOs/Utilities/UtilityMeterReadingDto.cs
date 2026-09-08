namespace RentalApp.Application.DTOs.Utilities;

public class UtilityMeterReadingDto
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
}
