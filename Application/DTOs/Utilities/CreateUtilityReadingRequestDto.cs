namespace RentalApp.Application.DTOs.Utilities;

public class CreateUtilityReadingRequestDto
{
    public int UtilityCustomerId { get; set; }
    public int UtilityTypeId { get; set; }
    public DateTime ReadingDate { get; set; }
    public decimal ReadingValue { get; set; }
}
