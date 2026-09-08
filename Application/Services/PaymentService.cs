using Microsoft.EntityFrameworkCore;
using RentalApp.Application.DTOs.Payments;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;
using RentalApp.Data;
using RentalApp.Domain.Entities;
using RentalApp.Domain.Enums;

namespace RentalApp.Application.Services;

public class PaymentService(RentalDbContext dbContext) : IPaymentService
{
    public async Task<IReadOnlyList<PaymentDto>> GetAllAsync(int? leaseId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Payments
            .AsNoTracking()
            .Include(x => x.Unit)
            .Include(x => x.Tenant)
            .AsQueryable();

        if (leaseId.HasValue)
        {
            query = query.Where(x => x.LeaseId == leaseId.Value);
        }

        return await query
            .OrderByDescending(x => x.PaymentDate)
            .ThenByDescending(x => x.Id)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<PaymentDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.Payments
            .AsNoTracking()
            .Include(x => x.Unit)
            .Include(x => x.Tenant)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Payment not found.");

        return ToDto(payment);
    }

    public async Task<PaymentDto> CreateAsync(CreatePaymentRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            throw new AppValidationException("Payment amount must be greater than zero.");
        }

        var lease = await dbContext.Leases
            .AsNoTracking()
            .Include(x => x.Unit)
            .Include(x => x.Tenant)
            .FirstOrDefaultAsync(x => x.Id == request.LeaseId, cancellationToken)
            ?? throw new AppValidationException("Lease does not exist.");

        if (lease.Status != LeaseStatus.Active && lease.Status != LeaseStatus.Ended)
        {
            throw new AppValidationException("Payments can only be recorded for active or ended leases.");
        }

        var payment = new Payment
        {
            LeaseId = lease.Id,
            TenantId = lease.TenantId,
            UnitId = lease.UnitId,
            Amount = request.Amount,
            PaymentDate = request.PaymentDate == default ? DateTime.UtcNow.Date : request.PaymentDate.Date,
            PaymentMethod = request.PaymentMethod,
            ReferenceNumber = request.ReferenceNumber?.Trim(),
            Notes = request.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync(cancellationToken);

        var created = await dbContext.Payments
            .AsNoTracking()
            .Include(x => x.Unit)
            .Include(x => x.Tenant)
            .FirstAsync(x => x.Id == payment.Id, cancellationToken);

        return ToDto(created);
    }

    private static PaymentDto ToDto(Payment entity) => new()
    {
        Id = entity.Id,
        LeaseId = entity.LeaseId,
        TenantId = entity.TenantId,
        TenantName = entity.Tenant is null ? string.Empty : $"{entity.Tenant.FirstName} {entity.Tenant.LastName}".Trim(),
        UnitId = entity.UnitId,
        UnitNumber = entity.Unit?.UnitNumber ?? string.Empty,
        Amount = entity.Amount,
        PaymentDate = entity.PaymentDate,
        PaymentMethod = entity.PaymentMethod,
        PaymentStatus = "Paid",
        ReferenceNumber = entity.ReferenceNumber,
        Notes = entity.Notes
    };
}
