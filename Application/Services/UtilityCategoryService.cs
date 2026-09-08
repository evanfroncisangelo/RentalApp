using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using RentalApp.Application.DTOs.Utilities;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;
using RentalApp.Data;
using RentalApp.Domain.Entities;

namespace RentalApp.Application.Services;

public partial class UtilityCategoryService(RentalDbContext dbContext) : IUtilityCategoryService
{
    public async Task<IReadOnlyList<UtilityCategoryDto>> GetAllAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = dbContext.UtilityTypes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x => x.Name.Contains(keyword));
        }

        return await query
            .OrderBy(x => x.Name)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<UtilityCategoryDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.UtilityTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Utility category not found.");

        return ToDto(entity);
    }

    public async Task<UtilityCategoryDto> CreateAsync(CreateUtilityCategoryRequestDto request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new AppValidationException("Category name is required.");
        }

        var exists = await dbContext.UtilityTypes.AnyAsync(x => x.Name == name, cancellationToken);
        if (exists)
        {
            throw new AppValidationException("Utility category name already exists.");
        }

        var entity = new UtilityType
        {
            Name = name,
            Code = await GenerateUniqueCodeAsync(name, cancellationToken),
            Unit = "unit",
            DefaultRate = null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.UtilityTypes.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<UtilityCategoryDto> UpdateAsync(int id, UpdateUtilityCategoryRequestDto request, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.UtilityTypes
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Utility category not found.");

        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new AppValidationException("Category name is required.");
        }

        var exists = await dbContext.UtilityTypes.AnyAsync(x => x.Id != id && x.Name == name, cancellationToken);
        if (exists)
        {
            throw new AppValidationException("Utility category name already exists.");
        }

        entity.Name = name;
        entity.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.UtilityTypes
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Utility category not found.");

        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> GenerateUniqueCodeAsync(string name, CancellationToken cancellationToken)
    {
        var baseCode = UtilityCodeRegex().Replace(name.ToUpperInvariant(), "_").Trim('_');
        if (string.IsNullOrWhiteSpace(baseCode))
        {
            baseCode = "CATEGORY";
        }

        var candidate = baseCode;
        var suffix = 1;
        while (await dbContext.UtilityTypes.AnyAsync(x => x.Code == candidate, cancellationToken))
        {
            suffix++;
            candidate = $"{baseCode}_{suffix}";
        }

        return candidate;
    }

    private static UtilityCategoryDto ToDto(UtilityType entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        IsActive = entity.IsActive
    };

    [GeneratedRegex("[^A-Z0-9]+")]
    private static partial Regex UtilityCodeRegex();
}
