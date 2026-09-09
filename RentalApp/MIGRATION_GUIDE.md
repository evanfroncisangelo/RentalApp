# SQLite to SQL Server Migration Guide

## Overview
This guide walks through migrating your RentalApp from SQLite to SQL Server. The migration is designed to be **batch-by-batch** to avoid major issues and maintain data integrity.

**Timeline**: Fresh database setup (no existing data to port)
**Entities Being Migrated**: 17 DbSets across Users, Properties, Leases, Payments, Expenses, Invoices, Utilities, etc.

---

## Architecture & Changes

### What Was Changed
1. **NuGet Packages**: Added `Microsoft.EntityFrameworkCore.SqlServer` (v10.0.11)
2. **Database Provider Logic**: `Program.cs` now detects provider based on connection string
3. **Connection Strings**:
   - **Development**: `Server=(local);Database=RentalAppDb;Integrated Security=true;Encrypt=false`
   - **Production**: Template provided in `appsettings.Production.json` (update with your server details)

### Backward Compatibility
- SQLite still supported via connection string format detection
- Can switch back to SQLite by changing `DefaultConnection` to `Data Source=Data/Rental.db`

---

## Phase 1: Database Initialization

### Prerequisites
- ✅ SQL Server installed locally (or accessible)
- ✅ Windows Authentication enabled (or SQL Auth configured)
- ✅ .NET 10 SDK installed

### Step 1: Run Database Setup Script

```powershell
# From the project root directory
cd RentalApp
. .\Scripts\Setup-SqlServer-Database.ps1

# Or if you prefer to recreate a fresh database:
. .\Scripts\Setup-SqlServer-Database.ps1 -DropExisting

```

**What this does:**
- Connects to local SQL Server
- Creates `RentalAppDb` database (or drops & recreates if `-DropExisting` is used)
- Runs EF Core migrations (applies all schema changes)
- Verifies tables were created

**Expected output:**
```
SQL Server Database Setup Script
=================================
Server: (local)
Database: RentalAppDb

Step 1: Checking database status...
✓ Database 'RentalAppDb' does not exist

Step 2: Applying EF Core migrations...
✓ Migrations applied successfully

Step 3: Verifying database schema...
✓ Database contains 17 tables

Setup Complete!
```

### Step 2: Verify Database Schema

Open SQL Server Management Studio (SSMS) and connect to your local SQL Server:

```sql
-- Check if database exists
SELECT name FROM sys.databases WHERE name = 'RentalAppDb';

-- List all tables
USE RentalAppDb;
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';

-- Expected tables: Users, Properties, Units, Tenants, Leases, Payments, 
-- Expenses, Invoices, InvoiceItems, UtilityTypes, UtilityCustomers, 
-- UtilityBills, UtilityBillPayments, UtilityCustomerCredits, UtilityAuditLogs, Rooms
```

---

## Phase 2: Data Migration (If Migrating Existing SQLite Data)

If you have existing SQLite data to migrate, use the batch migration script.

### Integration Points

The migration script is designed to be integrated into your application as a:
1. **Hosted service** (runs on app startup)
2. **Console command** (manual trigger)
3. **Admin endpoint** (web-based trigger)

### Option A: Standalone Console Application

Create a temporary console app to run the migration:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RentalApp.Application.Services;
using RentalApp.Data;
using RentalApp.Scripts;

var services = new ServiceCollection()
	.AddLogging(logging => logging.AddConsole())
	.AddDbContext<RentalDbContext>(options =>
	{
		// SQLite source
		options.UseSqlite("Data Source=Data/Rental.db");
	})
	.BuildServiceProvider();

var logger = services.GetRequiredService<ILogger<Program>>();
var migrator = new SqliteToSqlServerMigrationScript(logger);

try
{
	await migrator.MigrateAllDataAsync();
	logger.LogInformation("Migration completed successfully!");
}
catch (Exception ex)
{
	logger.LogError(ex, "Migration failed!");
	throw;
}
```

### Option B: Database Connection Context Switching

To simultaneously access SQLite and SQL Server contexts, you need two DbContext instances:

```csharp
// In Program.cs or during migration
var sqliteConnectionString = builder.Configuration.GetConnectionString("SqliteConnection");
var sqlServerConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Create both contexts
var sqliteContext = new RentalDbContext(new DbContextOptionsBuilder<RentalDbContext>()
	.UseSqlite(sqliteConnectionString)
	.Options);

var sqlServerContext = new RentalDbContext(new DbContextOptionsBuilder<RentalDbContext>()
	.UseSqlServer(sqlServerConnectionString)
	.Options);

// Pass both to migration script
var migrator = new SqliteToSqlServerMigrationScript(logger);
await migrator.MigrateAllDataAsync();
```

### Data Migration Order

The migration respects foreign key relationships:

1. **Independent entities** (no dependencies):
   - `Users` (no FK dependencies)
   - `ExpenseCategories`
   - `UtilityTypes`

2. **Property tier**:
   - `Properties` (depends on User)
   - `Units` (depends on Property)
   - `Rooms` (depends on Unit)

3. **Tenant tier**:
   - `Tenants` (depends on User)
   - `Leases` (depends on Tenant, Unit, Property)
   - `Payments` (depends on Lease)

4. **Expense tier**:
   - `Expenses` (depends on Property, ExpenseCategory, User)

5. **Invoice tier**:
   - `Invoices` (depends on Property, User)
   - `InvoiceItems` (depends on Invoice)

6. **Utility tier**:
   - `UtilityCustomers` (depends on UtilityType, Property)
   - `UtilityBills` (depends on UtilityCustomer)
   - `UtilityBillPayments` (depends on UtilityBill)
   - `UtilityCustomerCredits` (depends on UtilityCustomer)
   - `UtilityAuditLogs` (depends on UtilityCustomer)

---

## Phase 3: Data Validation

### Automated Validation

Use the built-in `DataValidationHelper`:

```csharp
var validator = new DataValidationHelper(logger);
await validator.ValidateAllDataAsync();
```

**Expected output:**
```
Data Validation Results:
=======================
✓ PASS Users: SQLite=5, SQL Server=5
✓ PASS Properties: SQLite=12, SQL Server=12
✓ PASS Units: SQLite=45, SQL Server=45
... (continues for all entities)
✓ All data validation passed! Migration successful.
```

### Manual Validation in SQL Server

```sql
USE RentalAppDb;

-- Count all records per table
SELECT 
	'Users' AS TableName, COUNT(*) AS RecordCount FROM Users
UNION ALL
SELECT 'Properties', COUNT(*) FROM Properties
UNION ALL
SELECT 'Units', COUNT(*) FROM Units
UNION ALL
SELECT 'Tenants', COUNT(*) FROM Tenants
UNION ALL
SELECT 'Leases', COUNT(*) FROM Leases
UNION ALL
SELECT 'Payments', COUNT(*) FROM Payments
UNION ALL
SELECT 'Expenses', COUNT(*) FROM Expenses
UNION ALL
SELECT 'Invoices', COUNT(*) FROM Invoices
-- ... continue for all tables

-- Check for orphaned records (FK violations)
SELECT * FROM Units WHERE PropertyId NOT IN (SELECT Id FROM Properties);
SELECT * FROM Leases WHERE TenantId NOT IN (SELECT Id FROM Tenants);
-- ... continue for all FKs
```

---

## Phase 4: Application Testing

### Step 1: Run Application

```bash
dotnet run
```

### Step 2: Test Key Workflows

Login → Create Property → Add Unit → Add Tenant → Create Lease → Process Payment → Generate Invoice

### Step 3: Health Checks

The application includes database health checks:

```bash
curl https://localhost:5001/health
```

Expected response:
```json
{
  "status": "Healthy",
  "checks": {
	"DbContextCheck": "Healthy"
  }
}
```

---

## Phase 5: Deployment & Switchover

### Pre-Deployment Checklist

- [ ] All EF migrations applied successfully
- [ ] SQL Server database verified in SSMS
- [ ] Data migration complete (if migrating existing data)
- [ ] Data validation passed
- [ ] Application tested against SQL Server
- [ ] All workflows verified (login, create, update, delete, search)
- [ ] Health checks passing
- [ ] Invoices & PDFs generating correctly
- [ ] JWT authentication working
- [ ] File uploads working (Uploads path configured)

### Production Connection String

Update `appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
	"DefaultConnection": "Server=YOUR_PRODUCTION_SERVER;Database=RentalAppDb;User Id=YOUR_USERNAME;Password=YOUR_PASSWORD;Encrypt=false;TrustServerCertificate=false"
  }
}
```

### Deployment Steps

1. Build production release:
   ```bash
   dotnet build -c Release
   ```

2. Run migrations in production environment:
   ```bash
   SET ASPNETCORE_ENVIRONMENT=Production
   dotnet ef database update
   ```

3. Deploy application to production server

---

## Rollback Plan

If issues occur after migration, you can quickly roll back to SQLite:

### Rollback Steps

1. Update `appsettings.Development.json` or `appsettings.Production.json`:
   ```json
   "DefaultConnection": "Data Source=Data/Rental.db"
   ```

2. Restart application:
   ```bash
   dotnet run
   ```

3. Application will reconnect to SQLite automatically

### Backup SQL Server Data First

Before rolling back, backup your SQL Server database:

```powershell
# Backup via SSMS or T-SQL
BACKUP DATABASE RentalAppDb 
TO DISK = 'C:\Backups\RentalAppDb_Backup.bak'
```

---

## Troubleshooting

### Error: "Cannot connect to SQL Server"

**Symptoms**: Connection timeout, "Login failed"

**Solutions**:
1. Verify SQL Server is running:
   ```powershell
   # In PowerShell
   Get-Service -Name MSSQLSERVER
   ```

2. Verify connection string:
   - Check `appsettings.Development.json`
   - Test in SSMS: Connect to `(local)` with Windows Auth

3. Verify database name:
   - Ensure `RentalAppDb` exists or let migration create it

### Error: "Column 'X' does not exist"

**Symptoms**: SqlException during DbContext.SaveChanges()

**Cause**: Schema mismatch between entities and database

**Solution**:
1. Check migration status:
   ```bash
   dotnet ef migrations list
   ```

2. Apply pending migrations:
   ```bash
   dotnet ef database update
   ```

3. If still failing, recreate database:
   ```powershell
   . .\Scripts\Setup-SqlServer-Database.ps1 -DropExisting
   ```

### Error: "Foreign key constraint violation"

**Symptoms**: "Cannot insert NULL into FK column" or similar

**Cause**: Parent record doesn't exist during data migration

**Solution**:
1. Verify data migration order (see Data Migration Order section)
2. Check for orphaned records in SQLite source
3. Manual cleanup:
   ```sql
   -- Delete orphaned records
   DELETE FROM Units WHERE PropertyId NOT IN (SELECT Id FROM Properties);
   ```

### Error: "Timeout expired"

**Symptoms**: Migration hangs or times out during large data transfers

**Solution**:
1. Increase SQL Server connection timeout in `Program.cs`:
   ```csharp
   options.UseSqlServer(
	   connectionString,
	   sqlOptions => sqlOptions.CommandTimeout(300) // 5 minutes
   );
   ```

2. Reduce batch size if needed in migration script (default 500):
   ```csharp
   const int batchSize = 250; // Smaller batches
   ```

---

## Data Type Compatibility Notes

### SQLite vs SQL Server

| Data Type | SQLite | SQL Server | Migration Notes |
|-----------|--------|-----------|-----------------|
| GUID | TEXT | UNIQUEIDENTIFIER | Automatic conversion |
| DateTime | TEXT (ISO 8601) | datetime2 | Automatic conversion via EF |
| Boolean | INTEGER (0/1) | BIT | Automatic conversion |
| Decimal | REAL | DECIMAL(18,2) | Automatic conversion |
| Text | TEXT | NVARCHAR(MAX) | Automatic conversion |
| Byte Array | BLOB | VARBINARY(MAX) | Automatic conversion |

EF Core handles all conversions automatically. No manual adjustments needed.

---

## Performance Tuning

### For Large Datasets

If migrating >100K records:

1. **Increase batch size** (if memory permits):
   ```csharp
   const int batchSize = 1000;
   ```

2. **Disable FK checks during migration**:
   ```sql
   -- Before migration
   ALTER TABLE Units NOCHECK CONSTRAINT ALL;

   -- After migration
   ALTER TABLE Units CHECK CONSTRAINT ALL;
   ```

3. **Create indexes after migration**:
   ```sql
   -- This happens automatically in EF migration
   -- But can be done manually if needed
   CREATE INDEX Idx_PropertyId ON Units(PropertyId);
   ```

---

## Support & Documentation

- **EF Core SQL Server**: https://learn.microsoft.com/en-us/ef/core/providers/sql-server/
- **SQL Server Express Download**: https://www.microsoft.com/en-us/sql-server/sql-server-express
- **Entity Framework Migrations**: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/

---

**Status**: ✓ Migration roadmap complete. Ready for deployment!
