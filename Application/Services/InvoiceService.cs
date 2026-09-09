using Microsoft.EntityFrameworkCore;
using RentalApp.Application.DTOs.Invoices;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;
using RentalApp.Data;
using RentalApp.Domain.Entities;
using RentalApp.Domain.Enums;

namespace RentalApp.Application.Services;

public class InvoiceService(
    RentalDbContext dbContext,
    IInvoiceNumberGenerator invoiceNumberGenerator,
    IInvoicePdfRenderer invoicePdfRenderer) : IInvoiceService
{
    public async Task<IReadOnlyList<InvoiceDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var invoices = await dbContext.Invoices
            .AsNoTracking()
            .Include(x => x.Tenant)
            .Include(x => x.Lease)
                .ThenInclude(x => x!.Unit)
            .Include(x => x.Items)
            .OrderByDescending(x => x.InvoiceDate)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

        return invoices.Select(ToDto).ToList();
    }

    public async Task<InvoiceDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.Invoices
            .AsNoTracking()
            .Include(x => x.Tenant)
            .Include(x => x.Lease)
                .ThenInclude(x => x!.Unit)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Invoice not found.");

        return ToDto(invoice);
    }

    public async Task<InvoiceDto> CreateAsync(CreateInvoiceRequestDto request, CancellationToken cancellationToken = default)
    {
        return await ExecuteInTransactionAsync(async () =>
        {
            await ValidateLeaseTenantAsync(request.LeaseId, request.TenantId, cancellationToken);
            ValidateDates(request.InvoiceDate, request.DueDate);

            var items = NormalizeItems(request.Items);
            var subtotal = items.Sum(x => x.Amount);
            var invoiceNumber = await invoiceNumberGenerator.GenerateAsync(cancellationToken);

            var invoice = new Invoice
            {
                InvoiceNumber = invoiceNumber,
                TenantId = request.TenantId,
                LeaseId = request.LeaseId,
                InvoiceDate = request.InvoiceDate.Date,
                DueDate = request.DueDate.Date,
                Subtotal = subtotal,
                Total = subtotal,
                Status = request.Status,
                Notes = request.Notes?.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Items = items
            };

            dbContext.Invoices.Add(invoice);
            await dbContext.SaveChangesAsync(cancellationToken);

            return await GetByIdAsync(invoice.Id, cancellationToken);
        }, cancellationToken);
    }

    public async Task<InvoiceDto> UpdateAsync(int id, UpdateInvoiceRequestDto request, CancellationToken cancellationToken = default)
    {
        return await ExecuteInTransactionAsync(async () =>
        {
            var invoice = await dbContext.Invoices
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new AppNotFoundException("Invoice not found.");

            ValidateDates(request.InvoiceDate, request.DueDate);

            var normalizedItems = NormalizeItems(request.Items);
            var subtotal = normalizedItems.Sum(x => x.Amount);

            invoice.InvoiceDate = request.InvoiceDate.Date;
            invoice.DueDate = request.DueDate.Date;
            invoice.Status = request.Status;
            invoice.Notes = request.Notes?.Trim();
            invoice.Subtotal = subtotal;
            invoice.Total = subtotal;
            invoice.UpdatedAt = DateTime.UtcNow;

            dbContext.InvoiceItems.RemoveRange(invoice.Items);
            invoice.Items = normalizedItems;

            await dbContext.SaveChangesAsync(cancellationToken);

            return await GetByIdAsync(id, cancellationToken);
        }, cancellationToken);
    }

    public async Task<InvoiceDto> GenerateFromLeaseAsync(GenerateInvoiceRequestDto request, CancellationToken cancellationToken = default)
    {
        var lease = await dbContext.Leases
            .AsNoTracking()
            .Include(x => x.Tenant)
            .FirstOrDefaultAsync(x => x.Id == request.LeaseId, cancellationToken)
            ?? throw new AppValidationException("Lease does not exist.");

        if (lease.Status == LeaseStatus.Terminated)
        {
            throw new AppValidationException("Cannot generate invoice for terminated lease.");
        }

        var invoiceDate = (request.InvoiceDate ?? DateTime.UtcNow).Date;
        var dueDate = (request.DueDate ?? invoiceDate.AddDays(7)).Date;

        var createRequest = new CreateInvoiceRequestDto
        {
            TenantId = lease.TenantId,
            LeaseId = lease.Id,
            InvoiceDate = invoiceDate,
            DueDate = dueDate,
            Status = InvoiceStatus.Issued,
            Notes = request.Notes,
            Items =
            [
                new CreateInvoiceItemRequestDto
                {
                    Description = $"Monthly Rent ({invoiceDate:MMMM yyyy})",
                    Amount = lease.MonthlyRent
                }
            ]
        };

        return await CreateAsync(createRequest, cancellationToken);
    }

    public async Task<byte[]> GeneratePdfAsync(int id, CancellationToken cancellationToken = default)
    {
        var invoice = await GetByIdAsync(id, cancellationToken);
        return invoicePdfRenderer.Render(invoice);
    }

    private async Task ValidateLeaseTenantAsync(int leaseId, int tenantId, CancellationToken cancellationToken)
    {
        var lease = await dbContext.Leases
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == leaseId, cancellationToken)
            ?? throw new AppValidationException("Lease does not exist.");

        if (lease.TenantId != tenantId)
        {
            throw new AppValidationException("Tenant does not match selected lease.");
        }
    }

    private async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        var ownsTransaction = dbContext.Database.CurrentTransaction is null &&
            !string.Equals(dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal);
        await using var transaction = ownsTransaction
            ? await dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken)
            : null;

        try
        {
            var result = await action();
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return result;
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw;
        }
    }

    private static void ValidateDates(DateTime invoiceDate, DateTime dueDate)
    {
        if (dueDate.Date < invoiceDate.Date)
        {
            throw new AppValidationException("Due date cannot be earlier than invoice date.");
        }
    }

    private static List<InvoiceItem> NormalizeItems(IEnumerable<CreateInvoiceItemRequestDto> items)
    {
        var normalized = items
            .Where(x => !string.IsNullOrWhiteSpace(x.Description))
            .Select(x => new InvoiceItem
            {
                Description = x.Description.Trim(),
                Amount = x.Amount
            })
            .Where(x => x.Amount > 0)
            .ToList();

        if (normalized.Count == 0)
        {
            throw new AppValidationException("Invoice must contain at least one item with amount greater than zero.");
        }

        return normalized;
    }

    private static InvoiceDto ToDto(Invoice entity) => new()
    {
        Id = entity.Id,
        InvoiceNumber = entity.InvoiceNumber,
        TenantId = entity.TenantId,
        TenantName = entity.Tenant is null ? string.Empty : $"{entity.Tenant.FirstName} {entity.Tenant.LastName}".Trim(),
        LeaseId = entity.LeaseId,
        UnitNumber = entity.Lease?.Unit?.UnitNumber ?? string.Empty,
        InvoiceDate = entity.InvoiceDate,
        DueDate = entity.DueDate,
        Subtotal = entity.Subtotal,
        Total = entity.Total,
        Status = entity.Status,
        Notes = entity.Notes,
        Items = entity.Items.Select(i => new InvoiceItemDto
        {
            Id = i.Id,
            Description = i.Description,
            Amount = i.Amount
        }).ToList()
    };
}
