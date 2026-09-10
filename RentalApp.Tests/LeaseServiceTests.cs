using Microsoft.EntityFrameworkCore;
using RentalApp.Application.DTOs.Leases;
using RentalApp.Application.DTOs.Tenants;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Services;
using RentalApp.Data;
using RentalApp.Domain.Entities;
using RentalApp.Domain.Enums;

namespace RentalApp.Tests;

public class LeaseServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenUnitBecomesFull_SetsUnitStatusToOccupied()
    {
        await using var fixture = await LeaseFixture.CreateAsync();
        var service = new LeaseService(fixture.DbContext);

        await service.CreateAsync(new CreateLeaseRequestDto
        {
            UnitId = fixture.UnitId,
            RoomId = fixture.RoomId,
            TenantId = fixture.AvailableTenantId,
            StartDate = new DateTime(2026, 1, 1),
            MonthlyRent = 5000m,
            SecurityDeposit = 5000m,
            DueDayOfMonth = 1,
            Status = LeaseStatus.Active
        });

        var unit = await fixture.DbContext.Units.AsNoTracking().SingleAsync(x => x.Id == fixture.UnitId);
        Assert.Equal(UnitStatus.Occupied, unit.Status);
    }

    [Fact]
    public async Task MoveOutAsync_WhenLastActiveLeaseEnds_SetsUnitStatusToAvailable()
    {
        await using var fixture = await LeaseFixture.CreateAsync();
        var service = new LeaseService(fixture.DbContext);

        var lease = await service.CreateAsync(new CreateLeaseRequestDto
        {
            UnitId = fixture.UnitId,
            RoomId = fixture.RoomId,
            TenantId = fixture.AvailableTenantId,
            StartDate = new DateTime(2026, 1, 1),
            MonthlyRent = 5000m,
            SecurityDeposit = 5000m,
            DueDayOfMonth = 1,
            Status = LeaseStatus.Active
        });

        await service.MoveOutAsync(lease.Id, new MoveOutRequestDto
        {
            MoveOutDate = new DateTime(2026, 2, 1),
            Notes = "Moved out"
        });

        var unit = await fixture.DbContext.Units.AsNoTracking().SingleAsync(x => x.Id == fixture.UnitId);
        Assert.Equal(UnitStatus.Available, unit.Status);
    }

    [Fact]
    public async Task TenantCreateAsync_WhenAssignedUnitHasNoAvailableRoom_RollsBackTenant()
    {
        await using var fixture = await LeaseFixture.CreateAsync();
        var leaseService = new LeaseService(fixture.DbContext);
        var tenantService = new TenantService(fixture.DbContext, leaseService);

        await leaseService.CreateAsync(new CreateLeaseRequestDto
        {
            UnitId = fixture.UnitId,
            RoomId = fixture.RoomId,
            TenantId = fixture.ExistingTenantId,
            StartDate = new DateTime(2026, 1, 1),
            MonthlyRent = 5000m,
            SecurityDeposit = 5000m,
            DueDayOfMonth = 1,
            Status = LeaseStatus.Active
        });

        await Assert.ThrowsAsync<AppValidationException>(() => tenantService.CreateAsync(new CreateTenantRequestDto
        {
            FirstName = "Blocked",
            LastName = "Tenant",
            UnitId = fixture.UnitId,
            UnitNumber = "U-1",
            MoveInDate = new DateTime(2026, 3, 1),
            IsActive = true
        }));

        var exists = await fixture.DbContext.Tenants.AsNoTracking()
            .AnyAsync(x => x.FirstName == "Blocked" && x.LastName == "Tenant");

        Assert.False(exists);
    }

    private sealed class LeaseFixture : IAsyncDisposable
    {
        private LeaseFixture(RentalDbContext dbContext, int unitId, int roomId, int existingTenantId, int availableTenantId)
        {
            DbContext = dbContext;
            UnitId = unitId;
            RoomId = roomId;
            ExistingTenantId = existingTenantId;
            AvailableTenantId = availableTenantId;
        }

        public RentalDbContext DbContext { get; }
        public int UnitId { get; }
        public int RoomId { get; }
        public int ExistingTenantId { get; }
        public int AvailableTenantId { get; }

        public static async Task<LeaseFixture> CreateAsync()
        {
            var options = new DbContextOptionsBuilder<RentalDbContext>()
                .UseInMemoryDatabase($"lease-test-{Guid.NewGuid():N}")
                .Options;

            var context = new RentalDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var property = new Property
            {
                Name = "P1",
                Address = "Address 1",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Properties.Add(property);
            await context.SaveChangesAsync();

            var unit = new Unit
            {
                PropertyId = property.Id,
                UnitNumber = "U-1",
                MonthlyRent = 5000m,
                MaxCapacity = 1,
                Status = UnitStatus.Available,
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
                MaxCapacity = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Rooms.Add(room);
            await context.SaveChangesAsync();

            var tenant1 = new Tenant
            {
                FirstName = "Primary",
                LastName = "Tenant",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var tenant2 = new Tenant
            {
                FirstName = "Secondary",
                LastName = "Tenant",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Tenants.AddRange(tenant1, tenant2);
            await context.SaveChangesAsync();

            return new LeaseFixture(context, unit.Id, room.Id, tenant1.Id, tenant2.Id);
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
        }
    }
}
