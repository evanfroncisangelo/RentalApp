using Microsoft.EntityFrameworkCore;
using RentalApp.Application.DTOs.Utilities;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;
using RentalApp.Data;
using RentalApp.Domain.Entities;
using RentalApp.Domain.Enums;

namespace RentalApp.Application.Services;

public class UtilityBillService(RentalDbContext dbContext) : IUtilityBillService
{
    public async Task EnsureCurrentDueBillsAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        var customers = await dbContext.UtilityCustomers
            .AsNoTracking()
            .Where(x => x.IsActive && x.UtilityCategoryId.HasValue && x.DefaultRate.HasValue && x.DefaultRate.Value > 0)
            .ToListAsync(cancellationToken);

        foreach (var customer in customers)
        {
            var utilityTypeId = customer.UtilityCategoryId ?? throw new InvalidOperationException("Active utility customer must have a utility type.");
            var defaultRate = customer.DefaultRate ?? throw new InvalidOperationException("Active utility customer must have a default rate.");
            var serviceStart = (customer.UtilityStartDate?.Date ?? customer.CreatedAt.Date);
            var periodCursor = new DateTime(serviceStart.Year, serviceStart.Month, 1);
            var currentPeriodStart = new DateTime(today.Year, today.Month, 1);

            while (periodCursor <= currentPeriodStart)
            {
                var periodKey = $"{periodCursor:yyyy-MM}";
                var dueDate = ResolveDueDate(customer, periodCursor, periodKey);

                var exists = await dbContext.UtilityBills.AnyAsync(
                    x => x.UtilityCustomerId == customer.Id
                         && x.UtilityTypeId == utilityTypeId
                         && x.BillingPeriod == periodKey,
                    cancellationToken);

                if (!exists)
                {
                    dbContext.UtilityBills.Add(new UtilityBill
                    {
                        UtilityCustomerId = customer.Id,
                        UtilityTypeId = utilityTypeId,
                        BillingPeriod = periodKey,
                        Amount = decimal.Round(defaultRate, 2, MidpointRounding.AwayFromZero),
                        DueDate = dueDate,
                        Status = UtilityBillStatus.Unpaid,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                periodCursor = periodCursor.AddMonths(1);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UtilityBillDto>> GetAllAsync(int? utilityCustomerId, int? utilityTypeId, string? billingPeriod, CancellationToken cancellationToken = default)
    {
        await EnsureCurrentDueBillsAsync(cancellationToken);

        var query = dbContext.UtilityBills
            .AsNoTracking()
            .Include(x => x.UtilityCustomer)
            .Include(x => x.UtilityType)
            .Include(x => x.Payments)
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
            .ThenByDescending(x => x.Id)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<UtilityBillDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.UtilityBills
            .AsNoTracking()
            .Include(x => x.UtilityCustomer)
            .Include(x => x.UtilityType)
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Utility bill not found.");

        return ToDto(entity);
    }

    public async Task<UtilityBillDto> RecomputeStatusAsync(int billId, CancellationToken cancellationToken = default)
    {
        var bill = await dbContext.UtilityBills
            .Include(x => x.Payments)
            .Include(x => x.UtilityCustomer)
            .Include(x => x.UtilityType)
            .FirstOrDefaultAsync(x => x.Id == billId, cancellationToken)
            ?? throw new AppNotFoundException("Utility bill not found.");

        if (bill.Status == UtilityBillStatus.Cancelled)
        {
            return ToDto(bill);
        }

        var totalPaid = bill.Payments
            .Where(x => !x.IsVoided)
            .Sum(x => x.Amount);

        bill.Status = totalPaid switch
        {
            <= 0 => UtilityBillStatus.Unpaid,
            _ when totalPaid < bill.Amount => UtilityBillStatus.Partial,
            _ => UtilityBillStatus.Paid
        };

        bill.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(bill);
    }

    internal static DateTime? ResolveDueDate(UtilityCustomer customer, DateTime referenceDate, string billingPeriod)
    {
        if (customer.DueDateRuleType == UtilityDueDateRuleType.FixedDayOfMonth)
        {
            if (!DateOnly.TryParse($"{billingPeriod}-01", out var periodStart))
            {
                throw new AppValidationException("Invalid billing period format.");
            }

            var dueDay = customer.DueDayOfMonth
                ?? customer.UtilityStartDate?.Day;

            if (!dueDay.HasValue)
            {
                return null;
            }

            var normalizedDueDay = Math.Clamp(dueDay.Value, 1, 31);
            var resolvedDay = Math.Min(normalizedDueDay, DateTime.DaysInMonth(periodStart.Year, periodStart.Month));
            var dueDate = new DateTime(periodStart.Year, periodStart.Month, resolvedDay);

            return dueDate.Date;
        }

        if (!customer.DueInDays.HasValue)
        {
            return null;
        }

        return referenceDate.Date.AddDays(customer.DueInDays.Value);
    }

    internal static UtilityBillDto ToDto(UtilityBill bill)
    {
        var totalPaid = bill.Payments.Where(x => !x.IsVoided).Sum(x => x.Amount);
        var balance = bill.Amount - totalPaid;

        return new UtilityBillDto
        {
            Id = bill.Id,
            UtilityCustomerId = bill.UtilityCustomerId,
            UtilityCustomerName = bill.UtilityCustomer?.Name ?? string.Empty,
            UtilityTypeId = bill.UtilityTypeId,
            UtilityTypeName = bill.UtilityType?.Name ?? string.Empty,
            BillingPeriod = bill.BillingPeriod,
            Amount = bill.Amount,
            TotalPaid = totalPaid,
            Balance = balance,
            DueDate = bill.DueDate,
            Status = bill.Status,
            LastPaymentId = bill.Payments
                .Where(x => !x.IsVoided)
                .OrderByDescending(x => x.PaymentDate)
                .ThenByDescending(x => x.Id)
                .Select(x => (int?)x.Id)
                .FirstOrDefault()
        };
    }
}
