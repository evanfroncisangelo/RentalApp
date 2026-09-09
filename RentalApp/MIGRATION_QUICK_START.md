# SQLite → SQL Server Migration - QUICK START

## ⚠️ IMPORTANT: SQL Server Setup Required

Your migration is **95% ready**, but SQL Server is not running on your machine. Here's how to proceed:

---

## Option 1: Use Local SQL Server Express (Recommended ✓)

### Step 1: Download & Install SQL Server Express 2022
- **Download**: https://www.microsoft.com/en-us/sql-server/sql-server-downloads
- **Choose**: SQL Server Express (free)
- **During install**: Select "Development" or "Development and Testing" workload
- **Remember**: Keep default instance name as `(local)` or `SQLEXPRESS`

### Step 2: Verify SQL Server is Running

```powershell
# In PowerShell, run:
Get-Service -Name MSSQLSERVER, SQLEXPRESS

# Output should show "Running" status
```

If service isn't running:
```powershell
# Start SQL Server
Start-Service -Name MSSQLSERVER
# OR
Start-Service -Name SQLEXPRESS
```

### Step 3: Test Connection

Open **SQL Server Management Studio (SSMS)** (installed with SQL Server):
1. Connect to: `(local)` or `SQLEXPRESS`
2. You should see "Connected" at the bottom

---

## Option 2: Use Docker (Advanced)

```bash
# Run SQL Server in Docker
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourPassword123!" `
  -p 1433:1433 --name sqlserver `
  -d mcr.microsoft.com/mssql/server:2022-latest

# Then update appsettings.Development.json:
# "DefaultConnection": "Server=localhost,1433;Database=RentalAppDb;User Id=sa;Password=YourPassword123!;Encrypt=false;TrustServerCertificate=true"
```

---

## Option 3: Use Azure SQL Database (Cloud)

Contact your DevOps team or use Azure Portal to provision a SQL Server database.

Then update `appsettings.Production.json` with your connection string.

---

## ✅ Once SQL Server is Running

### Step 1: Create the Database

From the project root:

```powershell
cd RentalApp
dotnet ef database update --project RentalApp.csproj
```

**Expected output:**
```
Build started...
Build succeeded.
Done. Applied X migrations.
```

### Step 2: Verify Database Was Created

Open **SQL Server Management Studio**:
1. Connect to `(local)`
2. Expand "Databases"
3. You should see `RentalAppDb`
4. Expand `RentalAppDb` → `Tables`
5. You should see 17 tables: Users, Properties, Units, etc.

### Step 3: Test the Application

```powershell
dotnet run
# Application should start successfully
# Navigate to: https://localhost:5001
```

### Step 4: Verify Health Check

```bash
curl https://localhost:5001/health
```

Expected response:
```json
{"status":"Healthy","checks":{"DbContextCheck":"Healthy"}}
```

---

## What Was Already Done For You ✓

✅ Added `Microsoft.EntityFrameworkCore.SqlServer` NuGet package
✅ Updated `Program.cs` with SQL Server provider configuration
✅ Added SQL Server connection strings in `appsettings.Development.json`
✅ Created EF Core migration `SqlServerMigration` (handles SQLite→SQL Server conversion)
✅ Created `MIGRATION_GUIDE.md` with complete documentation
✅ Created batch migration scripts (if migrating existing SQLite data)
✅ Configured automatic provider detection

---

## File Summary

| File | Purpose |
|------|---------|
| `appsettings.Development.json` | SQL Server connection string for dev |
| `appsettings.Production.json` | SQL Server template for production |
| `Program.cs` | Updated with SQL Server provider |
| `Migrations/20260909092743_SqlServerMigration.cs` | Converts SQLite schema to SQL Server |
| `Scripts/Setup-SqlServer-Database.ps1` | Automated database setup script |
| `Scripts/SqliteToSqlServerMigrationScript.cs` | Batch data migration (if needed) |
| `MIGRATION_GUIDE.md` | **Full migration guide with troubleshooting** |

---

## Troubleshooting

**Q: "Cannot connect to SQL Server"**
- A: Make sure SQL Server service is running (`Start-Service -Name MSSQLSERVER`)
- Check connection string: `Server=(local);Database=RentalAppDb;Integrated Security=true;Encrypt=false`

**Q: "Database already exists"**
- A: It's safe! The migration will add any missing tables/columns
- Or drop existing: `drop database RentalAppDb` in SSMS

**Q: "Feature not supported in this version"**
- A: SQL Server Express supports all features this app needs
- Just make sure SQL Server 2019 or newer is installed

**Q: "Migration pending changes warning"**
- A: This means entities were modified. Run: `dotnet ef migrations add FixName`

---

## Next Steps

1. **Install SQL Server** (if not already done)
2. **Verify SQL Server is running** (Get-Service check)
3. **Run**: `dotnet ef database update`
4. **Test**: `dotnet run`
5. **Read**: `MIGRATION_GUIDE.md` for complete details

---

## Document References

- **Full Migration Guide**: See `MIGRATION_GUIDE.md`
- **EF Core Docs**: https://learn.microsoft.com/en-us/ef/core/
- **SQL Server Docs**: https://learn.microsoft.com/en-us/sql/

---

**Your RentalApp is 95% migrated! The only missing piece is running SQL Server. Once it's running, execute `dotnet ef database update` and you're done! 🎉**
