namespace RentalApp.Application.Interfaces;

public interface IUtilityProrationService
{
    Task RecalculateResponsibilitiesAsync(int utilityBillId, CancellationToken cancellationToken = default);
}
