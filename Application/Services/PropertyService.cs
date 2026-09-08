using Microsoft.EntityFrameworkCore;
using RentalApp.Application.DTOs.Properties;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;
using RentalApp.Data;
using RentalApp.Domain.Entities;

namespace RentalApp.Application.Services;

public class PropertyService(RentalDbContext dbContext) : IPropertyService
{
    public async Task<IReadOnlyList<PropertyDto>> GetAllAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Properties.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x => x.Name.Contains(keyword) || x.Address.Contains(keyword));
        }

        return await query
            .OrderBy(x => x.Name)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<PropertyDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Properties
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Property not found.");

        return ToDto(entity);
    }

    public async Task<PropertyDto> CreateAsync(CreatePropertyRequestDto request, CancellationToken cancellationToken = default)
    {
        var entity = new Property
        {
            Name = request.Name.Trim(),
            Address = request.Address.Trim(),
            Description = request.Description?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.Properties.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(entity);
    }

    public async Task<PropertyDto> UpdateAsync(int id, UpdatePropertyRequestDto request, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Properties
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Property not found.");

        entity.Name = request.Name.Trim();
        entity.Address = request.Address.Trim();
        entity.Description = request.Description?.Trim();
        entity.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Properties
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Property not found.");

        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static PropertyDto ToDto(Property entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Address = entity.Address,
        Description = entity.Description,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
