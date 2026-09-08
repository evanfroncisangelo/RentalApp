using RentalApp.Application.DTOs.Utilities;

namespace RentalApp.Application.Interfaces;

public interface IUtilityReadingService
{
    Task<IReadOnlyList<UtilityMeterReadingDto>> GetAllAsync(int? utilityCustomerId, int? utilityTypeId, CancellationToken cancellationToken = default);
    Task<UtilityReadingCreateResultDto> CreateAndGenerateBillAsync(CreateUtilityReadingRequestDto request, CancellationToken cancellationToken = default);
}
