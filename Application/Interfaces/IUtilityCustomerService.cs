using RentalApp.Application.DTOs.Utilities;

namespace RentalApp.Application.Interfaces;

public interface IUtilityCustomerService
{
    Task<IReadOnlyList<UtilityCustomerDto>> GetAllAsync(string? search, CancellationToken cancellationToken = default);
    Task<UtilityCustomerDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UtilityCustomerDto> CreateAsync(CreateUtilityCustomerRequestDto request, CancellationToken cancellationToken = default);
    Task<UtilityCustomerDto> UpdateAsync(int id, UpdateUtilityCustomerRequestDto request, CancellationToken cancellationToken = default);
}
