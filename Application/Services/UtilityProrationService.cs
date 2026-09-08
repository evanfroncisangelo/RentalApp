using Microsoft.EntityFrameworkCore;
using RentalApp.Application.Interfaces;
using RentalApp.Data;
using RentalApp.Domain.Enums;

namespace RentalApp.Application.Services;

public class UtilityProrationService(RentalDbContext dbContext) : IUtilityProrationService
{
    public async Task RecalculateResponsibilitiesAsync(int utilityBillId, CancellationToken cancellationToken = default)
    {
        var bill = await dbContext.UtilityBills
            .Include(x => x.UtilityCustomer)
            .Include(x => x.Responsibilities)
            .FirstOrDefaultAsync(x => x.Id == utilityBillId, cancellationToken);

        if (bill is null)
        {
            return;
        }

        var period = ParsePeriod(bill.BillingPeriod);
        var periodStart = period;
        var periodEnd = new DateTime(period.Year, period.Month, DateTime.DaysInMonth(period.Year, period.Month));

        dbContext.UtilityBillResponsibilities.RemoveRange(bill.Responsibilities);

        var customer = bill.UtilityCustomer;
        if (customer is null)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var shares = new List<(int? TenantId, DateTime FromDate, DateTime ToDate, int Days)>();

        if (customer.RoomId.HasValue)
        {
            var leases = await dbContext.Leases
                .AsNoTracking()
                .Where(x => x.RoomId == customer.RoomId.Value
                            && (x.Status == LeaseStatus.Active || x.Status == LeaseStatus.Ended)
                            && x.StartDate.Date <= periodEnd.Date
                            && (!x.EndDate.HasValue || x.EndDate.Value.Date >= periodStart.Date))
                .OrderBy(x => x.StartDate)
                .ToListAsync(cancellationToken);

            foreach (var lease in leases)
            {
                var overlapStart = lease.StartDate.Date < periodStart.Date ? periodStart.Date : lease.StartDate.Date;
                var leaseEnd = lease.EndDate?.Date ?? periodEnd.Date;
                var overlapEnd = leaseEnd > periodEnd.Date ? periodEnd.Date : leaseEnd;

                if (overlapEnd < overlapStart)
                {
                    continue;
                }

                var days = (overlapEnd - overlapStart).Days + 1;
                shares.Add((lease.TenantId, overlapStart, overlapEnd, days));
            }
        }

        if (shares.Count == 0)
        {
            if (customer.TenantId.HasValue)
            {
                shares.Add((customer.TenantId.Value, periodStart.Date, periodEnd.Date, (periodEnd - periodStart).Days + 1));
            }
            else
            {
                shares.Add((null, periodStart.Date, periodEnd.Date, (periodEnd - periodStart).Days + 1));
            }
        }

        var grouped = shares
            .GroupBy(x => x.TenantId)
            .Select(g => new
            {
                TenantId = g.Key,
                FromDate = g.Min(x => x.FromDate),
                ToDate = g.Max(x => x.ToDate),
                Days = g.Sum(x => x.Days)
            })
            .ToList();

        var totalDays = grouped.Sum(x => x.Days);
        if (totalDays <= 0)
        {
            totalDays = 1;
        }

        var rows = new List<Domain.Entities.UtilityBillResponsibility>();
        decimal allocated = 0m;

        for (var i = 0; i < grouped.Count; i++)
        {
            var item = grouped[i];
            var percentage = decimal.Divide(item.Days, totalDays);
            var amount = i == grouped.Count - 1
                ? bill.Amount - allocated
                : decimal.Round(bill.Amount * percentage, 2, MidpointRounding.AwayFromZero);

            allocated += amount;

            rows.Add(new Domain.Entities.UtilityBillResponsibility
            {
                UtilityBillId = bill.Id,
                TenantId = item.TenantId,
                FromDate = item.FromDate,
                ToDate = item.ToDate,
                DaysCovered = item.Days,
                PercentageShare = percentage,
                AmountShare = amount,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        dbContext.UtilityBillResponsibilities.AddRange(rows);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static DateTime ParsePeriod(string billingPeriod)
    {
        if (!DateOnly.TryParse($"{billingPeriod}-01", out var periodDate))
        {
            throw new ArgumentException("Invalid billing period format.", nameof(billingPeriod));
        }

        return new DateTime(periodDate.Year, periodDate.Month, 1);
    }
}
