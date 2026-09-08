using Microsoft.EntityFrameworkCore;
using RentalApp.Application.DTOs.Utilities;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;
using RentalApp.Data;
using RentalApp.Domain.Entities;
using RentalApp.Domain.Enums;

namespace RentalApp.Application.Services;

public class UtilityReadingService(
    RentalDbContext dbContext,
    IUtilityRateService utilityRateService,
    IUtilityRecalculationService utilityRecalculationService,
    IUtilityProrationService utilityProrationService,
    IUtilityPeriodLockService utilityPeriodLockService) : IUtilityReadingService
{
    public async Task<IReadOnlyList<UtilityMeterReadingDto>> GetAllAsync(int? utilityCustomerId, int? utilityTypeId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.UtilityMeterReadings
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

        return await query
            .OrderByDescending(x => x.ReadingDate)
            .ThenByDescending(x => x.Id)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<UtilityReadingCreateResultDto> CreateAndGenerateBillAsync(CreateUtilityReadingRequestDto request, CancellationToken cancellationToken = default)
    {
        // Ensure no stale tracked entities from prior operations in the same scope.
        dbContext.ChangeTracker.Clear();

        if (request.ReadingValue < 0)
        {
            throw new AppValidationException("Reading cannot be negative.");
        }

        var readingDate = request.ReadingDate == default ? DateTime.UtcNow.Date : request.ReadingDate.Date;
        var billingPeriod = $"{readingDate:yyyy-MM}";

        var customer = await dbContext.UtilityCustomers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.UtilityCustomerId && x.IsActive, cancellationToken)
            ?? throw new AppValidationException("Utility customer does not exist or is inactive.");

        _ = await dbContext.UtilityTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.UtilityTypeId && x.IsActive, cancellationToken)
            ?? throw new AppValidationException("Utility type does not exist or is inactive.");

        if (await utilityPeriodLockService.IsLockedAsync(request.UtilityTypeId, request.UtilityCustomerId, billingPeriod, cancellationToken)
            || await utilityPeriodLockService.IsLockedAsync(request.UtilityTypeId, null, billingPeriod, cancellationToken))
        {
            throw new AppValidationException($"Billing period {billingPeriod} is locked for this utility type/customer.");
        }

        var existingBill = await dbContext.UtilityBills
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(
                x => x.UtilityCustomerId == request.UtilityCustomerId
                     && x.UtilityTypeId == request.UtilityTypeId
                     && x.BillingPeriod == billingPeriod,
                cancellationToken);

        if (existingBill is not null)
        {
            var totalPaid = existingBill.Payments.Where(x => !x.IsVoided).Sum(x => x.Amount);
            var balance = existingBill.Amount - totalPaid;

            if (balance > 0)
            {
                throw new AppValidationException($"Existing bill for period {billingPeriod} is still unpaid ({balance:N2}). Please pay it before saving another reading for this period.");
            }

            throw new AppValidationException($"A bill for period {billingPeriod} already exists and is fully paid.");
        }

        var duplicateExists = await dbContext.UtilityMeterReadings.AnyAsync(
            x => x.UtilityCustomerId == request.UtilityCustomerId
                && x.UtilityTypeId == request.UtilityTypeId
                && x.ReadingDate == readingDate,
            cancellationToken);

        if (duplicateExists)
        {
            throw new AppValidationException("A reading already exists for this customer, utility type, and date. Use a different reading date.");
        }

        var latestReading = await dbContext.UtilityMeterReadings
            .AsNoTracking()
            .Where(x => x.UtilityCustomerId == request.UtilityCustomerId && x.UtilityTypeId == request.UtilityTypeId)
            .OrderByDescending(x => x.ReadingDate)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var previousReadingForDate = await dbContext.UtilityMeterReadings
            .AsNoTracking()
            .Where(x => x.UtilityCustomerId == request.UtilityCustomerId
                && x.UtilityTypeId == request.UtilityTypeId
                && x.ReadingDate < readingDate)
            .OrderByDescending(x => x.ReadingDate)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var nextReadingForDate = await dbContext.UtilityMeterReadings
            .AsNoTracking()
            .Where(x => x.UtilityCustomerId == request.UtilityCustomerId
                && x.UtilityTypeId == request.UtilityTypeId
                && x.ReadingDate > readingDate)
            .OrderBy(x => x.ReadingDate)
            .ThenBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var previousReading = previousReadingForDate?.ReadingValue ?? 0m;
        var isBackdated = latestReading is not null && readingDate < latestReading.ReadingDate.Date;

        if (request.ReadingValue < previousReading)
        {
            throw new AppValidationException("Current reading cannot be lower than previous reading.");
        }

        if (nextReadingForDate is not null && request.ReadingValue > nextReadingForDate.ReadingValue)
        {
            throw new AppValidationException("Current reading cannot be greater than the next recorded reading for this utility customer.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var reading = new UtilityMeterReading
        {
            UtilityCustomerId = request.UtilityCustomerId,
            UtilityTypeId = request.UtilityTypeId,
            ReadingDate = readingDate,
            ReadingValue = request.ReadingValue,
            PreviousReadingValue = previousReading,
            Consumption = request.ReadingValue - previousReading,
            IsBackdated = isBackdated,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.UtilityMeterReadings.Add(reading);
        await dbContext.SaveChangesAsync(cancellationToken);

        var rate = await utilityRateService.ResolveRateAsync(
            request.UtilityCustomerId,
            request.UtilityTypeId,
            billingPeriod,
            cancellationToken);


        var bill = new UtilityBill
        {
            UtilityCustomerId = request.UtilityCustomerId,
            UtilityTypeId = request.UtilityTypeId,
            BillingPeriod = billingPeriod,
            PreviousReading = reading.PreviousReadingValue,
            CurrentReading = reading.ReadingValue,
            Consumption = reading.Consumption,
            Rate = rate,
            Amount = decimal.Round(reading.Consumption * rate, 2, MidpointRounding.AwayFromZero),
            DueDate = UtilityBillService.ResolveDueDate(customer, DateTime.UtcNow, billingPeriod),
            Status = UtilityBillStatus.Unpaid,
            CreatedFromReadingId = reading.Id,
            IsRecalculated = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.UtilityBills.Add(bill);
        await dbContext.SaveChangesAsync(cancellationToken);

        await utilityProrationService.RecalculateResponsibilitiesAsync(bill.Id, cancellationToken);

        dbContext.UtilityAuditLogs.Add(new()
        {
            EntityName = "UtilityMeterReading",
            EntityId = reading.Id.ToString(),
            Action = UtilityAuditActionType.Create,
            OldValuesJson = null,
            NewValuesJson = $"{{\"ReadingDate\":\"{reading.ReadingDate:yyyy-MM-dd}\",\"ReadingValue\":{reading.ReadingValue}}}",
            PerformedBy = "system",
            PerformedAt = DateTime.UtcNow,
            CorrelationId = Guid.NewGuid()
        });

        dbContext.UtilityAuditLogs.Add(new()
        {
            EntityName = "UtilityBill",
            EntityId = bill.Id.ToString(),
            Action = UtilityAuditActionType.Create,
            OldValuesJson = null,
            NewValuesJson = $"{{\"BillingPeriod\":\"{bill.BillingPeriod}\",\"Amount\":{bill.Amount}}}",
            PerformedBy = "system",
            PerformedAt = DateTime.UtcNow,
            CorrelationId = Guid.NewGuid()
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        Guid? recalculationBatchId = null;
        if (isBackdated)
        {
            // Avoid tracking conflict for UtilityMeterReading when recalculation loads and updates
            // the same reading in the same DbContext scope.
            dbContext.Entry(reading).State = EntityState.Detached;

            recalculationBatchId = await utilityRecalculationService.RecalculateFromReadingAsync(reading.Id, cancellationToken);

            bill.IsRecalculated = recalculationBatchId.HasValue;
            bill.RecalculationBatchId = recalculationBatchId;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        await dbContext.Entry(bill).Reference(x => x.UtilityCustomer).LoadAsync(cancellationToken);
        await dbContext.Entry(bill).Reference(x => x.UtilityType).LoadAsync(cancellationToken);

        return new UtilityReadingCreateResultDto
        {
            Reading = ToDto(reading),
            Bill = UtilityBillService.ToDto(bill),
            RecalculationTriggered = isBackdated,
            RecalculationBatchId = recalculationBatchId
        };
    }

    private static UtilityMeterReadingDto ToDto(UtilityMeterReading entity) => new()
    {
        Id = entity.Id,
        UtilityCustomerId = entity.UtilityCustomerId,
        UtilityTypeId = entity.UtilityTypeId,
        ReadingDate = entity.ReadingDate,
        ReadingValue = entity.ReadingValue,
        PreviousReadingValue = entity.PreviousReadingValue,
        Consumption = entity.Consumption,
        IsBackdated = entity.IsBackdated,
        RecalculationBatchId = entity.RecalculationBatchId
    };
}
