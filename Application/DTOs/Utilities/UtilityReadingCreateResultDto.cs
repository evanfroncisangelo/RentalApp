namespace RentalApp.Application.DTOs.Utilities;

public class UtilityReadingCreateResultDto
{
    public UtilityMeterReadingDto Reading { get; set; } = new();
    public UtilityBillDto Bill { get; set; } = new();
    public bool RecalculationTriggered { get; set; }
    public Guid? RecalculationBatchId { get; set; }
}
