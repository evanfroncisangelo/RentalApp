namespace RentalApp.Application.Interfaces;

public interface IUtilityPeriodLockService
{
    Task<bool> IsLockedAsync(int utilityTypeId, int? utilityCustomerId, string billingPeriod, CancellationToken cancellationToken = default);
    Task SetLockAsync(int utilityTypeId, int? utilityCustomerId, string billingPeriod, bool isLocked, string? notes, CancellationToken cancellationToken = default);
}
