using Microsoft.EntityFrameworkCore;
using RentalApp.Application.DTOs.Properties;
using RentalApp.Application.DTOs.Tenants;
using RentalApp.Application.DTOs.Units;
using RentalApp.Application.Interfaces;
using RentalApp.Application.Services;
using RentalApp.Data;
using RentalApp.Pages.ApartmentTenants;
using RentalApp.Domain.Enums;

namespace RentalApp.Tests;

public class Scenario1WorkflowSimulationTests
{
    [Fact]
    public async Task Scenario1_AddPropertyUnitTenant_PaymentsGridAndHistoryAlignWithExpectedWorkflow()
    {
        await using var fixture = await ScenarioFixture.CreateAsync();

        var property = await fixture.PropertyService.CreateAsync(new CreatePropertyRequestDto
        {
            Name = "Scenario Property 1",
            Address = "Scenario Address"
        });

        var unit = await fixture.UnitService.CreateAsync(new CreateUnitRequestDto
        {
            PropertyId = property.Id,
            UnitNumber = "Unit A",
            MonthlyRent = 5000m,
            MaxCapacity = 5,
            Status = UnitStatus.Available
        });

        await fixture.TenantService.CreateAsync(new CreateTenantRequestDto
        {
            FirstName = "John",
            LastName = "Doe",
            ContactNumber = "0912341234",
            Address = null,
            MoveInDate = new DateTime(2026, 5, 5),
            UnitId = unit.Id,
            UnitNumber = unit.UnitNumber,
            RoomNumber = string.Empty,
            IsActive = true
        });

        var pageModel = fixture.CreatePaymentsPageModel();
        await pageModel.OnGetAsync(CancellationToken.None);

        var paymentRow = Assert.Single(pageModel.PagedPaymentRows.Items.Where(x => x.RoomId == unit.Id));
        Assert.Equal("Unit A", paymentRow.UnitRoomLabel);
        Assert.Equal("Scenario Property 1", paymentRow.PropertyName);
        Assert.Equal(5000m, paymentRow.MonthlyRent);
        Assert.Equal(new DateTime(2026, 5, 5), paymentRow.DueDate);
        Assert.Equal("1 tenant(s)", paymentRow.TenantCountDisplay);
        Assert.Equal(UnitStatus.Available.ToString(), paymentRow.Status);
        Assert.True(paymentRow.CanAddPayment);
        Assert.Equal(5000m, paymentRow.DefaultAmount);
        Assert.Equal(25000m, paymentRow.UnpaidAmount);

        var history = Assert.Single(pageModel.PaymentHistoryByRoomId.Where(x => x.Key == unit.Id)).Value;
        Assert.Equal(5, history.Count);
        Assert.Collection(history,
            item => AssertDue(item, new DateTime(2026, 5, 5)),
            item => AssertDue(item, new DateTime(2026, 6, 5)),
            item => AssertDue(item, new DateTime(2026, 7, 5)),
            item => AssertDue(item, new DateTime(2026, 8, 5)),
            item => AssertDue(item, new DateTime(2026, 9, 5)));

        var lease = await fixture.DbContext.Leases.AsNoTracking().SingleAsync();
        Assert.Equal(5, lease.DueDayOfMonth);
    }

    private static void AssertDue(IndexModel.PaymentHistoryItemViewModel item, DateTime expectedDueDate)
    {
        Assert.Equal(expectedDueDate, item.DueDate);
        Assert.Equal(5000m, item.AmountDue);
        Assert.Equal(0m, item.PaidAmount);
        Assert.Equal(5000m, item.BalanceAmount);
        Assert.Null(item.PaymentDate);
        Assert.Null(item.PaymentMethod);
        Assert.Equal("Unpaid", item.Status);
        Assert.False(item.CanEdit);
    }

    private sealed class ScenarioFixture : IAsyncDisposable
    {
        private ScenarioFixture(RentalDbContext dbContext)
        {
            DbContext = dbContext;
            LeaseService = new LeaseService(dbContext);
            PaymentService = new PaymentService(dbContext);
            PropertyService = new PropertyService(dbContext);
            UnitService = new UnitService(dbContext);
            TenantService = new TenantService(dbContext, LeaseService);
        }

        public RentalDbContext DbContext { get; }
        public ILeaseService LeaseService { get; }
        public IPaymentService PaymentService { get; }
        public IPropertyService PropertyService { get; }
        public IUnitService UnitService { get; }
        public ITenantService TenantService { get; }

        public static async Task<ScenarioFixture> CreateAsync()
        {
            var options = new DbContextOptionsBuilder<RentalDbContext>()
                .UseInMemoryDatabase($"scenario1-simulation-{Guid.NewGuid():N}")
                .Options;

            var dbContext = new RentalDbContext(options);
            await dbContext.Database.EnsureCreatedAsync();
            return new ScenarioFixture(dbContext);
        }

        public IndexModel CreatePaymentsPageModel()
        {
            return new IndexModel(
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
