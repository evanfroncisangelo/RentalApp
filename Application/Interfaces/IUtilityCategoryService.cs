using RentalApp.Application.DTOs.Utilities;

namespace RentalApp.Application.Interfaces;

public interface IUtilityCategoryService
{
    Task<IReadOnlyList<UtilityCategoryDto>> GetAllAsync(string? search, CancellationToken cancellationToken = default);
    Task<UtilityCategoryDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UtilityCategoryDto> CreateAsync(CreateUtilityCategoryRequestDto request, CancellationToken cancellationToken = default);
    Task<UtilityCategoryDto> UpdateAsync(int id, UpdateUtilityCategoryRequestDto request, CancellationToken cancellationToken = default);
    Task DeactivateAsync(int id, CancellationToken cancellationToken = default);
}
