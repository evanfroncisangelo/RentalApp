using Microsoft.EntityFrameworkCore;
using RentalApp.Application.DTOs.Payments;
using RentalApp.Application.Services;
using RentalApp.Data;
using RentalApp.Domain.Entities;
using RentalApp.Domain.Enums;

namespace RentalApp.Tests;

public class DashboardServiceTests
{
    [Fact]
    public async Task GetSummaryAsync_AttributesRentToDueDateMonthInsteadOfPaymentDateMonth()
    {
        await using var fixture = await DashboardFixture.CreateAsync();

        await fixture.PaymentService.CreateAsync(new CreatePaymentRequestDto
        {
            LeaseId = fixture.LeaseId,
            Amount = 5000m,
            PaymentDate = new DateTime(2026, 6, 2),
            DueDate = new DateTime(2026, 5, 5),
            PaymentType = PaymentType.Rent,
            PaymentMethod = PaymentMethod.Cash
        });

        var summary = await fixture.DashboardService.GetSummaryAsync(2026, CancellationToken.None);
        var row = Assert.Single(summary.ApartmentTenantGrid);

        Assert.Equal(5000m, summary.CollectedRent);
        Assert.Equal(5000m, row.RentByMonth.GetValueOrDefault(5));
        Assert.Equal(0m, row.RentByMonth.GetValueOrDefault(6));
        Assert.Equal(5000m, row.TotalRent);
    }

    [Fact]
    public async Task GetSummaryAsync_AttributesDepositToPaymentDateMonthInsteadOfDueDateMonth()
    {
        await using var fixture = await DashboardFixture.CreateAsync();

        await fixture.PaymentService.CreateAsync(new CreatePaymentRequestDto
        {
            LeaseId = fixture.LeaseId,
            Amount = 5000m,
            PaymentDate = new DateTime(2026, 6, 2),
            DueDate = new DateTime(2026, 5, 5),
            PaymentType = PaymentType.Deposit,
            PaymentMethod = PaymentMethod.Cash
        });

        var summary = await fixture.DashboardService.GetSummaryAsync(2026, CancellationToken.None);
        var row = Assert.Single(summary.ApartmentTenantGrid);

        Assert.Equal(0m, row.DepositByMonth.GetValueOrDefault(5));
        Assert.Equal(5000m, row.DepositByMonth.GetValueOrDefault(6));
        Assert.Equal(5000m, row.TotalDeposit);
    }

    [Fact]
    public async Task CreateAsync_DepositWithSelectedDueDate_StillDisplaysOnDashboardByPaymentMonth()
    {
        await using var fixture = await DashboardFixture.CreateAsync();

        var created = await fixture.PaymentService.CreateAsync(new CreatePaymentRequestDto
        {
            LeaseId = fixture.LeaseId,
            Amount = 5000m,
            PaymentDate = new DateTime(2026, 9, 2),
            DueDate = new DateTime(2026, 8, 15),
            PaymentType = PaymentType.Deposit,
            PaymentMethod = PaymentMethod.Cash
        });

        Assert.Equal(new DateTime(2026, 8, 15), created.DueDate);

        var summary = await fixture.DashboardService.GetSummaryAsync(2026, CancellationToken.None);
        var row = Assert.Single(summary.ApartmentTenantGrid);

        Assert.Equal(0m, row.DepositByMonth.GetValueOrDefault(8));
        Assert.Equal(5000m, row.DepositByMonth.GetValueOrDefault(9));
    }

    [Fact]
    public async Task GetSummaryAsync_IncludesDepositPaidInYearEvenWhenDueDateIsOutsideYear()
    {
        await using var fixture = await DashboardFixture.CreateAsync();

        await fixture.PaymentService.CreateAsync(new CreatePaymentRequestDto
        {
            LeaseId = fixture.LeaseId,
            Amount = 5000m,
            PaymentDate = new DateTime(2026, 1, 3),
            DueDate = new DateTime(2025, 12, 15),
            PaymentType = PaymentType.Deposit,
            PaymentMethod = PaymentMethod.Cash
        });

        var summary = await fixture.DashboardService.GetSummaryAsync(2026, CancellationToken.None);
        var row = Assert.Single(summary.ApartmentTenantGrid);

        Assert.Equal(5000m, row.DepositByMonth.GetValueOrDefault(1));
        Assert.Equal(5000m, row.TotalDeposit);
    }

    private sealed class DashboardFixture : IAsyncDisposable
    {
        private DashboardFixture(RentalDbContext dbContext, DashboardService dashboardService, PaymentService paymentService, int leaseId)
        {
            DbContext = dbContext;
            DashboardService = dashboardService;
            PaymentService = paymentService;
            LeaseId = leaseId;
        }

        public RentalDbContext DbContext { get; }
        public DashboardService DashboardService { get; }
        public PaymentService PaymentService { get; }
        public int LeaseId { get; }

        public static async Task<DashboardFixture> CreateAsync()
        {
            var options = new DbContextOptionsBuilder<RentalDbContext>()
                .UseInMemoryDatabase($"dashboard-service-test-{Guid.NewGuid():N}")
                .Options;

            var dbContext = new RentalDbContext(options);
            await dbContext.Database.EnsureCreatedAsync();

            var property = new Property
            {
                Name = "Dashboard Property",
                Address = "Dashboard Address",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            dbContext.Properties.Add(property);
            await dbContext.SaveChangesAsync();

            var unit = new Unit
            {
                PropertyId = property.Id,
                UnitNumber = "Dashboard Unit",
                MonthlyRent = 5000m,
                MaxCapacity = 4,
                Status = UnitStatus.Occupied,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            dbContext.Units.Add(unit);
            await dbContext.SaveChangesAsync();

            var tenant = new Tenant
            {
                FirstName = "Dash",
                LastName = "Board",
                ContactNumber = "09123456789",
                UnitId = unit.Id,
                UnitNumber = unit.UnitNumber,
                MoveInDate = new DateTime(2026, 5, 5),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            dbContext.Tenants.Add(tenant);
            await dbContext.SaveChangesAsync();

            var lease = new Lease
            {
                UnitId = unit.Id,
                RoomId = 1,
                TenantId = tenant.Id,
                StartDate = new DateTime(2026, 5, 5),
                MonthlyRent = 5000m,
                SecurityDeposit = 5000m,
                DueDayOfMonth = 5,
                Status = LeaseStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            dbContext.Leases.Add(lease);
            await dbContext.SaveChangesAsync();

            return new DashboardFixture(dbContext, new DashboardService(dbContext), new PaymentService(dbContext), lease.Id);
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
        }
    }
}
