using Microsoft.EntityFrameworkCore;
using RentalApp.Application.DTOs.Tenants;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;
using RentalApp.Data;
using RentalApp.Domain.Entities;

namespace RentalApp.Application.Services;

public class TenantService(RentalDbContext dbContext) : ITenantService
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
            Notes = request.Notes?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.Tenants.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
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
        entity.Notes = request.Notes?.Trim();
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
        Notes = entity.Notes,
        IsActive = entity.IsActive
    };
}
