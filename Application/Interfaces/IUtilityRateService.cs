using RentalApp.Application.DTOs.Utilities;

namespace RentalApp.Application.Interfaces;

public interface IUtilityRateService
{
    Task<IReadOnlyList<UtilityCustomerRateDto>> GetAllAsync(int? utilityCustomerId, int? utilityTypeId, string? billingPeriod, CancellationToken cancellationToken = default);
    Task<UtilityCustomerRateDto> UpsertAsync(UpsertUtilityCustomerRateRequestDto request, CancellationToken cancellationToken = default);
    Task<decimal> ResolveRateAsync(int utilityCustomerId, int utilityTypeId, string billingPeriod, CancellationToken cancellationToken = default);
}
