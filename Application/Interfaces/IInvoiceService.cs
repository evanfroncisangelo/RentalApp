using RentalApp.Application.DTOs.Invoices;

namespace RentalApp.Application.Interfaces;

public interface IInvoiceService
{
    Task<IReadOnlyList<InvoiceDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<InvoiceDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<InvoiceDto> CreateAsync(CreateInvoiceRequestDto request, CancellationToken cancellationToken = default);
    Task<InvoiceDto> UpdateAsync(int id, UpdateInvoiceRequestDto request, CancellationToken cancellationToken = default);
    Task<InvoiceDto> GenerateFromLeaseAsync(GenerateInvoiceRequestDto request, CancellationToken cancellationToken = default);
    Task<byte[]> GeneratePdfAsync(int id, CancellationToken cancellationToken = default);
}
