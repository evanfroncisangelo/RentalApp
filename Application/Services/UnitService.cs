using Microsoft.EntityFrameworkCore;
using RentalApp.Application.DTOs.Units;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;
using RentalApp.Data;
using RentalApp.Domain.Entities;
using RentalApp.Domain.Enums;

namespace RentalApp.Application.Services;

public class UnitService(RentalDbContext dbContext) : IUnitService
{
    public async Task<IReadOnlyList<UnitDto>> GetAllAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Units
            .AsNoTracking()
            .Include(x => x.Property)
            .Include(x => x.Rooms)
            .Include(x => x.Leases)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x => x.UnitNumber.Contains(keyword) || (x.Property != null && x.Property.Name.Contains(keyword)));
        }

        var units = await query
            .OrderBy(x => x.PropertyId)
            .ThenBy(x => x.UnitNumber)
            .ToListAsync(cancellationToken);

        var activeTenantCountsByUnit = await dbContext.Tenants
            .AsNoTracking()
            .Where(x => x.IsActive && x.UnitId.HasValue)
            .GroupBy(x => x.UnitId!.Value)
            .Select(g => new { UnitId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UnitId, x => x.Count, cancellationToken);

        return units.Select(x => ToDto(x, activeTenantCountsByUnit)).ToList();
    }

    public async Task<UnitDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Units
            .AsNoTracking()
            .Include(x => x.Property)
            .Include(x => x.Rooms)
            .Include(x => x.Leases)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Unit not found.");

        var assignedCount = await dbContext.Tenants
            .AsNoTracking()
            .CountAsync(x => x.IsActive && x.UnitId == entity.Id, cancellationToken);

        var tenantCounts = new Dictionary<int, int>
        {
            [entity.Id] = assignedCount
        };

        return ToDto(entity, tenantCounts);
    }

    public async Task<UnitDto> CreateAsync(CreateUnitRequestDto request, CancellationToken cancellationToken = default)
    {
        var propertyExists = await dbContext.Properties.AnyAsync(x => x.Id == request.PropertyId && x.IsActive, cancellationToken);
        if (!propertyExists)
        {
            throw new AppValidationException("Selected property does not exist or is inactive.");
        }

        var entity = new Unit
        {
            PropertyId = request.PropertyId,
            UnitNumber = request.UnitNumber.Trim(),
            MonthlyRent = request.MonthlyRent,
            Status = request.Status,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.Units.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<UnitDto> UpdateAsync(int id, UpdateUnitRequestDto request, CancellationToken cancellationToken = default)
    {
        var propertyExists = await dbContext.Properties.AnyAsync(x => x.Id == request.PropertyId && x.IsActive, cancellationToken);
        if (!propertyExists)
        {
            throw new AppValidationException("Selected property does not exist or is inactive.");
        }

        var entity = await dbContext.Units
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Unit not found.");

        entity.PropertyId = request.PropertyId;
        entity.UnitNumber = request.UnitNumber.Trim();
        entity.MonthlyRent = request.MonthlyRent;
        entity.Status = request.Status;
        entity.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Units
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Unit not found.");

        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateAsync(int propertyId, int roomCount, int roomMaxCapacity, CancellationToken cancellationToken)
    {
        var propertyExists = await dbContext.Properties.AnyAsync(x => x.Id == propertyId && x.IsActive, cancellationToken);
        if (!propertyExists)
        {
            throw new AppValidationException("Selected property does not exist or is inactive.");
        }

        if (roomCount < 1)
        {
            throw new AppValidationException("Room count is required and must be at least 1.");
        }

        if (roomMaxCapacity < 1 || roomMaxCapacity > 3)
        {
            throw new AppValidationException("Room max capacity must be between 1 and 3 tenants.");
        }
    }

    private static List<Room> BuildRoomList(int count, int capacity)
    {
        var rooms = new List<Room>(count);
        for (var i = 1; i <= count; i++)
        {
            rooms.Add(new Room
            {
                RoomNumber = $"Room {i}",
                MaxCapacity = capacity,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        return rooms;
    }

    private static UnitDto ToDto(Unit entity, IReadOnlyDictionary<int, int> activeTenantCountsByUnit)
    {
        return new UnitDto
        {
            Id = entity.Id,
            PropertyId = entity.PropertyId,
            PropertyName = entity.Property?.Name ?? string.Empty,
            UnitNumber = entity.UnitNumber,
            MonthlyRent = entity.MonthlyRent,
            Status = entity.Status,
            IsActive = entity.IsActive
        };
    }
}
