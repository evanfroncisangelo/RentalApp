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
            var lease = await ValidateLeaseTenantAsync(request.LeaseId, request.TenantId, cancellationToken);
            ValidateDates(request.InvoiceDate, request.DueDate);

            var items = await BuildStaticItemsFromConnectedRecordsAsync(lease, request.TenantId, cancellationToken);
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

            invoice.InvoiceDate = request.InvoiceDate.Date;
            invoice.DueDate = request.DueDate.Date;
            invoice.Status = request.Status;
            invoice.Notes = request.Notes?.Trim();
            invoice.UpdatedAt = DateTime.UtcNow;

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
            Notes = request.Notes
        };

        return await CreateAsync(createRequest, cancellationToken);
    }

    public async Task<byte[]> GeneratePdfAsync(int id, CancellationToken cancellationToken = default)
    {
        var invoice = await GetByIdAsync(id, cancellationToken);
        return invoicePdfRenderer.Render(invoice);
    }

    private async Task<Lease> ValidateLeaseTenantAsync(int leaseId, int tenantId, CancellationToken cancellationToken)
    {
        var lease = await dbContext.Leases
            .AsNoTracking()
            .Include(x => x.Unit)
            .FirstOrDefaultAsync(x => x.Id == leaseId, cancellationToken)
            ?? throw new AppValidationException("Lease does not exist.");

        if (lease.TenantId != tenantId)
        {
            throw new AppValidationException("Tenant does not match selected lease.");
        }

        return lease;
    }

    private async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        if (dbContext.Database.CurrentTransaction is not null ||
            string.Equals(dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal))
        {
            return await action();
        }

        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            try
            {
                var result = await action();
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    private static void ValidateDates(DateTime invoiceDate, DateTime dueDate)
    {
        if (dueDate.Date < invoiceDate.Date)
        {
            throw new AppValidationException("Due date cannot be earlier than invoice date.");
        }
    }

    private async Task<List<InvoiceItem>> BuildStaticItemsFromConnectedRecordsAsync(Lease lease, int tenantId, CancellationToken cancellationToken)
    {
        var rentAndDepositPayments = await dbContext.Payments
            .AsNoTracking()
            .Where(x => x.LeaseId == lease.Id && x.TenantId == tenantId && x.UnitId == lease.UnitId)
            .OrderBy(x => x.DueDate)
            .ThenBy(x => x.PaymentDate)
            .ThenBy(x => x.Id)
            .Select(x => new
            {
                x.PaymentType,
                x.DueDate,
                x.PaymentDate,
                x.Amount
            })
            .ToListAsync(cancellationToken);

        var utilityBills = await dbContext.UtilityBills
            .AsNoTracking()
            .Include(x => x.UtilityType)
            .Include(x => x.UtilityCustomer)
                .ThenInclude(x => x!.Room)
            .Where(x =>
                x.Status != UtilityBillStatus.Cancelled &&
                x.UtilityCustomer != null &&
                (
                    x.UtilityCustomer.TenantId == tenantId ||
                    (x.UtilityCustomer.Room != null && x.UtilityCustomer.Room.UnitId == lease.UnitId)
                ))
            .OrderBy(x => x.DueDate ?? DateTime.MaxValue)
            .ThenBy(x => x.BillingPeriod)
            .ThenBy(x => x.Id)
            .Select(x => new
            {
                UtilityTypeName = x.UtilityType != null ? x.UtilityType.Name : "Utility",
                x.BillingPeriod,
                x.Amount,
                x.Status
            })
            .ToListAsync(cancellationToken);

        var items = new List<InvoiceItem>();

        foreach (var payment in rentAndDepositPayments)
        {
            var paymentTypeText = payment.PaymentType == PaymentType.Deposit ? "Deposit" : "Rent";
            items.Add(new InvoiceItem
            {
                Description = $"{paymentTypeText} Payment ({payment.DueDate:MMM yyyy})",
                Amount = payment.Amount
            });
        }

        foreach (var bill in utilityBills)
        {
            var billingPeriodText = string.IsNullOrWhiteSpace(bill.BillingPeriod)
                ? "N/A"
                : bill.BillingPeriod;

            items.Add(new InvoiceItem
            {
                Description = $"{bill.UtilityTypeName} Bill ({billingPeriodText})",
                Amount = bill.Amount
            });
        }

        if (items.Count == 0)
        {
            throw new AppValidationException("No connected payment or utility records were found for the selected tenant and lease.");
        }

        return items;
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
