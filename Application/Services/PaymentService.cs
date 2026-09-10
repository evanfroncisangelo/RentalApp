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

        var paymentDate = request.PaymentDate == default ? DateTime.UtcNow.Date : request.PaymentDate.Date;

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

        if (request.PaymentType == PaymentType.Deposit)
        {
            var hasExistingDepositForMonth = await dbContext.Payments
                .AsNoTracking()
                .AnyAsync(x =>
                    x.UnitId == lease.UnitId &&
                    x.PaymentType == PaymentType.Deposit &&
                    x.PaymentDate.Year == paymentDate.Year &&
                    x.PaymentDate.Month == paymentDate.Month,
                    cancellationToken);

            if (hasExistingDepositForMonth)
            {
                throw new AppValidationException("A deposit payment already exists for this unit in the selected month.");
            }
        }

        // Set DueDate from tenant's MoveInDate
        var dueDate = request.DueDate ?? lease.Tenant?.MoveInDate ?? DateTime.UtcNow.Date;

        if (request.PaymentType == PaymentType.Rent)
        {
            var duplicateRentPaymentExists = await dbContext.Payments
                .AsNoTracking()
                .AnyAsync(x =>
                    x.LeaseId == lease.Id &&
                    x.PaymentType == PaymentType.Rent &&
                    x.DueDate.Date == dueDate.Date &&
                    x.PaymentDate == paymentDate &&
                    x.Amount == request.Amount &&
                    x.PaymentMethod == request.PaymentMethod,
                    cancellationToken);

            if (duplicateRentPaymentExists)
            {
                throw new AppValidationException("A payment for this due is already being processed. Please refresh and try again.");
            }
        }

        var payment = new Payment
        {
            LeaseId = lease.Id,
            TenantId = lease.TenantId,
            UnitId = lease.UnitId,
            Amount = request.Amount,
            PaymentDate = paymentDate,
            DueDate = dueDate,
            PaymentType = request.PaymentType,
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

    public async Task<PaymentDto> UpdateAsync(int id, UpdatePaymentRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            throw new AppValidationException("Payment amount must be greater than zero.");
        }

        var payment = await dbContext.Payments
            .Include(x => x.Unit)
            .Include(x => x.Tenant)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Payment not found.");

        payment.Amount = request.Amount;
        payment.PaymentDate = request.PaymentDate == default ? payment.PaymentDate : request.PaymentDate.Date;
        if (request.DueDate.HasValue)
        {
            payment.DueDate = request.DueDate.Value.Date;
        }
        payment.PaymentMethod = request.PaymentMethod;
        payment.ReferenceNumber = request.ReferenceNumber?.Trim();
        payment.Notes = request.Notes?.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(payment);
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
        DueDate = entity.DueDate,
        PaymentType = entity.PaymentType,
        PaymentMethod = entity.PaymentMethod,
        PaymentStatus = "Paid",
        ReferenceNumber = entity.ReferenceNumber,
        Notes = entity.Notes
    };
}
