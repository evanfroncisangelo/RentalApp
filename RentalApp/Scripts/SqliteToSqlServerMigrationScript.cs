// Data Migration Script - SQLite to SQL Server
// This script handles batch-by-batch migration of data from SQLite to SQL Server
// Run this in a temporary console app or as EF Core migrations

using Microsoft.EntityFrameworkCore;
using RentalApp.Data;
using RentalApp.Domain.Entities;

namespace RentalApp.Scripts;

public class SqliteToSqlServerMigrationScript
{
    private readonly RentalDbContext _sqliteContext;
    private readonly RentalDbContext _sqlServerContext;
    private readonly ILogger<SqliteToSqlServerMigrationScript> _logger;

    public SqliteToSqlServerMigrationScript(
        ILogger<SqliteToSqlServerMigrationScript> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Migrates all data from SQLite to SQL Server in batch operations
    /// Order matters: Users first, then Properties, then Units, etc.
    /// This respects foreign key relationships
    /// </summary>
    public async Task MigrateAllDataAsync()
    {
        _logger.LogInformation("Starting SQLite to SQL Server data migration...");
        _logger.LogInformation("This migration respects entity relationships and will execute in dependency order.");

        try
        {
            // Order based on foreign key dependencies
            await MigrateUsersAsync();
            await MigrateExpenseCategoriesAsync();
            await MigrateUtilityTypesAsync();
            await MigratePropertiesAsync();
            await MigrateUnitsAsync();
            await MigrateRoomsAsync();
            await MigrateTenantsAsync();
            await MigrateLeasesAsync();
            await MigratePaymentsAsync();
            await MigrateExpensesAsync();
            await MigrateInvoicesAsync();
            await MigrateInvoiceItemsAsync();
            await MigrateUtilityCustomersAsync();
            await MigrateUtilityBillsAsync();
            await MigrateUtilityBillPaymentsAsync();
            await MigrateUtilityCustomerCreditsAsync();
            await MigrateUtilityAuditLogsAsync();

            _logger.LogInformation("✓ All data migration completed successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ Data migration failed");
            throw;
        }
    }

    private async Task MigrateUsersAsync() => 
        await MigrateGenericBatchAsync("Users", async ctx => await ctx.Users.ToListAsync());

    private async Task MigrateExpenseCategoriesAsync() => 
        await MigrateGenericBatchAsync("ExpenseCategories", async ctx => await ctx.ExpenseCategories.ToListAsync());

    private async Task MigrateUtilityTypesAsync() => 
        await MigrateGenericBatchAsync("UtilityTypes", async ctx => await ctx.UtilityTypes.ToListAsync());

    private async Task MigratePropertiesAsync() => 
        await MigrateGenericBatchAsync("Properties", async ctx => await ctx.Properties.ToListAsync());

    private async Task MigrateUnitsAsync() => 
        await MigrateGenericBatchAsync("Units", async ctx => await ctx.Units.ToListAsync());

    private async Task MigrateRoomsAsync() => 
        await MigrateGenericBatchAsync("Rooms", async ctx => await ctx.Rooms.ToListAsync());

    private async Task MigrateTenantsAsync() => 
        await MigrateGenericBatchAsync("Tenants", async ctx => await ctx.Tenants.ToListAsync());

    private async Task MigrateLeasesAsync() => 
        await MigrateGenericBatchAsync("Leases", async ctx => await ctx.Leases.ToListAsync());

    private async Task MigratePaymentsAsync() => 
        await MigrateGenericBatchAsync("Payments", async ctx => await ctx.Payments.ToListAsync());

    private async Task MigrateExpensesAsync() => 
        await MigrateGenericBatchAsync("Expenses", async ctx => await ctx.Expenses.ToListAsync());

    private async Task MigrateInvoicesAsync() => 
        await MigrateGenericBatchAsync("Invoices", async ctx => await ctx.Invoices.ToListAsync());

    private async Task MigrateInvoiceItemsAsync() => 
        await MigrateGenericBatchAsync("InvoiceItems", async ctx => await ctx.InvoiceItems.ToListAsync());

    private async Task MigrateUtilityCustomersAsync() => 
        await MigrateGenericBatchAsync("UtilityCustomers", async ctx => await ctx.UtilityCustomers.ToListAsync());

    private async Task MigrateUtilityBillsAsync() => 
        await MigrateGenericBatchAsync("UtilityBills", async ctx => await ctx.UtilityBills.ToListAsync());

    private async Task MigrateUtilityBillPaymentsAsync() => 
        await MigrateGenericBatchAsync("UtilityBillPayments", async ctx => await ctx.UtilityBillPayments.ToListAsync());

    private async Task MigrateUtilityCustomerCreditsAsync() => 
        await MigrateGenericBatchAsync("UtilityCustomerCredits", async ctx => await ctx.UtilityCustomerCredits.ToListAsync());

    private async Task MigrateUtilityAuditLogsAsync() => 
        await MigrateGenericBatchAsync("UtilityAuditLogs", async ctx => await ctx.UtilityAuditLogs.ToListAsync());

    private async Task MigrateGenericBatchAsync<T>(string entityName, Func<RentalDbContext, Task<List<T>>> queryFunc) 
        where T : class
    {
        _logger.LogInformation($"Migrating {entityName}...");

        try
        {
            // Query from SQLite context
            var items = await queryFunc(_sqliteContext);

            if (items.Count == 0)
            {
                _logger.LogInformation($"  ℹ No {entityName} to migrate");
                return;
            }

            // Batch insert to SQL Server (batches of 500)
            const int batchSize = 500;
            for (int i = 0; i < items.Count; i += batchSize)
            {
                var batch = items.Skip(i).Take(batchSize).ToList();

                foreach (var item in batch)
                {
                    _sqlServerContext.Set<T>().Add(item);
                }

                await _sqlServerContext.SaveChangesAsync();
                _logger.LogInformation($"  ✓ Inserted {Math.Min(i + batchSize, items.Count)}/{items.Count} {entityName}");
            }

            _logger.LogInformation($"✓ {entityName}: {items.Count} records migrated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"✗ Failed to migrate {entityName}");
            throw;
        }
    }
}

/// <summary>
/// Helper class for data validation after migration
/// </summary>
public class DataValidationHelper
{
    private readonly RentalDbContext _sqliteContext;
    private readonly RentalDbContext _sqlServerContext;
    private readonly ILogger<DataValidationHelper> _logger;

    public DataValidationHelper(
        ILogger<DataValidationHelper> logger)
    {
        _logger = logger;
    }

    public async Task ValidateAllDataAsync()
    {
        _logger.LogInformation("Starting data validation...");

        var validationResults = new Dictionary<string, (int SqliteCou, int SqlServerCount, bool Match)>();

        // Validate each table
        validationResults["Users"] = await CompareCountsAsync(
            () => _sqliteContext.Users.CountAsync(),
            () => _sqlServerContext.Users.CountAsync()
        );

        validationResults["Properties"] = await CompareCountsAsync(
            () => _sqliteContext.Properties.CountAsync(),
            () => _sqlServerContext.Properties.CountAsync()
        );

        validationResults["Units"] = await CompareCountsAsync(
            () => _sqliteContext.Units.CountAsync(),
            () => _sqlServerContext.Units.CountAsync()
        );

        validationResults["Tenants"] = await CompareCountsAsync(
            () => _sqliteContext.Tenants.CountAsync(),
            () => _sqlServerContext.Tenants.CountAsync()
        );

        validationResults["Leases"] = await CompareCountsAsync(
            () => _sqliteContext.Leases.CountAsync(),
            () => _sqlServerContext.Leases.CountAsync()
        );

        validationResults["Payments"] = await CompareCountsAsync(
            () => _sqliteContext.Payments.CountAsync(),
            () => _sqlServerContext.Payments.CountAsync()
        );

        validationResults["Expenses"] = await CompareCountsAsync(
            () => _sqliteContext.Expenses.CountAsync(),
            () => _sqlServerContext.Expenses.CountAsync()
        );

        validationResults["Invoices"] = await CompareCountsAsync(
            () => _sqliteContext.Invoices.CountAsync(),
            () => _sqlServerContext.Invoices.CountAsync()
        );

        // Print results
        _logger.LogInformation("");
        _logger.LogInformation("Data Validation Results:");
        _logger.LogInformation("=======================");

        bool allMatch = true;
        foreach (var (tableName, (sqliteCount, sqlServerCount, match)) in validationResults)
        {
            var status = match ? "✓ PASS" : "✗ FAIL";
            _logger.LogInformation($"{status} {tableName}: SQLite={sqliteCount}, SQL Server={sqlServerCount}");

            if (!match)
                allMatch = false;
        }

        if (allMatch)
            _logger.LogInformation("✓ All data validation passed! Migration successful.");
        else
            _logger.LogWarning("✗ Data validation failed. Review mismatches above.");
    }

    private async Task<(int SqliteCount, int SqlServerCount, bool Match)> CompareCountsAsync(
        Func<Task<int>> sqliteQuery,
        Func<Task<int>> sqlServerQuery)
    {
        var sqliteCount = await sqliteQuery();
        var sqlServerCount = await sqlServerQuery();
        return (sqliteCount, sqlServerCount, sqliteCount == sqlServerCount);
    }
}
