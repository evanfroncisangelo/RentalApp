using RentalApp.Application.DTOs.Utilities;

namespace RentalApp.Application.Interfaces;

public interface IUtilityBillService
{
    Task<IReadOnlyList<UtilityBillDto>> GetAllAsync(int? utilityCustomerId, int? utilityTypeId, string? billingPeriod, CancellationToken cancellationToken = default);
    Task<UtilityBillDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UtilityBillDto> RecomputeStatusAsync(int billId, CancellationToken cancellationToken = default);
}
