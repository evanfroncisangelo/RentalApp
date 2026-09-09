using RentalApp.Application.DTOs.Tenants;

namespace RentalApp.Application.Interfaces;

public interface ITenantService
{
    Task<IReadOnlyList<TenantDto>> GetAllAsync(string? search, CancellationToken cancellationToken = default);
    Task<TenantDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<TenantDto> CreateAsync(CreateTenantRequestDto request, CancellationToken cancellationToken = default);
    Task<TenantDto> UpdateAsync(int id, UpdateTenantRequestDto request, CancellationToken cancellationToken = default);
    Task DeactivateAsync(int id, CancellationToken cancellationToken = default);
    Task ReactivateAsync(int id, CancellationToken cancellationToken = default);
}
