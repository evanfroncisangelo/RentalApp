using Microsoft.EntityFrameworkCore;
using RentalApp.Application.DTOs.Finance;
using RentalApp.Application.Interfaces;
using RentalApp.Data;
using RentalApp.Domain.Enums;

namespace RentalApp.Application.Services;

public class FinancialReportService(RentalDbContext dbContext) : IFinancialReportService
{
    public async Task<MonthlyFinancialSummaryDto> GetMonthlySummaryAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var from = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddMonths(1);

        var expectedRent = await dbContext.Leases
            .AsNoTracking()
            .Where(x =>
                (x.Status == LeaseStatus.Active || x.Status == LeaseStatus.Ended) &&
                x.StartDate < to &&
                (x.EndDate == null || x.EndDate >= from))
            .SumAsync(x => (decimal?)x.MonthlyRent, cancellationToken) ?? 0m;

        var collectedRent = await dbContext.Payments
            .AsNoTracking()
            .Where(x => x.PaymentDate >= from && x.PaymentDate < to)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        var totalExpenses = await dbContext.Expenses
            .AsNoTracking()
            .Where(x => x.ExpenseDate >= from && x.ExpenseDate < to)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        var utilityIncome = await dbContext.UtilityBillPayments
            .AsNoTracking()
            .Where(x => !x.IsVoided && x.PaymentDate >= from && x.PaymentDate < to)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        var utilityExpenses = await dbContext.Expenses
            .AsNoTracking()
            .Include(x => x.Category)
            .Where(x => x.ExpenseDate >= from && x.ExpenseDate < to && x.Category != null && x.Category.Name.Contains("Utility"))
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        var periodKey = $"{year:D4}-{month:D2}";
        var outstandingUtilityBills = await dbContext.UtilityBills
            .AsNoTracking()
            .Include(x => x.Payments)
            .Where(x => x.BillingPeriod == periodKey)
            .Select(x => x.Amount - x.Payments.Where(p => !p.IsVoided).Sum(p => p.Amount))
            .SumAsync(cancellationToken);

        var totalUtilityConsumption = await dbContext.UtilityBills
            .AsNoTracking()
            .Where(x => x.BillingPeriod == periodKey)
            .SumAsync(x => (decimal?)x.Consumption, cancellationToken) ?? 0m;

        var outstanding = expectedRent > collectedRent ? expectedRent - collectedRent : 0m;

        return new MonthlyFinancialSummaryDto
        {
            Year = year,
            Month = month,
            ExpectedRent = expectedRent,
            CollectedRent = collectedRent,
            OutstandingRent = outstanding,
            TotalExpenses = totalExpenses,
            NetProfit = (collectedRent + utilityIncome) - (totalExpenses + utilityExpenses),
            UtilityIncome = utilityIncome,
            UtilityExpenses = utilityExpenses,
            OutstandingUtilityBills = outstandingUtilityBills,
            UtilityNet = utilityIncome - utilityExpenses,
            TotalUtilityConsumption = totalUtilityConsumption
        };
    }

    public async Task<IReadOnlyList<MonthlyFinancialHistoryItemDto>> GetMonthlyHistoryAsync(int monthsBack, CancellationToken cancellationToken = default)
    {
        if (monthsBack < 1)
        {
            monthsBack = 1;
        }

        if (monthsBack > 36)
        {
            monthsBack = 36;
        }

        var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var results = new List<MonthlyFinancialHistoryItemDto>(monthsBack);

        for (var i = monthsBack - 1; i >= 0; i--)
        {
            var target = currentMonth.AddMonths(-i);
            var summary = await GetMonthlySummaryAsync(target.Year, target.Month, cancellationToken);
            results.Add(new MonthlyFinancialHistoryItemDto
            {
                Year = summary.Year,
                Month = summary.Month,
                ExpectedRent = summary.ExpectedRent,
                CollectedRent = summary.CollectedRent,
                OutstandingRent = summary.OutstandingRent,
                TotalExpenses = summary.TotalExpenses,
                NetProfit = summary.NetProfit,
                UtilityIncome = summary.UtilityIncome,
                UtilityExpenses = summary.UtilityExpenses,
                OutstandingUtilityBills = summary.OutstandingUtilityBills,
                UtilityNet = summary.UtilityNet,
                TotalUtilityConsumption = summary.TotalUtilityConsumption
            });
        }

        return results;
    }
}
