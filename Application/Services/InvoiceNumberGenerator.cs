using Microsoft.EntityFrameworkCore;
using RentalApp.Application.Interfaces;
using RentalApp.Data;

namespace RentalApp.Application.Services;

public class InvoiceNumberGenerator(RentalDbContext dbContext) : IInvoiceNumberGenerator
{
    public async Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var prefix = $"INV-{DateTime.UtcNow:yyyyMM}";
        var countThisMonth = await dbContext.Invoices
            .AsNoTracking()
            .CountAsync(x => x.InvoiceNumber.StartsWith(prefix), cancellationToken);

        return $"{prefix}-{(countThisMonth + 1):D4}";
    }
}
