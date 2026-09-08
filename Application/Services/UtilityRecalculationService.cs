using Microsoft.EntityFrameworkCore;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;
using RentalApp.Data;
using RentalApp.Domain.Enums;

namespace RentalApp.Application.Services;

public class UtilityRecalculationService(
    RentalDbContext dbContext,
    IUtilityRateService utilityRateService,
    IUtilityBillService utilityBillService,
    IUtilityProrationService utilityProrationService,
    IUtilityPeriodLockService utilityPeriodLockService) : IUtilityRecalculationService
{
    public async Task<Guid?> RecalculateFromReadingAsync(int readingId, CancellationToken cancellationToken = default)
    {
        var triggerReading = await dbContext.UtilityMeterReadings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == readingId, cancellationToken);

        if (triggerReading is null)
        {
            return null;
        }

        var batchId = Guid.NewGuid();
        var startedAt = DateTime.UtcNow;

        var batch = new Domain.Entities.UtilityRecalculationBatch
        {
            Id = batchId,
            UtilityCustomerId = triggerReading.UtilityCustomerId,
            UtilityTypeId = triggerReading.UtilityTypeId,
            TriggerReadingId = triggerReading.Id,
            Status = UtilityRecalculationBatchStatus.Running,
            StartedAt = startedAt
        };

        dbContext.UtilityRecalculationBatches.Add(batch);
        await dbContext.SaveChangesAsync(cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var readings = await dbContext.UtilityMeterReadings
                .Where(x => x.UtilityCustomerId == triggerReading.UtilityCustomerId && x.UtilityTypeId == triggerReading.UtilityTypeId)
                .OrderBy(x => x.ReadingDate)
                .ThenBy(x => x.Id)
                .ToListAsync(cancellationToken);

            decimal previous = 0m;
            foreach (var reading in readings)
            {
                if (reading.ReadingValue < previous)
                {
                    throw new AppValidationException("Recalculation failed: encountered a reading lower than the recalculated previous reading.");
                }

                reading.PreviousReadingValue = previous;
                reading.Consumption = reading.ReadingValue - previous;
                reading.IsBackdated = reading.ReadingDate < readings[^1].ReadingDate;
                reading.RecalculationBatchId = batchId;
                reading.UpdatedAt = DateTime.UtcNow;

                previous = reading.ReadingValue;
            }

            var bills = await dbContext.UtilityBills
                .Include(x => x.Payments)
                .Include(x => x.UtilityCustomer)
                .Where(x => x.UtilityCustomerId == triggerReading.UtilityCustomerId && x.UtilityTypeId == triggerReading.UtilityTypeId)
                .ToListAsync(cancellationToken);

            var readingsByPeriod = readings
                .GroupBy(x => $"{x.ReadingDate:yyyy-MM}")
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.ReadingDate).ThenBy(x => x.Id).Last());

            foreach (var bill in bills)
            {
                if (!readingsByPeriod.TryGetValue(bill.BillingPeriod, out var periodReading))
                {
                    continue;
                }

                if (await utilityPeriodLockService.IsLockedAsync(bill.UtilityTypeId, bill.UtilityCustomerId, bill.BillingPeriod, cancellationToken)
                    || await utilityPeriodLockService.IsLockedAsync(bill.UtilityTypeId, null, bill.BillingPeriod, cancellationToken))
                {
                    continue;
                }

                var rate = await utilityRateService.ResolveRateAsync(
                    bill.UtilityCustomerId,
                    bill.UtilityTypeId,
                    bill.BillingPeriod,
                    cancellationToken);

                bill.PreviousReading = periodReading.PreviousReadingValue;
                bill.CurrentReading = periodReading.ReadingValue;
                bill.Consumption = periodReading.Consumption;
                bill.Rate = rate;
                bill.Amount = decimal.Round(periodReading.Consumption * rate, 2, MidpointRounding.AwayFromZero);
                bill.CreatedFromReadingId = periodReading.Id;
                bill.DueDate = UtilityBillService.ResolveDueDate(
                    bill.UtilityCustomer ?? throw new AppValidationException("Utility customer not found for bill recalculation."),
                    DateTime.UtcNow,
                    bill.BillingPeriod);
                bill.IsRecalculated = true;
                bill.RecalculationBatchId = batchId;
                bill.UpdatedAt = DateTime.UtcNow;

                await utilityBillService.RecomputeStatusAsync(bill.Id, cancellationToken);
                await utilityProrationService.RecalculateResponsibilitiesAsync(bill.Id, cancellationToken);
            }

            batch.Status = UtilityRecalculationBatchStatus.Completed;
            batch.CompletedAt = DateTime.UtcNow;
            batch.Summary = $"Recalculated {readings.Count} readings and {bills.Count} bills.";

            dbContext.UtilityAuditLogs.Add(new Domain.Entities.UtilityAuditLog
            {
                EntityName = "UtilityMeterReading",
                EntityId = triggerReading.Id.ToString(),
                Action = UtilityAuditActionType.Recalculate,
                OldValuesJson = null,
                NewValuesJson = $"{{\"BatchId\":\"{batchId}\",\"Readings\":{readings.Count},\"Bills\":{bills.Count}}}",
                PerformedBy = "system",
                PerformedAt = DateTime.UtcNow,
                CorrelationId = batchId
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return batchId;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);

            batch.Status = UtilityRecalculationBatchStatus.Failed;
            batch.CompletedAt = DateTime.UtcNow;
            batch.Summary = "Recalculation failed.";
            await dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }
}
