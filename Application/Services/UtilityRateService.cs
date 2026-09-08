using Microsoft.EntityFrameworkCore;
using RentalApp.Application.DTOs.Utilities;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;
using RentalApp.Data;
using RentalApp.Domain.Entities;

namespace RentalApp.Application.Services;

public class UtilityRateService(RentalDbContext dbContext) : IUtilityRateService
{
    public async Task<IReadOnlyList<UtilityCustomerRateDto>> GetAllAsync(int? utilityCustomerId, int? utilityTypeId, string? billingPeriod, CancellationToken cancellationToken = default)
    {
        var query = dbContext.UtilityCustomerRates
            .AsNoTracking()
            .AsQueryable();

        if (utilityCustomerId.HasValue)
        {
            query = query.Where(x => x.UtilityCustomerId == utilityCustomerId.Value);
        }

        if (utilityTypeId.HasValue)
        {
            query = query.Where(x => x.UtilityTypeId == utilityTypeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(billingPeriod))
        {
            query = query.Where(x => x.BillingPeriod == billingPeriod.Trim());
        }

        return await query
            .OrderByDescending(x => x.BillingPeriod)
            .ThenBy(x => x.UtilityCustomerId)
            .ThenBy(x => x.UtilityTypeId)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<UtilityCustomerRateDto> UpsertAsync(UpsertUtilityCustomerRateRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidatePeriod(request.BillingPeriod);

        if (request.Rate < 0)
        {
            throw new AppValidationException("Rate cannot be negative.");
        }

        _ = await dbContext.UtilityCustomers.FirstOrDefaultAsync(x => x.Id == request.UtilityCustomerId && x.IsActive, cancellationToken)
            ?? throw new AppValidationException("Utility customer does not exist or is inactive.");

        _ = await dbContext.UtilityTypes.FirstOrDefaultAsync(x => x.Id == request.UtilityTypeId && x.IsActive, cancellationToken)
            ?? throw new AppValidationException("Utility type does not exist or is inactive.");

        var normalizedPeriod = request.BillingPeriod.Trim();

        var existing = await dbContext.UtilityCustomerRates
            .FirstOrDefaultAsync(x => x.UtilityCustomerId == request.UtilityCustomerId
                && x.UtilityTypeId == request.UtilityTypeId
                && x.BillingPeriod == normalizedPeriod, cancellationToken);

        if (existing is null)
        {
            existing = new UtilityCustomerRate
            {
                UtilityCustomerId = request.UtilityCustomerId,
                UtilityTypeId = request.UtilityTypeId,
                BillingPeriod = normalizedPeriod,
                Rate = request.Rate,
                IsLocked = request.IsLocked,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            dbContext.UtilityCustomerRates.Add(existing);
        }
        else
        {
            existing.Rate = request.Rate;
            existing.IsLocked = request.IsLocked;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(existing);
    }

    public async Task<decimal> ResolveRateAsync(int utilityCustomerId, int utilityTypeId, string billingPeriod, CancellationToken cancellationToken = default)
    {
        ValidatePeriod(billingPeriod);
        var period = billingPeriod.Trim();

        var periodRate = await dbContext.UtilityCustomerRates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UtilityCustomerId == utilityCustomerId
                && x.UtilityTypeId == utilityTypeId
                && x.BillingPeriod == period, cancellationToken);

        if (periodRate is not null)
        {
            return periodRate.Rate;
        }

        var customer = await dbContext.UtilityCustomers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == utilityCustomerId && x.IsActive, cancellationToken)
            ?? throw new AppValidationException("Utility customer does not exist or is inactive.");

        if (customer.DefaultRate.HasValue)
        {
            return customer.DefaultRate.Value;
        }

        var utilityType = await dbContext.UtilityTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == utilityTypeId && x.IsActive, cancellationToken)
            ?? throw new AppValidationException("Utility type does not exist or is inactive.");

        if (utilityType.DefaultRate.HasValue)
        {
            return utilityType.DefaultRate.Value;
        }

        throw new AppValidationException("No rate configured for the selected utility customer/type/period.");
    }

    private static void ValidatePeriod(string billingPeriod)
    {
        if (string.IsNullOrWhiteSpace(billingPeriod)
            || billingPeriod.Trim().Length != 7
            || !DateOnly.TryParse($"{billingPeriod.Trim()}-01", out _))
        {
            throw new AppValidationException("Billing period must be in YYYY-MM format.");
        }
    }

    private static UtilityCustomerRateDto ToDto(UtilityCustomerRate entity) => new()
    {
        Id = entity.Id,
        UtilityCustomerId = entity.UtilityCustomerId,
        UtilityTypeId = entity.UtilityTypeId,
        BillingPeriod = entity.BillingPeriod,
        Rate = entity.Rate,
        IsLocked = entity.IsLocked
    };
}
