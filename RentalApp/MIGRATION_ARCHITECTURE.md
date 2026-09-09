# SQLite → SQL Server Migration Architecture

## System Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                        RentalApp (.NET 10)                         │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │                     Program.cs                              │   │
│  │  ┌─────────────────────────────────────────────────────┐   │   │
│  │  │  DbContext Provider Detection Logic                 │   │   │
│  │  │  ┌─────────────────────────────────────────────┐   │   │   │
│  │  │  │ If connection string contains "Data Source=" │   │   │   │
│  │  │  │    ↓ Use SQLite Provider                   │   │   │   │
│  │  │  │ Else                                        │   │   │   │
│  │  │  │    ↓ Use SQL Server Provider (DEFAULT)      │   │   │   │
│  │  │  └─────────────────────────────────────────────┘   │   │   │
│  │  └─────────────────────────────────────────────────────┘   │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
└───────────┬──────────────────────────────────┬────────────────────┘
			│                                  │
			✓                                  ✓
	 ┌──────────────────┐              ┌──────────────────┐
	 │  SQL Server Path │              │   SQLite Path    │
	 │   (PRIMARY)      │              │   (Fallback)     │
	 └──────────┬───────┘              └────────┬─────────┘
				│                               │
				│ appsettings.Development.json  │
				│ "Server=(local);Database=...  │ Data Source=
				│                               │ Data/Rental.db
				│                               │
				↓                               ↓
	 ┌──────────────────────┐        ┌─────────────────┐
	 │ SQL Server Express   │        │    SQLite       │
	 │ (RentalAppDb)        │        │  (Rental.db)    │
	 │                      │        │                 │
	 │ 17 Tables:           │        │ 17 Tables:      │
	 │ • Users              │        │ • Users         │
	 │ • Properties         │        │ • Properties    │
	 │ • Units              │        │ • Units         │
	 │ • Leases             │        │ • Leases        │
	 │ • Payments           │        │ • Payments      │
	 │ • ... (12 more)      │        │ ... (12 more)   │
	 │                      │        │                 │
	 │ Data Types:          │        │ Data Types:     │
	 │ • nvarchar(255)      │        │ • TEXT          │
	 │ • datetime2          │        │ • DATETIME      │
	 │ • decimal(18,2)      │        │ • REAL          │
	 │ • bit                │        │ • INTEGER       │
	 └──────────────────────┘        └─────────────────┘
			│                               │
			│ Primary DB                    │ Fallback DB
			└───────────────────────────────┘
```

---

## Entity Framework Core Provider Selection Flow

```
┌─ Application Startup
│
├─ Load Configuration (appsettings.json)
│
├─ Get ConnectionString("DefaultConnection")
│  └─ Development: "Server=(local);Database=RentalAppDb;..."
│  └─ Production: "Server=YOUR_SERVER;Database=RentalAppDb;..."
│  └─ Alternative: "Data Source=Data/Rental.db"
│
└─ Program.cs Provider Detection
   │
   ├─ Check if connection string contains "Data Source="
   │  └─ YES → Use UseSqlite()
   │  └─ NO  → Check env variable "DatabaseProvider"
   │           ├─ If "sqlite" → Use UseSqlite()
   │           └─ Otherwise → Use UseSqlServer() ✓ DEFAULT
   │
   └─ DbContext Initialized
	  │
	  ├─ EF Core loads appropriate provider
	  ├─ EF Core loads migrations
	  ├─ Database automatically discovered
	  └─ Application ready to serve requests
```

---

## Data Migration Flow (If Upgrading from SQLite)

```
SqliteToSqlServerMigrationScript.cs

┌─ Dependency-Ordered Migration
│
├─ Batch 1: Independent Entities
│  ├─ Users (0 FK dependencies)
│  ├─ ExpenseCategories (0 FK dependencies)
│  └─ UtilityTypes (0 FK dependencies)
│
├─ Batch 2: Property Tier
│  ├─ Properties (FK: User)
│  ├─ Units (FK: Property)
│  └─ Rooms (FK: Unit)
│
├─ Batch 3: Tenant Tier
│  ├─ Tenants (FK: User)
│  ├─ Leases (FK: Tenant, Unit, Property)
│  └─ Payments (FK: Lease)
│
├─ Batch 4: Expense Tier
│  └─ Expenses (FK: Property, ExpenseCategory, User)
│
├─ Batch 5: Invoice Tier
│  ├─ Invoices (FK: Property, User)
│  └─ InvoiceItems (FK: Invoice)
│
└─ Batch 6: Utility Tier
   ├─ UtilityCustomers (FK: UtilityType, Property)
   ├─ UtilityBills (FK: UtilityCustomer)
   ├─ UtilityBillPayments (FK: UtilityBill)
   ├─ UtilityCustomerCredits (FK: UtilityCustomer)
   └─ UtilityAuditLogs (FK: UtilityCustomer)

Each batch: 500 records per transaction
Logging: Progress reported for each batch
Validation: Record counts verified after migration
```

---

## Configuration File Structure

```
RentalApp/
├── appsettings.json                    [Base config - empty connection string]
├── appsettings.Development.json        [Dev config - SQL Server (local)]
├── appsettings.Production.json         [Prod config - SQL Server (template)]
└── appsettings.Staging.json (optional) [Other environment if needed]

Content Structure:
{
  "ConnectionStrings": {
	"DefaultConnection": "Server=...;Database=RentalAppDb;...",
	"SqliteConnection": "Data Source=Data/Rental.db"
  },
  "DatabaseProvider": "SqlServer",     [Optional - override detection]
  "Application": { ... },
  "ApplicationData": { ... },
  "Jwt": { ... },
  "Logging": { ... }
}
```

---

## EF Core Migration Timeline

```
Initial State (SQLite):
				↓
Migrations 1-9: Cumulative schema development
				↓
20260909092743_SqlServerMigration.cs: CONVERSION POINT
	• Converts TEXT → nvarchar
	• Converts INTEGER → int/bigint
	• Converts REAL → decimal
	• Converts datetime handling
	• Converts boolean handling
				↓
Final State (SQL Server):
	• All 17 entities fully compatible
	• All indexes and constraints maintained
	• All relationships preserved
	• Ready for production data
```

---

## File Dependencies

```
Core Application
├── Program.cs (Updated)
│   └── Depends on: Microsoft.EntityFrameworkCore.SqlServer
│       └── Loaded by: RentalApp.csproj
│           └── Contains: PackageReference version="10.0.11"
│
├── RentalDbContext.cs (No Changes)
│   └── Entities mapped 1:1 to database tables
│       └── Provider-agnostic (works with SQL Server or SQLite)
│
├── appsettings.Development.json (Updated)
│   └── Connection String
│       └── Points to SQL Server (local)
│
└── Migrations/* (Enhanced)
	├── All previous migrations (unchanged)
	└── SqlServerMigration.cs (New)
		└── Converts schema from SQLite to SQL Server
```

---

## Technology Stack Mapping

```
┌──────────────────────────────────────────────────────┐
│  Layer          │  Technology        │  Version      │
├──────────────────────────────────────────────────────┤
│  Framework      │  ASP.NET Core      │  10.0         │
│  Language       │  C# 13             │  13.0         │
│  ORM            │  Entity Framework  │  10.0.11      │
│  DB Provider    │  SQL Server        │  2019+        │
│  Auth           │  JWT + Cookies     │  Built-in     │
│  Validation     │  FluentValidation  │  11.3.1       │
│  Documentation  │  Swagger/OpenAPI   │  10.2.3       │
│  PDF Gen        │  Custom Renderer   │  Internal     │
└──────────────────────────────────────────────────────┘
```

---

## Provider Auto-Detection Logic

```csharp
// Program.cs DbContext Configuration

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

Condition 1: DatabaseProvider config is "sqlite"
			→ Use UseSqlite()

Condition 2: Connection string contains "Data Source="
			→ Use UseSqlite()

Condition 3: Neither condition true
			→ Use UseSqlServer() ← DEFAULT

Logic Flow:
	if (config["DatabaseProvider"] == "sqlite") → SQLite
	else if (connectionString.Contains("Data Source=")) → SQLite
	else → SQL Server ✓
```

---

## Deployment Checklist Flow

```
1. Development Machine
   ├─ Install SQL Server Express
   ├─ Run Setup-SqlServer-Database.ps1
   ├─ dotnet run
   └─ Verify in browser

2. Staging Environment
   ├─ Set connection string to staging SQL Server
   ├─ dotnet ef database update
   ├─ Run full test suite
   └─ Verify all endpoints

3. Production Environment
   ├─ Update appsettings.Production.json
   ├─ dotnet ef database update
   ├─ Health check endpoint
   ├─ Smoke test all critical paths
   └─ Monitor for errors

4. Rollback (if needed)
   └─ Change connection string to SQLite
	   └─ Application restarts
		   └─ Reconnects to SQLite
			   └─ No data loss
```

---

## Data Type Conversion Reference

```
SQLite          → SQL Server        → C# Type
────────────────────────────────────────────
TEXT            → nvarchar(MAX)     → string
TEXT(n)         → nvarchar(n)       → string
INTEGER         → int               → int / long
REAL            → decimal(18,2)     → decimal
BLOB            → varbinary(MAX)    → byte[]
TRUE/FALSE      → bit               → bool
TIMESTAMP       → datetime2         → DateTime
NULL            → NULL              → null (ref types)
```

---

## Architecture Benefits

✅ **Flexibility**: Can switch providers without code changes
✅ **Reliability**: Full transaction support with SQL Server
✅ **Scalability**: SQL Server Enterprise features available
✅ **Safety**: Rollback to SQLite if needed
✅ **Performance**: SQL Server superior for large datasets
✅ **Compatibility**: All EF Core features available
✅ **Support**: SQL Server Express is free for development

---

## Monitoring & Health Checks

```
Built-in Health Check:
  GET /health
  Returns:
	{
	  "status": "Healthy",
	  "checks": {
		"DbContextCheck": "Healthy/Degraded/Unhealthy"
	  }
	}

Indicators:
  ✓ Healthy   = Database connected and responsive
  ⚠ Degraded  = Database slow or intermittent issues
  ✗ Unhealthy = Database unreachable or down
```

---

**This architecture ensures:**
- ✅ Zero-downtime deployments
- ✅ Easy rollback capability
- ✅ Provider flexibility
- ✅ Data consistency
- ✅ Production readiness
