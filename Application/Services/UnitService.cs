using Microsoft.EntityFrameworkCore;
using RentalApp.Application.DTOs.Units;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;
using RentalApp.Data;
using RentalApp.Domain.Entities;
using RentalApp.Domain.Enums;

namespace RentalApp.Application.Services;

public class UnitService(RentalDbContext dbContext) : IUnitService
{
    public async Task<IReadOnlyList<UnitDto>> GetAllAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Units
            .AsNoTracking()
            .Include(x => x.Property)
            .Include(x => x.Rooms)
            .Include(x => x.Leases)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x => x.UnitNumber.Contains(keyword) || (x.Property != null && x.Property.Name.Contains(keyword)));
        }

        var units = await query
            .OrderBy(x => x.PropertyId)
            .ThenBy(x => x.UnitNumber)
            .ToListAsync(cancellationToken);

        var activeTenantCountsByUnit = await dbContext.Tenants
            .AsNoTracking()
            .Where(x => x.IsActive && x.UnitId.HasValue)
            .GroupBy(x => x.UnitId!.Value)
            .Select(g => new { UnitId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UnitId, x => x.Count, cancellationToken);

        return units.Select(x => ToDto(x, activeTenantCountsByUnit)).ToList();
    }

    public async Task<UnitDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Units
            .AsNoTracking()
            .Include(x => x.Property)
            .Include(x => x.Rooms)
            .Include(x => x.Leases)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Unit not found.");

        var assignedCount = await dbContext.Tenants
            .AsNoTracking()
            .CountAsync(x => x.IsActive && x.UnitId == entity.Id, cancellationToken);

        var tenantCounts = new Dictionary<int, int>
        {
            [entity.Id] = assignedCount
        };

        return ToDto(entity, tenantCounts);
    }

    public async Task<UnitDto> CreateAsync(CreateUnitRequestDto request, CancellationToken cancellationToken = default)
    {
        var normalizedUnitNumber = await ValidateAndNormalizeAsync(request.PropertyId, request.UnitNumber, request.MonthlyRent, request.MaxCapacity, null, cancellationToken);

        var entity = new Unit
        {
            PropertyId = request.PropertyId,
            UnitNumber = normalizedUnitNumber,
            MonthlyRent = request.MonthlyRent,
            MaxCapacity = request.MaxCapacity,
            Status = request.Status,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Rooms =
            [
                CreateDefaultRoom()
            ]
        };

        dbContext.Units.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<UnitDto> UpdateAsync(int id, UpdateUnitRequestDto request, CancellationToken cancellationToken = default)
    {
        var normalizedUnitNumber = await ValidateAndNormalizeAsync(request.PropertyId, request.UnitNumber, request.MonthlyRent, request.MaxCapacity, id, cancellationToken);

        var entity = await dbContext.Units
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Unit not found.");

        entity.PropertyId = request.PropertyId;
        entity.UnitNumber = normalizedUnitNumber;
        entity.MonthlyRent = request.MonthlyRent;
        entity.MaxCapacity = request.MaxCapacity;
        entity.Status = request.Status;
        entity.UpdatedAt = DateTime.UtcNow;

        var tenantsToSync = await dbContext.Tenants
            .Where(x => x.UnitId == entity.Id)
            .ToListAsync(cancellationToken);

        foreach (var tenant in tenantsToSync)
        {
            tenant.UnitNumber = normalizedUnitNumber;
            tenant.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Units
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Unit not found.");

        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task HardDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var entity = await dbContext.Units
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new AppNotFoundException("Unit not found.");

            var hasActiveTenants = await dbContext.Tenants
                .AsNoTracking()
                .AnyAsync(x => x.IsActive && x.UnitId == id, cancellationToken);

            if (hasActiveTenants)
            {
                throw new AppValidationException("Cannot delete unit with active tenants.");
            }

            var leaseIds = await dbContext.Leases
                .Where(x => x.UnitId == id)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            if (leaseIds.Count > 0)
            {
                var invoiceIds = await dbContext.Invoices
                    .Where(x => leaseIds.Contains(x.LeaseId))
                    .Select(x => x.Id)
                    .ToListAsync(cancellationToken);

                if (invoiceIds.Count > 0)
                {
                    var invoiceItems = await dbContext.InvoiceItems
                        .Where(x => invoiceIds.Contains(x.InvoiceId))
                        .ToListAsync(cancellationToken);
                    dbContext.InvoiceItems.RemoveRange(invoiceItems);

                    var invoices = await dbContext.Invoices
                        .Where(x => invoiceIds.Contains(x.Id))
                        .ToListAsync(cancellationToken);
                    dbContext.Invoices.RemoveRange(invoices);
                }

                var leasePayments = await dbContext.Payments
                    .Where(x => leaseIds.Contains(x.LeaseId))
                    .ToListAsync(cancellationToken);
                dbContext.Payments.RemoveRange(leasePayments);

                var leases = await dbContext.Leases
                    .Where(x => leaseIds.Contains(x.Id))
                    .ToListAsync(cancellationToken);
                dbContext.Leases.RemoveRange(leases);
            }

            var directUnitPayments = await dbContext.Payments
                .Where(x => x.UnitId == id)
                .ToListAsync(cancellationToken);
            dbContext.Payments.RemoveRange(directUnitPayments);

            var expenses = await dbContext.Expenses
                .Where(x => x.UnitId == id)
                .ToListAsync(cancellationToken);
            dbContext.Expenses.RemoveRange(expenses);

            var assignedTenants = await dbContext.Tenants
                .Where(x => x.UnitId == id)
                .ToListAsync(cancellationToken);

            foreach (var tenant in assignedTenants)
            {
                tenant.UnitId = null;
                tenant.UnitNumber = null;
                tenant.RoomNumber = null;
                tenant.UpdatedAt = DateTime.UtcNow;
            }

            var rooms = await dbContext.Rooms
                .Where(x => x.UnitId == id)
                .ToListAsync(cancellationToken);
            dbContext.Rooms.RemoveRange(rooms);

            dbContext.Units.Remove(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    private async Task<string> ValidateAndNormalizeAsync(int propertyId, string? unitNumber, decimal monthlyRent, int maxCapacity, int? existingUnitId, CancellationToken cancellationToken)
    {
        if (propertyId <= 0)
        {
            throw new AppValidationException("Property is required.");
        }

        var normalizedUnitNumber = unitNumber?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedUnitNumber))
        {
            throw new AppValidationException("Unit name is required.");
        }

        if (monthlyRent < 0)
        {
            throw new AppValidationException("Monthly rent cannot be negative.");
        }

        if (maxCapacity < 0)
        {
            throw new AppValidationException("Unit max capacity cannot be negative.");
        }

        var propertyExists = await dbContext.Properties.AnyAsync(x => x.Id == propertyId && x.IsActive, cancellationToken);
        if (!propertyExists)
        {
            throw new AppValidationException("Selected property does not exist or is inactive.");
        }

        var duplicateExists = await dbContext.Units.AnyAsync(
            x => x.PropertyId == propertyId
                && x.UnitNumber == normalizedUnitNumber
                && (!existingUnitId.HasValue || x.Id != existingUnitId.Value),
            cancellationToken);

        if (duplicateExists)
        {
            throw new AppValidationException("A unit with the same name already exists for this property.");
        }

        return normalizedUnitNumber;
    }

    private static Room CreateDefaultRoom()
    {
        return new Room
        {
            RoomNumber = "Default Room",
            MaxCapacity = 999,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static UnitDto ToDto(Unit entity, IReadOnlyDictionary<int, int> activeTenantCountsByUnit)
    {
        return new UnitDto
        {
            Id = entity.Id,
            PropertyId = entity.PropertyId,
            PropertyName = entity.Property?.Name ?? string.Empty,
            UnitNumber = entity.UnitNumber,
            MonthlyRent = entity.MonthlyRent,
            MaxCapacity = entity.MaxCapacity,
            Status = entity.Status,
            IsActive = entity.IsActive
        };
    }
}
