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
        var (name, roomId, tenantId, startDate) = await NormalizeAndValidateRequestAsync(request, cancellationToken);

        await EnsureCategoryExistsAsync(request.UtilityCategoryId, cancellationToken);
        ValidateBillingPreferences(request.AmountToPay, startDate?.Day);

        var entity = new UtilityCustomer
        {
            RoomId = roomId,
            TenantId = tenantId,
            UtilityCategoryId = request.UtilityCategoryId,
            Name = name,
            CustomerType = request.CustomerType,
            DueDateRuleType = UtilityDueDateRuleType.FixedDayOfMonth,
            UtilityStartDate = startDate,
            DueInDays = null,
            DueDayOfMonth = null,
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

        var (name, roomId, tenantId, startDate) = await NormalizeAndValidateRequestAsync(request, cancellationToken);

        await EnsureCategoryExistsAsync(request.UtilityCategoryId, cancellationToken);
        ValidateBillingPreferences(request.AmountToPay, startDate?.Day);

        entity.RoomId = roomId;
        entity.TenantId = tenantId;
        entity.UtilityCategoryId = request.UtilityCategoryId;
        entity.Name = name;
        entity.CustomerType = request.CustomerType;
        entity.DueDateRuleType = UtilityDueDateRuleType.FixedDayOfMonth;
        entity.UtilityStartDate = startDate;
        entity.DueDayOfMonth = null;
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

        if (dueDayOfMonth.HasValue && (dueDayOfMonth.Value < 1 || dueDayOfMonth.Value > 31))
        {
            throw new AppValidationException("Due date day must be between 1 and 31.");
        }
    }

    private async Task<(string Name, int? RoomId, int? TenantId, DateTime? UtilityStartDate)> NormalizeAndValidateRequestAsync(CreateUtilityCustomerRequestDto request, CancellationToken cancellationToken)
    {
        if (request.CustomerType is not UtilityCustomerType.Tenant and not UtilityCustomerType.External)
        {
            throw new AppValidationException("Only Tenant and External utility customer types are allowed.");
        }

        if (request.CustomerType == UtilityCustomerType.External)
        {
            var externalName = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(externalName))
            {
                throw new AppValidationException("Customer name is required for external utility customer.");
            }

            return (externalName, null, null, request.UtilityStartDate?.Date);
        }

        if (!request.RoomId.HasValue)
        {
            throw new AppValidationException("Unit is required for tenant utility customer.");
        }

        var room = await dbContext.Rooms
            .AsNoTracking()
            .Include(x => x.Unit)
            .ThenInclude(x => x!.Property)
            .FirstOrDefaultAsync(x => x.Id == request.RoomId.Value && x.IsActive, cancellationToken)
            ?? throw new AppValidationException("Selected unit is invalid.");

        var label = $"{room.Unit!.Property!.Name} - {room.Unit.UnitNumber}";

        var tenantMoveInDate = await dbContext.Tenants
            .AsNoTracking()
            .Where(x => x.IsActive && x.UnitId == room.UnitId && x.MoveInDate.HasValue)
            .OrderBy(x => x.MoveInDate)
            .Select(x => x.MoveInDate)
            .FirstOrDefaultAsync(cancellationToken);

        var startDate = (request.UtilityStartDate?.Date ?? tenantMoveInDate?.Date);

        return (label, room.Id, null, startDate);
    }

    private Task<(string Name, int? RoomId, int? TenantId, DateTime? UtilityStartDate)> NormalizeAndValidateRequestAsync(UpdateUtilityCustomerRequestDto request, CancellationToken cancellationToken)
        => NormalizeAndValidateRequestAsync(new CreateUtilityCustomerRequestDto
        {
            RoomId = request.RoomId,
            TenantId = request.TenantId,
            UtilityCategoryId = request.UtilityCategoryId,
            Name = request.Name,
            CustomerType = request.CustomerType,
            UtilityStartDate = request.UtilityStartDate,
            AmountToPay = request.AmountToPay,
            DueDayOfMonth = request.DueDayOfMonth
        }, cancellationToken);

    private static UtilityCustomerDto ToDto(UtilityCustomer entity) => new()
    {
        Id = entity.Id,
        RoomId = entity.RoomId,
        TenantId = entity.TenantId,
        UtilityCategoryId = entity.UtilityCategoryId,
        Name = entity.Name,
        CustomerType = entity.CustomerType,
        DueDateRuleType = entity.DueDateRuleType,
        UtilityStartDate = entity.UtilityStartDate,
        DueDayOfMonth = entity.DueDayOfMonth,
        DueInDays = entity.DueInDays,
        DefaultRate = entity.DefaultRate,
        IsActive = entity.IsActive
    };
}
