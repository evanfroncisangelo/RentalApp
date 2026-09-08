using Microsoft.EntityFrameworkCore;
using RentalApp.Application.DTOs.Utilities;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;
using RentalApp.Data;
using RentalApp.Domain.Entities;
using RentalApp.Domain.Enums;

namespace RentalApp.Application.Services;

public class UtilityCustomerService(RentalDbContext dbContext) : IUtilityCustomerService
{
    public async Task<IReadOnlyList<UtilityCustomerDto>> GetAllAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = dbContext.UtilityCustomers
            .AsNoTracking()
            .AsQueryable();

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

    public async Task<UtilityCustomerDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.UtilityCustomers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Utility customer not found.");

        return ToDto(entity);
    }

    public async Task<UtilityCustomerDto> CreateAsync(CreateUtilityCustomerRequestDto request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new AppValidationException("Customer name is required.");
        }

        await EnsureCategoryExistsAsync(request.UtilityCategoryId, cancellationToken);
        ValidateBillingPreferences(request.AmountToPay, request.DueDayOfMonth);

        var entity = new UtilityCustomer
        {
            RoomId = request.RoomId,
            TenantId = request.TenantId,
            UtilityCategoryId = request.UtilityCategoryId,
            Name = name,
            CustomerType = request.CustomerType,
            DueDateRuleType = UtilityDueDateRuleType.FixedDayOfMonth,
            DueInDays = null,
            DueDayOfMonth = request.DueDayOfMonth,
            DefaultRate = request.AmountToPay,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.UtilityCustomers.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<UtilityCustomerDto> UpdateAsync(int id, UpdateUtilityCustomerRequestDto request, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.UtilityCustomers
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Utility customer not found.");

        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new AppValidationException("Customer name is required.");
        }

        await EnsureCategoryExistsAsync(request.UtilityCategoryId, cancellationToken);
        ValidateBillingPreferences(request.AmountToPay, request.DueDayOfMonth);

        entity.RoomId = request.RoomId;
        entity.TenantId = request.TenantId;
        entity.UtilityCategoryId = request.UtilityCategoryId;
        entity.Name = name;
        entity.CustomerType = request.CustomerType;
        entity.DueDateRuleType = UtilityDueDateRuleType.FixedDayOfMonth;
        entity.DueDayOfMonth = request.DueDayOfMonth;
        entity.DueInDays = null;
        entity.DefaultRate = request.AmountToPay;
        entity.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    private async Task EnsureCategoryExistsAsync(int? utilityCategoryId, CancellationToken cancellationToken)
    {
        if (!utilityCategoryId.HasValue)
        {
            return;
        }

        var exists = await dbContext.UtilityTypes.AnyAsync(x => x.Id == utilityCategoryId.Value && x.IsActive, cancellationToken);
        if (!exists)
        {
            throw new AppValidationException("Selected utility category is invalid.");
        }
    }

    private static void ValidateBillingPreferences(decimal? amountToPay, int? dueDayOfMonth)
    {
        if (!amountToPay.HasValue || amountToPay.Value <= 0)
        {
            throw new AppValidationException("Amount to pay must be greater than zero.");
        }

        if (!dueDayOfMonth.HasValue || dueDayOfMonth.Value < 1 || dueDayOfMonth.Value > 28)
        {
            throw new AppValidationException("Due date day must be between 1 and 28.");
        }
    }

    private static UtilityCustomerDto ToDto(UtilityCustomer entity) => new()
    {
        Id = entity.Id,
        RoomId = entity.RoomId,
        TenantId = entity.TenantId,
        UtilityCategoryId = entity.UtilityCategoryId,
        Name = entity.Name,
        CustomerType = entity.CustomerType,
        DueDateRuleType = entity.DueDateRuleType,
        DueDayOfMonth = entity.DueDayOfMonth,
        DueInDays = entity.DueInDays,
        DefaultRate = entity.DefaultRate,
        IsActive = entity.IsActive
    };
}
