using RentalApp.Application.DTOs.Units;

namespace RentalApp.Application.Interfaces;

public interface IUnitService
{
    Task<IReadOnlyList<UnitDto>> GetAllAsync(string? search, CancellationToken cancellationToken = default);
    Task<UnitDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UnitDto> CreateAsync(CreateUnitRequestDto request, CancellationToken cancellationToken = default);
    Task<UnitDto> UpdateAsync(int id, UpdateUnitRequestDto request, CancellationToken cancellationToken = default);
    Task DeactivateAsync(int id, CancellationToken cancellationToken = default);
    Task HardDeleteAsync(int id, CancellationToken cancellationToken = default);
}
