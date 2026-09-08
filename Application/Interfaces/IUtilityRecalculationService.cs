namespace RentalApp.Application.Interfaces;

public interface IUtilityRecalculationService
{
    Task<Guid?> RecalculateFromReadingAsync(int readingId, CancellationToken cancellationToken = default);
}
