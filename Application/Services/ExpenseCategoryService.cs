using Microsoft.EntityFrameworkCore;
using RentalApp.Application.DTOs.ExpenseCategories;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;
using RentalApp.Data;
using RentalApp.Domain.Entities;

namespace RentalApp.Application.Services;

public class ExpenseCategoryService(RentalDbContext dbContext) : IExpenseCategoryService
{
    public async Task<IReadOnlyList<ExpenseCategoryDto>> GetAllAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = dbContext.ExpenseCategories.AsNoTracking();

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

    public async Task<ExpenseCategoryDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ExpenseCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Expense category not found.");

        return ToDto(entity);
    }

    public async Task<ExpenseCategoryDto> CreateAsync(CreateExpenseCategoryRequestDto request, CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.ExpenseCategories.AnyAsync(x => x.Name == request.Name.Trim(), cancellationToken);
        if (exists)
        {
            throw new AppValidationException("Expense category name already exists.");
        }

        var entity = new ExpenseCategory
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            IsActive = true
        };

        dbContext.ExpenseCategories.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<ExpenseCategoryDto> UpdateAsync(int id, UpdateExpenseCategoryRequestDto request, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ExpenseCategories
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Expense category not found.");

        var exists = await dbContext.ExpenseCategories.AnyAsync(x => x.Id != id && x.Name == request.Name.Trim(), cancellationToken);
        if (exists)
        {
            throw new AppValidationException("Expense category name already exists.");
        }

        entity.Name = request.Name.Trim();
        entity.Description = request.Description?.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ExpenseCategories
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Expense category not found.");

        entity.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static ExpenseCategoryDto ToDto(ExpenseCategory entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        IsActive = entity.IsActive
    };
}
