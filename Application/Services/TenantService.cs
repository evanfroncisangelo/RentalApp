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
                (x.ContactNumber != null && x.ContactNumber.Contains(keyword)) ||
                (x.Email != null && x.Email.Contains(keyword)));
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
        var entity = new Tenant
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            ContactNumber = request.ContactNumber?.Trim(),
            Email = request.Email?.Trim(),
            Address = request.Address?.Trim(),
            UnitId = request.UnitId,
            UnitNumber = request.UnitNumber?.Trim(),
            RoomNumber = request.RoomNumber?.Trim(),
            DateOfBirth = request.DateOfBirth,
            MoveInDate = request.MoveInDate,
            Notes = request.Notes?.Trim(),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.Tenants.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        // If tenant is assigned to a unit and is active, create an active lease
        if (entity.IsActive && entity.UnitId.HasValue)
        {
            // Get the unit to find rent and room info
            var unit = await dbContext.Units
                .AsNoTracking()
                .Include(u => u.Rooms)
                .FirstOrDefaultAsync(u => u.Id == entity.UnitId, cancellationToken);

            if (unit != null)
            {
                // Find first available active room
                var availableRoom = unit.Rooms
                    .FirstOrDefault(r => r.IsActive);

                if (availableRoom != null)
                    {
                        // Create active lease with MoveInDate as StartDate
                        // SecurityDeposit defaults to one month's rent if not customized
                        var leaseStartDate = request.MoveInDate ?? DateTime.UtcNow.Date;
                        await leaseService.CreateAsync(new CreateLeaseRequestDto
                        {
                            UnitId = unit.Id,
                            RoomId = availableRoom.Id,
                            TenantId = entity.Id,
                            StartDate = leaseStartDate,
                            MonthlyRent = unit.MonthlyRent,
                            SecurityDeposit = unit.MonthlyRent, // Default to 1 month rent
                            DueDayOfMonth = 1,
                            Status = LeaseStatus.Active
                        }, cancellationToken);
                    }
            }
        }

        return ToDto(entity);
    }

    public async Task<TenantDto> UpdateAsync(int id, UpdateTenantRequestDto request, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Tenants
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Tenant not found.");

        entity.FirstName = request.FirstName.Trim();
        entity.LastName = request.LastName.Trim();
        entity.ContactNumber = request.ContactNumber?.Trim();
        entity.Email = request.Email?.Trim();
        entity.Address = request.Address?.Trim();
        entity.UnitId = request.UnitId;
        entity.UnitNumber = request.UnitNumber?.Trim();
        entity.RoomNumber = request.RoomNumber?.Trim();
        entity.DateOfBirth = request.DateOfBirth;
        entity.MoveInDate = request.MoveInDate;
        entity.Notes = request.Notes?.Trim();
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
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
        Email = entity.Email,
        Address = entity.Address,
        UnitId = entity.UnitId,
        UnitNumber = entity.UnitNumber,
        RoomNumber = entity.RoomNumber,
        DateOfBirth = entity.DateOfBirth,
        MoveInDate = entity.MoveInDate,
        Notes = entity.Notes,
        IsActive = entity.IsActive
    };
}
