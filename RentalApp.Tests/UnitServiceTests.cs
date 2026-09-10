using Microsoft.EntityFrameworkCore;
using RentalApp.Application.DTOs.Units;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Services;
using RentalApp.Data;
using RentalApp.Domain.Entities;
using RentalApp.Domain.Enums;

namespace RentalApp.Tests;

public class UnitServiceTests
{
    [Fact]
    public async Task CreateAsync_PersistsTrimmedUnitNameAndMonthlyRent()
    {
        await using var fixture = await UnitServiceFixture.CreateAsync();
        var service = new UnitService(fixture.DbContext);

        var created = await service.CreateAsync(new CreateUnitRequestDto
        {
            PropertyId = fixture.PropertyId,
            UnitNumber = "  Unit Alpha  ",
            MonthlyRent = 5500.75m,
            MaxCapacity = 3,
            Status = UnitStatus.Occupied
        });

        Assert.Equal("Unit Alpha", created.UnitNumber);
        Assert.Equal(5500.75m, created.MonthlyRent);
        Assert.Equal(3, created.MaxCapacity);
        Assert.Equal(UnitStatus.Occupied, created.Status);

        var savedUnit = await fixture.DbContext.Units
            .AsNoTracking()
            .SingleAsync(x => x.Id == created.Id);

        Assert.Equal("Unit Alpha", savedUnit.UnitNumber);
        Assert.Equal(5500.75m, savedUnit.MonthlyRent);
        Assert.Equal(3, savedUnit.MaxCapacity);
        Assert.Equal(UnitStatus.Occupied, savedUnit.Status);

        var savedRoom = await fixture.DbContext.Rooms
            .AsNoTracking()
            .SingleAsync(x => x.UnitId == created.Id);

        Assert.Equal("Default Room", savedRoom.RoomNumber);
    }

    [Fact]
    public async Task UpdateAsync_PersistsEditedUnitNameAndMonthlyRent()
    {
        await using var fixture = await UnitServiceFixture.CreateAsync();
        var service = new UnitService(fixture.DbContext);

        var existing = await service.CreateAsync(new CreateUnitRequestDto
        {
            PropertyId = fixture.PropertyId,
            UnitNumber = "Unit Beta",
            MonthlyRent = 4000m,
            MaxCapacity = 2,
            Status = UnitStatus.Available
        });

        var updated = await service.UpdateAsync(existing.Id, new UpdateUnitRequestDto
        {
            PropertyId = fixture.PropertyId,
            UnitNumber = "  Unit Beta Updated  ",
            MonthlyRent = 6200.50m,
            MaxCapacity = 4,
            Status = UnitStatus.Maintenance
        });

        Assert.Equal("Unit Beta Updated", updated.UnitNumber);
        Assert.Equal(6200.50m, updated.MonthlyRent);
        Assert.Equal(4, updated.MaxCapacity);
        Assert.Equal(UnitStatus.Maintenance, updated.Status);

        var savedUnit = await fixture.DbContext.Units
            .AsNoTracking()
            .SingleAsync(x => x.Id == existing.Id);

        Assert.Equal("Unit Beta Updated", savedUnit.UnitNumber);
        Assert.Equal(6200.50m, savedUnit.MonthlyRent);
        Assert.Equal(4, savedUnit.MaxCapacity);
        Assert.Equal(UnitStatus.Maintenance, savedUnit.Status);
    }

    [Fact]
    public async Task CreateAsync_DuplicateUnitNameWithinProperty_ThrowsValidation()
    {
        await using var fixture = await UnitServiceFixture.CreateAsync();
        var service = new UnitService(fixture.DbContext);

        await service.CreateAsync(new CreateUnitRequestDto
        {
            PropertyId = fixture.PropertyId,
            UnitNumber = "Unit Gamma",
            MonthlyRent = 3000m,
            MaxCapacity = 2,
            Status = UnitStatus.Available
        });

        var ex = await Assert.ThrowsAsync<AppValidationException>(() => service.CreateAsync(new CreateUnitRequestDto
        {
            PropertyId = fixture.PropertyId,
            UnitNumber = " Unit Gamma ",
            MonthlyRent = 3200m,
            MaxCapacity = 2,
            Status = UnitStatus.Available
        }));

        Assert.Contains("same name already exists", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class UnitServiceFixture : IAsyncDisposable
    {
        private UnitServiceFixture(RentalDbContext dbContext, int propertyId)
        {
            DbContext = dbContext;
            PropertyId = propertyId;
        }

        public RentalDbContext DbContext { get; }
        public int PropertyId { get; }

        public static async Task<UnitServiceFixture> CreateAsync()
        {
            var options = new DbContextOptionsBuilder<RentalDbContext>()
                .UseInMemoryDatabase($"unit-service-test-{Guid.NewGuid():N}")
                .Options;

            var dbContext = new RentalDbContext(options);
            await dbContext.Database.EnsureCreatedAsync();

            var property = new Property
            {
                Name = "Test Property",
                Address = "Test Address",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.Properties.Add(property);
            await dbContext.SaveChangesAsync();

            return new UnitServiceFixture(dbContext, property.Id);
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
        }
    }
}
