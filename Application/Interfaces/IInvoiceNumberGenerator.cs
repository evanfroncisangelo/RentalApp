namespace RentalApp.Application.Interfaces;

public interface IInvoiceNumberGenerator
{
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}
