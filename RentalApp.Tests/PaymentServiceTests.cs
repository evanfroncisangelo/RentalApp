using Microsoft.EntityFrameworkCore;
using RentalApp.Application.DTOs.Payments;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Services;
using RentalApp.Data;
using RentalApp.Domain.Entities;
using RentalApp.Domain.Enums;

namespace RentalApp.Tests;

public class PaymentServiceTests
{
    [Fact]
    public async Task CreateAsync_DepositDuplicateInSameMonth_ThrowsValidation()
    {
        await using var fixture = await RentalDbFixture.CreateAsync();
        var service = new PaymentService(fixture.DbContext);

        await service.CreateAsync(new CreatePaymentRequestDto
        {
            LeaseId = fixture.LeaseId,
            Amount = 1000m,
            PaymentDate = new DateTime(2026, 1, 5),
            PaymentType = PaymentType.Deposit,
            PaymentMethod = PaymentMethod.Cash
        });

        var ex = await Assert.ThrowsAsync<AppValidationException>(() => service.CreateAsync(new CreatePaymentRequestDto
        {
            LeaseId = fixture.LeaseId,
            Amount = 1200m,
            PaymentDate = new DateTime(2026, 1, 20),
            PaymentType = PaymentType.Deposit,
            PaymentMethod = PaymentMethod.Cash
        }));

        Assert.Contains("deposit payment already exists", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_DepositDifferentMonth_Succeeds()
    {
        await using var fixture = await RentalDbFixture.CreateAsync();
        var service = new PaymentService(fixture.DbContext);

        await service.CreateAsync(new CreatePaymentRequestDto
        {
            LeaseId = fixture.LeaseId,
            Amount = 1000m,
            PaymentDate = new DateTime(2026, 1, 5),
            PaymentType = PaymentType.Deposit,
            PaymentMethod = PaymentMethod.Cash
        });

        var created = await service.CreateAsync(new CreatePaymentRequestDto
        {
            LeaseId = fixture.LeaseId,
            Amount = 1200m,
            PaymentDate = new DateTime(2026, 2, 2),
            PaymentType = PaymentType.Deposit,
            PaymentMethod = PaymentMethod.EWallet
        });

        Assert.Equal(new DateTime(2026, 2, 2), created.PaymentDate.Date);
        Assert.Equal(PaymentType.Deposit, created.PaymentType);
    }

    [Fact]
    public async Task CreateAsync_RentDuplicateSameDueDateAndAmount_ThrowsValidation()
    {
        await using var fixture = await RentalDbFixture.CreateAsync();
        var service = new PaymentService(fixture.DbContext);

        var dueDate = new DateTime(2026, 1, 5);
        var paymentDate = new DateTime(2026, 1, 5);

        await service.CreateAsync(new CreatePaymentRequestDto
        {
            LeaseId = fixture.LeaseId,
            Amount = 5000m,
            PaymentDate = paymentDate,
            DueDate = dueDate,
            PaymentType = PaymentType.Rent,
            PaymentMethod = PaymentMethod.Cash
        });

        var ex = await Assert.ThrowsAsync<AppValidationException>(() => service.CreateAsync(new CreatePaymentRequestDto
        {
            LeaseId = fixture.LeaseId,
            Amount = 5000m,
            PaymentDate = paymentDate,
            DueDate = dueDate,
            PaymentType = PaymentType.Rent,
            PaymentMethod = PaymentMethod.Cash
        }));

        Assert.Contains("already being processed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class RentalDbFixture : IAsyncDisposable
    {
        private RentalDbFixture(RentalDbContext dbContext, int leaseId)
        {
            DbContext = dbContext;
            LeaseId = leaseId;
        }

        public RentalDbContext DbContext { get; }
        public int LeaseId { get; }

        public static async Task<RentalDbFixture> CreateAsync()
        {
            var options = new DbContextOptionsBuilder<RentalDbContext>()
                .UseInMemoryDatabase($"rentalapp-test-{Guid.NewGuid():N}")
                .Options;

            var context = new RentalDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var property = new Property { Name = "P1", Address = "Address 1", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            context.Properties.Add(property);
            await context.SaveChangesAsync();

            var unit = new Unit
            {
                PropertyId = property.Id,
                UnitNumber = "U-1",
                MonthlyRent = 5000m,
                MaxCapacity = 3,
                Status = UnitStatus.Occupied,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Units.Add(unit);
            await context.SaveChangesAsync();

            var room = new Room
            {
                UnitId = unit.Id,
                RoomNumber = "R-1",
                MaxCapacity = 3,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Rooms.Add(room);
            await context.SaveChangesAsync();

            var tenant = new Tenant
            {
                FirstName = "Test",
                LastName = "Tenant",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Tenants.Add(tenant);
            await context.SaveChangesAsync();

            var lease = new Lease
            {
                UnitId = unit.Id,
                RoomId = room.Id,
                TenantId = tenant.Id,
                StartDate = new DateTime(2026, 1, 1),
                MonthlyRent = 5000m,
                SecurityDeposit = 5000m,
                DueDayOfMonth = 5,
                Status = LeaseStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Leases.Add(lease);
            await context.SaveChangesAsync();

            return new RentalDbFixture(context, lease.Id);
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
        }
    }
}
