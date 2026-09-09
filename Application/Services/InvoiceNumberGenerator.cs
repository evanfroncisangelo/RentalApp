using Microsoft.EntityFrameworkCore;
using RentalApp.Application.Interfaces;
using RentalApp.Data;

namespace RentalApp.Application.Services;

public class InvoiceNumberGenerator(RentalDbContext dbContext) : IInvoiceNumberGenerator
{
    public async Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var prefix = $"INV-{DateTime.UtcNow:yyyyMM}";
        var latestInvoiceNumber = await dbContext.Invoices
            .AsNoTracking()
            .Where(x => x.InvoiceNumber.StartsWith(prefix))
            .OrderByDescending(x => x.InvoiceNumber)
            .Select(x => x.InvoiceNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var nextSequence = 1;
        if (!string.IsNullOrWhiteSpace(latestInvoiceNumber))
        {
            var lastDashIndex = latestInvoiceNumber.LastIndexOf('-');
            if (lastDashIndex >= 0 && int.TryParse(latestInvoiceNumber[(lastDashIndex + 1)..], out var parsedSequence))
            {
                nextSequence = parsedSequence + 1;
            }
        }

        return $"{prefix}-{nextSequence:D4}";
    }
}
