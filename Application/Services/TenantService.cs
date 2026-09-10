using Microsoft.EntityFrameworkCore;
using RentalApp.Application.DTOs.Leases;
using RentalApp.Application.DTOs.Tenants;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;
using RentalApp.Data;
using RentalApp.Domain.Entities;
using RentalApp.Domain.Enums;

namespace RentalApp.Application.Services;

public class TenantService(RentalDbContext dbContext, ILeaseService leaseService) : ITenantService
{
    public async Task<IReadOnlyList<TenantDto>> GetAllAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Tenants.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x =>
                x.FirstName.Contains(keyword) ||
                x.LastName.Contains(keyword) ||
                (x.ContactNumber != null && x.ContactNumber.Contains(keyword)));
        }

        return await query
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<TenantDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Tenant not found.");

        return ToDto(entity);
    }

    public async Task<TenantDto> CreateAsync(CreateTenantRequestDto request, CancellationToken cancellationToken = default)
    {
        if (dbContext.Database.CurrentTransaction is not null ||
            string.Equals(dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal))
        {
            return await CreateInternalAsync(request, cancellationToken);
        }

        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await CreateInternalAsync(request, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public async Task<TenantDto> UpdateAsync(int id, UpdateTenantRequestDto request, CancellationToken cancellationToken = default)
    {
        if (dbContext.Database.CurrentTransaction is not null ||
            string.Equals(dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal))
        {
            return await UpdateInternalAsync(id, request, cancellationToken);
        }

        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await UpdateInternalAsync(id, request, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public async Task DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Tenants
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Tenant not found.");

        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<TenantDto> CreateInternalAsync(CreateTenantRequestDto request, CancellationToken cancellationToken)
    {
        var entity = new Tenant
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            ContactNumber = request.ContactNumber?.Trim(),
            Address = request.Address?.Trim(),
            UnitId = request.UnitId,
            UnitNumber = request.UnitNumber?.Trim(),
            RoomNumber = request.RoomNumber?.Trim(),
            MoveInDate = request.MoveInDate,
            Notes = request.Notes?.Trim(),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.Tenants.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await EnsureTenantLeaseAsync(entity, cancellationToken);
            return ToDto(entity);
        }
        catch
        {
            dbContext.Tenants.Remove(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private async Task<TenantDto> UpdateInternalAsync(int id, UpdateTenantRequestDto request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Tenants
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Tenant not found.");

        entity.FirstName = request.FirstName.Trim();
        entity.LastName = request.LastName.Trim();
        entity.ContactNumber = request.ContactNumber?.Trim();
        entity.Address = request.Address?.Trim();
        entity.UnitId = request.UnitId;
        entity.UnitNumber = request.UnitNumber?.Trim();
        entity.RoomNumber = request.RoomNumber?.Trim();
        entity.MoveInDate = request.MoveInDate;
        entity.Notes = request.Notes?.Trim();
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await EnsureTenantLeaseAsync(entity, cancellationToken);
        return ToDto(entity);
    }

    private async Task EnsureTenantLeaseAsync(Tenant tenant, CancellationToken cancellationToken)
    {
        if (!tenant.IsActive || !tenant.UnitId.HasValue)
        {
            return;
        }

        var unit = await dbContext.Units
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == tenant.UnitId && u.IsActive, cancellationToken)
            ?? throw new AppValidationException("Assigned unit does not exist.");

        var leaseStartDate = (tenant.MoveInDate ?? DateTime.UtcNow.Date).Date;
        var dueDayOfMonth = Math.Clamp(leaseStartDate.Day, 1, 28);
        var activeLease = await dbContext.Leases
            .AsNoTracking()
            .Where(x => x.TenantId == tenant.Id && x.Status == LeaseStatus.Active)
            .OrderByDescending(x => x.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeLease is null)
        {
            await EnsureUnitHasCapacityAsync(unit.Id, unit.MaxCapacity, tenant.Id, cancellationToken);
            var assignedRoomId = await GetExistingRoomIdAsync(unit.Id, cancellationToken);
            await leaseService.CreateAsync(new CreateLeaseRequestDto
            {
                UnitId = unit.Id,
                RoomId = assignedRoomId,
                TenantId = tenant.Id,
                StartDate = leaseStartDate,
                MonthlyRent = unit.MonthlyRent,
                SecurityDeposit = unit.MonthlyRent,
                DueDayOfMonth = dueDayOfMonth,
                Status = LeaseStatus.Active
            }, cancellationToken);

            return;
        }

        if (activeLease.UnitId != unit.Id)
        {
            await EnsureUnitHasCapacityAsync(unit.Id, unit.MaxCapacity, tenant.Id, cancellationToken);
            var assignedRoomId = await GetExistingRoomIdAsync(unit.Id, cancellationToken);
            await leaseService.TransferAsync(new TransferLeaseRequestDto
            {
                CurrentLeaseId = activeLease.Id,
                NewUnitId = unit.Id,
                NewRoomId = assignedRoomId,
                TransferDate = leaseStartDate,
                Notes = "Tenant assignment updated."
            }, cancellationToken);

            return;
        }

        await leaseService.UpdateAsync(activeLease.Id, new UpdateLeaseRequestDto
        {
            UnitId = activeLease.UnitId,
            RoomId = activeLease.RoomId,
            StartDate = leaseStartDate,
            EndDate = activeLease.EndDate,
            MonthlyRent = unit.MonthlyRent,
            SecurityDeposit = activeLease.SecurityDeposit > 0 ? activeLease.SecurityDeposit : unit.MonthlyRent,
            DueDayOfMonth = dueDayOfMonth,
            Notes = activeLease.Notes
        }, cancellationToken);
    }

    private async Task EnsureUnitHasCapacityAsync(int unitId, int maxCapacity, int tenantId, CancellationToken cancellationToken)
    {
        var activeTenantCount = await dbContext.Leases
            .AsNoTracking()
            .Where(x => x.UnitId == unitId && x.Status == LeaseStatus.Active && x.TenantId != tenantId)
            .Select(x => x.TenantId)
            .Distinct()
            .CountAsync(cancellationToken);

        if (activeTenantCount >= maxCapacity)
        {
            throw new AppValidationException("Selected unit is already at maximum capacity.");
        }
    }

    private async Task<int> GetExistingRoomIdAsync(int unitId, CancellationToken cancellationToken)
    {
        var roomId = await dbContext.Rooms
            .AsNoTracking()
            .Where(x => x.UnitId == unitId && x.IsActive)
            .OrderBy(x => x.RoomNumber)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (roomId <= 0)
        {
            throw new AppValidationException("Assigned unit has no room configured for lease linkage.");
        }

        return roomId;
    }

    public async Task ReactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Tenants
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Tenant not found.");

        entity.IsActive = true;
        entity.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static TenantDto ToDto(Tenant entity) => new()
    {
        Id = entity.Id,
        FirstName = entity.FirstName,
        LastName = entity.LastName,
        ContactNumber = entity.ContactNumber,
        Address = entity.Address,
        UnitId = entity.UnitId,
        UnitNumber = entity.UnitNumber,
        RoomNumber = entity.RoomNumber,
        MoveInDate = entity.MoveInDate,
        Notes = entity.Notes,
        IsActive = entity.IsActive
    };
}
