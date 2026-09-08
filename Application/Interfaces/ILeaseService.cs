using RentalApp.Application.DTOs.Leases;

namespace RentalApp.Application.Interfaces;

public interface ILeaseService
{
    Task<IReadOnlyList<LeaseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<LeaseDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<LeaseDto> CreateAsync(CreateLeaseRequestDto request, CancellationToken cancellationToken = default);
    Task<LeaseDto> UpdateAsync(int id, UpdateLeaseRequestDto request, CancellationToken cancellationToken = default);
    Task<LeaseDto> MoveOutAsync(int id, MoveOutRequestDto request, CancellationToken cancellationToken = default);
    Task<LeaseDto> TransferAsync(TransferLeaseRequestDto request, CancellationToken cancellationToken = default);
    Task<UnitLeaseInfoDto> GetUnitLeaseInfoAsync(int unitId, CancellationToken cancellationToken = default);
}
