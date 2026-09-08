using Microsoft.EntityFrameworkCore;
using RentalApp.Application.DTOs.Expenses;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;
using RentalApp.Data;
using RentalApp.Domain.Entities;

namespace RentalApp.Application.Services;

public class ExpenseService(RentalDbContext dbContext) : IExpenseService
{
    public async Task<IReadOnlyList<ExpenseDto>> GetAllAsync(DateTime? fromDate, DateTime? toDate, int? categoryId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Expenses
            .AsNoTracking()
            .Include(x => x.Property)
            .Include(x => x.Unit)
            .Include(x => x.Category)
            .AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(x => x.ExpenseDate >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            query = query.Where(x => x.ExpenseDate <= toDate.Value.Date);
        }

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        return await query
            .OrderByDescending(x => x.ExpenseDate)
            .ThenByDescending(x => x.Id)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<ExpenseDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var expense = await dbContext.Expenses
            .AsNoTracking()
            .Include(x => x.Property)
            .Include(x => x.Unit)
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Expense not found.");

        return ToDto(expense);
    }

    public async Task<ExpenseDto> CreateAsync(CreateExpenseRequestDto request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request.PropertyId, request.UnitId, request.CategoryId, cancellationToken);

        var entity = new Expense
        {
            PropertyId = request.PropertyId,
            UnitId = request.UnitId,
            CategoryId = request.CategoryId,
            Description = request.Description.Trim(),
            Amount = request.Amount,
            ExpenseDate = request.ExpenseDate.Date,
            Notes = request.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.Expenses.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<ExpenseDto> UpdateAsync(int id, UpdateExpenseRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            throw new AppValidationException("Amount must be greater than zero.");
        }

        var entity = await dbContext.Expenses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Expense not found.");

        await ValidateAsync(request.PropertyId, request.UnitId, request.CategoryId, cancellationToken);

        entity.PropertyId = request.PropertyId;
        entity.UnitId = request.UnitId;
        entity.CategoryId = request.CategoryId;
        entity.Description = request.Description.Trim();
        entity.Amount = request.Amount;
        entity.ExpenseDate = request.ExpenseDate.Date;
        entity.Notes = request.Notes?.Trim();
        entity.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    private async Task ValidateAsync(int propertyId, int? unitId, int categoryId, CancellationToken cancellationToken)
    {
        var propertyExists = await dbContext.Properties.AnyAsync(x => x.Id == propertyId && x.IsActive, cancellationToken);
        if (!propertyExists)
        {
            throw new AppValidationException("Property does not exist or is inactive.");
        }

        var categoryExists = await dbContext.ExpenseCategories.AnyAsync(x => x.Id == categoryId && x.IsActive, cancellationToken);
        if (!categoryExists)
        {
            throw new AppValidationException("Expense category does not exist or is inactive.");
        }

        if (unitId.HasValue)
        {
            var unitBelongsToProperty = await dbContext.Units.AnyAsync(
                x => x.Id == unitId.Value && x.PropertyId == propertyId,
                cancellationToken);

            if (!unitBelongsToProperty)
            {
                throw new AppValidationException("Selected unit does not belong to selected property.");
            }
        }
    }

    private static ExpenseDto ToDto(Expense entity) => new()
    {
        Id = entity.Id,
        PropertyId = entity.PropertyId,
        PropertyName = entity.Property?.Name ?? string.Empty,
        UnitId = entity.UnitId,
        UnitNumber = entity.Unit?.UnitNumber,
        CategoryId = entity.CategoryId,
        CategoryName = entity.Category?.Name ?? string.Empty,
        Description = entity.Description,
        Amount = entity.Amount,
        ExpenseDate = entity.ExpenseDate,
        Notes = entity.Notes
    };
}
