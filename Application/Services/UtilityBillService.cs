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
    public async Task<IReadOnlyList<UtilityBillDto>> GetAllAsync(int? utilityCustomerId, int? utilityTypeId, string? billingPeriod, CancellationToken cancellationToken = default)
    {
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

    internal static DateTime ResolveDueDate(UtilityCustomer customer, DateTime referenceDate, string billingPeriod)
    {
        if (customer.DueDateRuleType == UtilityDueDateRuleType.FixedDayOfMonth)
        {
            if (!DateOnly.TryParse($"{billingPeriod}-01", out var periodStart))
            {
                throw new AppValidationException("Invalid billing period format.");
            }

            var dueDay = customer.DueDayOfMonth ?? 1;
            var dueDate = new DateTime(periodStart.Year, periodStart.Month, dueDay);

            return referenceDate.Date <= dueDate.Date
                ? dueDate.Date
                : dueDate.AddMonths(1).Date;
        }

        var dueInDays = customer.DueInDays ?? 0;
        return referenceDate.Date.AddDays(dueInDays);
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
            PreviousReading = bill.PreviousReading,
            CurrentReading = bill.CurrentReading,
            Consumption = bill.Consumption,
            Rate = bill.Rate,
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
                .FirstOrDefault(),
            CreatedFromReadingId = bill.CreatedFromReadingId,
            IsRecalculated = bill.IsRecalculated
        };
    }
}
