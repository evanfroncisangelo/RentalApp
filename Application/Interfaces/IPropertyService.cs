using RentalApp.Application.DTOs.Properties;

namespace RentalApp.Application.Interfaces;

public interface IPropertyService
{
    Task<IReadOnlyList<PropertyDto>> GetAllAsync(string? search, CancellationToken cancellationToken = default);
    Task<PropertyDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PropertyDto> CreateAsync(CreatePropertyRequestDto request, CancellationToken cancellationToken = default);
    Task<PropertyDto> UpdateAsync(int id, UpdatePropertyRequestDto request, CancellationToken cancellationToken = default);
    Task DeactivateAsync(int id, CancellationToken cancellationToken = default);
    Task HardDeleteAsync(int id, CancellationToken cancellationToken = default);
}
