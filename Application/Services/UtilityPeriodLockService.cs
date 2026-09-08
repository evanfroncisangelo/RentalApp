using Microsoft.EntityFrameworkCore;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;
using RentalApp.Data;
using RentalApp.Domain.Entities;

namespace RentalApp.Application.Services;

public class UtilityPeriodLockService(RentalDbContext dbContext) : IUtilityPeriodLockService
{
    public async Task<bool> IsLockedAsync(int utilityTypeId, int? utilityCustomerId, string billingPeriod, CancellationToken cancellationToken = default)
    {
        ValidatePeriod(billingPeriod);
        var period = billingPeriod.Trim();

        var customerScoped = await dbContext.UtilityBillingPeriodLocks
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UtilityTypeId == utilityTypeId
                && x.UtilityCustomerId == utilityCustomerId
                && x.BillingPeriod == period,
                cancellationToken);

        if (customerScoped is not null)
        {
            return customerScoped.IsLocked;
        }

        var utilityScoped = await dbContext.UtilityBillingPeriodLocks
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UtilityTypeId == utilityTypeId
                && x.UtilityCustomerId == null
                && x.BillingPeriod == period,
                cancellationToken);

        return utilityScoped?.IsLocked ?? false;
    }

    public async Task SetLockAsync(int utilityTypeId, int? utilityCustomerId, string billingPeriod, bool isLocked, string? notes, CancellationToken cancellationToken = default)
    {
        ValidatePeriod(billingPeriod);

        var period = billingPeriod.Trim();

        var row = await dbContext.UtilityBillingPeriodLocks
            .FirstOrDefaultAsync(x => x.UtilityTypeId == utilityTypeId
                && x.UtilityCustomerId == utilityCustomerId
                && x.BillingPeriod == period,
                cancellationToken);

        if (row is null)
        {
            row = new UtilityBillingPeriodLock
            {
                UtilityTypeId = utilityTypeId,
                UtilityCustomerId = utilityCustomerId,
                BillingPeriod = period,
                IsLocked = isLocked,
                Notes = notes?.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.UtilityBillingPeriodLocks.Add(row);
        }
        else
        {
            row.IsLocked = isLocked;
            row.Notes = notes?.Trim();
            row.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
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
}
