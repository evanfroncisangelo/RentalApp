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

        return units.Select(ToDto).ToList();
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

        return ToDto(entity);
    }

    public async Task<UnitDto> CreateAsync(CreateUnitRequestDto request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request.PropertyId, request.RoomCount, request.RoomMaxCapacity, cancellationToken);

        var entity = new Unit
        {
            PropertyId = request.PropertyId,
            UnitNumber = request.UnitNumber.Trim(),
            Description = request.Description?.Trim(),
            MonthlyRent = request.MonthlyRent,
            Status = request.Status,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Rooms = BuildRoomList(request.RoomCount, request.RoomMaxCapacity)
        };

        dbContext.Units.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<UnitDto> UpdateAsync(int id, UpdateUnitRequestDto request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request.PropertyId, request.RoomCount, request.RoomMaxCapacity, cancellationToken);

        var entity = await dbContext.Units
            .Include(x => x.Rooms)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Unit not found.");

        var activeLeasesByRoom = await dbContext.Leases
            .AsNoTracking()
            .Where(x => x.UnitId == id && x.Status == LeaseStatus.Active)
            .GroupBy(x => x.RoomId)
            .Select(x => x.Key)
            .ToListAsync(cancellationToken);

        entity.PropertyId = request.PropertyId;
        entity.UnitNumber = request.UnitNumber.Trim();
        entity.Description = request.Description?.Trim();
        entity.MonthlyRent = request.MonthlyRent;
        entity.Status = request.Status;
        entity.UpdatedAt = DateTime.UtcNow;

        var currentRooms = entity.Rooms.OrderBy(x => x.RoomNumber).ToList();

        if (request.RoomCount < currentRooms.Count)
        {
            var removableRooms = currentRooms
                .Where(r => !activeLeasesByRoom.Contains(r.Id))
                .OrderByDescending(r => r.RoomNumber)
                .Take(currentRooms.Count - request.RoomCount)
                .ToList();

            if (removableRooms.Count < currentRooms.Count - request.RoomCount)
            {
                throw new AppValidationException("Cannot reduce room count because some rooms have active tenants.");
            }

            dbContext.Rooms.RemoveRange(removableRooms);
        }
        else if (request.RoomCount > currentRooms.Count)
        {
            var start = currentRooms.Count + 1;
            var toAdd = request.RoomCount - currentRooms.Count;

            for (var i = 0; i < toAdd; i++)
            {
                entity.Rooms.Add(new Room
                {
                    RoomNumber = $"Room {start + i}",
                    MaxCapacity = request.RoomMaxCapacity,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        foreach (var room in entity.Rooms)
        {
            room.MaxCapacity = request.RoomMaxCapacity;
            room.UpdatedAt = DateTime.UtcNow;
        }

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

    private static UnitDto ToDto(Unit entity)
    {
        var activeLeasesByRoom = entity.Leases
            .Where(x => x.Status == LeaseStatus.Active)
            .GroupBy(x => x.RoomId)
            .ToDictionary(g => g.Key, g => g.Count());

        var roomStatuses = entity.Rooms
            .Where(x => x.IsActive)
            .OrderBy(x => x.RoomNumber)
            .Select(room =>
            {
                var occupiedCount = activeLeasesByRoom.TryGetValue(room.Id, out var count) ? count : 0;
                var availableSlots = Math.Max(room.MaxCapacity - occupiedCount, 0);

                return new UnitRoomStatusDto
                {
                    RoomId = room.Id,
                    RoomNumber = room.RoomNumber,
                    MaxCapacity = room.MaxCapacity,
                    OccupiedCount = occupiedCount,
                    AvailableSlots = availableSlots,
                    Status = availableSlots > 0 ? "Available" : "Occupied"
                };
            })
            .ToList();

        var allRoomsOccupied = roomStatuses.Count > 0 && roomStatuses.All(x => x.AvailableSlots == 0);
        var occupancyDerivedStatus = allRoomsOccupied ? UnitStatus.Occupied : UnitStatus.Available;

        var mappedStatus = entity.Status is UnitStatus.Maintenance or UnitStatus.Inactive
            ? entity.Status
            : occupancyDerivedStatus;

        return new UnitDto
        {
            Id = entity.Id,
            PropertyId = entity.PropertyId,
            PropertyName = entity.Property?.Name ?? string.Empty,
            UnitNumber = entity.UnitNumber,
            Description = entity.Description,
            MonthlyRent = entity.MonthlyRent,
            RoomCount = roomStatuses.Count,
            RoomMaxCapacity = roomStatuses.OrderBy(x => x.RoomNumber).Select(x => x.MaxCapacity).FirstOrDefault(),
            Status = mappedStatus,
            IsActive = entity.IsActive,
            RoomStatuses = roomStatuses
        };
    }
}
