using Microsoft.EntityFrameworkCore;
using RentalApp.Application.DTOs.Leases;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;
using RentalApp.Data;
using RentalApp.Domain.Entities;
using RentalApp.Domain.Enums;

namespace RentalApp.Application.Services;

public class LeaseService(RentalDbContext dbContext) : ILeaseService
{
    public async Task<IReadOnlyList<LeaseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Leases
            .AsNoTracking()
            .Include(x => x.Unit)
            .Include(x => x.Room)
            .Include(x => x.Tenant)
            .OrderByDescending(x => x.StartDate)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<LeaseDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Leases
            .AsNoTracking()
            .Include(x => x.Unit)
            .Include(x => x.Room)
            .Include(x => x.Tenant)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Lease not found.");

        return ToDto(entity);
    }

    public async Task<UnitLeaseInfoDto> GetUnitLeaseInfoAsync(int unitId, CancellationToken cancellationToken = default)
    {
        var unit = await dbContext.Units
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == unitId, cancellationToken)
            ?? throw new AppValidationException("Unit does not exist.");

        if (!unit.IsActive)
        {
            throw new AppValidationException("Selected unit is inactive.");
        }

        var activeCounts = await dbContext.Leases
            .AsNoTracking()
            .Where(x => x.UnitId == unitId && x.Status == LeaseStatus.Active)
            .GroupBy(x => x.RoomId)
            .Select(x => new { RoomId = x.Key, Count = x.Count() })
            .ToListAsync(cancellationToken);

        var countsByRoom = activeCounts.ToDictionary(x => x.RoomId, x => x.Count);

        var rooms = await dbContext.Rooms
            .AsNoTracking()
            .Where(x => x.UnitId == unitId && x.IsActive)
            .OrderBy(x => x.RoomNumber)
            .ToListAsync(cancellationToken);

        return new UnitLeaseInfoDto
        {
            UnitId = unit.Id,
            MonthlyRent = unit.MonthlyRent,
            Rooms = rooms.Select(r =>
            {
                var occupied = countsByRoom.TryGetValue(r.Id, out var count) ? count : 0;
                var available = Math.Max(r.MaxCapacity - occupied, 0);
                return new LeaseRoomOptionDto
                {
                    RoomId = r.Id,
                    RoomNumber = r.RoomNumber,
                    MaxCapacity = r.MaxCapacity,
                    OccupiedCount = occupied,
                    AvailableSlots = available
                };
            }).ToList()
        };
    }

    public async Task<LeaseDto> CreateAsync(CreateLeaseRequestDto request, CancellationToken cancellationToken = default)
    {
        return await ExecuteInTransactionAsync(async () =>
        {
            ValidateLeaseDates(request.StartDate, request.EndDate);
            ValidateDueDay(request.DueDayOfMonth);

            var unit = await dbContext.Units.FirstOrDefaultAsync(x => x.Id == request.UnitId && x.IsActive, cancellationToken)
                ?? throw new AppValidationException("Unit does not exist or is inactive.");

            var room = await dbContext.Rooms.FirstOrDefaultAsync(x => x.Id == request.RoomId && x.UnitId == request.UnitId && x.IsActive, cancellationToken)
                ?? throw new AppValidationException("Selected room is invalid for this unit.");

            _ = await dbContext.Tenants.FirstOrDefaultAsync(x => x.Id == request.TenantId && x.IsActive, cancellationToken)
                ?? throw new AppValidationException("Tenant does not exist or is inactive.");

            var tenantActiveLease = await dbContext.Leases.AnyAsync(
                x => x.TenantId == request.TenantId && x.Status == LeaseStatus.Active,
                cancellationToken);

            if (tenantActiveLease)
            {
                throw new AppValidationException("Tenant already has an active unit/room lease.");
            }

            var currentOccupancy = await dbContext.Leases.CountAsync(
                x => x.RoomId == room.Id && x.Status == LeaseStatus.Active,
                cancellationToken);

            if (currentOccupancy >= room.MaxCapacity)
            {
                throw new AppValidationException("Selected room is already at maximum capacity.");
            }

            var lease = new Lease
            {
                UnitId = request.UnitId,
                RoomId = request.RoomId,
                TenantId = request.TenantId,
                StartDate = request.StartDate.Date,
                EndDate = request.EndDate?.Date,
                MonthlyRent = request.MonthlyRent,
                SecurityDeposit = request.SecurityDeposit,
                DueDayOfMonth = request.DueDayOfMonth,
                Status = request.Status,
                Notes = request.Notes?.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.Leases.Add(lease);
            await dbContext.SaveChangesAsync(cancellationToken);
            await RefreshUnitStatusAsync(unit.Id, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            return await GetByIdAsync(lease.Id, cancellationToken);
        }, cancellationToken);
    }

    public async Task<LeaseDto> UpdateAsync(int id, UpdateLeaseRequestDto request, CancellationToken cancellationToken = default)
    {
        return await ExecuteInTransactionAsync(async () =>
        {
            ValidateLeaseDates(request.StartDate, request.EndDate);
            ValidateDueDay(request.DueDayOfMonth);

            var lease = await dbContext.Leases
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new AppNotFoundException("Lease not found.");

            var room = await dbContext.Rooms.FirstOrDefaultAsync(x => x.Id == request.RoomId && x.UnitId == request.UnitId && x.IsActive, cancellationToken)
                ?? throw new AppValidationException("Selected room is invalid for this unit.");

            var currentOccupancy = await dbContext.Leases.CountAsync(
                x => x.RoomId == room.Id && x.Status == LeaseStatus.Active && x.Id != lease.Id,
                cancellationToken);

            if (lease.Status == LeaseStatus.Active && currentOccupancy >= room.MaxCapacity)
            {
                throw new AppValidationException("Selected room is already at maximum capacity.");
            }

            var previousUnitId = lease.UnitId;

            lease.UnitId = request.UnitId;
            lease.RoomId = request.RoomId;
            lease.StartDate = request.StartDate.Date;
            lease.EndDate = request.EndDate?.Date;
            lease.MonthlyRent = request.MonthlyRent;
            lease.SecurityDeposit = request.SecurityDeposit;
            lease.DueDayOfMonth = request.DueDayOfMonth;
            lease.Notes = request.Notes?.Trim();
            lease.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);
            await RefreshUnitStatusAsync(previousUnitId, cancellationToken);
            await RefreshUnitStatusAsync(request.UnitId, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            return await GetByIdAsync(id, cancellationToken);
        }, cancellationToken);
    }

    public async Task<LeaseDto> MoveOutAsync(int id, MoveOutRequestDto request, CancellationToken cancellationToken = default)
    {
        return await ExecuteInTransactionAsync(async () =>
        {
            var lease = await dbContext.Leases
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new AppNotFoundException("Lease not found.");

            if (lease.Status != LeaseStatus.Active)
            {
                throw new AppValidationException("Only active leases can be moved out.");
            }

            if (request.MoveOutDate.Date < lease.StartDate.Date)
            {
                throw new AppValidationException("Move-out date cannot be before lease start date.");
            }

            lease.EndDate = request.MoveOutDate.Date;
            lease.Status = LeaseStatus.Ended;
            lease.Notes = string.IsNullOrWhiteSpace(request.Notes)
                ? lease.Notes
                : $"{lease.Notes}\nMove-out: {request.Notes.Trim()}".Trim();
            lease.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);
            await RefreshUnitStatusAsync(lease.UnitId, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            return await GetByIdAsync(id, cancellationToken);
        }, cancellationToken);
    }

    public async Task<LeaseDto> TransferAsync(TransferLeaseRequestDto request, CancellationToken cancellationToken = default)
    {
        return await ExecuteInTransactionAsync(async () =>
        {
            var currentLease = await dbContext.Leases
                .FirstOrDefaultAsync(x => x.Id == request.CurrentLeaseId, cancellationToken)
                ?? throw new AppNotFoundException("Current lease not found.");

            if (currentLease.Status != LeaseStatus.Active)
            {
                throw new AppValidationException("Only active leases can be transferred.");
            }

            if (request.TransferDate.Date < currentLease.StartDate.Date)
            {
                throw new AppValidationException("Transfer date cannot be earlier than lease start date.");
            }

            var target = await GetUnitLeaseInfoAsync(request.NewUnitId, cancellationToken);
            var room = target.Rooms.FirstOrDefault(x => x.RoomId == request.NewRoomId)
                ?? throw new AppValidationException("Selected target room is invalid.");

            if (room.AvailableSlots <= 0)
            {
                throw new AppValidationException("Selected target room has no remaining vacancy.");
            }

            currentLease.Status = LeaseStatus.Ended;
            currentLease.EndDate = request.TransferDate.Date;
            currentLease.UpdatedAt = DateTime.UtcNow;

            var newLease = new Lease
            {
                UnitId = request.NewUnitId,
                RoomId = request.NewRoomId,
                TenantId = currentLease.TenantId,
                StartDate = request.TransferDate.Date,
                MonthlyRent = target.MonthlyRent,
                SecurityDeposit = currentLease.SecurityDeposit,
                DueDayOfMonth = currentLease.DueDayOfMonth,
                Status = LeaseStatus.Active,
                Notes = request.Notes?.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.Leases.Add(newLease);

            await dbContext.SaveChangesAsync(cancellationToken);
            await RefreshUnitStatusAsync(currentLease.UnitId, cancellationToken);
            await RefreshUnitStatusAsync(request.NewUnitId, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            return await GetByIdAsync(newLease.Id, cancellationToken);
        }, cancellationToken);
    }

    private async Task RefreshUnitStatusAsync(int unitId, CancellationToken cancellationToken)
    {
        var unit = await dbContext.Units.FirstOrDefaultAsync(x => x.Id == unitId, cancellationToken);
        if (unit is null)
        {
            return;
        }

        if (unit.Status is UnitStatus.Maintenance or UnitStatus.Inactive)
        {
            unit.UpdatedAt = DateTime.UtcNow;
            return;
        }

        var rooms = await dbContext.Rooms
            .AsNoTracking()
            .Where(x => x.UnitId == unitId && x.IsActive)
            .Select(x => new { x.Id, x.MaxCapacity })
            .ToListAsync(cancellationToken);

        if (rooms.Count == 0)
        {
            unit.Status = UnitStatus.Available;
            unit.UpdatedAt = DateTime.UtcNow;
            return;
        }

        var activeCountsByRoom = await dbContext.Leases
            .AsNoTracking()
            .Where(x => x.UnitId == unitId && x.Status == LeaseStatus.Active)
            .GroupBy(x => x.RoomId)
            .Select(g => new { RoomId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var occupancyMap = activeCountsByRoom.ToDictionary(x => x.RoomId, x => x.Count);
        var allRoomsOccupied = rooms.All(room =>
        {
            var occupiedCount = occupancyMap.TryGetValue(room.Id, out var count) ? count : 0;
            return occupiedCount >= room.MaxCapacity;
        });

        unit.Status = allRoomsOccupied ? UnitStatus.Occupied : UnitStatus.Available;
        unit.UpdatedAt = DateTime.UtcNow;
    }

    private async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        if (dbContext.Database.CurrentTransaction is not null ||
            string.Equals(dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal))
        {
            return await action();
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await action();
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static void ValidateLeaseDates(DateTime startDate, DateTime? endDate)
    {
        if (endDate.HasValue && endDate.Value.Date < startDate.Date)
        {
            throw new AppValidationException("End date cannot be earlier than start date.");
        }
    }

    private static void ValidateDueDay(int dueDay)
    {
        if (dueDay < 1 || dueDay > 28)
        {
            throw new AppValidationException("Due day must be between 1 and 28.");
        }
    }

    private static LeaseDto ToDto(Lease entity) => new()
    {
        Id = entity.Id,
        UnitId = entity.UnitId,
        UnitNumber = entity.Unit?.UnitNumber ?? string.Empty,
        RoomId = entity.RoomId,
        RoomNumber = entity.Room?.RoomNumber ?? string.Empty,
        TenantId = entity.TenantId,
        TenantName = entity.Tenant is null ? string.Empty : $"{entity.Tenant.FirstName} {entity.Tenant.LastName}".Trim(),
        StartDate = entity.StartDate,
        EndDate = entity.EndDate,
        MonthlyRent = entity.MonthlyRent,
        SecurityDeposit = entity.SecurityDeposit,
        DueDayOfMonth = entity.DueDayOfMonth,
        Status = entity.Status,
        Notes = entity.Notes
    };
}
