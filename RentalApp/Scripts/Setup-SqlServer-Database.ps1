# SQL Server Database Setup Script for RentalApp
# This script creates the RentalAppDb database and applies EF Core migrations

param(
	[string]$ServerName = "(local)",
	[string]$DatabaseName = "RentalAppDb",
	[switch]$DropExisting = $false
)

Write-Host "SQL Server Database Setup Script" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host "Server: $ServerName" -ForegroundColor Yellow
Write-Host "Database: $DatabaseName" -ForegroundColor Yellow
Write-Host ""

# Step 1: Check if database exists and handle accordingly
Write-Host "Step 1: Checking database status..." -ForegroundColor Green

$connectionString = "Server=$ServerName;Integrated Security=true;Encrypt=false;TrustServerCertificate=true"

try {
	$connection = New-Object System.Data.SqlClient.SqlConnection $connectionString
	$connection.Open()

	$command = New-Object System.Data.SqlClient.SqlCommand
	$command.Connection = $connection
	$command.CommandText = "SELECT COUNT(*) FROM sys.databases WHERE name = '$DatabaseName'"

	$dbExists = $command.ExecuteScalar()
	$connection.Close()

	if ($dbExists -gt 0) {
		Write-Host "✓ Database '$DatabaseName' already exists" -ForegroundColor Yellow

		if ($DropExisting) {
			Write-Host "  Dropping existing database..." -ForegroundColor Yellow
			$connection = New-Object System.Data.SqlClient.SqlConnection $connectionString
			$connection.Open()
			$command = New-Object System.Data.SqlClient.SqlCommand
			$command.Connection = $connection
			$command.CommandText = "ALTER DATABASE [$DatabaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$DatabaseName]"
			$command.ExecuteNonQuery()
			$connection.Close()
			Write-Host "✓ Database dropped successfully" -ForegroundColor Green
		} else {
			Write-Host "  Use -DropExisting flag to recreate the database" -ForegroundColor Cyan
		}
	} else {
		Write-Host "✓ Database '$DatabaseName' does not exist" -ForegroundColor Green
	}
} catch {
	Write-Host "✗ Error: Cannot connect to SQL Server at $ServerName" -ForegroundColor Red
	Write-Host "  Make sure SQL Server is running and you have the proper permissions" -ForegroundColor Red
	exit 1
}

# Step 2: Apply EF Core migrations
Write-Host ""
Write-Host "Step 2: Applying EF Core migrations..." -ForegroundColor Green

try {
	$env:ASPNETCORE_ENVIRONMENT = "Development"

	# This will create the database if it doesn't exist and apply all migrations
	dotnet ef database update --project RentalApp.csproj

	if ($LASTEXITCODE -ne 0) {
		Write-Host "✗ Migration failed" -ForegroundColor Red
		exit 1
	}

	Write-Host "✓ Migrations applied successfully" -ForegroundColor Green
} catch {
	Write-Host "✗ Error during migration: $_" -ForegroundColor Red
	exit 1
}

# Step 3: Verify database and tables
Write-Host ""
Write-Host "Step 3: Verifying database schema..." -ForegroundColor Green

try {
	$connectionString = "Server=$ServerName;Database=$DatabaseName;Integrated Security=true;Encrypt=false;TrustServerCertificate=true"
	$connection = New-Object System.Data.SqlClient.SqlConnection $connectionString
	$connection.Open()

	$command = New-Object System.Data.SqlClient.SqlCommand
	$command.Connection = $connection
	$command.CommandText = "SELECT COUNT(*) FROM sys.tables"

	$tableCount = $command.ExecuteScalar()
	$connection.Close()

	Write-Host "✓ Database contains $tableCount tables" -ForegroundColor Green
} catch {
	Write-Host "✗ Error verifying database: $_" -ForegroundColor Red
	exit 1
}

Write-Host ""
Write-Host "Setup Complete!" -ForegroundColor Cyan
Write-Host "Your SQL Server database '$DatabaseName' is ready for use." -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Update appsettings.Production.json with your SQL Server connection details"
Write-Host "2. Test the application with 'dotnet run'"
Write-Host "3. Verify all data migrations are complete"
