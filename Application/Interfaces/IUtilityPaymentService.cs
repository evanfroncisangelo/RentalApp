using RentalApp.Application.DTOs.Utilities;

namespace RentalApp.Application.Interfaces;

public interface IUtilityPaymentService
{
    Task<IReadOnlyList<UtilityBillPaymentDto>> GetAllAsync(int? utilityBillId, CancellationToken cancellationToken = default);
    Task<UtilityBillPaymentDto> CreateAsync(CreateUtilityBillPaymentRequestDto request, CancellationToken cancellationToken = default);
    Task<UtilityBillPaymentDto> VoidAsync(int paymentId, string? reason, CancellationToken cancellationToken = default);
    Task<UtilityBillPaymentDto> ApplyCreditAsync(int utilityBillId, decimal amount, string? notes, CancellationToken cancellationToken = default);
}
