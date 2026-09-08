using RentalApp.Application.DTOs.Payments;

namespace RentalApp.Application.Interfaces;

public interface IPaymentService
{
    Task<IReadOnlyList<PaymentDto>> GetAllAsync(int? leaseId, CancellationToken cancellationToken = default);
    Task<PaymentDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PaymentDto> CreateAsync(CreatePaymentRequestDto request, CancellationToken cancellationToken = default);
}
