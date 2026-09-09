using Microsoft.EntityFrameworkCore;
using RentalApp.Application.DTOs.Dashboard;
using RentalApp.Application.Interfaces;
using RentalApp.Data;
using RentalApp.Domain.Enums;

namespace RentalApp.Application.Services;

public class DashboardService(RentalDbContext dbContext) : IDashboardService
{
    public async Task<DashboardSummaryDto> GetSummaryAsync(int year, CancellationToken cancellationToken = default)
    {
        if (year < 2026)
        {
            year = 2026;
        }
        var month = DateTime.UtcNow.Month;
        var from = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddYears(1);

        var totalUnits = await dbContext.Units.CountAsync(cancellationToken);
        var occupiedUnits = await dbContext.Units.CountAsync(x => x.Status == UnitStatus.Occupied, cancellationToken);
        var availableUnits = await dbContext.Units.CountAsync(x => x.Status == UnitStatus.Available, cancellationToken);
        var maintenanceUnits = await dbContext.Units.CountAsync(x => x.Status == UnitStatus.Maintenance, cancellationToken);

        var currentTenants = await dbContext.Leases
            .AsNoTracking()
            .Where(x => x.Status == LeaseStatus.Active)
            .Select(x => x.TenantId)
            .Distinct()
            .CountAsync(cancellationToken);

        var expectedRent = await dbContext.Leases
            .AsNoTracking()
            .Where(x =>
                (x.Status == LeaseStatus.Active || x.Status == LeaseStatus.Ended) &&
                x.StartDate < to &&
                (x.EndDate == null || x.EndDate >= from))
            .SumAsync(x => (decimal?)x.MonthlyRent, cancellationToken) ?? 0m;

        var collectedRent = await dbContext.Payments
            .AsNoTracking()
            .Where(x => x.PaymentDate >= from && x.PaymentDate < to && x.PaymentType == PaymentType.Rent)
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

        var outstandingUtilityBills = await dbContext.UtilityBills
            .AsNoTracking()
            .Include(x => x.Payments)
            .Where(x => x.BillingPeriod.StartsWith($"{year:D4}-"))
            .Select(x => x.Amount - x.Payments.Where(p => !p.IsVoided).Sum(p => p.Amount))
            .SumAsync(cancellationToken);

        var totalUtilityConsumption = 0m;

        var apartmentTenantGrid = await BuildApartmentTenantGridAsync(year, cancellationToken);
        var expenseGrid = await BuildExpenseGridAsync(year, cancellationToken);
        var utilityCustomerGrid = await BuildUtilityCustomerGridAsync(year, cancellationToken);

        var outstandingRent = expectedRent > collectedRent ? expectedRent - collectedRent : 0m;

        return new DashboardSummaryDto
        {
            Year = year,
            Month = month,
            TotalUnits = totalUnits,
            OccupiedUnits = occupiedUnits,
            AvailableUnits = availableUnits,
            UnitsUnderMaintenance = maintenanceUnits,
            CurrentTenants = currentTenants,
            ExpectedRent = expectedRent,
            CollectedRent = collectedRent,
            OutstandingRent = outstandingRent,
            TotalExpenses = totalExpenses,
            NetProfit = (collectedRent + utilityIncome) - (totalExpenses + utilityExpenses),
            UtilityIncome = utilityIncome,
            UtilityExpenses = utilityExpenses,
            OutstandingUtilityBills = outstandingUtilityBills,
            UtilityNet = utilityIncome - utilityExpenses,
            TotalUtilityConsumption = totalUtilityConsumption,
            ApartmentTenantGrid = apartmentTenantGrid,
            ExpenseGrid = expenseGrid,
            UtilityCustomerGrid = utilityCustomerGrid
        };
    }

    private async Task<IReadOnlyList<ApartmentTenantDashboardRowDto>> BuildApartmentTenantGridAsync(int year, CancellationToken cancellationToken)
    {
        var from = new DateTime(year, 1, 1);
        var to = from.AddYears(1);

        var payments = await dbContext.Payments
            .AsNoTracking()
            .Include(x => x.Unit)
            .Where(x => x.PaymentDate >= from && x.PaymentDate < to)
            .ToListAsync(cancellationToken);

        var rows = new Dictionary<string, ApartmentTenantDashboardRowDto>(StringComparer.OrdinalIgnoreCase);

        foreach (var payment in payments)
        {
            var unit = payment.Unit;
            if (unit is null)
            {
                continue;
            }

            var label = unit.UnitNumber;
            if (!rows.TryGetValue(label, out var row))
            {
                row = new ApartmentTenantDashboardRowDto { RoomUnitLabel = label };
                rows[label] = row;
            }

            var month = payment.PaymentDate.Month;
            if (payment.PaymentType == PaymentType.Deposit)
            {
                row.DepositByMonth[month] = row.DepositByMonth.GetValueOrDefault(month) + payment.Amount;
                row.TotalDeposit += payment.Amount;
            }
            else
            {
                row.RentByMonth[month] = row.RentByMonth.GetValueOrDefault(month) + payment.Amount;
                row.TotalRent += payment.Amount;
            }
        }

        return rows.Values
            .OrderBy(x => x.RoomUnitLabel)
            .ToList();
    }

    private async Task<IReadOnlyList<ExpenseDashboardRowDto>> BuildExpenseGridAsync(int year, CancellationToken cancellationToken)
    {
        var from = new DateTime(year, 1, 1);
        var to = from.AddYears(1);

        return await dbContext.Expenses
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Property)
            .Include(x => x.Unit)
            .Where(x => x.ExpenseDate >= from && x.ExpenseDate < to)
            .OrderByDescending(x => x.ExpenseDate)
            .ThenByDescending(x => x.Id)
            .Select(x => new ExpenseDashboardRowDto
            {
                ExpenseCategory = x.Category != null ? x.Category.Name : "-",
                PropertyUnit = x.Unit != null
                    ? $"{x.Property!.Name} | {x.Unit.UnitNumber}"
                    : x.Property!.Name,
                Amount = x.Amount,
                Date = x.ExpenseDate
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<UtilityCustomerDashboardRowDto>> BuildUtilityCustomerGridAsync(int year, CancellationToken cancellationToken)
    {
        var from = new DateTime(year, 1, 1);
        var to = from.AddYears(1);

        var payments = await dbContext.UtilityBillPayments
            .AsNoTracking()
            .Include(x => x.UtilityBill)
            .ThenInclude(x => x!.UtilityCustomer)
            .Where(x => !x.IsVoided && x.PaymentDate >= from && x.PaymentDate < to)
            .ToListAsync(cancellationToken);

        var rows = new Dictionary<string, UtilityCustomerDashboardRowDto>(StringComparer.OrdinalIgnoreCase);

        foreach (var payment in payments)
        {
            var bill = payment.UtilityBill;
            if (bill is null)
            {
                continue;
            }

            var customerName = bill.UtilityCustomer?.Name ?? $"Customer #{bill.UtilityCustomerId}";
            if (!rows.TryGetValue(customerName, out var row))
            {
                row = new UtilityCustomerDashboardRowDto
                {
                    CustomerLabel = customerName
                };
                rows[customerName] = row;
            }

            var month = payment.PaymentDate.Month;
            row.AmountByMonth[month] = row.AmountByMonth.GetValueOrDefault(month) + payment.Amount;
            row.TotalAmount += payment.Amount;
        }

        return rows.Values
            .OrderBy(x => x.CustomerLabel)
            .ToList();
    }
}
