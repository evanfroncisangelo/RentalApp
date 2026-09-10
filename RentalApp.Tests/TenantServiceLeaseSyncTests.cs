using Microsoft.EntityFrameworkCore;
using RentalApp.Application.DTOs.Payments;
using RentalApp.Application.DTOs.Properties;
using RentalApp.Application.DTOs.Tenants;
using RentalApp.Application.DTOs.Units;
using RentalApp.Application.Services;
using RentalApp.Data;
using RentalApp.Domain.Enums;

namespace RentalApp.Tests;

public class TenantServiceLeaseSyncTests
{
    [Fact]
    public async Task UpdateAsync_MoveInDateChanged_SynchronizesActiveLeaseSchedule()
    {
        await using var fixture = await TenantLeaseSyncFixture.CreateAsync();

        var tenant = await fixture.TenantService.CreateAsync(new CreateTenantRequestDto
        {
            FirstName = "John",
            LastName = "Doe",
            UnitId = fixture.PrimaryUnit.Id,
            UnitNumber = fixture.PrimaryUnit.UnitNumber,
            MoveInDate = new DateTime(2026, 5, 5),
            IsActive = true
        });

        await fixture.TenantService.UpdateAsync(tenant.Id, new UpdateTenantRequestDto
        {
            FirstName = "John",
            LastName = "Doe",
            UnitId = fixture.PrimaryUnit.Id,
            UnitNumber = fixture.PrimaryUnit.UnitNumber,
            MoveInDate = new DateTime(2026, 2, 10),
            IsActive = true
        });

        var lease = await fixture.DbContext.Leases
            .AsNoTracking()
            .SingleAsync(x => x.TenantId == tenant.Id && x.Status == LeaseStatus.Active);

        Assert.Equal(new DateTime(2026, 2, 10), lease.StartDate);
        Assert.Equal(10, lease.DueDayOfMonth);
        Assert.Equal(fixture.PrimaryUnit.Id, lease.UnitId);
    }

    [Fact]
    public async Task UpdateAsync_NoActiveLease_CreatesLeaseFromCurrentMoveInDate()
    {
        await using var fixture = await TenantLeaseSyncFixture.CreateAsync();

        var tenant = await fixture.TenantService.CreateAsync(new CreateTenantRequestDto
        {
            FirstName = "Jane",
            LastName = "Doe",
            UnitId = fixture.PrimaryUnit.Id,
            UnitNumber = fixture.PrimaryUnit.UnitNumber,
            MoveInDate = new DateTime(2026, 5, 5),
            IsActive = true
        });

        var activeLease = await fixture.DbContext.Leases.SingleAsync(x => x.TenantId == tenant.Id && x.Status == LeaseStatus.Active);
        activeLease.Status = LeaseStatus.Ended;
        activeLease.EndDate = new DateTime(2026, 5, 31);
        await fixture.DbContext.SaveChangesAsync();

        await fixture.TenantService.UpdateAsync(tenant.Id, new UpdateTenantRequestDto
        {
            FirstName = "Jane",
            LastName = "Doe",
            UnitId = fixture.PrimaryUnit.Id,
            UnitNumber = fixture.PrimaryUnit.UnitNumber,
            MoveInDate = new DateTime(2026, 6, 10),
            IsActive = true
        });

        var leases = await fixture.DbContext.Leases
            .AsNoTracking()
            .Where(x => x.TenantId == tenant.Id)
            .OrderBy(x => x.StartDate)
            .ToListAsync();

        var latestLease = leases.Last();
        Assert.Equal(2, leases.Count);
        Assert.Equal(LeaseStatus.Active, latestLease.Status);
        Assert.Equal(new DateTime(2026, 6, 10), latestLease.StartDate);
        Assert.Equal(10, latestLease.DueDayOfMonth);
    }

    [Fact]
    public async Task PaymentsPage_UsesUpdatedMoveInDateForDueDateAndHistory()
    {
        await using var fixture = await TenantLeaseSyncFixture.CreateAsync();

        var tenant = await fixture.TenantService.CreateAsync(new CreateTenantRequestDto
        {
            FirstName = "Mark",
            LastName = "Smith",
            UnitId = fixture.PrimaryUnit.Id,
            UnitNumber = fixture.PrimaryUnit.UnitNumber,
            MoveInDate = new DateTime(2026, 5, 5),
            IsActive = true
        });

        await fixture.TenantService.UpdateAsync(tenant.Id, new UpdateTenantRequestDto
        {
            FirstName = "Mark",
            LastName = "Smith",
            UnitId = fixture.PrimaryUnit.Id,
            UnitNumber = fixture.PrimaryUnit.UnitNumber,
            MoveInDate = new DateTime(2026, 2, 10),
            IsActive = true
        });

        var pageModel = fixture.CreatePaymentsPageModel();
        await pageModel.OnGetAsync(CancellationToken.None);

        var paymentRow = Assert.Single(pageModel.PagedPaymentRows.Items.Where(x => x.RoomId == fixture.PrimaryUnit.Id));
        Assert.Equal(new DateTime(2026, 2, 10), paymentRow.DueDate);

        var history = Assert.Single(pageModel.PaymentHistoryByRoomId.Where(x => x.Key == fixture.PrimaryUnit.Id)).Value;
        Assert.NotEmpty(history);
        Assert.Equal(new DateTime(2026, 2, 10), history.First().DueDate);
        Assert.All(history, item => Assert.Equal(fixture.PrimaryUnit.MonthlyRent, item.AmountDue));
    }

    [Fact]
    public async Task CreatePaymentAsync_WithSelectedDueDate_PersistsThatDueMonthForDashboard()
    {
        await using var fixture = await TenantLeaseSyncFixture.CreateAsync();

        var tenant = await fixture.TenantService.CreateAsync(new CreateTenantRequestDto
        {
            FirstName = "Paul",
            LastName = "Jones",
            UnitId = fixture.PrimaryUnit.Id,
            UnitNumber = fixture.PrimaryUnit.UnitNumber,
            MoveInDate = new DateTime(2026, 2, 10),
            IsActive = true
        });

        var activeLease = await fixture.DbContext.Leases
            .AsNoTracking()
            .SingleAsync(x => x.TenantId == tenant.Id && x.Status == LeaseStatus.Active);

        var createdPayment = await fixture.PaymentService.CreateAsync(new CreatePaymentRequestDto
        {
            LeaseId = activeLease.Id,
            Amount = fixture.PrimaryUnit.MonthlyRent,
            PaymentDate = new DateTime(2026, 9, 10),
            DueDate = new DateTime(2026, 4, 10),
            PaymentType = PaymentType.Rent,
            PaymentMethod = PaymentMethod.Cash
        });

        Assert.Equal(new DateTime(2026, 4, 10), createdPayment.DueDate);

        var savedPayment = await fixture.DbContext.Payments
            .AsNoTracking()
            .SingleAsync(x => x.Id == createdPayment.Id);

        Assert.Equal(new DateTime(2026, 4, 10), savedPayment.DueDate);
    }

    private sealed class TenantLeaseSyncFixture : IAsyncDisposable
    {
        private TenantLeaseSyncFixture(RentalDbContext dbContext, PropertyService propertyService, UnitService unitService, LeaseService leaseService, PaymentService paymentService, TenantService tenantService, UnitDto primaryUnit)
        {
            DbContext = dbContext;
            PropertyService = propertyService;
            UnitService = unitService;
            LeaseService = leaseService;
            PaymentService = paymentService;
            TenantService = tenantService;
            PrimaryUnit = primaryUnit;
        }

        public RentalDbContext DbContext { get; }
        public PropertyService PropertyService { get; }
        public UnitService UnitService { get; }
        public LeaseService LeaseService { get; }
        public PaymentService PaymentService { get; }
        public TenantService TenantService { get; }
        public UnitDto PrimaryUnit { get; }

        public static async Task<TenantLeaseSyncFixture> CreateAsync()
        {
            var options = new DbContextOptionsBuilder<RentalDbContext>()
                .UseInMemoryDatabase($"tenant-lease-sync-{Guid.NewGuid():N}")
                .Options;

            var dbContext = new RentalDbContext(options);
            await dbContext.Database.EnsureCreatedAsync();

            var propertyService = new PropertyService(dbContext);
            var unitService = new UnitService(dbContext);
            var leaseService = new LeaseService(dbContext);
            var paymentService = new PaymentService(dbContext);
            var tenantService = new TenantService(dbContext, leaseService);

            var property = await propertyService.CreateAsync(new CreatePropertyRequestDto
            {
                Name = "Sync Property",
                Address = "Sync Address"
            });

            var unit = await unitService.CreateAsync(new CreateUnitRequestDto
            {
                PropertyId = property.Id,
                UnitNumber = "Unit Sync",
                MonthlyRent = 5000m,
                MaxCapacity = 5,
                Status = UnitStatus.Available
            });

            return new TenantLeaseSyncFixture(dbContext, propertyService, unitService, leaseService, paymentService, tenantService, unit);
        }

        public Pages.ApartmentTenants.IndexModel CreatePaymentsPageModel()
        {
            return new Pages.ApartmentTenants.IndexModel(
                LeaseService,
                PaymentService,
                UnitService,
                PropertyService,
                TenantService);
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
        }
    }
}
